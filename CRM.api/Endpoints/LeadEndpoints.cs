using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class LeadEndpoints
{
    public static void MapLeadEndpoints(this WebApplication app)
    {
        // CREATE LEAD
        app.MapPost("/tenant/{companyId:int}/leads", async (
            int companyId,
            HttpContext httpContext,
            Lead lead,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            // Always use the company from the route
            lead.CompanyId = companyId;

            db.Leads.Add(lead);

            await db.SaveChangesAsync();

            return Results.Created(
                $"/tenant/{companyId}/leads/{lead.LeadId}",
                lead);
        })
        .RequireAuthorization();


        // GET ALL LEADS
        app.MapGet("/tenant/{companyId:int}/leads", async (
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

            var leads = await db.Leads
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Results.Ok(leads);
        })
        .RequireAuthorization();


        // GET LEAD BY ID
        app.MapGet("/tenant/{companyId:int}/leads/{id:int}", async (
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

            var lead = await db.Leads
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.LeadId == id &&
                    x.CompanyId == companyId);

            return lead is null
                ? Results.NotFound(new
                {
                    message = "Lead not found."
                })
                : Results.Ok(lead);
        })
        .RequireAuthorization();


        // UPDATE LEAD
        app.MapPut("/tenant/{companyId:int}/leads/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            Lead request,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var lead = await db.Leads
                .FirstOrDefaultAsync(x =>
                    x.LeadId == id &&
                    x.CompanyId == companyId);

            if (lead is null)
            {
                return Results.NotFound(new
                {
                    message = "Lead not found."
                });
            }

            lead.FirstName = request.FirstName;
            lead.LastName = request.LastName;
            lead.Email = request.Email;
            lead.Phone = request.Phone;
            lead.LeadSource = request.LeadSource;
            lead.Status = request.Status;
            lead.ServiceInterest = request.ServiceInterest;
            lead.Notes = request.Notes;
            lead.IsActive = request.IsActive;

            await db.SaveChangesAsync();

            return Results.Ok(lead);
        })
        .RequireAuthorization();


        // DELETE LEAD
        app.MapDelete("/tenant/{companyId:int}/leads/{id:int}", async (
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

            var lead = await db.Leads
                .FirstOrDefaultAsync(x =>
                    x.LeadId == id &&
                    x.CompanyId == companyId);

            if (lead is null)
            {
                return Results.NotFound(new
                {
                    message = "Lead not found."
                });
            }

            db.Leads.Remove(lead);

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Lead deleted successfully."
            });
        })
        .RequireAuthorization();
    }
}