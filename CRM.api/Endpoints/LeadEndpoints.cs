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
            lead.FirstName = lead.FirstName?.Trim() ?? string.Empty;
            lead.LastName = lead.LastName?.Trim() ?? string.Empty;
            lead.Email = lead.Email?.Trim() ?? string.Empty;
            lead.Phone = lead.Phone?.Trim() ?? string.Empty;
            lead.LeadSource = string.IsNullOrWhiteSpace(lead.LeadSource) ? "Other" : lead.LeadSource.Trim();
            lead.Status = string.IsNullOrWhiteSpace(lead.Status) ? "New" : lead.Status.Trim();
            lead.ServiceInterest = lead.ServiceInterest?.Trim() ?? string.Empty;
            lead.Notes = lead.Notes?.Trim() ?? string.Empty;
            lead.CreatedAt = DateTime.UtcNow;

            if (!lead.BranchId.HasValue)
            {
                var branchClaim = httpContext.User.FindFirst("BranchId")?.Value;
                if (int.TryParse(branchClaim, out var bClaimVal))
                    lead.BranchId = bClaimVal;
            }

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
            int? branchId,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            int? effectiveBranchId = branchId;
            if (!effectiveBranchId.HasValue)
            {
                var branchClaim = httpContext.User.FindFirst("BranchId")?.Value;
                if (int.TryParse(branchClaim, out var bClaimVal))
                    effectiveBranchId = bClaimVal;
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var query = db.Leads
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId);

            if (effectiveBranchId.HasValue)
            {
                query = query.Where(x => x.BranchId == effectiveBranchId.Value);
            }

            var leads = await query
                .OrderByDescending(x => x.LeadId)
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

            lead.FirstName = request.FirstName?.Trim() ?? lead.FirstName ?? string.Empty;
            lead.LastName = request.LastName?.Trim() ?? lead.LastName ?? string.Empty;
            lead.Email = request.Email?.Trim() ?? lead.Email ?? string.Empty;
            lead.Phone = request.Phone?.Trim() ?? lead.Phone ?? string.Empty;
            lead.LeadSource = string.IsNullOrWhiteSpace(request.LeadSource) ? (lead.LeadSource ?? "Other") : request.LeadSource.Trim();
            lead.Status = string.IsNullOrWhiteSpace(request.Status) ? (lead.Status ?? "New") : request.Status.Trim();
            lead.ServiceInterest = request.ServiceInterest?.Trim() ?? lead.ServiceInterest ?? string.Empty;
            lead.Notes = request.Notes?.Trim() ?? lead.Notes ?? string.Empty;
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