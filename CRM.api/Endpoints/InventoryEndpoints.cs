using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public class InventoryRequestDto
{
    public int? ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderLevel { get; set; }
}

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        // CREATE
        app.MapPost("/tenant/{companyId:int}/inventory", async (
            int companyId,
            HttpContext httpContext,
            InventoryRequestDto req,
            ITenantDbContextFactory tenantFactory) =>
        {
            return await TenantEndpointHelper.WithTenantDb(
                companyId,
                httpContext,
                tenantFactory,
                async db =>
                {
                    Product? product = null;
                    if (req.ProductId.HasValue && req.ProductId.Value > 0)
                    {
                        product = await db.Products.FirstOrDefaultAsync(p => p.ProductId == req.ProductId.Value);
                    }

                    if (product == null && !string.IsNullOrWhiteSpace(req.ProductName))
                    {
                        product = await db.Products.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.ProductName == req.ProductName.Trim());
                        if (product == null)
                        {
                            product = new Product
                            {
                                CompanyId = companyId,
                                ProductCode = !string.IsNullOrWhiteSpace(req.ProductCode) ? req.ProductCode.Trim() : ("PRD-" + Guid.NewGuid().ToString("N")[..6].ToUpper()),
                                ProductName = req.ProductName.Trim(),
                                UnitPrice = req.UnitPrice ?? 0m
                            };
                            db.Products.Add(product);
                            await db.SaveChangesAsync();
                        }
                    }

                    if (product == null)
                    {
                        return Results.BadRequest(new { message = "Product does not exist or product name was not provided." });
                    }

                    var inventory = new Inventory
                    {
                        ProductId = product.ProductId,
                        QuantityOnHand = req.QuantityOnHand,
                        ReorderLevel = req.ReorderLevel,
                        LastUpdatedAt = DateTime.UtcNow
                    };

                    db.Inventories.Add(inventory);
                    await db.SaveChangesAsync();

                    return Results.Created($"/tenant/{companyId}/inventory/{inventory.InventoryId}", new
                    {
                        inventory.InventoryId,
                        inventory.ProductId,
                        ProductCode = product.ProductCode,
                        ProductName = product.ProductName,
                        UnitPrice = product.UnitPrice,
                        inventory.QuantityOnHand,
                        inventory.ReorderLevel,
                        inventory.LastUpdatedAt
                    });
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
                        .OrderByDescending(x => x.InventoryId)
                        .Select(x => new
                        {
                            x.InventoryId,
                            x.ProductId,
                            ProductCode = x.Product != null ? x.Product.ProductCode : "",
                            ProductName = x.Product != null ? x.Product.ProductName : "Unassigned",
                            UnitPrice = x.Product != null ? x.Product.UnitPrice : 0m,
                            x.QuantityOnHand,
                            x.ReorderLevel,
                            x.LastUpdatedAt
                        })
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
                        .FirstOrDefaultAsync(x => x.InventoryId == id);

                    if (inventory is null)
                    {
                        return Results.NotFound(new { message = "Inventory record not found." });
                    }

                    return Results.Ok(new
                    {
                        inventory.InventoryId,
                        inventory.ProductId,
                        ProductCode = inventory.Product != null ? inventory.Product.ProductCode : "",
                        ProductName = inventory.Product != null ? inventory.Product.ProductName : "Unassigned",
                        UnitPrice = inventory.Product != null ? inventory.Product.UnitPrice : 0m,
                        inventory.QuantityOnHand,
                        inventory.ReorderLevel,
                        inventory.LastUpdatedAt
                    });
                });
        }).RequireAuthorization();

        // UPDATE
        app.MapPut("/tenant/{companyId:int}/inventory/{id:int}", async (
            int companyId,
            int id,
            InventoryRequestDto req,
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
                        .Include(x => x.Product)
                        .FirstOrDefaultAsync(x => x.InventoryId == id);

                    if (inventory is null)
                    {
                        return Results.NotFound(new { message = "Inventory record not found." });
                    }

                    if (!string.IsNullOrWhiteSpace(req.ProductName) && inventory.Product != null)
                    {
                        inventory.Product.ProductName = req.ProductName.Trim();
                        if (req.UnitPrice.HasValue) inventory.Product.UnitPrice = req.UnitPrice.Value;
                    }

                    inventory.QuantityOnHand = req.QuantityOnHand;
                    inventory.ReorderLevel = req.ReorderLevel;
                    inventory.LastUpdatedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        inventory.InventoryId,
                        inventory.ProductId,
                        ProductCode = inventory.Product != null ? inventory.Product.ProductCode : "",
                        ProductName = inventory.Product != null ? inventory.Product.ProductName : "Unassigned",
                        UnitPrice = inventory.Product != null ? inventory.Product.UnitPrice : 0m,
                        inventory.QuantityOnHand,
                        inventory.ReorderLevel,
                        inventory.LastUpdatedAt
                    });
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
                        .FirstOrDefaultAsync(x => x.InventoryId == id);

                    if (inventory is null)
                    {
                        return Results.NotFound(new { message = "Inventory record not found." });
                    }

                    db.Inventories.Remove(inventory);
                    await db.SaveChangesAsync();

                    return Results.Ok(new { message = "Inventory deleted successfully." });
                });
        }).RequireAuthorization();
    }
}