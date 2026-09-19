using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class QuotationItemEndpoints
{
    public static void MapQuotationItemEndpoints(this WebApplication app)
    {
        // ADD QUOTATION ITEM
        app.MapPost(
            "/tenant/{companyId:int}/quotations/{quotationId:int}/items",
            async (
                int companyId,
                int quotationId,
                HttpContext httpContext,
                QuotationItem item,
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
                        x.QuotationId == quotationId &&
                        x.CompanyId == companyId);

                if (quotation is null)
                {
                    return Results.NotFound(new
                    {
                        message = "Quotation not found."
                    });
                }

                if (item.Quantity <= 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "Quantity must be greater than zero."
                    });
                }

                if (item.UnitPrice < 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "Unit price cannot be negative."
                    });
                }

                // Never trust QuotationId from the request body
                item.QuotationId = quotationId;

                item.Amount = item.Quantity * item.UnitPrice;

                db.QuotationItems.Add(item);

                await db.SaveChangesAsync();

                // Recalculate quotation totals
                var items = await db.QuotationItems
                    .Where(x => x.QuotationId == quotationId)
                    .ToListAsync();

                quotation.Subtotal = items.Sum(x => x.Amount);

                if (quotation.Discount > quotation.Subtotal)
                    quotation.Discount = quotation.Subtotal;

                quotation.TotalAmount =
                    quotation.Subtotal - quotation.Discount;

                await db.SaveChangesAsync();

                return Results.Created(
                    $"/tenant/{companyId}/quotations/{quotationId}/items/{item.QuotationItemId}",
                    item);
            })
            .RequireAuthorization();


        // GET QUOTATION ITEMS
        app.MapGet(
            "/tenant/{companyId:int}/quotations/{quotationId:int}/items",
            async (
                int companyId,
                int quotationId,
                HttpContext httpContext,
                ITenantDbContextFactory tenantFactory) =>
            {
                if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
                {
                    return Results.Forbid();
                }

                await using var db =
                    await tenantFactory.CreateAsync(companyId);

                var quotationExists = await db.Quotations
                    .AnyAsync(x =>
                        x.QuotationId == quotationId &&
                        x.CompanyId == companyId);

                if (!quotationExists)
                {
                    return Results.NotFound(new
                    {
                        message = "Quotation not found."
                    });
                }

                var items = await db.QuotationItems
                    .AsNoTracking()
                    .Where(x => x.QuotationId == quotationId)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.QuotationItemId)
                    .ToListAsync();

                return Results.Ok(items);
            })
            .RequireAuthorization();


        // UPDATE QUOTATION ITEM
        app.MapPut(
            "/tenant/{companyId:int}/quotations/{quotationId:int}/items/{itemId:int}",
            async (
                int companyId,
                int quotationId,
                int itemId,
                HttpContext httpContext,
                QuotationItem request,
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
                        x.QuotationId == quotationId &&
                        x.CompanyId == companyId);

                if (quotation is null)
                {
                    return Results.NotFound(new
                    {
                        message = "Quotation not found."
                    });
                }

                var item = await db.QuotationItems
                    .FirstOrDefaultAsync(x =>
                        x.QuotationItemId == itemId &&
                        x.QuotationId == quotationId);

                if (item is null)
                {
                    return Results.NotFound(new
                    {
                        message = "Quotation item not found."
                    });
                }

                if (request.Quantity <= 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "Quantity must be greater than zero."
                    });
                }

                if (request.UnitPrice < 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "Unit price cannot be negative."
                    });
                }

                item.Description = request.Description;
                item.Quantity = request.Quantity;
                item.Unit = request.Unit;
                item.UnitPrice = request.UnitPrice;
                item.SortOrder = request.SortOrder;

                item.Amount =
                    item.Quantity * item.UnitPrice;

                var items = await db.QuotationItems
                    .Where(x => x.QuotationId == quotationId)
                    .ToListAsync();

                quotation.Subtotal = items.Sum(x => x.Amount);

                if (quotation.Discount > quotation.Subtotal)
                    quotation.Discount = quotation.Subtotal;

                quotation.TotalAmount =
                    quotation.Subtotal - quotation.Discount;

                await db.SaveChangesAsync();

                return Results.Ok(item);
            })
            .RequireAuthorization();


        // DELETE QUOTATION ITEM
        app.MapDelete(
            "/tenant/{companyId:int}/quotations/{quotationId:int}/items/{itemId:int}",
            async (
                int companyId,
                int quotationId,
                int itemId,
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
                        x.QuotationId == quotationId &&
                        x.CompanyId == companyId);

                if (quotation is null)
                {
                    return Results.NotFound(new
                    {
                        message = "Quotation not found."
                    });
                }

                var item = await db.QuotationItems
                    .FirstOrDefaultAsync(x =>
                        x.QuotationItemId == itemId &&
                        x.QuotationId == quotationId);

                if (item is null)
                {
                    return Results.NotFound(new
                    {
                        message = "Quotation item not found."
                    });
                }

                db.QuotationItems.Remove(item);

                await db.SaveChangesAsync();

                // Recalculate quotation totals
                var items = await db.QuotationItems
                    .Where(x => x.QuotationId == quotationId)
                    .ToListAsync();

                quotation.Subtotal = items.Sum(x => x.Amount);

                if (quotation.Discount > quotation.Subtotal)
                    quotation.Discount = quotation.Subtotal;

                quotation.TotalAmount =
                    quotation.Subtotal - quotation.Discount;

                await db.SaveChangesAsync();

                return Results.Ok(new
                {
                    message = "Quotation item deleted successfully."
                });
            })
            .RequireAuthorization();
    }
}