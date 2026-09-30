using CRM.domain.Entities;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class SuperAdminEndpoints
{
    public record CreateCompanyRequest(
        string CompanyCode,
        string CompanyName,
        string PlanName = "Professional",
        decimal MonthlyFee = 2499m,
        string AvailedModules = "Main Transaction,Data Collection");

    public record UpdateCompanyRequest(
        string CompanyCode,
        string CompanyName,
        bool IsActive);

    public record UpdateSubscriptionRequest(
        string PlanName,
        string Status,
        decimal MonthlyFee,
        string AvailedModules,
        DateTime? EndDate,
        bool? IsActive = null);

    public record UpdateCompanyStatusRequest(
        string Status,
        bool? IsActive = null);

    public static void MapSuperAdminEndpoints(this WebApplication app)
    {
        // 1. GET ALL COMPANIES WITH SUBSCRIPTIONS (SUPER ADMIN)
        app.MapGet("/superadmin/companies", async (MasterErpDbContext db) =>
        {
            var companies = await db.Companies
                .OrderByDescending(c => c.CompanyId)
                .Select(c => new
                {
                    c.CompanyId,
                    c.CompanyCode,
                    c.CompanyName,
                    c.IsActive,
                    c.CreatedAt,
                    Subscription = db.CompanySubscriptions
                        .Where(s => s.CompanyId == c.CompanyId)
                        .Select(s => new
                        {
                            s.SubscriptionId,
                            s.PlanName,
                            s.Status,
                            s.MonthlyFee,
                            s.StartDate,
                            s.EndDate,
                            s.AvailedModules
                        })
                        .FirstOrDefault(),
                    Databases = db.CompanyDatabases
                        .Where(d => d.CompanyId == c.CompanyId && d.IsActive)
                        .Select(d => new
                        {
                            d.CompanyDatabaseId,
                            d.ServerName,
                            d.DatabaseName
                        })
                        .ToList()
                })
                .ToListAsync();

            return Results.Ok(companies);
        })
        .RequireAuthorization(policy => policy.RequireRole("Super Admin"));

        // 2. CREATE COMPANY WITH SUBSCRIPTION (SUPER ADMIN)
        app.MapPost("/superadmin/companies", async (
            CreateCompanyRequest req,
            MasterErpDbContext db,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (string.IsNullOrWhiteSpace(req.CompanyCode) || string.IsNullOrWhiteSpace(req.CompanyName))
            {
                return Results.BadRequest(new { message = "Company Code and Company Name are required." });
            }

            var cleanCode = req.CompanyCode.Trim().ToUpperInvariant();
            if (await db.Companies.AnyAsync(c => c.CompanyCode == cleanCode))
            {
                return Results.BadRequest(new { message = $"Company code '{cleanCode}' is already in use." });
            }

            var company = new Company
            {
                CompanyCode = cleanCode,
                CompanyName = req.CompanyName.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Companies.Add(company);
            await db.SaveChangesAsync();

            // Create CompanyDatabase mapping
            var dbName = $"CRM_{cleanCode}";
            var compDb = new CompanyDatabase
            {
                CompanyId = company.CompanyId,
                ServerName = "(localdb)\\MSSQLLocalDB",
                DatabaseName = dbName,
                CredentialKey = "TenantA",
                IsActive = true
            };
            db.CompanyDatabases.Add(compDb);

            // Create CompanySubscription
            var sub = new CompanySubscription
            {
                CompanyId = company.CompanyId,
                PlanName = string.IsNullOrWhiteSpace(req.PlanName) ? "Professional" : req.PlanName.Trim(),
                Status = "Active",
                MonthlyFee = req.MonthlyFee > 0 ? req.MonthlyFee : 2499m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1),
                AvailedModules = string.IsNullOrWhiteSpace(req.AvailedModules) ? "Main Transaction,Data Collection" : req.AvailedModules.Trim()
            };
            db.CompanySubscriptions.Add(sub);
            await db.SaveChangesAsync();

            // Initialize tenant DB schema
            try
            {
                await using var tenantDb = await tenantFactory.CreateAsync(company.CompanyId);
                await tenantDb.Database.EnsureCreatedAsync();
            }
            catch { }

            return Results.Created($"/companies/{company.CompanyId}", new
            {
                company.CompanyId,
                company.CompanyCode,
                company.CompanyName,
                sub.PlanName,
                sub.MonthlyFee,
                sub.AvailedModules
            });
        })
        .RequireAuthorization(policy => policy.RequireRole("Super Admin"));

        // 3. UPDATE COMPANY (SUPER ADMIN)
        app.MapPut("/superadmin/companies/{id:int}", async (
            int id,
            UpdateCompanyRequest req,
            MasterErpDbContext db) =>
        {
            var company = await db.Companies.FindAsync(id);
            if (company is null) return Results.NotFound(new { message = "Company not found." });

            company.CompanyCode = req.CompanyCode.Trim().ToUpperInvariant();
            company.CompanyName = req.CompanyName.Trim();
            company.IsActive = req.IsActive;

            await db.SaveChangesAsync();
            return Results.Ok(company);
        })
        .RequireAuthorization(policy => policy.RequireRole("Super Admin"));

        // 4. UPDATE COMPANY SUBSCRIPTION / AVAILED MODULES (SUPER ADMIN)
        app.MapPut("/superadmin/companies/{id:int}/subscription", async (
            int id,
            UpdateSubscriptionRequest req,
            MasterErpDbContext db) =>
        {
            var company = await db.Companies.FindAsync(id);
            if (company is null) return Results.NotFound(new { message = "Company not found." });

            var sub = await db.CompanySubscriptions.FirstOrDefaultAsync(s => s.CompanyId == id);
            if (sub is null)
            {
                sub = new CompanySubscription
                {
                    CompanyId = id,
                    PlanName = req.PlanName,
                    Status = req.Status,
                    MonthlyFee = req.MonthlyFee,
                    AvailedModules = req.AvailedModules,
                    StartDate = DateTime.UtcNow,
                    EndDate = req.EndDate ?? DateTime.UtcNow.AddYears(1)
                };
                db.CompanySubscriptions.Add(sub);
            }
            else
            {
                sub.PlanName = req.PlanName;
                sub.Status = req.Status;
                sub.MonthlyFee = req.MonthlyFee;
                sub.AvailedModules = req.AvailedModules;
                if (req.EndDate.HasValue) sub.EndDate = req.EndDate.Value;
            }

            // Sync company operational active status
            if (req.IsActive.HasValue)
            {
                company.IsActive = req.IsActive.Value;
            }
            else if (req.Status.Equals("Suspended", StringComparison.OrdinalIgnoreCase) ||
                     req.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                     req.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase) ||
                     req.Status.Equals("Expired", StringComparison.OrdinalIgnoreCase))
            {
                company.IsActive = false;
            }
            else if (req.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                     req.Status.Equals("Trial", StringComparison.OrdinalIgnoreCase))
            {
                company.IsActive = true;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                message = "Subscription and company status updated successfully.",
                sub.SubscriptionId,
                sub.CompanyId,
                sub.PlanName,
                sub.Status,
                sub.MonthlyFee,
                sub.AvailedModules,
                sub.EndDate,
                isCompanyActive = company.IsActive
            });
        })
        .RequireAuthorization(policy => policy.RequireRole("Super Admin"));

        // 4b. UPDATE COMPANY STATUS QUICK ENDPOINT (SUPER ADMIN)
        app.MapPut("/superadmin/companies/{id:int}/status", async (
            int id,
            UpdateCompanyStatusRequest req,
            MasterErpDbContext db) =>
        {
            var company = await db.Companies.FindAsync(id);
            if (company is null) return Results.NotFound(new { message = "Company not found." });

            bool newActive = req.IsActive ?? (!req.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase) &&
                                              !req.Status.Equals("Suspended", StringComparison.OrdinalIgnoreCase) &&
                                              !req.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
                                              !req.Status.Equals("Expired", StringComparison.OrdinalIgnoreCase));
            company.IsActive = newActive;

            var sub = await db.CompanySubscriptions.FirstOrDefaultAsync(s => s.CompanyId == id);
            if (sub != null)
            {
                sub.Status = req.Status;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                message = $"Company status updated to '{req.Status}' (Active: {company.IsActive}).",
                companyId = company.CompanyId,
                companyCode = company.CompanyCode,
                companyName = company.CompanyName,
                isActive = company.IsActive,
                subscriptionStatus = sub?.Status ?? req.Status
            });
        })
        .RequireAuthorization(policy => policy.RequireRole("Super Admin"));

        // 5. PLATFORM BUSINESS INTELLIGENCE (SUPER ADMIN BI)
        app.MapGet("/superadmin/bi", async (
            MasterErpDbContext db,
            UserManager<ApplicationUser> userManager) =>
        {
            var companies = await db.Companies.AsNoTracking().ToListAsync();
            var subscriptions = await db.CompanySubscriptions.AsNoTracking().ToListAsync();
            var allUsers = await userManager.Users.AsNoTracking().ToListAsync();

            int totalCompanies = companies.Count;
            int activeCompanies = companies.Count(c => c.IsActive);
            int inactiveCompanies = totalCompanies - activeCompanies;

            decimal mrr = subscriptions.Where(s => s.Status == "Active").Sum(s => s.MonthlyFee);
            decimal arr = mrr * 12;

            // Plan distribution
            var planDist = subscriptions
                .GroupBy(s => s.PlanName)
                .Select(g => new { Plan = g.Key, Count = g.Count() })
                .ToList();

            // Module adoption counts
            int mainTxCount = 0;
            int dataColCount = 0;
            int biCount = 0;
            int actionCount = 0;

            foreach (var sub in subscriptions)
            {
                var mods = sub.GetModuleList();
                if (mods.Contains("Main Transaction", StringComparer.OrdinalIgnoreCase)) mainTxCount++;
                if (mods.Contains("Data Collection", StringComparer.OrdinalIgnoreCase)) dataColCount++;
                if (mods.Contains("Business Intelligence", StringComparer.OrdinalIgnoreCase)) biCount++;
                if (mods.Contains("Action", StringComparer.OrdinalIgnoreCase)) actionCount++;
            }

            // Tenant cards
            var tenantStats = companies.Select(c =>
            {
                var sub = subscriptions.FirstOrDefault(s => s.CompanyId == c.CompanyId);
                int userCount = allUsers.Count(u => u.CompanyId == c.CompanyId);
                return new
                {
                    c.CompanyId,
                    c.CompanyCode,
                    c.CompanyName,
                    c.IsActive,
                    Plan = sub?.PlanName ?? "Unassigned",
                    Status = sub?.Status ?? "None",
                    MonthlyFee = sub?.MonthlyFee ?? 0m,
                    AvailedModules = sub?.AvailedModules ?? "None",
                    UserCount = userCount,
                    c.CreatedAt
                };
            }).ToList();

            return Results.Ok(new
            {
                TotalCompanies = totalCompanies,
                ActiveCompanies = activeCompanies,
                InactiveCompanies = inactiveCompanies,
                MRR = mrr,
                ARR = arr,
                TotalPlatformUsers = allUsers.Count,
                PlanDistribution = planDist,
                ModuleAdoption = new
                {
                    MainTransaction = mainTxCount,
                    DataCollection = dataColCount,
                    BusinessIntelligence = biCount,
                    Action = actionCount
                },
                Tenants = tenantStats
            });
        })
        .RequireAuthorization(policy => policy.RequireRole("Super Admin"));

        // 6. GET ALL USERS ACROSS ALL TENANTS (SUPER ADMIN)
        app.MapGet("/superadmin/users", async (
            MasterErpDbContext db,
            UserManager<ApplicationUser> userManager) =>
        {
            var companies = await db.Companies.AsNoTracking().ToDictionaryAsync(c => c.CompanyId, c => c);
            var users = await userManager.Users.AsNoTracking().ToListAsync();

            var result = new List<object>();
            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                if (roles.Contains("Super Admin") || roles.Contains("SuperAdmin"))
                    continue; // Exclude super admin from tenant user list

                string role = roles.FirstOrDefault() ?? "Staff";
                string companyName = "Unassigned";
                string companyCode = "—";

                if (u.CompanyId.HasValue && companies.TryGetValue(u.CompanyId.Value, out var comp))
                {
                    companyName = comp.CompanyName;
                    companyCode = comp.CompanyCode;
                }

                result.Add(new
                {
                    userId = u.Id,
                    email = u.Email,
                    fullName = string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName,
                    companyId = u.CompanyId,
                    companyName,
                    companyCode,
                    role,
                    roles = string.Join(", ", roles),
                    isActive = !u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTimeOffset.UtcNow
                });
            }

            var sorted = result
                .OrderBy(r => ((dynamic)r).companyName)
                .ThenBy(r => ((dynamic)r).role == "Admin" ? 0 : 1)
                .ThenBy(r => ((dynamic)r).fullName)
                .ToList();

            return Results.Ok(sorted);
        })
        .RequireAuthorization(policy => policy.RequireRole("Super Admin"));
    }
}
