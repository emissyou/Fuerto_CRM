using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        // CREATE
        app.MapPost("/tenant/{companyId:int}/inventory", async (
            int companyId,
            HttpContext httpContext,
            Inventory inventory,
            ITenantDbContextFactory tenantFactory) =>
        {
            return await TenantEndpointHelper.WithTenantDb(
                companyId,
                httpContext,
                tenantFactory,
                async db =>
                {
                    var productExists = await db.Products
                        .AnyAsync(x => x.ProductId == inventory.ProductId);

                    if (!productExists)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Product does not exist."
                        });
                    }

                    inventory.LastUpdatedAt = DateTime.UtcNow;

                    db.Inventories.Add(inventory);

                    await db.SaveChangesAsync();

                    return Results.Created(
                        $"/tenant/{companyId}/inventory/{inventory.InventoryId}",
                        inventory);
                });
        }).RequireAuthorization();

        // GET ALL
        app.MapGet("/tenant/{companyId:int}/inventory", async (
            int companyId,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            return await TenantEndpointHelper.WithTenantDb(
                companyId,
                httpContext,
                tenantFactory,
                async db =>
                {
                    var inventory = await db.Inventories
                        .AsNoTracking()
                        .Include(x => x.Product)
                        .OrderBy(x => x.InventoryId)
                        .ToListAsync();

                    return Results.Ok(inventory);
                });
        }).RequireAuthorization();

        // GET ONE
        app.MapGet("/tenant/{companyId:int}/inventory/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            return await TenantEndpointHelper.WithTenantDb(
                companyId,
                httpContext,
                tenantFactory,
                async db =>
                {
                    var inventory = await db.Inventories
                        .AsNoTracking()
                        .Include(x => x.Product)
                        .FirstOrDefaultAsync(x =>
                            x.InventoryId == id);

                    return inventory is null
                        ? Results.NotFound(new
                        {
                            message = "Inventory record not found."
                        })
                        : Results.Ok(inventory);
                });
        }).RequireAuthorization();

        // UPDATE
        app.MapPut("/tenant/{companyId:int}/inventory/{id:int}", async (
            int companyId,
            int id,
            Inventory request,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            return await TenantEndpointHelper.WithTenantDb(
                companyId,
                httpContext,
                tenantFactory,
                async db =>
                {
                    var inventory = await db.Inventories
                        .FirstOrDefaultAsync(x =>
                            x.InventoryId == id);

                    if (inventory is null)
                    {
                        return Results.NotFound(new
                        {
                            message = "Inventory record not found."
                        });
                    }

                    var productExists = await db.Products
                        .AnyAsync(x => x.ProductId == request.ProductId);

                    if (!productExists)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Product does not exist."
                        });
                    }

                    inventory.ProductId = request.ProductId;
                    inventory.QuantityOnHand = request.QuantityOnHand;
                    inventory.ReorderLevel = request.ReorderLevel;
                    inventory.LastUpdatedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync();

                    return Results.Ok(inventory);
                });
        }).RequireAuthorization();

        // DELETE
        app.MapDelete("/tenant/{companyId:int}/inventory/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            return await TenantEndpointHelper.WithTenantDb(
                companyId,
                httpContext,
                tenantFactory,
                async db =>
                {
                    var inventory = await db.Inventories
                        .FirstOrDefaultAsync(x =>
                            x.InventoryId == id);

                    if (inventory is null)
                    {
                        return Results.NotFound(new
                        {
                            message = "Inventory record not found."
                        });
                    }

                    db.Inventories.Remove(inventory);

                    await db.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        message = "Inventory deleted successfully."
                    });
                });
        }).RequireAuthorization();
    }
}