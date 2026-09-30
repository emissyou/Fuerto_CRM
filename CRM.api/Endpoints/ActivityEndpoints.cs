using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this WebApplication app)
    {
        // CREATE ACTIVITY
        app.MapPost("/tenant/{companyId:int}/activities", async (
            int companyId,
            HttpContext httpContext,
            Activity activity,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            // Validate Customer
            if (activity.CustomerId.HasValue)
            {
                var customerExists = await db.Customers
                    .AnyAsync(x =>
                        x.CustomerId == activity.CustomerId.Value &&
                        x.CompanyId == companyId);

                if (!customerExists)
                {
                    return Results.BadRequest(new
                    {
                        message = "Customer does not exist in this company."
                    });
                }
            }

            // Validate Lead
            if (activity.LeadId.HasValue)
            {
                var leadExists = await db.Leads
                    .AnyAsync(x =>
                        x.LeadId == activity.LeadId.Value &&
                        x.CompanyId == companyId);

                if (!leadExists)
                {
                    return Results.BadRequest(new
                    {
                        message = "Lead does not exist in this company."
                    });
                }
            }

            // Validate Project
            if (activity.ProjectId.HasValue)
            {
                var projectExists = await db.Projects
                    .AnyAsync(x =>
                        x.ProjectId == activity.ProjectId.Value &&
                        x.CompanyId == companyId);

                if (!projectExists)
                {
                    return Results.BadRequest(new
                    {
                        message = "Project does not exist in this company."
                    });
                }
            }

            // Always use the authenticated route company
            activity.CompanyId = companyId;

            activity.ActivityDate = activity.ActivityDate == default
                ? DateTime.UtcNow
                : activity.ActivityDate;

            db.Activities.Add(activity);

            await db.SaveChangesAsync();

            return Results.Created(
                $"/tenant/{companyId}/activities/{activity.ActivityId}",
                activity);
        })
        .RequireAuthorization();


        // GET ALL ACTIVITIES
        app.MapGet("/tenant/{companyId:int}/activities", async (
            int companyId,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var activities = await db.Activities
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Lead)
                .Include(x => x.Project)
                .Where(x => x.CompanyId == companyId)
                .OrderByDescending(x => x.ActivityId)
                .ToListAsync();

            return Results.Ok(activities);
        })
        .RequireAuthorization();


        // GET ONE ACTIVITY
        app.MapGet("/tenant/{companyId:int}/activities/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var activity = await db.Activities
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Lead)
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x =>
                    x.ActivityId == id &&
                    x.CompanyId == companyId);

            if (activity is null)
            {
                return Results.NotFound(new
                {
                    message = "Activity not found."
                });
            }

            return Results.Ok(activity);
        })
        .RequireAuthorization();


        // UPDATE ACTIVITY
        app.MapPut("/tenant/{companyId:int}/activities/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            Activity request,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var activity = await db.Activities
                .FirstOrDefaultAsync(x =>
                    x.ActivityId == id &&
                    x.CompanyId == companyId);

            if (activity is null)
            {
                return Results.NotFound(new
                {
                    message = "Activity not found."
                });
            }

            // Validate Customer
            if (request.CustomerId.HasValue)
            {
                var customerExists = await db.Customers
                    .AnyAsync(x =>
                        x.CustomerId == request.CustomerId.Value &&
                        x.CompanyId == companyId);

                if (!customerExists)
                {
                    return Results.BadRequest(new
                    {
                        message = "Customer does not exist in this company."
                    });
                }
            }

            // Validate Lead
            if (request.LeadId.HasValue)
            {
                var leadExists = await db.Leads
                    .AnyAsync(x =>
                        x.LeadId == request.LeadId.Value &&
                        x.CompanyId == companyId);

                if (!leadExists)
                {
                    return Results.BadRequest(new
                    {
                        message = "Lead does not exist in this company."
                    });
                }
            }

            // Validate Project
            if (request.ProjectId.HasValue)
            {
                var projectExists = await db.Projects
                    .AnyAsync(x =>
                        x.ProjectId == request.ProjectId.Value &&
                        x.CompanyId == companyId);

                if (!projectExists)
                {
                    return Results.BadRequest(new
                    {
                        message = "Project does not exist in this company."
                    });
                }
            }

            activity.CustomerId = request.CustomerId;
            activity.LeadId = request.LeadId;
            activity.ProjectId = request.ProjectId;
            activity.ActivityType = request.ActivityType;
            activity.Subject = request.Subject;
            activity.Description = request.Description;
            activity.ActivityDate = request.ActivityDate;
            activity.FollowUpDate = request.FollowUpDate;
            activity.Status = request.Status;
            activity.Notes = request.Notes;

            await db.SaveChangesAsync();

            return Results.Ok(activity);
        })
        .RequireAuthorization();


        // DELETE ACTIVITY
        app.MapDelete("/tenant/{companyId:int}/activities/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var activity = await db.Activities
                .FirstOrDefaultAsync(x =>
                    x.ActivityId == id &&
                    x.CompanyId == companyId);

            if (activity is null)
            {
                return Results.NotFound(new
                {
                    message = "Activity not found."
                });
            }

            db.Activities.Remove(activity);

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Activity deleted successfully."
            });
        })
        .RequireAuthorization();
    }
}