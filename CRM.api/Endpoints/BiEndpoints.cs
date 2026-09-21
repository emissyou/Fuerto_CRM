using CRM.api.Security;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class BiEndpoints
{
    public static void MapBiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenant/{companyId:int}/bi")
                       .RequireAuthorization();

        // ============================================================
        // KPIs — top-level business metrics
        // ============================================================
        group.MapGet("/kpis", async (
            int companyId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var ninetyDaysAgo = now.AddDays(-90);
            var oneYearAgo = now.AddDays(-365);

            // ---- Lead conversion ----
            var totalLeads = await db.Leads.CountAsync(l => l.CompanyId == companyId);
            var convertedLeads = await db.Leads.CountAsync(l =>
                l.CompanyId == companyId && l.Status == "Converted");
            var conversionRate = totalLeads > 0
                ? Math.Round((double)convertedLeads / totalLeads * 100, 2) : 0.0;

            // ---- Average project value ----
            var avgProjectValue = await db.Quotations
                .Where(q => q.CompanyId == companyId
                         && (q.Status == "Accepted" || q.Status == "Issued"))
                .Select(q => (double?)q.TotalAmount)
                .AverageAsync() ?? 0;

            // ---- Days to accept quote ----
            var acceptedQuotes = await db.Quotations
                .Where(q => q.CompanyId == companyId
                         && q.IssuedAt != null
                         && q.AcceptedAt != null)
                .Select(q => EF.Functions.DateDiffDay(q.IssuedAt!.Value, q.AcceptedAt!.Value))
                .ToListAsync();
            var avgDaysToAccept = acceptedQuotes.Any() ? acceptedQuotes.Average() : 0;

            // ---- Days to complete project ----
            var completedProjects = await db.Projects
                .Where(p => p.CompanyId == companyId
                         && p.DesignStartDate != null
                         && p.DesignCompletionDate != null)
                .Select(p => EF.Functions.DateDiffDay(p.DesignStartDate!.Value, p.DesignCompletionDate!.Value))
                .ToListAsync();
            var avgDaysToComplete = completedProjects.Any() ? completedProjects.Average() : 0;

            // ---- Feedback ratings ----
            var feedbacks = await db.ProjectFeedbacks
                .Where(f => f.CompanyId == companyId)
                .Select(f => new { f.OverallRating, f.WouldRecommend })
                .ToListAsync();

            var avgRating = feedbacks.Any()
                ? Math.Round(feedbacks.Average(f => f.OverallRating), 2) : 0.0;
            var recommendRate = feedbacks.Any()
                ? Math.Round(feedbacks.Count(f => f.WouldRecommend) * 100.0 / feedbacks.Count, 2) : 0.0;

            // ---- Revenue windows ----
            var revenueLast30 = await db.Quotations
                .Where(q => q.CompanyId == companyId && q.FullyPaidDate >= thirtyDaysAgo)
                .SumAsync(q => (decimal?)q.AmountPaid) ?? 0m;

            var revenueLast90 = await db.Quotations
                .Where(q => q.CompanyId == companyId && q.FullyPaidDate >= ninetyDaysAgo)
                .SumAsync(q => (decimal?)q.AmountPaid) ?? 0m;

            var revenueLast365 = await db.Quotations
                .Where(q => q.CompanyId == companyId && q.FullyPaidDate >= oneYearAgo)
                .SumAsync(q => (decimal?)q.AmountPaid) ?? 0m;

            // ---- Repeat rate ----
            var customerIdsWithProjects = await db.Projects
                .Where(p => p.CompanyId == companyId)
                .GroupBy(p => p.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalCustomers = customerIdsWithProjects.Count;
            var repeatCustomers = customerIdsWithProjects.Count(c => c.Count >= 2);
            var repeatRate = totalCustomers > 0
                ? Math.Round((double)repeatCustomers / totalCustomers * 100, 2) : 0.0;

            // ---- Churn (no project in 365 days) ----
            var activeCutoff = now.AddDays(-365);
            var customersWithRecentProject = await db.Projects
                .Where(p => p.CompanyId == companyId && p.CreatedAt >= activeCutoff)
                .Select(p => p.CustomerId)
                .Distinct()
                .ToListAsync();
            var churnedCustomers = totalCustomers - customersWithRecentProject.Count;
            var churnRate = totalCustomers > 0
                ? Math.Round((double)churnedCustomers / totalCustomers * 100, 2) : 0.0;

            // ---- Issues metrics ----
            var totalIssues = await db.ProjectIssues.CountAsync(i => i.CompanyId == companyId);
            var openIssues = await db.ProjectIssues.CountAsync(i =>
                i.CompanyId == companyId
                && (i.Status == "Open" || i.Status == "InProgress"));
            var totalProjects = await db.Projects.CountAsync(p => p.CompanyId == companyId);
            var issueRate = totalProjects > 0
                ? Math.Round((double)totalIssues / totalProjects * 100, 2) : 0.0;

            return Results.Ok(new
            {
                // Counts
                totalLeads,
                convertedLeads,
                totalCustomers,
                repeatCustomers,
                totalProjects,
                totalIssues,
                openIssues,

                // Rates (%)
                leadConversionRate = conversionRate,
                repeatRate,
                churnRate,
                recommendRate,

                // Averages
                avgProjectValue = Math.Round(avgProjectValue, 2),
                avgDaysToAccept = Math.Round(avgDaysToAccept, 1),
                avgDaysToComplete = Math.Round(avgDaysToComplete, 1),
                avgRating,
                issueRate,

                // Revenue
                revenueLast30Days = revenueLast30,
                revenueLast90Days = revenueLast90,
                revenueLast365Days = revenueLast365,

                // Generated
                computedAt = now
            });
        });


        // ============================================================
        // RETENTION — customer segments + recommended actions
        // ============================================================
        group.MapGet("/retention", async (
            int companyId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var now = DateTime.UtcNow;

            // Pull customers with their project + feedback + issue stats
            var customers = await db.Customers
                .Where(c => c.CompanyId == companyId && c.IsActive)
                .Select(c => new
                {
                    c.CustomerId,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.Phone,
                    c.CustomerType,
                    c.CreatedAt
                })
                .ToListAsync();

            var projects = await db.Projects
                .Where(p => p.CompanyId == companyId)
                .Select(p => new
                {
                    p.ProjectId,
                    p.CustomerId,
                    p.CreatedAt,
                    p.ProjectType,
                    p.DesignCompletionDate,
                    p.Status
                })
                .ToListAsync();

            var feedbacks = await db.ProjectFeedbacks
                .Where(f => f.CompanyId == companyId)
                .Select(f => new { f.CustomerId, f.OverallRating })
                .ToListAsync();

            var issues = await db.ProjectIssues
                .Where(i => i.CompanyId == companyId)
                .Select(i => new { i.CustomerId, i.Status })
                .ToListAsync();

            var quotations = await db.Quotations
                .Where(q => q.CompanyId == companyId)
                .Select(q => new { q.CustomerId, q.AmountPaid })
                .ToListAsync();

            var result = new List<object>();

            foreach (var c in customers)
            {
                var myProjects = projects.Where(p => p.CustomerId == c.CustomerId).ToList();
                var myFeedbacks = feedbacks.Where(f => f.CustomerId == c.CustomerId).ToList();
                var myIssues = issues.Where(i => i.CustomerId == c.CustomerId).ToList();
                var myQuotes = quotations.Where(q => q.CustomerId == c.CustomerId).ToList();

                var projectCount = myProjects.Count;
                if (projectCount == 0) continue; // skip customers with no history

                var lastProjectDate = myProjects.Max(p => p.CreatedAt);
                var daysSinceLastProject = (int)(now - lastProjectDate).TotalDays;

                var ratingAvg = myFeedbacks.Any()
                    ? (double?)Math.Round(myFeedbacks.Average(f => f.OverallRating), 2) : null;

                var totalRevenue = myQuotes.Sum(q => q.AmountPaid);
                var openIssueCount = myIssues.Count(i =>
                    i.Status == "Open" || i.Status == "InProgress");

                // ---- Apply segment rules (in priority order) ----
                string segment;
                string basis;
                string action;
                int priority;

                if (projectCount >= 2 && ratingAvg >= 4.5 && daysSinceLastProject <= 180)
                {
                    segment = "Champion";
                    basis = $"{projectCount} projects · avg {ratingAvg:F1}★ · {daysSinceLastProject} days since last project";
                    action = "Send appreciation email + suggest portfolio add-on";
                    priority = 1;
                }
                else if (projectCount >= 2 && ratingAvg >= 4.0 && daysSinceLastProject <= 365)
                {
                    segment = "Loyal";
                    basis = $"{projectCount} projects · avg {ratingAvg:F1}★ · {daysSinceLastProject} days since last project";
                    action = "Offer 10% loyalty discount + quarterly check-in";
                    priority = 2;
                }
                else if (ratingAvg <= 3.0 && projectCount >= 1)
                {
                    segment = "Detractor";
                    basis = $"avg rating {ratingAvg:F1}★ — escalate to management";
                    action = "Personal apology call + free consultation";
                    priority = 3;
                }
                else if (projectCount == 1 && ratingAvg >= 4.0 && daysSinceLastProject <= 180)
                {
                    segment = "Promising";
                    basis = $"1 project · avg {ratingAvg:F1}★ · {daysSinceLastProject} days since last project";
                    action = "Call to discuss next room or property";
                    priority = 4;
                }
                else if (daysSinceLastProject > 730)
                {
                    segment = "Lost";
                    basis = $"{daysSinceLastProject} days since last project (>2 years)";
                    action = "Add to quarterly newsletter only";
                    priority = 6;
                }
                else if (daysSinceLastProject > 540 && (ratingAvg ?? 0) < 4.0)
                {
                    segment = "Dormant";
                    basis = $"{daysSinceLastProject} days inactive · low engagement";
                    action = "Final reactivation attempt";
                    priority = 7;
                }
                else if (daysSinceLastProject > 365)
                {
                    segment = "At Risk";
                    basis = $"{daysSinceLastProject} days since last project (>1 year)";
                    action = "Win-back email + 15% discount";
                    priority = 5;
                }
                else
                {
                    segment = "Active";
                    basis = $"Recent activity within 12 months";
                    action = "No action required";
                    priority = 99;
                }

                result.Add(new
                {
                    customerId = c.CustomerId,
                    fullName = $"{c.FirstName} {c.LastName}".Trim(),
                    email = c.Email,
                    phone = c.Phone,
                    customerType = c.CustomerType,
                    projectCount,
                    lastProjectDate,
                    daysSinceLastProject,
                    avgRating = ratingAvg,
                    feedbackCount = myFeedbacks.Count,
                    openIssueCount,
                    totalRevenue,
                    segment,
                    priority,
                    basis,
                    action
                });
            }

            // Sort by priority, then by days since last project (descending)
            var sorted = result
                .OrderBy(r => ((dynamic)r).priority)
                .ThenByDescending(r => ((dynamic)r).daysSinceLastProject)
                .ToList();

            return Results.Ok(sorted);
        });


        // ============================================================
        // DESIGNERS — leaderboard
        // ============================================================
        group.MapGet("/designers", async (
            int companyId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            // Designer snapshots from projects
            var designerGroups = await db.Projects
                .Where(p => p.CompanyId == companyId && p.DesignerId != null)
                .GroupBy(p => new { p.DesignerId, p.DesignerName })
                .Select(g => new
                {
                    DesignerId = g.Key.DesignerId!,
                    DesignerName = g.Key.DesignerName,
                    TotalProjects = g.Count(),
                    CompletedProjects = g.Count(p => p.DesignStage == "Completed"),
                    ActiveProjects = g.Count(p => p.DesignStage != "Completed"
                                               && p.DesignStage != "Cancelled"),
                    AvgCompletionDays = g.Where(p => p.DesignStartDate != null
                                                  && p.DesignCompletionDate != null)
                                         .Select(p => (double?)EF.Functions.DateDiffDay(
                                             p.DesignStartDate!.Value,
                                             p.DesignCompletionDate!.Value))
                                         .Average()
                })
                .ToListAsync();

            var feedbackByDesigner = await db.ProjectFeedbacks
                .Where(f => f.CompanyId == companyId)
                .Join(
                    db.Projects.Where(p => p.DesignerId != null),
                    f => f.ProjectId,
                    p => p.ProjectId,
                    (f, p) => new
                    {
                        DesignerId = p.DesignerId!,
                        f.OverallRating,
                        f.TimelinessRating,
                        f.CommunicationRating,
                        f.ValueRating,
                        f.WouldRecommend
                    })
                .ToListAsync();

            var issuesByDesigner = await db.ProjectIssues
                .Where(i => i.CompanyId == companyId)
                .Join(
                    db.Projects.Where(p => p.DesignerId != null),
                    i => i.ProjectId,
                    p => p.ProjectId,
                    (i, p) => new { DesignerId = p.DesignerId!, i.Status })
                .ToListAsync();

            var result = new List<object>();

            foreach (var g in designerGroups)
            {
                var fbs = feedbackByDesigner.Where(f => f.DesignerId == g.DesignerId).ToList();
                var iss = issuesByDesigner.Where(i => i.DesignerId == g.DesignerId).ToList();

                var avgOverall = fbs.Any() ? Math.Round(fbs.Average(f => f.OverallRating), 2) : (double?)null;
                var avgTimeliness = fbs.Any() ? Math.Round(fbs.Average(f => f.TimelinessRating), 2) : (double?)null;
                var avgComm = fbs.Any() ? Math.Round(fbs.Average(f => f.CommunicationRating), 2) : (double?)null;
                var avgValue = fbs.Any() ? Math.Round(fbs.Average(f => f.ValueRating), 2) : (double?)null;
                var recommendRate = fbs.Any()
                    ? Math.Round(fbs.Count(f => f.WouldRecommend) * 100.0 / fbs.Count, 2)
                    : (double?)null;

                double? overallScore = (avgOverall.HasValue && avgTimeliness.HasValue
                                     && avgComm.HasValue && avgValue.HasValue)
                    ? Math.Round((avgOverall.Value + avgTimeliness.Value
                                + avgComm.Value + avgValue.Value) / 4.0, 2)
                    : (double?)null;

                result.Add(new
                {
                    designerId = g.DesignerId,
                    designerName = g.DesignerName,
                    totalProjects = g.TotalProjects,
                    completedProjects = g.CompletedProjects,
                    activeProjects = g.ActiveProjects,
                    avgCompletionDays = Math.Round(g.AvgCompletionDays ?? 0, 1),
                    feedbackCount = fbs.Count,
                    avgOverallRating = avgOverall,
                    avgTimelinessRating = avgTimeliness,
                    avgCommunicationRating = avgComm,
                    avgValueRating = avgValue,
                    recommendRate,
                    overallScore,
                    totalIssues = iss.Count,
                    openIssues = iss.Count(i => i.Status == "Open" || i.Status == "InProgress"),
                    issueRate = g.TotalProjects > 0
                        ? Math.Round((double)iss.Count / g.TotalProjects * 100, 2)
                        : 0.0
                });
            }

            // Sort by overall score descending
            var sorted = result
                .OrderByDescending(r => ((dynamic)r).overallScore ?? 0)
                .ThenByDescending(r => ((dynamic)r).totalProjects)
                .ToList();

            return Results.Ok(sorted);
        });


        // ============================================================
        // REVENUE — monthly trend, last 12 months
        // ============================================================
        group.MapGet("/revenue", async (
            int companyId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var cutoff = DateTime.UtcNow.AddMonths(-12);

            var rows = await db.Quotations
                .Where(q => q.CompanyId == companyId
                         && q.FullyPaidDate != null
                         && q.FullyPaidDate >= cutoff)
                .Select(q => new
                {
                    Year = q.FullyPaidDate!.Value.Year,
                    Month = q.FullyPaidDate!.Value.Month,
                    q.AmountPaid,
                    q.TotalAmount,
                    ProjectId = q.ProjectId
                })
                .ToListAsync();

            // Project count per month (for pipeline context)
            var projectRows = await db.Projects
                .Where(p => p.CompanyId == companyId && p.CreatedAt >= cutoff)
                .Select(p => new { p.CreatedAt, p.ProjectId })
                .ToListAsync();

            var months = Enumerable.Range(0, 12)
                .Select(i => DateTime.UtcNow.AddMonths(-11 + i))
                .Select(d => new
                {
                    Year = d.Year,
                    Month = d.Month,
                    Label = d.ToString("MMM yyyy")
                })
                .ToList();

            var result = months.Select(m =>
            {
                var matching = rows.Where(r => r.Year == m.Year && r.Month == m.Month).ToList();
                var projectCount = projectRows.Count(p => p.CreatedAt.Year == m.Year && p.CreatedAt.Month == m.Month);

                return new
                {
                    year = m.Year,
                    month = m.Month,
                    label = m.Label,
                    revenue = matching.Sum(r => r.AmountPaid),
                    invoiced = matching.Sum(r => r.TotalAmount),
                    transactionCount = matching.Count,
                    projectCount
                };
            }).ToList();

            return Results.Ok(result);
        });

        // ============================================================
        // LOG RETENTION ACTION
        // POST /tenant/{companyId}/bi/retention/actions
        // ============================================================
        group.MapPost("/retention/actions", async (
          int companyId,
          RetentionActionPayload request,
          HttpContext http,
          ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (request.CustomerId <= 0)
                return Results.BadRequest(new { message = "CustomerId is required." });

            if (string.IsNullOrWhiteSpace(request.Segment))
                return Results.BadRequest(new { message = "Segment is required." });

            await using var db = await tenantFactory.CreateAsync(companyId);

            // Validate customer exists in this company
            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId
                                       && c.CompanyId == companyId);

            if (customer is null)
                return Results.NotFound(new { message = "Customer not found." });

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var activity = new CRM.domain.Entities.Activity
            {
                CompanyId = companyId,
                CustomerId = customer.CustomerId,
                ActivityType = "RetentionAction",
                Subject = $"[{request.Segment}] {request.ActionTaken}",
                Description = request.Script ?? "",
                ActivityDate = DateTime.UtcNow,
                FollowUpDate = request.FollowUpDate,
                Status = "Completed",
                Notes = $"Basis: {request.Basis ?? "n/a"}"
            };

            db.Activities.Add(activity);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/tenant/{companyId}/activities/{activity.ActivityId}",
                new
                {
                    message = "Retention action logged.",
                    activityId = activity.ActivityId,
                    customerId = customer.CustomerId,
                    customerName = $"{customer.FirstName} {customer.LastName}".Trim(),
                    segment = request.Segment,
                    actionTaken = request.ActionTaken,
                    activityDate = activity.ActivityDate
                });
        })
        .RequireAuthorization();

    }

    // ============================================================
    // Inline DTO for retention action
    // ============================================================
    public class RetentionActionPayload
    {
        public int CustomerId { get; set; }
        public string Segment { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public string? Basis { get; set; }
        public string? Script { get; set; }
        public DateTime? FollowUpDate { get; set; }
    }
}