using CRM.api.Security;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class BranchEndpoints
{
    public static void MapBranchEndpoints(this WebApplication app)
    {
        // GET ALL BRANCHES
        app.MapGet("/tenant/{companyId:int}/branches", async (
            int companyId,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db = await tenantFactory.CreateAsync(companyId);
            await EnsureBranchesTableExistsAsync(db);

            var branches = await db.Branches
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .OrderByDescending(x => x.BranchId)
                .ToListAsync();

            return Results.Ok(branches);
        })
        .RequireAuthorization();

        // GET BRANCH BY ID
        app.MapGet("/tenant/{companyId:int}/branches/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db = await tenantFactory.CreateAsync(companyId);
            await EnsureBranchesTableExistsAsync(db);

            var branch = await db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == id && x.CompanyId == companyId);

            return branch is null
                ? Results.NotFound(new { message = $"Branch with ID {id} not found." })
                : Results.Ok(branch);
        })
        .RequireAuthorization();

        // CREATE BRANCH
        app.MapPost("/tenant/{companyId:int}/branches", async (
            int companyId,
            Branch branch,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(branch.BranchCode) || string.IsNullOrWhiteSpace(branch.BranchName))
            {
                return Results.BadRequest(new { message = "Branch code and branch name are required." });
            }

            await using var db = await tenantFactory.CreateAsync(companyId);
            await EnsureBranchesTableExistsAsync(db);

            // If this is set as main branch, unset existing main branch
            if (branch.IsMainBranch)
            {
                var existingMains = await db.Branches
                    .Where(x => x.CompanyId == companyId && x.IsMainBranch)
                    .ToListAsync();
                foreach (var em in existingMains)
                {
                    em.IsMainBranch = false;
                }
            }

            branch.CompanyId = companyId;
            branch.CreatedAt = DateTime.UtcNow;

            db.Branches.Add(branch);
            await db.SaveChangesAsync();

            return Results.Created($"/tenant/{companyId}/branches/{branch.BranchId}", branch);
        })
        .RequireAuthorization();

        // UPDATE BRANCH
        app.MapPut("/tenant/{companyId:int}/branches/{id:int}", async (
            int companyId,
            int id,
            Branch updated,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db = await tenantFactory.CreateAsync(companyId);
            await EnsureBranchesTableExistsAsync(db);

            var branch = await db.Branches
                .FirstOrDefaultAsync(x => x.BranchId == id && x.CompanyId == companyId);

            if (branch is null)
            {
                return Results.NotFound(new { message = $"Branch with ID {id} not found." });
            }

            if (updated.IsMainBranch && !branch.IsMainBranch)
            {
                var existingMains = await db.Branches
                    .Where(x => x.CompanyId == companyId && x.IsMainBranch && x.BranchId != id)
                    .ToListAsync();
                foreach (var em in existingMains)
                {
                    em.IsMainBranch = false;
                }
            }

            branch.BranchCode = updated.BranchCode;
            branch.BranchName = updated.BranchName;
            branch.Address = updated.Address;
            branch.ContactNumber = updated.ContactNumber;
            branch.Email = updated.Email;
            branch.IsMainBranch = updated.IsMainBranch;
            branch.IsActive = updated.IsActive;

            await db.SaveChangesAsync();

            return Results.Ok(branch);
        })
        .RequireAuthorization();

        // DELETE BRANCH
        app.MapDelete("/tenant/{companyId:int}/branches/{id:int}", async (
            int companyId,
            int id,
            HttpContext httpContext,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
            {
                return Results.Forbid();
            }

            await using var db = await tenantFactory.CreateAsync(companyId);
            await EnsureBranchesTableExistsAsync(db);

            var branch = await db.Branches
                .FirstOrDefaultAsync(x => x.BranchId == id && x.CompanyId == companyId);

            if (branch is null)
            {
                return Results.NotFound(new { message = $"Branch with ID {id} not found." });
            }

            db.Branches.Remove(branch);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Branch deleted successfully." });
        })
        .RequireAuthorization();
    }

    public static async Task EnsureBranchesTableExistsAsync(CRM.infrastructure.Data.TenantErpDbContext db)
    {
        try
        {
            const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Branches')
BEGIN
    CREATE TABLE [Branches] (
        [BranchId] int NOT NULL IDENTITY(1,1),
        [CompanyId] int NOT NULL,
        [BranchCode] nvarchar(50) NOT NULL,
        [BranchName] nvarchar(200) NOT NULL,
        [Address] nvarchar(500) NULL,
        [ContactNumber] nvarchar(50) NULL,
        [Email] nvarchar(100) NULL,
        [IsMainBranch] bit NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Branches] PRIMARY KEY ([BranchId])
    );
END";
            await db.Database.ExecuteSqlRawAsync(sql);
        }
        catch { }
    }
}
