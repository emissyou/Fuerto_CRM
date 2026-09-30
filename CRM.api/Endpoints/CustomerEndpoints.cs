using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        // GET ALL CUSTOMERS
        app.MapGet("/tenant/{companyId:int}/customers", async (
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

            var query = db.Customers
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId);

            if (effectiveBranchId.HasValue)
            {
                query = query.Where(x => x.BranchId == effectiveBranchId.Value);
            }

            var customers = await query
                .OrderByDescending(x => x.CustomerId)
                .ToListAsync();

            return Results.Ok(customers);
        })
        .RequireAuthorization();


        // GET CUSTOMER BY ID
        app.MapGet("/tenant/{companyId:int}/customers/{id:int}", async (
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

            var customer = await db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.CustomerId == id &&
                    x.CompanyId == companyId);

            return customer is null
                ? Results.NotFound(new
                {
                    message = "Customer not found."
                })
                : Results.Ok(customer);
        })
        .RequireAuthorization();


        // CREATE CUSTOMER
        app.MapPost("/tenant/{companyId:int}/customers", async (
            int companyId,
            HttpContext httpContext,
            Customer customer,
            ITenantDbContextFactory tenantFactory,
            ITenantDatabaseSyncService syncService) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            // Never trust CompanyId coming from the request body
            customer.CompanyId = companyId;
            customer.FirstName = customer.FirstName?.Trim() ?? string.Empty;
            customer.LastName = customer.LastName?.Trim() ?? string.Empty;
            customer.Email = customer.Email?.Trim() ?? string.Empty;
            customer.Phone = customer.Phone?.Trim() ?? string.Empty;
            customer.Address = customer.Address?.Trim() ?? string.Empty;
            customer.Notes = customer.Notes?.Trim() ?? string.Empty;
            customer.CustomerType = string.IsNullOrWhiteSpace(customer.CustomerType) ? "Regular" : customer.CustomerType.Trim();

            if (!customer.BranchId.HasValue)
            {
                var branchClaim = httpContext.User.FindFirst("BranchId")?.Value;
                if (int.TryParse(branchClaim, out var bClaimVal))
                    customer.BranchId = bClaimVal;
            }

            db.Customers.Add(customer);

            await db.SaveChangesAsync();

            // Real-time Dual-Storage Mirror (Save in both Cloud and Local at the same time)
            _ = Task.Run(async () =>
            {
                try { await syncService.MirrorCustomerAsync(customer); } catch { }
            });

            return Results.Ok(customer);
        })
        .RequireAuthorization();


        // UPDATE CUSTOMER
        app.MapPut("/tenant/{companyId:int}/customers/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            Customer request,
            ITenantDbContextFactory tenantFactory,
            ITenantDatabaseSyncService syncService) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var customer = await db.Customers
                .FirstOrDefaultAsync(x =>
                    x.CustomerId == id &&
                    x.CompanyId == companyId);

            if (customer is null)
            {
                return Results.NotFound(new
                {
                    message = "Customer not found."
                });
            }

            customer.FirstName = request.FirstName?.Trim() ?? string.Empty;
            customer.LastName = request.LastName?.Trim() ?? string.Empty;
            customer.Email = request.Email?.Trim() ?? string.Empty;
            customer.Phone = request.Phone?.Trim() ?? string.Empty;
            customer.Address = request.Address?.Trim() ?? string.Empty;
            customer.CustomerType = string.IsNullOrWhiteSpace(request.CustomerType) ? "Regular" : request.CustomerType.Trim();
            customer.Notes = request.Notes?.Trim() ?? string.Empty;
            customer.IsActive = request.IsActive;

            await db.SaveChangesAsync();

            // Real-time Dual-Storage Mirror (Update in both Cloud and Local at the same time)
            _ = Task.Run(async () =>
            {
                try { await syncService.MirrorCustomerAsync(customer); } catch { }
            });

            return Results.Ok(customer);
        })
        .RequireAuthorization();


        // DELETE CUSTOMER
        app.MapDelete("/tenant/{companyId:int}/customers/{id:int}", async (
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

            var customer = await db.Customers
                .FirstOrDefaultAsync(x =>
                    x.CustomerId == id &&
                    x.CompanyId == companyId);

            if (customer is null)
            {
                return Results.NotFound(new
                {
                    message = "Customer not found."
                });
            }

            db.Customers.Remove(customer);

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Customer deleted successfully."
            });
        })
        .RequireAuthorization();


        // UPDATE CUSTOMER STATUS / LOYALTY TIER
        app.MapPut("/tenant/{companyId:int}/customers/{id:int}/status", async (
            int companyId,
            int id,
            CustomerStatusUpdateDto dto,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                return Results.BadRequest(new { message = "Status cannot be empty." });
            }

            await using var db = await tenantFactory.CreateAsync(companyId);

            var customer = await db.Customers
                .FirstOrDefaultAsync(x => x.CustomerId == id && x.CompanyId == companyId);

            if (customer is null)
            {
                return Results.NotFound(new { message = "Customer not found." });
            }

            var oldStatus = customer.CustomerType;
            customer.CustomerType = dto.Status.Trim();

            db.Activities.Add(new Activity
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
                message = $"Customer status successfully changed to {customer.CustomerType}.",
                customerId = customer.CustomerId,
                status = customer.CustomerType
            });
        })
        .RequireAuthorization();
    }
}

public record CustomerStatusUpdateDto(string Status);