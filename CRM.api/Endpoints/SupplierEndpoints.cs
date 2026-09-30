using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class SupplierEndpoints
{
        public static void MapSupplierEndpoints(this WebApplication app)
        {
            // CREATE
            app.MapPost("/tenant/{companyId:int}/suppliers", async (
                int companyId,
                HttpContext httpContext,
                Supplier supplier,
                ITenantDbContextFactory tenantFactory) =>
            {
                return await TenantEndpointHelper.WithTenantDb(
                    companyId,
                    httpContext,
                    tenantFactory,
                    async db =>
                    {
                        // Always use route company id
                        supplier.CompanyId = companyId;

                        db.Suppliers.Add(supplier);

                        await db.SaveChangesAsync();

                        return Results.Created(
                            $"/tenant/{companyId}/suppliers/{supplier.SupplierId}",
                            supplier);
                    });
            })
            .RequireAuthorization();


            // GET ALL
            app.MapGet("/tenant/{companyId:int}/suppliers", async (
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
                        var suppliers = await db.Suppliers
                            .AsNoTracking()
                            .Where(x => x.CompanyId == companyId)
                            .OrderByDescending(x => x.SupplierId)
                            .ToListAsync();

                        return Results.Ok(suppliers);
                    });
            })
            .RequireAuthorization();


            // GET BY ID
            app.MapGet("/tenant/{companyId:int}/suppliers/{id:int}", async (
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
                        var supplier = await db.Suppliers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.SupplierId == id &&
                                x.CompanyId == companyId);

                        return supplier is null
                            ? Results.NotFound(new { message = "Supplier not found." })
                            : Results.Ok(supplier);
                    });
            })
            .RequireAuthorization();


            // UPDATE
            app.MapPut("/tenant/{companyId:int}/suppliers/{id:int}", async (
                int companyId,
                int id,
                HttpContext httpContext,
                Supplier request,
                ITenantDbContextFactory tenantFactory) =>
            {
                return await TenantEndpointHelper.WithTenantDb(
                    companyId,
                    httpContext,
                    tenantFactory,
                    async db =>
                    {
                        var supplier = await db.Suppliers
                            .FirstOrDefaultAsync(x =>
                                x.SupplierId == id &&
                                x.CompanyId == companyId);

                        if (supplier is null)
                        {
                            return Results.NotFound(new { message = "Supplier not found." });
                        }

                        supplier.SupplierName = request.SupplierName;
                        supplier.ContactPerson = request.ContactPerson;
                        supplier.EmailAddress = request.EmailAddress;
                        supplier.ContactNumber = request.ContactNumber;
                        supplier.Address = request.Address;
                        supplier.IsActive = request.IsActive;

                        await db.SaveChangesAsync();

                        return Results.Ok(supplier);
                    });
            })
            .RequireAuthorization();


            // DELETE
            app.MapDelete("/tenant/{companyId:int}/suppliers/{id:int}", async (
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
                        var supplier = await db.Suppliers
                            .FirstOrDefaultAsync(x =>
                                x.SupplierId == id &&
                                x.CompanyId == companyId);

                        if (supplier is null)
                        {
                            return Results.NotFound(new { message = "Supplier not found." });
                        }

                        db.Suppliers.Remove(supplier);

                        await db.SaveChangesAsync();

                        return Results.Ok(new { message = "Supplier deleted successfully." });
                    });
            })
            .RequireAuthorization();
        }
}