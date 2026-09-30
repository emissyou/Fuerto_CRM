using CRM.api.Models;
using CRM.api.Security;
using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class QuotationEndpoints
{
    public static void MapQuotationEndpoints(this WebApplication app)
    {
        // ============================================================
        // CREATE QUOTATION
        //  - QuotationNumber: AUTO-GENERATED (QT-202609-0001)
        //  - CustomerId:      AUTO-FILLED from the Project
        // ============================================================
        app.MapPost("/tenant/{companyId:int}/quotations", async (
            int companyId,
            HttpContext httpContext,
            Quotation quotation,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            // ---- Look up the project (also gives us the CustomerId) ----
            var project = await db.Projects
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == quotation.ProjectId &&
                    x.CompanyId == companyId);

            if (project is null)
            {
                return Results.BadRequest(new
                {
                    message = "Project does not exist in this company."
                });
            }

            // ---- Auto-fill identifiers ----
            quotation.CompanyId = companyId;
            quotation.CustomerId = project.CustomerId;   // inherit from project
            quotation.BranchId = quotation.BranchId ?? project.BranchId;
            if (!quotation.BranchId.HasValue)
            {
                var branchClaim = httpContext.User.FindFirst("BranchId")?.Value;
                if (int.TryParse(branchClaim, out var bClaimVal))
                    quotation.BranchId = bClaimVal;
            }
            quotation.CreatedAt = DateTime.UtcNow;
            quotation.QuotationDate = DateTime.UtcNow;

            // ---- Auto-generate quotation number ----
            quotation.QuotationNumber =
                await GenerateQuotationNumberAsync(db, companyId);

            // ---- Compute total ----
            quotation.TotalAmount =
                quotation.Subtotal - quotation.Discount;

            if (quotation.TotalAmount < 0)
            {
                return Results.BadRequest(new
                {
                    message = "Discount cannot be greater than subtotal."
                });
            }

            quotation.Status = string.IsNullOrWhiteSpace(quotation.Status) ? QuotationStatus.Draft : quotation.Status;
            quotation.PaymentStatus = string.IsNullOrWhiteSpace(quotation.PaymentStatus) ? PaymentStatus.Pending : quotation.PaymentStatus;
            quotation.ApprovalStatus = string.IsNullOrWhiteSpace(quotation.ApprovalStatus) ? "Pending" : quotation.ApprovalStatus;
            quotation.Notes ??= string.Empty;
            quotation.PaymentMethod ??= string.Empty;
            quotation.PaymentReference ??= string.Empty;
            quotation.ApprovedByName ??= string.Empty;
            quotation.ApprovedByUserId ??= string.Empty;
            quotation.RejectionReason ??= string.Empty;

            db.Quotations.Add(quotation);

            await db.SaveChangesAsync();

            return Results.Created(
                $"/tenant/{companyId}/quotations/{quotation.QuotationId}",
                quotation);
        })
        .RequireAuthorization();


        // GET ALL QUOTATIONS
        app.MapGet("/tenant/{companyId:int}/quotations", async (
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

            var query = db.Quotations
                .AsNoTracking()
                .Include(x => x.Project)
                .Where(x => x.CompanyId == companyId);

            if (effectiveBranchId.HasValue)
            {
                query = query.Where(x => x.BranchId == effectiveBranchId.Value);
            }

            var quotations = await query
                .OrderByDescending(x => x.QuotationId)
                .ToListAsync();

            return Results.Ok(quotations);
        })
        .RequireAuthorization();


        // GET ONE QUOTATION
        app.MapGet("/tenant/{companyId:int}/quotations/{id:int}", async (
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

            var quotation = await db.Quotations
                .AsNoTracking()
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x =>
                    x.QuotationId == id &&
                    x.CompanyId == companyId);

            if (quotation is null)
            {
                return Results.NotFound(new
                {
                    message = "Quotation not found."
                });
            }

            return Results.Ok(quotation);
        })
        .RequireAuthorization();


        // UPDATE QUOTATION
        app.MapPut("/tenant/{companyId:int}/quotations/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            Quotation request,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var quotation = await db.Quotations
                .FirstOrDefaultAsync(x =>
                    x.QuotationId == id &&
                    x.CompanyId == companyId);

            if (quotation is null)
            {
                return Results.NotFound(new
                {
                    message = "Quotation not found."
                });
            }

            var project = await db.Projects
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == request.ProjectId &&
                    x.CompanyId == companyId);

            if (project is null)
            {
                return Results.BadRequest(new
                {
                    message = "Project does not exist in this company."
                });
            }

            if (request.Discount > request.Subtotal)
            {
                return Results.BadRequest(new
                {
                    message = "Discount cannot be greater than subtotal."
                });
            }

            // QuotationNumber stays immutable.
            quotation.ProjectId = request.ProjectId;
            quotation.CustomerId = project.CustomerId;   // keep in sync
            quotation.Subtotal = request.Subtotal;
            quotation.Discount = request.Discount;
            quotation.TotalAmount = request.Subtotal - request.Discount;
            quotation.Status = request.Status;
            quotation.Notes = request.Notes;
            quotation.ValidUntil = request.ValidUntil;

            await db.SaveChangesAsync();

            return Results.Ok(quotation);
        })
        .RequireAuthorization();


        // DELETE QUOTATION
        app.MapDelete("/tenant/{companyId:int}/quotations/{id:int}", async (
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

            var quotation = await db.Quotations
                .FirstOrDefaultAsync(x =>
                    x.QuotationId == id &&
                    x.CompanyId == companyId);

            if (quotation is null)
            {
                return Results.NotFound(new
                {
                    message = "Quotation not found."
                });
            }

            db.Quotations.Remove(quotation);

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Quotation deleted successfully."
            });
        })
        .RequireAuthorization();
    }


    // ============================================================
    // AUTO-GENERATE QUOTATION NUMBER
    // Format: QT-{yyyyMM}-{seq:D4}
    // ============================================================
    private static async Task<string> GenerateQuotationNumberAsync(
        TenantErpDbContext db, int companyId)
    {
        var now = DateTime.UtcNow;
        var prefix = $"QT-{now:yyyyMM}-";

        var lastNumber = await db.Quotations
            .Where(q => q.CompanyId == companyId
                     && q.QuotationNumber.StartsWith(prefix))
            .OrderByDescending(q => q.QuotationNumber)
            .Select(q => q.QuotationNumber)
            .FirstOrDefaultAsync();

        int next = 1;
        if (!string.IsNullOrWhiteSpace(lastNumber))
        {
            var seqPart = lastNumber.Substring(prefix.Length);
            if (int.TryParse(seqPart, out var n))
                next = n + 1;
        }

        return $"{prefix}{next:D4}";
    }
}