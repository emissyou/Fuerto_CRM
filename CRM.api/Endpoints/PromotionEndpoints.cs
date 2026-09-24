using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class PromotionEndpoints
{
    public static void MapPromotionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenant/{companyId:int}/promotions")
                       .RequireAuthorization();

        // ============================================================
        // LIST PROMOTIONS
        // GET /tenant/{companyId}/promotions
        // Query: ?activeOnly=true
        // ============================================================
        group.MapGet("", async (
            int companyId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory,
            bool? activeOnly = null) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var query = db.Promotions
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId);

            if (activeOnly == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(p => p.IsActive
                                      && (p.ValidFrom == null || p.ValidFrom <= now)
                                      && (p.ValidUntil == null || p.ValidUntil >= now));
            }

            var promotions = await query
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.PromotionId,
                    p.Name,
                    p.Code,
                    p.Description,
                    p.OfferType,
                    p.OfferValue,
                    p.TargetSegment,
                    p.ValidFrom,
                    p.ValidUntil,
                    p.IsActive,
                    p.MaxUses,
                    p.UsedCount,
                    p.Notes,
                    p.CreatedByName,
                    p.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(promotions);
        });

        // ============================================================
        // GET ONE PROMOTION
        // ============================================================
        group.MapGet("/{id:int}", async (
            int companyId,
            int id,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var promo = await db.Promotions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PromotionId == id
                                       && p.CompanyId == companyId);

            return promo is null
                ? Results.NotFound(new { message = "Promotion not found." })
                : Results.Ok(promo);
        });

        // ============================================================
        // CREATE PROMOTION
        // POST /tenant/{companyId}/promotions
        // ============================================================
        group.MapPost("", async (
            int companyId,
            HttpContext http,
            Promotion request,
            ITenantDbContextFactory tenantFactory,
            UserManager<ApplicationUser> userManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { message = "Promotion name is required." });

            var allowedTypes = new[] { "Percentage", "FixedAmount", "FreeService", "Custom" };
            if (!allowedTypes.Contains(request.OfferType))
                return Results.BadRequest(new { message = "Invalid offer type." });

            if ((request.OfferType == "Percentage" || request.OfferType == "FixedAmount")
                && (!request.OfferValue.HasValue || request.OfferValue <= 0))
            {
                return Results.BadRequest(new { message = "Offer value must be greater than zero." });
            }

            if (request.OfferType == "Percentage" && request.OfferValue > 100)
                return Results.BadRequest(new { message = "Percentage cannot exceed 100." });

            await using var db = await tenantFactory.CreateAsync(companyId);

            // Check for duplicate code
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var codeExists = await db.Promotions
                    .AnyAsync(p => p.CompanyId == companyId
                                && p.Code == request.Code.Trim());

                if (codeExists)
                    return Results.BadRequest(new
                    {
                        message = $"Promotion code '{request.Code}' already exists."
                    });
            }

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var user = await userManager.FindByIdAsync(userId);
            var userName = user?.FullName ?? user?.Email ?? "Staff";

            var promo = new Promotion
            {
                CompanyId = companyId,
                Name = request.Name.Trim(),
                Code = request.Code?.Trim() ?? "",
                Description = request.Description?.Trim() ?? "",
                OfferType = request.OfferType,
                OfferValue = request.OfferValue,
                TargetSegment = string.IsNullOrWhiteSpace(request.TargetSegment)
                    ? "Any" : request.TargetSegment.Trim(),
                ValidFrom = request.ValidFrom,
                ValidUntil = request.ValidUntil,
                IsActive = request.IsActive,
                MaxUses = request.MaxUses,
                UsedCount = 0,
                Notes = request.Notes?.Trim() ?? "",
                CreatedByUserId = userId,
                CreatedByName = userName,
                CreatedAt = DateTime.UtcNow
            };

            db.Promotions.Add(promo);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/tenant/{companyId}/promotions/{promo.PromotionId}",
                new
                {
                    message = "Promotion created successfully.",
                    promo.PromotionId,
                    promo.Name,
                    promo.Code,
                    promo.OfferType,
                    promo.OfferValue,
                    promo.TargetSegment,
                    promo.IsActive
                });
        });

        // ============================================================
        // UPDATE PROMOTION
        // PUT /tenant/{companyId}/promotions/{id}
        // ============================================================
        group.MapPut("/{id:int}", async (
            int companyId,
            int id,
            HttpContext http,
            Promotion request,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var promo = await db.Promotions
                .FirstOrDefaultAsync(p => p.PromotionId == id
                                       && p.CompanyId == companyId);

            if (promo is null)
                return Results.NotFound(new { message = "Promotion not found." });

            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { message = "Promotion name is required." });

            var allowedTypes = new[] { "Percentage", "FixedAmount", "FreeService", "Custom" };
            if (!allowedTypes.Contains(request.OfferType))
                return Results.BadRequest(new { message = "Invalid offer type." });

            if (request.OfferType == "Percentage" && request.OfferValue > 100)
                return Results.BadRequest(new { message = "Percentage cannot exceed 100." });

            // Check code uniqueness (excluding this one)
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var codeExists = await db.Promotions
                    .AnyAsync(p => p.CompanyId == companyId
                                && p.Code == request.Code.Trim()
                                && p.PromotionId != id);

                if (codeExists)
                    return Results.BadRequest(new
                    {
                        message = $"Promotion code '{request.Code}' already exists."
                    });
            }

            promo.Name = request.Name.Trim();
            promo.Code = request.Code?.Trim() ?? "";
            promo.Description = request.Description?.Trim() ?? "";
            promo.OfferType = request.OfferType;
            promo.OfferValue = request.OfferValue;
            promo.TargetSegment = string.IsNullOrWhiteSpace(request.TargetSegment)
                ? "Any" : request.TargetSegment.Trim();
            promo.ValidFrom = request.ValidFrom;
            promo.ValidUntil = request.ValidUntil;
            promo.IsActive = request.IsActive;
            promo.MaxUses = request.MaxUses;
            promo.Notes = request.Notes?.Trim() ?? "";

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Promotion updated successfully.",
                promo.PromotionId,
                promo.Name,
                promo.Code,
                promo.OfferType,
                promo.OfferValue,
                promo.TargetSegment,
                promo.IsActive
            });
        });

        // ============================================================
        // TOGGLE ACTIVE STATUS
        // POST /tenant/{companyId}/promotions/{id}/toggle
        // ============================================================
        group.MapPost("/{id:int}/toggle", async (
            int companyId,
            int id,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var promo = await db.Promotions
                .FirstOrDefaultAsync(p => p.PromotionId == id
                                       && p.CompanyId == companyId);

            if (promo is null)
                return Results.NotFound(new { message = "Promotion not found." });

            promo.IsActive = !promo.IsActive;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = promo.IsActive ? "Promotion activated." : "Promotion deactivated.",
                promo.PromotionId,
                promo.IsActive
            });
        });

        // ============================================================
        // DELETE PROMOTION
        // DELETE /tenant/{companyId}/promotions/{id}
        // ============================================================
        group.MapDelete("/{id:int}", async (
            int companyId,
            int id,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var promo = await db.Promotions
                .FirstOrDefaultAsync(p => p.PromotionId == id
                                       && p.CompanyId == companyId);

            if (promo is null)
                return Results.NotFound(new { message = "Promotion not found." });

            // Check if it's been used
            if (promo.UsedCount > 0)
                return Results.BadRequest(new
                {
                    message = $"Cannot delete: promotion has been used {promo.UsedCount} time(s). Deactivate it instead."
                });

            db.Promotions.Remove(promo);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Promotion deleted successfully." });
        });

        // ============================================================
        // MARK USED (called from retention action logging)
        // POST /tenant/{companyId}/promotions/{id}/use
        // ============================================================
        group.MapPost("/{id:int}/use", async (
            int companyId,
            int id,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var promo = await db.Promotions
                .FirstOrDefaultAsync(p => p.PromotionId == id
                                       && p.CompanyId == companyId);

            if (promo is null)
                return Results.NotFound(new { message = "Promotion not found." });

            if (!promo.IsActive)
                return Results.BadRequest(new { message = "Promotion is inactive." });

            if (promo.MaxUses.HasValue && promo.UsedCount >= promo.MaxUses.Value)
                return Results.BadRequest(new
                {
                    message = $"Promotion has reached its usage limit ({promo.MaxUses})."
                });

            var now = DateTime.UtcNow;
            if (promo.ValidFrom.HasValue && promo.ValidFrom > now)
                return Results.BadRequest(new { message = "Promotion has not started yet." });

            if (promo.ValidUntil.HasValue && promo.ValidUntil < now)
                return Results.BadRequest(new { message = "Promotion has expired." });

            promo.UsedCount++;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Promotion usage recorded.",
                promo.PromotionId,
                promo.UsedCount,
                promo.MaxUses
            });
        });
    }
}