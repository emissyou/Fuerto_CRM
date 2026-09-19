using CRM.api.Models;
using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        // CREATE PROJECT
        app.MapPost("/tenant/{companyId:int}/projects", async (
            int companyId,
            HttpContext httpContext,
            Project project,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var customerExists = await db.Customers
                .AnyAsync(x =>
                    x.CustomerId == project.CustomerId &&
                    x.CompanyId == companyId);

            if (!customerExists)
            {
                return Results.BadRequest(new
                {
                    message = "Customer does not exist in this company."
                });
            }

            project.CompanyId = companyId;
            project.CreatedAt = DateTime.UtcNow;

            // Ensure designer fields are preserved or default
            project.DesignerId = project.DesignerId;
            project.DesignerName = project.DesignerName ?? string.Empty;

            db.Projects.Add(project);

            await db.SaveChangesAsync();

            return Results.Created(
                $"/tenant/{companyId}/projects/{project.ProjectId}",
                project);
        })
        .RequireAuthorization();


        // GET ALL PROJECTS
        app.MapGet("/tenant/{companyId:int}/projects", async (
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

            var projects = await db.Projects
                .AsNoTracking()
                .Include(x => x.Customer)
                .Where(x => x.CompanyId == companyId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Results.Ok(projects);
        })
        .RequireAuthorization();


        // GET ONE PROJECT
        app.MapGet("/tenant/{companyId:int}/projects/{id:int}", async (
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

            var project = await db.Projects
                .AsNoTracking()
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == id &&
                    x.CompanyId == companyId);

            if (project is null)
            {
                return Results.NotFound(new
                {
                    message = "Project not found."
                });
            }

            return Results.Ok(project);
        })
        .RequireAuthorization();


        // UPDATE PROJECT
        app.MapPut("/tenant/{companyId:int}/projects/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            Project request,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var project = await db.Projects
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == id &&
                    x.CompanyId == companyId);

            if (project is null)
            {
                return Results.NotFound(new
                {
                    message = "Project not found."
                });
            }

            var customerExists = await db.Customers
                .AnyAsync(x =>
                    x.CustomerId == request.CustomerId &&
                    x.CompanyId == companyId);

            if (!customerExists)
            {
                return Results.BadRequest(new
                {
                    message = "Customer does not exist in this company."
                });
            }

            project.ProjectCode = request.ProjectCode;
            project.ProjectName = request.ProjectName;
            project.CustomerId = request.CustomerId;
            project.DesignerId = request.DesignerId;
            project.DesignerName = request.DesignerName ?? string.Empty;
            project.ProjectType = request.ProjectType;
            project.Location = request.Location;
            project.Description = request.Description;
            project.StartDate = request.StartDate;
            project.TargetEndDate = request.TargetEndDate;
            project.Status = request.Status;
            project.Notes = request.Notes;
            project.IsActive = request.IsActive;

            await db.SaveChangesAsync();

            return Results.Ok(project);
        })
        .RequireAuthorization();


        // =============================================================
        // NOTE: Designer assignment and progress updates are handled
        // by WorkflowEndpoints at:
        //   POST /tenant/{companyId}/workflow/projects/{projectId}/assign-designer
        //   POST /tenant/{companyId}/workflow/projects/{projectId}/progress
        // =============================================================


        // DELETE PROJECT
        app.MapDelete("/tenant/{companyId:int}/projects/{id:int}", async (
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

            var project = await db.Projects
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == id &&
                    x.CompanyId == companyId);

            if (project is null)
            {
                return Results.NotFound(new
                {
                    message = "Project not found."
                });
            }

            db.Projects.Remove(project);

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Project deleted successfully."
            });
        })
        .RequireAuthorization();
    }
}