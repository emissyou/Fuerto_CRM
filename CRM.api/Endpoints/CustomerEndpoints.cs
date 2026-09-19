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
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            var customers = await db.Customers
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
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
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db =
                await tenantFactory.CreateAsync(companyId);

            // Never trust CompanyId coming from the request body
            customer.CompanyId = companyId;

            db.Customers.Add(customer);

            await db.SaveChangesAsync();

            return Results.Ok(customer);
        })
        .RequireAuthorization();


        // UPDATE CUSTOMER
        app.MapPut("/tenant/{companyId:int}/customers/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            Customer request,
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

            customer.FirstName = request.FirstName;
            customer.LastName = request.LastName;
            customer.Email = request.Email;
            customer.Phone = request.Phone;
            customer.Address = request.Address;
            customer.CustomerType = request.CustomerType;
            customer.Notes = request.Notes;
            customer.IsActive = request.IsActive;

            await db.SaveChangesAsync();

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
    }
}