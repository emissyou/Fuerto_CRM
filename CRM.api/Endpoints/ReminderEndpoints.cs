using CRM.api.Security;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class ReminderEndpoints
{
    public static void MapReminderEndpoints(this WebApplication app)
    {
        // ============================================================
        // GET REMINDERS
        // GET /tenant/{companyId}/reminders
        //
        // Returns computed reminders based on current data.
        // No DB storage — always fresh.
        //
        // Designer role sees ONLY: StalledProject reminders for their own projects.
        // Staff/Admin see everything.
        // ============================================================
        app.MapGet("/tenant/{companyId:int}/reminders", async (
            int companyId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var now = DateTime.UtcNow;

            // ---- Determine current user's role ----
            var roles = http.User
                .FindAll(System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            bool isDesigner = roles.Contains("Designer")
                           && !roles.Contains("Admin")
                           && !roles.Contains("Super Admin")
                           && !roles.Contains("Staff");

            // Designer's own user id (to filter stalled projects)
            var currentUserId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var reminders = new List<object>();

            // =====================================================
            // RULE 1 — DEPOSIT OVERDUE
            // (skipped for designers)
            // =====================================================
            if (!isDesigner)
            {
                var depositCutoff = now.AddDays(-7);

                var overdueQuotes = await db.Quotations
                    .Where(q => q.CompanyId == companyId
                             && q.Status == "Accepted"
                             && q.AcceptedAt != null
                             && q.AcceptedAt < depositCutoff
                             && q.AmountPaid < q.DepositRequired
                             && q.PaymentStatus != "FullyPaid"
                             && q.PaymentStatus != "DepositReceived")
                    .Select(q => new
                    {
                        q.QuotationId,
                        q.QuotationNumber,
                        q.ProjectId,
                        q.CustomerId,
                        q.TotalAmount,
                        q.AmountPaid,
                        q.DepositRequired,
                        q.AcceptedAt
                    })
                    .ToListAsync();

                // Get project + customer lookups
                var projIds = overdueQuotes.Select(q => q.ProjectId).Distinct().ToList();
                var custIds = overdueQuotes.Select(q => q.CustomerId).Distinct().ToList();

                var projectsLookup = await db.Projects
                    .Where(p => p.CompanyId == companyId && projIds.Contains(p.ProjectId))
                    .Select(p => new { p.ProjectId, p.ProjectCode, p.ProjectName })
                    .ToDictionaryAsync(p => p.ProjectId);

                var customersLookup = await db.Customers
                    .Where(c => c.CompanyId == companyId && custIds.Contains(c.CustomerId))
                    .Select(c => new { c.CustomerId, c.FirstName, c.LastName, c.Phone, c.Email })
                    .ToDictionaryAsync(c => c.CustomerId);

                foreach (var q in overdueQuotes)
                {
                    projectsLookup.TryGetValue(q.ProjectId, out var proj);
                    customersLookup.TryGetValue(q.CustomerId, out var cust);

                    var daysOverdue = (int)(now - q.AcceptedAt!.Value).TotalDays;
                    var depositOutstanding = q.DepositRequired - q.AmountPaid;

                    reminders.Add(new
                    {
                        category = "DepositOverdue",
                        priority = "High",
                        customerId = q.CustomerId,
                        customerName = cust is null
                            ? "Unknown"
                            : $"{cust.FirstName} {cust.LastName}".Trim(),
                        customerPhone = cust?.Phone ?? "",
                        customerEmail = cust?.Email ?? "",
                        projectId = q.ProjectId,
                        projectCode = proj?.ProjectCode ?? "",
                        projectName = proj?.ProjectName ?? "",
                        quotationNumber = q.QuotationNumber,
                        basis = $"Quote accepted {daysOverdue} days ago · ₱{depositOutstanding:N0} deposit unpaid",
                        actionText = $"Call to collect ₱{depositOutstanding:N0} deposit",
                        reference = q.QuotationNumber,
                        daysPending = daysOverdue
                    });
                }
            }

            // =====================================================
            // RULE 2 — DETRACTOR (rating <= 3.0)
            // (skipped for designers)
            // =====================================================
            if (!isDesigner)
            {
                var customerRatings = await db.ProjectFeedbacks
                    .Where(f => f.CompanyId == companyId)
                    .GroupBy(f => f.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        AvgRating = g.Average(f => (double)f.OverallRating),
                        Count = g.Count(),
                        LastSubmitted = g.Max(f => f.SubmittedAt)
                    })
                    .Where(x => x.AvgRating <= 3.0)
                    .ToListAsync();

                var detractorCustIds = customerRatings.Select(x => x.CustomerId).ToList();
                var detractorCustomers = await db.Customers
                    .Where(c => c.CompanyId == companyId
                             && detractorCustIds.Contains(c.CustomerId))
                    .Select(c => new { c.CustomerId, c.FirstName, c.LastName, c.Phone, c.Email })
                    .ToDictionaryAsync(c => c.CustomerId);

                foreach (var x in customerRatings)
                {
                    detractorCustomers.TryGetValue(x.CustomerId, out var cust);
                    if (cust is null) continue;

                    reminders.Add(new
                    {
                        category = "Detractor",
                        priority = "High",
                        customerId = x.CustomerId,
                        customerName = $"{cust.FirstName} {cust.LastName}".Trim(),
                        customerPhone = cust.Phone,
                        customerEmail = cust.Email,
                        projectId = 0,
                        projectCode = "",
                        projectName = "",
                        quotationNumber = "",
                        basis = $"Avg rating {x.AvgRating:F1}★ from {x.Count} feedback(s) — escalate",
                        actionText = "Personal apology call + free consultation",
                        reference = $"rating {x.AvgRating:F1}",
                        daysPending = (int)(now - x.LastSubmitted).TotalDays
                    });
                }
            }

            // =====================================================
            // RULE 3 — AT-RISK CUSTOMER
            // (skipped for designers)
            // =====================================================
            if (!isDesigner)
            {
                var customerProjects = await db.Projects
                    .Where(p => p.CompanyId == companyId)
                    .GroupBy(p => p.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        LastProject = g.Max(p => p.CreatedAt)
                    })
                    .ToListAsync();

                var atRiskCutoff = now.AddDays(-365);
                var atRisk = customerProjects
                    .Where(x => x.LastProject < atRiskCutoff)
                    .Select(x => new
                    {
                        x.CustomerId,
                        x.LastProject,
                        DaysInactive = (int)(now - x.LastProject).TotalDays
                    })
                    .ToList();

                var atRiskCustIds = atRisk.Select(x => x.CustomerId).ToList();
                var atRiskCustomers = await db.Customers
                    .Where(c => c.CompanyId == companyId
                             && atRiskCustIds.Contains(c.CustomerId))
                    .Select(c => new { c.CustomerId, c.FirstName, c.LastName, c.Phone, c.Email })
                    .ToDictionaryAsync(c => c.CustomerId);

                foreach (var x in atRisk)
                {
                    atRiskCustomers.TryGetValue(x.CustomerId, out var cust);
                    if (cust is null) continue;

                    reminders.Add(new
                    {
                        category = "AtRisk",
                        priority = "Medium",
                        customerId = x.CustomerId,
                        customerName = $"{cust.FirstName} {cust.LastName}".Trim(),
                        customerPhone = cust.Phone,
                        customerEmail = cust.Email,
                        projectId = 0,
                        projectCode = "",
                        projectName = "",
                        quotationNumber = "",
                        basis = $"No project in {x.DaysInactive} days (>1 year)",
                        actionText = "Win-back email + 15% discount",
                        reference = "at-risk",
                        daysPending = x.DaysInactive
                    });
                }
            }

            // =====================================================
            // RULE 4 — STALLED PROJECT
            // (VISIBLE TO DESIGNERS for their own projects)
            // =====================================================
            {
                var stalledCutoff = now.AddDays(-30);

                var stalledQuery = db.Projects
                    .Where(p => p.CompanyId == companyId
                             && p.DesignStage != "Completed"
                             && p.DesignStage != "Cancelled"
                             && p.Status != "Completed"
                             && p.ProgressPercentage < 100
                             && p.ProgressPercentage > 0);

                // Designer: restrict to their own projects
                if (isDesigner && !string.IsNullOrEmpty(currentUserId))
                    stalledQuery = stalledQuery.Where(p => p.DesignerId == currentUserId);

                var stalledCandidates = await stalledQuery
                    .Select(p => new
                    {
                        p.ProjectId,
                        p.ProjectCode,
                        p.ProjectName,
                        p.CustomerId,
                        p.DesignerId,
                        p.DesignerName,
                        p.ProgressPercentage,
                        p.DesignerAssignedAt,
                        p.DesignStartDate
                    })
                    .ToListAsync();

                // For each stalled candidate, find last progress activity
                var stalledProjIds = stalledCandidates.Select(p => p.ProjectId).ToList();

                var lastProgressByProject = await db.Activities
                    .Where(a => a.CompanyId == companyId
                             && a.ProjectId != null
                             && stalledProjIds.Contains(a.ProjectId!.Value)
                             && a.ActivityType == "ProgressUpdate")
                    .GroupBy(a => a.ProjectId!.Value)
                    .Select(g => new
                    {
                        ProjectId = g.Key,
                        LastProgress = g.Max(a => a.ActivityDate)
                    })
                    .ToDictionaryAsync(x => x.ProjectId, x => x.LastProgress);

                var stalledCustIds = stalledCandidates.Select(p => p.CustomerId).Distinct().ToList();
                var stalledCustomers = await db.Customers
                    .Where(c => c.CompanyId == companyId && stalledCustIds.Contains(c.CustomerId))
                    .Select(c => new { c.CustomerId, c.FirstName, c.LastName, c.Phone, c.Email })
                    .ToDictionaryAsync(c => c.CustomerId);

                foreach (var p in stalledCandidates)
                {
                    // Determine "last touched" date
                    var startedAt = p.DesignerAssignedAt ?? p.DesignStartDate ?? now.AddDays(-60);
                    var lastProgress = lastProgressByProject.TryGetValue(p.ProjectId, out var lp)
                        ? lp
                        : startedAt;

                    if (lastProgress >= stalledCutoff) continue; // not stalled

                    var daysStalled = (int)(now - lastProgress).TotalDays;

                    stalledCustomers.TryGetValue(p.CustomerId, out var cust);
                    if (cust is null) continue;

                    reminders.Add(new
                    {
                        category = "StalledProject",
                        priority = "Medium",
                        customerId = p.CustomerId,
                        customerName = $"{cust.FirstName} {cust.LastName}".Trim(),
                        customerPhone = cust.Phone,
                        customerEmail = cust.Email,
                        projectId = p.ProjectId,
                        projectCode = p.ProjectCode,
                        projectName = p.ProjectName,
                        quotationNumber = "",
                        basis = $"Stalled at {p.ProgressPercentage}% for {daysStalled} days · no progress update",
                        actionText = "Follow up with designer or update progress",
                        reference = p.ProjectCode,
                        daysPending = daysStalled
                    });
                }
            }

            // =====================================================
            // RULE 5 — HIGH-SEVERITY OPEN ISSUE 3+ DAYS
            // (skipped for designers)
            // =====================================================
            if (!isDesigner)
            {
                var issueCutoff = now.AddDays(-3);

                var openIssues = await db.ProjectIssues
                    .Where(i => i.CompanyId == companyId
                             && (i.Status == "Open" || i.Status == "InProgress")
                             && (i.Severity == "High" || i.Severity == "Critical")
                             && i.ReportedAt < issueCutoff)
                    .Select(i => new
                    {
                        i.ProjectIssueId,
                        i.ProjectId,
                        i.CustomerId,
                        i.Title,
                        i.Severity,
                        i.ReportedAt,
                        i.IssueType
                    })
                    .ToListAsync();

                var issueProjIds = openIssues.Select(i => i.ProjectId).Distinct().ToList();
                var issueCustIds = openIssues.Select(i => i.CustomerId).Distinct().ToList();

                var issueProjects = await db.Projects
                    .Where(p => p.CompanyId == companyId && issueProjIds.Contains(p.ProjectId))
                    .Select(p => new { p.ProjectId, p.ProjectCode })
                    .ToDictionaryAsync(p => p.ProjectId);

                var issueCustomers = await db.Customers
                    .Where(c => c.CompanyId == companyId && issueCustIds.Contains(c.CustomerId))
                    .Select(c => new { c.CustomerId, c.FirstName, c.LastName, c.Phone, c.Email })
                    .ToDictionaryAsync(c => c.CustomerId);

                foreach (var i in openIssues)
                {
                    var daysOpen = (int)(now - i.ReportedAt).TotalDays;
                    issueProjects.TryGetValue(i.ProjectId, out var proj);
                    issueCustomers.TryGetValue(i.CustomerId, out var cust);
                    if (cust is null) continue;

                    reminders.Add(new
                    {
                        category = "IssueOpen",
                        priority = i.Severity == "Critical" ? "High" : "Medium",
                        customerId = i.CustomerId,
                        customerName = $"{cust.FirstName} {cust.LastName}".Trim(),
                        customerPhone = cust.Phone,
                        customerEmail = cust.Email,
                        projectId = i.ProjectId,
                        projectCode = proj?.ProjectCode ?? "",
                        projectName = "",
                        quotationNumber = "",
                        basis = $"{i.Severity} severity {i.IssueType} open for {daysOpen} days",
                        actionText = $"Resolve: {i.Title}",
                        reference = $"issue-{i.ProjectIssueId}",
                        daysPending = daysOpen
                    });
                }
            }

            // ---- Sort by priority, then by days pending ----
            var sorted = reminders
                .OrderBy(r => ((dynamic)r).priority == "High" ? 1 : 2)
                .ThenByDescending(r => ((dynamic)r).daysPending)
                .ToList();

            return Results.Ok(new
            {
                count = sorted.Count,
                highPriority = sorted.Count(r => ((dynamic)r).priority == "High"),
                mediumPriority = sorted.Count(r => ((dynamic)r).priority == "Medium"),
                computedAt = now,
                reminders = sorted
            });
        })
        .RequireAuthorization();
    }
}