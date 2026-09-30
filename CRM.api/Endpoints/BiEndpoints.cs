using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Identity;
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

            try
            {
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
            }
            catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException || ex is System.ComponentModel.Win32Exception || ex.InnerException is Microsoft.Data.SqlClient.SqlException)
            {
                return Results.Json(new { error = "Database unavailable. Using offline mode — data may not be current.", offline = true }, statusCode: 503);
            }
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

            try
            {
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

                var lastProjectDate = projectCount > 0 ? myProjects.Max(p => p.CreatedAt) : (DateTime?)null;
                var daysSinceLastProject = lastProjectDate.HasValue 
                    ? (int)(now - lastProjectDate.Value).TotalDays 
                    : (int)(now - c.CreatedAt).TotalDays;

                var ratingAvg = myFeedbacks.Any()
                    ? (double?)Math.Round(myFeedbacks.Average(f => f.OverallRating), 2) : null;

                var totalRevenue = myQuotes.Sum(q => q.AmountPaid);
                var openIssueCount = myIssues.Count(i =>
                    i.Status == "Open" || i.Status == "InProgress");

                // ---- Apply segment rules (respecting explicit customer loyalty tier if set) ----
                string segment;
                string basis;
                string action;
                int priority;

                var explicitType = (c.CustomerType ?? "").Trim();

                if (string.Equals(explicitType, "Champion", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(explicitType, "VIP", StringComparison.OrdinalIgnoreCase))
                {
                    segment = "Champion";
                    basis = projectCount > 0
                        ? $"Tier: Champion · {projectCount} projects · avg {ratingAvg:F1}★ · {daysSinceLastProject}d"
                        : $"Tier: Champion · Registered {daysSinceLastProject} days ago · 0 projects";
                    action = "Send VIP appreciation email + premium rewards";
                    priority = 1;
                }
                else if (string.Equals(explicitType, "Loyal", StringComparison.OrdinalIgnoreCase))
                {
                    segment = "Loyal";
                    basis = projectCount > 0
                        ? $"Tier: Loyal · {projectCount} projects · avg {ratingAvg:F1}★ · {daysSinceLastProject}d"
                        : $"Tier: Loyal · Registered {daysSinceLastProject} days ago · 0 projects";
                    action = "Offer 10% loyalty discount + quarterly check-in";
                    priority = 2;
                }
                else if (string.Equals(explicitType, "Detractor", StringComparison.OrdinalIgnoreCase))
                {
                    segment = "Detractor";
                    basis = $"Tier: Detractor · Escalate for service recovery";
                    action = "Personal apology call + free consultation";
                    priority = 3;
                }
                else if (string.Equals(explicitType, "Promising", StringComparison.OrdinalIgnoreCase))
                {
                    segment = "Promising";
                    basis = projectCount > 0
                        ? $"Tier: Promising · {projectCount} project(s) · {daysSinceLastProject}d"
                        : $"Tier: Promising · Registered {daysSinceLastProject} days ago";
                    action = "Call to discuss next room or property";
                    priority = 4;
                }
                else if (string.Equals(explicitType, "At Risk", StringComparison.OrdinalIgnoreCase))
                {
                    segment = "At Risk";
                    basis = $"Tier: At Risk · Inactive {daysSinceLastProject} days";
                    action = "Win-back email + 15% discount";
                    priority = 5;
                }
                else if (string.Equals(explicitType, "Dormant", StringComparison.OrdinalIgnoreCase))
                {
                    segment = "Dormant";
                    basis = $"Tier: Dormant · Inactive {daysSinceLastProject} days";
                    action = "Final reactivation attempt";
                    priority = 7;
                }
                else if (string.Equals(explicitType, "Lost", StringComparison.OrdinalIgnoreCase))
                {
                    segment = "Lost";
                    basis = $"Tier: Lost · Inactive {daysSinceLastProject} days";
                    action = "Add to quarterly newsletter only";
                    priority = 6;
                }
                else if (projectCount == 0)
                {
                    segment = "Active";
                    basis = $"Registered {daysSinceLastProject} days ago · 0 projects completed";
                    action = "Send onboarding welcome email + initial consultation offer";
                    priority = 20;
                }
                else if (projectCount >= 2 && ratingAvg >= 4.5 && daysSinceLastProject <= 180)
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
                    action = "Send check-in email + loyalty reward offer";
                    priority = 10;
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
            }
            catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException || ex is System.ComponentModel.Win32Exception || ex.InnerException is Microsoft.Data.SqlClient.SqlException)
            {
                return Results.Json(new { error = "Database unavailable. Using offline mode — retention data may not be current.", offline = true }, statusCode: 503);
            }
        });


        // ============================================================
        // DESIGNERS / STAFF LIST (with stats)
        // Returns all Staff members (Staff now does design work).
        // ============================================================
        group.MapGet("/designers", async (
            int companyId,
            HttpContext http,
            UserManager<ApplicationUser> userManager,   
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            // Get all users in this company
            // If cloud is offline the Identity store may be unreachable — return empty list gracefully.
            List<ApplicationUser> users;
            try
            {
                users = userManager.Users
                    .Where(u => u.CompanyId == companyId)
                    .ToList();
            }
            catch
            {
                users = new List<ApplicationUser>();
            }

            // Filter to Staff members only
            var designers = new List<ApplicationUser>();
            foreach (var u in users)
            {
                try
                {
                    if (await userManager.IsInRoleAsync(u, "Staff"))
                        designers.Add(u);
                }
                catch { /* skip if role check fails offline */ }
            }

            // Get tenant DB to compute stats
            try
            {
            await using var db = await tenantFactory.CreateAsync(companyId);

            var designerIds = designers.Select(d => d.Id).ToList();

            var projectsByDesigner = await db.Projects
                .Where(p => p.CompanyId == companyId
                         && p.DesignerId != null
                         && designerIds.Contains(p.DesignerId))
                .Select(p => new
                {
                    p.ProjectId,
                    p.DesignerId,
                    p.DesignStage,
                    p.ProgressPercentage
                })
                .ToListAsync();

            var feedbackRows = await db.ProjectFeedbacks
                .Where(f => f.CompanyId == companyId)
                .Join(
                    db.Projects.Where(p => p.DesignerId != null
                                        && designerIds.Contains(p.DesignerId)),
                    f => f.ProjectId,
                    p => p.ProjectId,
                    (f, p) => new
                    {
                        DesignerId = p.DesignerId!,
                        f.OverallRating,
                        f.TimelinessRating,
                        f.CommunicationRating,
                        f.ValueRating
                    })
                .ToListAsync();

            var issuesByDesigner = await db.ProjectIssues
                .Where(i => i.CompanyId == companyId)
                .Join(
                    db.Projects.Where(p => p.DesignerId != null
                                        && designerIds.Contains(p.DesignerId)),
                    i => i.ProjectId,
                    p => p.ProjectId,
                    (i, p) => new
                    {
                        DesignerId = p.DesignerId!,
                        i.Status
                    })
                .ToListAsync();

            var result = new List<object>();

            foreach (var d in designers)
            {
                var myProjects = projectsByDesigner
                    .Where(p => p.DesignerId == d.Id)
                    .ToList();

                var myFeedback = feedbackRows
                    .Where(f => f.DesignerId == d.Id)
                    .ToList();

                var myIssues = issuesByDesigner
                    .Where(i => i.DesignerId == d.Id)
                    .ToList();

                var totalProjects = myProjects.Count;
                var activeProjects = myProjects.Count(p =>
                    p.DesignStage != "Completed" && p.DesignStage != "Cancelled");
                var completedProjects = myProjects.Count(p => p.DesignStage == "Completed");
                var openIssues = myIssues.Count(i =>
                    i.Status == "Open" || i.Status == "InProgress");

                double? avgOverall = myFeedback.Count > 0
                    ? Math.Round(myFeedback.Average(f => f.OverallRating), 2) : null;
                double? avgTimeliness = myFeedback.Count > 0
                    ? Math.Round(myFeedback.Average(f => f.TimelinessRating), 2) : null;
                double? avgCommunication = myFeedback.Count > 0
                    ? Math.Round(myFeedback.Average(f => f.CommunicationRating), 2) : null;
                double? avgValue = myFeedback.Count > 0
                    ? Math.Round(myFeedback.Average(f => f.ValueRating), 2) : null;

                double? overallScore = (avgOverall.HasValue && avgTimeliness.HasValue
                                     && avgCommunication.HasValue && avgValue.HasValue)
                    ? Math.Round((avgOverall.Value + avgTimeliness.Value
                                + avgCommunication.Value + avgValue.Value) / 4.0, 2)
                    : (double?)null;

                result.Add(new
                {
                    userId = d.Id,
                    fullName = string.IsNullOrWhiteSpace(d.FullName)
                        ? (d.Email ?? d.UserName ?? "Staff")
                        : d.FullName,
                    email = d.Email,
                    totalProjects,
                    activeProjects,
                    completedProjects,
                    openIssues,
                    feedbackCount = myFeedback.Count,
                    avgOverallRating = avgOverall,
                    avgTimelinessRating = avgTimeliness,
                    avgCommunicationRating = avgCommunication,
                    avgValueRating = avgValue,
                    overallScore
                });
            }

            return Results.Ok(result);
            }
            catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException || ex is System.ComponentModel.Win32Exception || ex.InnerException is Microsoft.Data.SqlClient.SqlException)
            {
                return Results.Json(new { error = "Database unavailable. Designers data not available in offline mode.", offline = true }, statusCode: 503);
            }
        })
        .RequireAuthorization();


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

            try
            {
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
            }
            catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException || ex is System.ComponentModel.Win32Exception || ex.InnerException is Microsoft.Data.SqlClient.SqlException)
            {
                return Results.Json(new { error = "Database unavailable. Revenue data not available in offline mode.", offline = true }, statusCode: 503);
            }
        });

       
        // ============================================================
        // LOG RETENTION ACTION (supports manual retention of any customer)
        // POST /tenant/{companyId}/bi/retention/actions
        // ============================================================
        group.MapPost("/retention/actions", async (
            int companyId,
            RetentionActionPayload request,
            HttpContext http,
            ITenantDbContextFactory tenantFactory,
            UserManager<ApplicationUser> userManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (request.CustomerId <= 0)
                return Results.BadRequest(new { message = "CustomerId is required." });

            await using var db = await tenantFactory.CreateAsync(companyId);

            // Validate customer exists in this company
            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId
                                       && c.CompanyId == companyId);

            if (customer is null)
                return Results.NotFound(new { message = "Customer not found." });

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var user = await userManager.FindByIdAsync(userId);
            var userName = user?.FullName ?? user?.Email ?? "Staff";

            // Determine if this is a manual retention (any customer)
            var isManual = string.Equals(request.Source, "Manual",
                StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(request.Segment)
                || request.Segment == "Manual";

            var segment = string.IsNullOrWhiteSpace(request.Segment)
                ? "Manual"
                : request.Segment;

            // Build offer description
            var offerDesc = request.OfferDescription;
            if (string.IsNullOrWhiteSpace(offerDesc) && request.OfferValue.HasValue)
            {
                offerDesc = request.OfferType switch
                {
                    "Percentage" => $"{request.OfferValue}% discount",
                    "FixedAmount" => $"₱{request.OfferValue:N0} discount",
                    "FreeService" => "Free consultation",
                    _ => "Custom offer"
                };
            }

            // Create RetentionAction record
            var action = new CRM.domain.Entities.RetentionAction
            {
                CompanyId = companyId,
                CustomerId = customer.CustomerId,
                ProjectId = request.ProjectId,
                PromotionId = request.PromotionId,
                OfferType = string.IsNullOrWhiteSpace(request.OfferType)
                    ? "Custom"
                    : request.OfferType,
                OfferValue = request.OfferValue,
                OfferDescription = offerDesc ?? "",
                Segment = segment,
                Basis = request.Basis ?? "",
                Notes = request.Notes ?? "",
                ScriptUsed = request.Script ?? "",
                Source = isManual ? "Manual" : "Automated",
                ActionTaken = string.IsNullOrWhiteSpace(request.ActionTaken)
                    ? "Retention offer"
                    : request.ActionTaken,
                Status = "Logged",
                FollowUpDate = request.FollowUpDate,
                CreatedByUserId = userId,
                CreatedByName = userName,
                CreatedAt = DateTime.UtcNow
            };

            db.RetentionActions.Add(action);

            // If this retention action uses a promotion, mark it as used
            if (request.PromotionId.HasValue && request.PromotionId.Value > 0)
            {
                var promo = await db.Promotions
                    .FirstOrDefaultAsync(p => p.PromotionId == request.PromotionId.Value
                                           && p.CompanyId == companyId);

                if (promo is not null)
                {
                    promo.UsedCount++;

                    // Auto-deactivate if max uses reached
                    if (promo.MaxUses.HasValue && promo.UsedCount >= promo.MaxUses.Value)
                        promo.IsActive = false;
                }
            }

            // Also log an Activity for the timeline
            db.Activities.Add(new CRM.domain.Entities.Activity
            {
                CompanyId = companyId,
                CustomerId = customer.CustomerId,
                ProjectId = request.ProjectId,
                ActivityType = "RetentionAction",
                Subject = $"[{segment}] {action.ActionTaken}",
                Description = string.IsNullOrWhiteSpace(offerDesc)
                    ? (request.Script ?? "")
                    : $"Offer: {offerDesc}\n{request.Script}",
                ActivityDate = DateTime.UtcNow,
                FollowUpDate = request.FollowUpDate,
                Status = "Completed",
                Notes = $"Basis: {request.Basis ?? "n/a"}  ·  By: {userName}"
            });

            // If customer status/loyalty tier was updated
            if (!string.IsNullOrWhiteSpace(request.NewCustomerStatus))
            {
                customer.CustomerType = request.NewCustomerStatus.Trim();
            }

            await db.SaveChangesAsync();

            return Results.Created(
                $"/tenant/{companyId}/bi/retention/actions/{action.RetentionActionId}",
                new
                {
                    message = isManual
                        ? "Manual retention logged successfully."
                        : "Retention action logged successfully.",
                    retentionActionId = action.RetentionActionId,
                    customerId = customer.CustomerId,
                    customerName = $"{customer.FirstName} {customer.LastName}".Trim(),
                    segment,
                    source = action.Source,
                    offerType = action.OfferType,
                    offerValue = action.OfferValue,
                    offerDescription = action.OfferDescription,
                    actionTaken = action.ActionTaken,
                    newCustomerStatus = customer.CustomerType,
                    createdAt = action.CreatedAt
                });
        })
        .RequireAuthorization();


        // ============================================================
        // UPDATE CUSTOMER STATUS / LOYALTY TIER DIRECTLY
        // ============================================================
        group.MapPut("/customers/{customerId:int}/status", async (
            int companyId,
            int customerId,
            CustomerStatusUpdateDto dto,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(dto.Status))
                return Results.BadRequest(new { message = "Status cannot be empty." });

            await using var db = await tenantFactory.CreateAsync(companyId);
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.CompanyId == companyId);
            if (customer is null)
                return Results.NotFound(new { message = "Customer not found." });

            var oldStatus = customer.CustomerType;
            customer.CustomerType = dto.Status.Trim();

            db.Activities.Add(new CRM.domain.Entities.Activity
            {
                CompanyId = companyId,
                CustomerId = customer.CustomerId,
                ActivityType = "StatusChange",
                Subject = $"Customer loyalty tier updated to {customer.CustomerType}",
                Description = $"Customer status changed from '{oldStatus}' to '{customer.CustomerType}'.",
                ActivityDate = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = $"Customer status successfully updated to {customer.CustomerType}.",
                customerId = customer.CustomerId,
                status = customer.CustomerType
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
        public int? ProjectId { get; set; }
        public string Segment { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public string? Basis { get; set; }
        public string? Script { get; set; }
        public DateTime? FollowUpDate { get; set; }

        // ---- Offer ----
        public string? OfferType { get; set; }
        public decimal? OfferValue { get; set; }
        public string? OfferDescription { get; set; }

        // ---- NEW: link to a promotion ----
        public int? PromotionId { get; set; }

        // ---- Customer status / loyalty tier change ----
        public string? NewCustomerStatus { get; set; }

        public string? Notes { get; set; }
        public string? Source { get; set; }
    }
}