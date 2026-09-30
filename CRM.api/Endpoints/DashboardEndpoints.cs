using CRM.api.Security;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/tenant/{companyId:int}/dashboard", async (
            int companyId,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            // Tenant isolation
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var totalCustomers = await db.Customers
                .CountAsync(x =>
                    x.CompanyId == companyId &&
                    x.IsActive);

            var totalLeads = await db.Leads
                .CountAsync(x =>
                    x.CompanyId == companyId &&
                    x.IsActive);

            var activeProjects = await db.Projects
                .CountAsync(x =>
                    x.CompanyId == companyId &&
                    x.IsActive &&
                    x.Status != "Completed" &&
                    x.Status != "Cancelled");

            var pendingQuotations = await db.Quotations
                .CountAsync(x =>
                    x.CompanyId == companyId &&
                    (x.Status == "Draft" ||
                     x.Status == "Sent" ||
                     x.Status == "Under Review"));

            var totalQuotationValue = await db.Quotations
                .Where(x =>
                    x.CompanyId == companyId &&
                    x.Status != "Cancelled" &&
                    x.Status != "Rejected")
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

            var pendingFollowUps = await db.Activities
                .CountAsync(x =>
                    x.CompanyId == companyId &&
                    x.Status == "Pending");

            var recentActivities = await db.Activities
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .OrderByDescending(x => x.ActivityId)
                .Take(5)
                .Select(x => new
                {
                    x.ActivityId,
                    x.ActivityType,
                    x.Subject,
                    x.ActivityDate,
                    x.FollowUpDate,
                    x.Status,
                    x.CustomerId,
                    x.LeadId,
                    x.ProjectId
                })
                .ToListAsync();

            return Results.Ok(new
            {
                companyId,
                summary = new
                {
                    totalCustomers,
                    totalLeads,
                    activeProjects,
                    pendingQuotations,
                    totalQuotationValue,
                    pendingFollowUps
                },
                recentActivities
            });
        })
        .RequireAuthorization();
    }
}