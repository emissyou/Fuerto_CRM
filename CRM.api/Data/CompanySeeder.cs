using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Data;

public static class CompanySeeder
{
    public static async Task SeedCompaniesAndTenantsAsync(
        MasterErpDbContext masterDb,
        UserManager<ApplicationUser> userManager,
        ITenantDbContextFactory tenantFactory)
    {
        // -------------------------------------------------------------
        // 1. MASTER COMPANIES
        // -------------------------------------------------------------
        var fuerto = await masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyCode == "FUERTO");
        if (fuerto == null)
        {
            fuerto = new Company
            {
                CompanyCode = "FUERTO",
                CompanyName = "Fuerto Interior Design Services",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            masterDb.Companies.Add(fuerto);
            await masterDb.SaveChangesAsync();
        }

        var gli = await masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyCode == "GILBB" || c.CompanyCode == "LRSALON");
        if (gli == null)
        {
            gli = new Company
            {
                CompanyCode = "GILBB",
                CompanyName = "GLI Bahay Builds",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            masterDb.Companies.Add(gli);
            await masterDb.SaveChangesAsync();
        }
        else if (gli.CompanyCode != "GILBB" || gli.CompanyName != "GLI Bahay Builds")
        {
            gli.CompanyCode = "GILBB";
            gli.CompanyName = "GLI Bahay Builds";
            await masterDb.SaveChangesAsync();
        }

        var ccd = await masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyCode == "CCDAVAO" || c.CompanyCode == "MRDONUT");
        if (ccd == null)
        {
            ccd = new Company
            {
                CompanyCode = "CCDAVAO",
                CompanyName = "Custom Crafters Davao",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            masterDb.Companies.Add(ccd);
            await masterDb.SaveChangesAsync();
        }
        else if (ccd.CompanyCode != "CCDAVAO" || ccd.CompanyName != "Custom Crafters Davao")
        {
            ccd.CompanyCode = "CCDAVAO";
            ccd.CompanyName = "Custom Crafters Davao";
            await masterDb.SaveChangesAsync();
        }

        // -------------------------------------------------------------
        // 2. COMPANY DATABASES MAPPING
        // -------------------------------------------------------------
        async Task EnsureCompanyDb(int companyId, string defaultLocalDbName, string cloudServer, string cloudDb, string credentialKey)
        {
            var dbEntry = await masterDb.CompanyDatabases.FirstOrDefaultAsync(d => d.CompanyId == companyId);
            bool isCloudMaster = masterDb.Database.GetDbConnection().ConnectionString.Contains("databaseasp.net", StringComparison.OrdinalIgnoreCase);

            string targetServer = isCloudMaster ? cloudServer : "(localdb)\\MSSQLLocalDB";
            string targetDb = isCloudMaster ? cloudDb : defaultLocalDbName;
            string targetKey = isCloudMaster ? credentialKey : "TenantA";

            if (dbEntry == null)
            {
                masterDb.CompanyDatabases.Add(new CompanyDatabase
                {
                    CompanyId = companyId,
                    ServerName = targetServer,
                    DatabaseName = targetDb,
                    CredentialKey = targetKey,
                    IsActive = true
                });
                await masterDb.SaveChangesAsync();
            }
            else if (dbEntry.ServerName != targetServer || dbEntry.DatabaseName != targetDb || dbEntry.CredentialKey != targetKey)
            {
                dbEntry.ServerName = targetServer;
                dbEntry.DatabaseName = targetDb;
                dbEntry.CredentialKey = targetKey;
                await masterDb.SaveChangesAsync();
            }
        }

        await EnsureCompanyDb(fuerto.CompanyId, "CRM_Fuerto", "db67080.public.databaseasp.net", "db67080", "Fuerto");
        await EnsureCompanyDb(gli.CompanyId, "CRM_GILBB", "db70838.public.databaseasp.net", "db70838", "GLIBahayBuilds");
        await EnsureCompanyDb(ccd.CompanyId, "CRM_CCDavao", "db70839.public.databaseasp.net", "db70839", "CustomCraftersDavao");

        // -------------------------------------------------------------
        // 3. COMPANY SUBSCRIPTIONS (Module Entitlements)
        // -------------------------------------------------------------
        async Task EnsureSubscription(int companyId, string plan, decimal fee, string modules)
        {
            var sub = await masterDb.CompanySubscriptions.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (sub == null)
            {
                masterDb.CompanySubscriptions.Add(new CompanySubscription
                {
                    CompanyId = companyId,
                    PlanName = plan,
                    Status = "Active",
                    MonthlyFee = fee,
                    StartDate = DateTime.UtcNow.AddMonths(-3),
                    EndDate = DateTime.UtcNow.AddMonths(9),
                    AvailedModules = modules
                });
                await masterDb.SaveChangesAsync();
            }
            // If already seeded, preserve Super Admin's configuration and do not overwrite
        }

        // Fuerto: All modules + Branching
        await EnsureSubscription(fuerto.CompanyId, "Enterprise", 4999m, "All");

        // GLI Bahay Builds (Construction): Business Intelligence + Action
        await EnsureSubscription(gli.CompanyId, "Professional", 3499m, "Business Intelligence,Action");

        // Custom Crafters Davao (Renovation): Main Transaction + Data Collection
        await EnsureSubscription(ccd.CompanyId, "Professional", 3499m, "Main Transaction,Data Collection");

        // -------------------------------------------------------------
        // 4. ADMIN USERS PER COMPANY
        // -------------------------------------------------------------
        async Task EnsureAdminUser(string email, string fullName, int companyId)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    CompanyId = companyId
                };
                var res = await userManager.CreateAsync(user, "Admin@12345");
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, ApplicationRoles.Admin);
                }
            }
        }

        // Remove legacy credentials
        var oldLeo = await userManager.FindByEmailAsync("admin@leorevita.local");
        if (oldLeo != null) await userManager.DeleteAsync(oldLeo);

        var oldMrd = await userManager.FindByEmailAsync("admin@misterdonut.local");
        if (oldMrd != null) await userManager.DeleteAsync(oldMrd);

        // Ensure active company admin logins
        await EnsureAdminUser("admin@fuerto.com", "Fuerto Admin", fuerto.CompanyId);
        await EnsureAdminUser("admin@glibahaybuilds.local", "GLI Bahay Builds Admin", gli.CompanyId);
        await EnsureAdminUser("admin@customcraftersdavao.local", "Custom Crafters Davao Admin", ccd.CompanyId);

        async Task<ApplicationUser> EnsureManagerUser(string email, string fullName, int companyId, int branchId)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    CompanyId = companyId,
                    BranchId = branchId
                };
                var res = await userManager.CreateAsync(user, "Manager123!");
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, ApplicationRoles.Manager);
                }
            }
            else
            {
                user.BranchId = branchId;
                user.FullName = fullName;
                await userManager.UpdateAsync(user);
                if (!await userManager.IsInRoleAsync(user, ApplicationRoles.Manager))
                {
                    await userManager.AddToRoleAsync(user, ApplicationRoles.Manager);
                }
            }
            return user;
        }

        var makatiMgr = await EnsureManagerUser("makati.manager@fuerto.local", "Makati Branch Manager", fuerto.CompanyId, 1);
        await EnsureManagerUser("manager@fuerto.com", "Makati Branch Manager", fuerto.CompanyId, 1);

        // -------------------------------------------------------------
        // 5. SEED FUERTO BRANCHES
        // -------------------------------------------------------------
        try
        {
            await using var dbFuerto = await tenantFactory.CreateAsync(fuerto.CompanyId);
            await dbFuerto.Database.EnsureCreatedAsync();
            await CRM.api.Endpoints.BranchEndpoints.EnsureBranchesTableExistsAsync(dbFuerto);

            if (!await dbFuerto.Branches.AnyAsync())
            {
                dbFuerto.Branches.AddRange(
                    new Branch
                    {
                        CompanyId = fuerto.CompanyId,
                        BranchCode = "FUERTO-HQ",
                        BranchName = "Fuerto Makati Flagship Studio",
                        Address = "Ayala Avenue, Makati City, Metro Manila",
                        ContactNumber = "+63 2 8888 1001",
                        Email = "makati@fuerto.local",
                        IsMainBranch = true,
                        IsActive = true,
                        ManagerUserId = makatiMgr.Id,
                        ManagerName = makatiMgr.FullName,
                        ManagerEmail = makatiMgr.Email
                    },
                    new Branch
                    {
                        CompanyId = fuerto.CompanyId,
                        BranchCode = "FUERTO-BGC",
                        BranchName = "Fuerto BGC Design Gallery",
                        Address = "Bonifacio High Street, Taguig City",
                        ContactNumber = "+63 2 8888 1002",
                        Email = "bgc@fuerto.local",
                        IsMainBranch = false,
                        IsActive = true
                    },
                    new Branch
                    {
                        CompanyId = fuerto.CompanyId,
                        BranchCode = "FUERTO-CEB",
                        BranchName = "Fuerto Cebu Studio",
                        Address = "Cebu IT Park, Cebu City",
                        ContactNumber = "+63 32 234 5001",
                        Email = "cebu@fuerto.local",
                        IsMainBranch = false,
                        IsActive = true
                    },
                    new Branch
                    {
                        CompanyId = fuerto.CompanyId,
                        BranchCode = "FUERTO-DVO",
                        BranchName = "Fuerto Davao Design Hub",
                        Address = "Lanang Premier, Davao City",
                        ContactNumber = "+63 82 299 4001",
                        Email = "davao@fuerto.local",
                        IsMainBranch = false,
                        IsActive = true
                    }
                );
                await dbFuerto.SaveChangesAsync();
            }
            else
            {
                var mainB = await dbFuerto.Branches.FirstOrDefaultAsync(b => b.BranchId == 1 || b.IsMainBranch);
                if (mainB != null && string.IsNullOrEmpty(mainB.ManagerName))
                {
                    mainB.ManagerUserId = makatiMgr.Id;
                    mainB.ManagerName = makatiMgr.FullName;
                    mainB.ManagerEmail = makatiMgr.Email;
                    await dbFuerto.SaveChangesAsync();
                }
            }

            await SeedInventoryAndSuppliersAsync(dbFuerto, fuerto.CompanyId, "FUERTO");
        }
        catch { }

        // -------------------------------------------------------------
        // 6. SEED GLI BAHAY BUILDS (Construction Company - No Branches)
        // -------------------------------------------------------------
        try
        {
            await using var dbGli = await tenantFactory.CreateAsync(gli.CompanyId);
            await dbGli.Database.EnsureCreatedAsync();
            await CRM.api.Endpoints.BranchEndpoints.EnsureBranchesTableExistsAsync(dbGli);

            // If GLI has no branches yet, seed default main headquarters branch
            if (!await dbGli.Branches.AnyAsync())
            {
                dbGli.Branches.Add(new Branch
                {
                    CompanyId = gli.CompanyId,
                    BranchCode = "GLI-HQ",
                    BranchName = "GLI Bahay Builds - Main Operations HQ",
                    Address = "Km 7, Lanang, Davao City",
                    ContactNumber = "+63 82 288 9001",
                    Email = "hq@glibuilds.local",
                    IsMainBranch = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                await dbGli.SaveChangesAsync();
            }

            await SeedGliConstruction200Async(dbGli, gli.CompanyId);
            await SeedInventoryAndSuppliersAsync(dbGli, gli.CompanyId, "GILBB");
        }
        catch { }

        // -------------------------------------------------------------
        // 7. SEED CUSTOM CRAFTERS DAVAO (Renovation Company)
        // -------------------------------------------------------------
        try
        {
            await using var dbCcd = await tenantFactory.CreateAsync(ccd.CompanyId);
            await dbCcd.Database.EnsureCreatedAsync();
            await CRM.api.Endpoints.BranchEndpoints.EnsureBranchesTableExistsAsync(dbCcd);

            if (!await dbCcd.Branches.AnyAsync())
            {
                dbCcd.Branches.AddRange(
                    new Branch
                    {
                        CompanyId = ccd.CompanyId,
                        BranchCode = "CCD-DVO",
                        BranchName = "Custom Crafters Davao - Lanang Design Hub",
                        Address = "Lanang Business Park, JP Laurel Ave, Davao City",
                        ContactNumber = "+63 82 299 8811",
                        Email = "lanang@customcrafters.ph",
                        IsMainBranch = true,
                        IsActive = true
                    },
                    new Branch
                    {
                        CompanyId = ccd.CompanyId,
                        BranchCode = "CCD-MAT",
                        BranchName = "Custom Crafters Davao - Matina Studio",
                        Address = "McArthur Highway, Matina, Davao City",
                        ContactNumber = "+63 82 299 8822",
                        Email = "matina@customcrafters.ph",
                        IsMainBranch = false,
                        IsActive = true
                    }
                );
                await dbCcd.SaveChangesAsync();
            }

            await SeedCustomCrafters200Async(dbCcd, ccd.CompanyId);
            await SeedInventoryAndSuppliersAsync(dbCcd, ccd.CompanyId, "CCDAVAO");
        }
        catch { }
    }

    // =========================================================================
    // SEED GLI BAHAY BUILDS (200 Customers, 200 Construction Builds, 200 Estimates)
    // =========================================================================
    private static async Task SeedGliConstruction200Async(TenantErpDbContext dbGli, int companyId)
    {
        int existingCount = await dbGli.Customers.CountAsync();
        var rng = new Random(101);

        // Shared name arrays – used by both Customer seeding and Leads seeding below
        string[] firstNames = {
            "Camille", "Bea", "Kathryn", "Sarah", "Marian", "Anne", "Nadine", "Liza", "Yassi", "Heart",
            "Kristine", "Angel", "Julia", "Janella", "Gabbi", "Francine", "Andrea", "Ivana", "Sue", "Miles",
            "Arci", "Kylie", "Coleen", "Ryza", "Maxene", "Rhian", "Chie", "Carla", "Bianca", "Megan",
            "Maja", "Kim", "Shaina", "Erich", "Glaiza", "Jennylyn", "Lovi", "Solenn", "Rachelle", "Karylle",
            "Belle", "Kyline", "Sofia", "Heaven", "Jane", "Barbie", "Kisses", "Vivoree", "Chienna", "Alexa",
            "Dennis", "Dingdong", "Jericho", "Piolo", "Daniel", "Enrique", "James", "Alden", "Gerald", "John Lloyd",
            "Richard", "Coco", "Ian", "Derek", "Paulo", "Sam", "Xian", "Joshua", "Donny", "Seth"
        };

        string[] lastNames = {
            "Reyes", "Santos", "Cruz", "Bautista", "Ocampo", "Garcia", "Mendoza", "Torres", "Ramos", "Gonzales",
            "Aquino", "Villanueva", "Castillo", "Flores", "Rivera", "Domingo", "Navarro", "Salazar", "Pascual",
            "Fernandez", "Lopez", "Perez", "Roman", "Aguilar", "Fernando", "Roxas", "Lim", "Tan", "Chua", "Sy",
            "Mercado", "Soriano", "De Leon", "Pangilinan", "Padilla", "Dela Cruz", "Estrada", "Alvarez", "Velasco", "Morales"
        };

        if (existingCount < 200)
        {
            string[] cities = { "Quezon City", "Makati City", "Taguig (BGC)", "Mandaluyong", "Pasig City", "San Juan", "Manila", "Alabang", "Paranaque", "Marikina" };
            string[] types = { "Developer", "Property Owner", "Commercial", "Residential Client" };

            var newCustomers = new List<Customer>();
            for (int i = existingCount + 1; i <= 200; i++)
            {
                string fn = firstNames[rng.Next(firstNames.Length)];
                string ln = lastNames[rng.Next(lastNames.Length)];
                string email = $"{fn.ToLower()}.{ln.ToLower()}{i}@phmail.com";
                string phone = $"0917{rng.Next(1000000, 9999999)}";
                string city = cities[rng.Next(cities.Length)];
                string cType = types[rng.Next(types.Length)];

                newCustomers.Add(new Customer
                {
                    CompanyId = companyId,
                    FirstName = fn,
                    LastName = ln,
                    Email = email,
                    Phone = phone,
                    CustomerType = cType,
                    Address = $"{rng.Next(12, 850)} Katipunan Ave, {city}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(10, 360)),
                    Notes = $"Property owner - {cType} build inquiry"
                });
            }

            dbGli.Customers.AddRange(newCustomers);
            await dbGli.SaveChangesAsync();
        }

        var allCustomers = await dbGli.Customers.OrderBy(c => c.CustomerId).ToListAsync();

        // 2. Ensure 200 Construction Projects
        int existingProjects = await dbGli.Projects.CountAsync();
        if (existingProjects < 200)
        {
            string[] constructionPackages = {
                "2-Storey Modern Residential Concrete Build",
                "Bungalow Family Home Construction & Roofing",
                "Structural Steel Framing & Foundation Pouring",
                "Architectural Concrete Villa Construction",
                "Commercial Building Perimeter & Civil Works",
                "Multi-Level Townhouse Construction Project",
                "Custom Duplex Residential Development",
                "Full Turnkey House Construction & Turnover",
                "Subdivision Model House Architectural Build",
                "Perimeter Fence, Gate & Concrete Driveway Build",
                "Reinforced Concrete Foundation & Retaining Wall",
                "Commercial Warehouse Structural Build"
            };

            var newProjects = new List<Project>();
            for (int i = existingProjects + 1; i <= 200; i++)
            {
                var cust = allCustomers[(i - 1) % allCustomers.Count];
                string srv = constructionPackages[rng.Next(constructionPackages.Length)];
                int daysAgo = rng.Next(2, 340);
                DateTime start = DateTime.UtcNow.AddDays(-daysAgo);

                string status;
                string stage;
                int progress;
                DateTime? compDate = null;

                int roll = rng.Next(100);
                if (roll < 72)
                {
                    status = "Completed";
                    stage = ProjectDesignStage.Completed;
                    progress = 100;
                    compDate = start.AddDays(rng.Next(30, 90));
                }
                else if (roll < 90)
                {
                    status = "In Progress";
                    stage = ProjectDesignStage.InProgress;
                    progress = rng.Next(40, 85);
                }
                else
                {
                    status = "Planning";
                    stage = ProjectDesignStage.Inquiry;
                    progress = rng.Next(10, 30);
                }

                newProjects.Add(new Project
                {
                    CompanyId = companyId,
                    CustomerId = cust.CustomerId,
                    ProjectCode = $"GLI-BLD-{i:D4}",
                    ProjectName = $"{cust.FirstName} {cust.LastName} - {srv}",
                    ProjectType = "Residential Construction",
                    Location = cust.Address.Contains("Quezon") ? "Quezon City Construction Site" : "Makati Job Site",
                    Description = $"{srv} for {cust.FirstName}. Account type: {cust.CustomerType}",
                    StartDate = start,
                    TargetEndDate = start.AddDays(120),
                    Status = status,
                    DesignStage = stage,
                    ProgressPercentage = progress,
                    DesignStartDate = start,
                    DesignCompletionDate = compDate,
                    IsActive = true,
                    CreatedAt = start.AddDays(-5)
                });
            }

            dbGli.Projects.AddRange(newProjects);
            await dbGli.SaveChangesAsync();
        }

        var allProjects = await dbGli.Projects.OrderBy(p => p.ProjectId).ToListAsync();

        // 3. Ensure 200 Construction Estimates / Quotations
        int existingQuotes = await dbGli.Quotations.CountAsync();
        if (existingQuotes < 200)
        {
            var newQuotes = new List<Quotation>();
            for (int i = existingQuotes + 1; i <= 200; i++)
            {
                var proj = allProjects[(i - 1) % allProjects.Count];
                var cust = allCustomers.FirstOrDefault(c => c.CustomerId == proj.CustomerId) ?? allCustomers[0];

                decimal subtotal = rng.Next(250, 4500) * 1000m; // 250,000 to 4,500,000
                decimal discount = rng.Next(10) == 0 ? 25000m : 0m;
                decimal total = Math.Max(200000m, subtotal - discount);

                string qStatus;
                string pStatus;
                decimal paid;

                if (proj.Status == "Completed")
                {
                    qStatus = QuotationStatus.Accepted;
                    pStatus = PaymentStatus.FullyPaid;
                    paid = total;
                }
                else if (proj.Status == "In Progress")
                {
                    qStatus = QuotationStatus.Accepted;
                    pStatus = PaymentStatus.DepositReceived;
                    paid = Math.Round(total * 0.5m, 2);
                }
                else
                {
                    qStatus = QuotationStatus.Issued;
                    pStatus = PaymentStatus.Pending;
                    paid = 0;
                }

                newQuotes.Add(new Quotation
                {
                    CompanyId = companyId,
                    QuotationNumber = $"QUO-GLI-2026-{i:D4}",
                    ProjectId = proj.ProjectId,
                    CustomerId = cust.CustomerId,
                    QuotationDate = proj.StartDate ?? DateTime.UtcNow.AddDays(-rng.Next(10, 300)),
                    Subtotal = subtotal,
                    Discount = discount,
                    TotalAmount = total,
                    Status = qStatus,
                    PaymentStatus = pStatus,
                    AmountPaid = paid,
                    DepositRequired = Math.Round(total * 0.3m, 2),
                    PaymentMethod = pStatus != PaymentStatus.Pending ? (rng.Next(2) == 0 ? "Bank Transfer (BDO)" : "Corporate Check") : "Pending",
                    Notes = $"Construction estimate for {proj.ProjectName}",
                    CreatedAt = proj.CreatedAt
                });
            }

            dbGli.Quotations.AddRange(newQuotes);
            await dbGli.SaveChangesAsync();
        }

        // 4. Ensure 50+ Feedback & Reviews
        if (await dbGli.ProjectFeedbacks.CountAsync() < 50)
        {
            string[] comments = {
                "Outstanding structural build quality. The engineers were on-site daily and provided transparent progress updates.",
                "Turnkey residential construction was delivered right on time with flawless concrete and roofing works.",
                "Superb engineering craftsmanship and strict adherence to structural safety standards.",
                "Very clean job site and respectful construction crew. 5 stars!",
                "Architectural blueprint translation into actual construction exceeded our expectations.",
                "Professional project management and accurate bill of quantities with zero hidden costs.",
                "Foundation and framing passed all municipal building inspections on first review."
            };

            var newFeedbacks = new List<ProjectFeedback>();
            for (int i = 1; i <= 50; i++)
            {
                var proj = allProjects[rng.Next(allProjects.Count)];
                newFeedbacks.Add(new ProjectFeedback
                {
                    CompanyId = companyId,
                    ProjectId = proj.ProjectId,
                    OverallRating = rng.Next(4, 6),
                    TimelinessRating = rng.Next(4, 6),
                    CommunicationRating = rng.Next(4, 6),
                    ValueRating = rng.Next(4, 6),
                    Comments = comments[rng.Next(comments.Length)],
                    SubmittedAt = proj.StartDate ?? DateTime.UtcNow.AddDays(-rng.Next(5, 200))
                });
            }
            dbGli.ProjectFeedbacks.AddRange(newFeedbacks);
            await dbGli.SaveChangesAsync();
        }

        // 5. Ensure 30+ Leads
        if (await dbGli.Leads.CountAsync() < 30)
        {
            string[] sources = { "Direct Inquiry", "Architectural Referral", "Website", "Site Banner", "Developer Network" };
            string[] statuses = { "New", "Contacted", "Qualified", "Proposal", "Won" };

            var newLeads = new List<Lead>();
            for (int i = 1; i <= 30; i++)
            {
                string fn = firstNames[rng.Next(firstNames.Length)];
                string ln = lastNames[rng.Next(lastNames.Length)];
                newLeads.Add(new Lead
                {
                    CompanyId = companyId,
                    FirstName = fn,
                    LastName = ln,
                    Email = $"lead.{fn.ToLower()}.{ln.ToLower()}{i}@email.com",
                    Phone = $"0918{rng.Next(1000000, 9999999)}",
                    Status = statuses[rng.Next(statuses.Length)],
                    LeadSource = sources[rng.Next(sources.Length)],
                    Notes = "Interested in 2-Storey Turnkey House Construction / Architectural Blueprint",
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 90))
                });
            }
            dbGli.Leads.AddRange(newLeads);
            await dbGli.SaveChangesAsync();
        }
    }

    // =========================================================================
    // SEED CUSTOM CRAFTERS DAVAO (200 Customers, 200 Renovation Projects, 200 Quotes)
    // =========================================================================
    private static async Task SeedCustomCrafters200Async(TenantErpDbContext dbCcd, int companyId)
    {
        int existingCount = await dbCcd.Customers.CountAsync();
        var rng = new Random(202);

        string[] filipinoFirst = {
            "Vicente", "Joey", "Vic", "Tito", "Francis", "Eduardo", "Ramon", "Jaime", "Manuel", "Antonio",
            "Carlos", "Felipe", "Lorenzo", "Gabriel", "Mateo", "Enrico", "Dante", "Arthur", "Rolando", "Danilo",
            "Maria", "Teresa", "Lourdes", "Carmela", "Corazon", "Esperanza", "Leticia", "Rosario", "Divina", "Josefina",
            "Camille", "Bea", "Kathryn", "Sarah", "Marian", "Anne", "Nadine", "Liza", "Yassi", "Heart"
        };

        string[] filipinoLast = {
            "Sotto", "De Leon", "Concepcion", "Zobel", "Ayala", "Cojuangco", "Gokongwei", "Sy", "Tan", "Lucio",
            "Pangilinan", "Villar", "Razon", "Aboitiz", "Consunji", "Lopez", "Ortigas", "Araneta", "Tuason", "Roxas",
            "Duterte", "Garcia", "Flores", "Lim", "Chua", "Yap", "Uy", "Villafuerte", "Alvarez", "Castillo"
        };

        string[] davaoLocations = {
            "Lanang Executive Homes, Davao City",
            "Abreeza Residences, Bajada, Davao City",
            "Ecoland Phase 3, Davao City",
            "Marfori Heights Subd, Davao City",
            "Insular Village 1, Lanang, Davao City",
            "Las Terrazas, Maa, Davao City",
            "Woodridge Park Subd, Maa, Davao City",
            "Damosa IT Park District, Davao City",
            "Matina Enclaves, Davao City",
            "Belisario Heights, Bajada, Davao City",
            "Solariega Subd, Talomo, Davao City",
            "One Oasis Condominiums, Ecoland, Davao City"
        };

        if (existingCount < 200)
        {
            var newCustomers = new List<Customer>();
            for (int i = existingCount + 1; i <= 200; i++)
            {
                string fn = filipinoFirst[rng.Next(filipinoFirst.Length)];
                string ln = filipinoLast[rng.Next(filipinoLast.Length)];
                string email = $"{fn.ToLower()}.{ln.ToLower()}{i}@craftersdavao.ph";
                string phone = $"0918{rng.Next(1000000, 9999999)}";
                string cType = (i % 4 == 0) ? "Commercial" : ((i % 3 == 0) ? "Condo" : ((i % 2 == 0) ? "VIP Residential" : "Residential"));
                string loc = davaoLocations[rng.Next(davaoLocations.Length)];

                newCustomers.Add(new Customer
                {
                    CompanyId = companyId,
                    FirstName = fn,
                    LastName = ln,
                    Email = email,
                    Phone = phone,
                    CustomerType = cType,
                    Address = $"{rng.Next(12, 888)} {loc}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(15, 360)),
                    Notes = cType == "Commercial" ? "Commercial office / retail renovation client" : "Residential interior makeover & custom cabinetry"
                });
            }

            dbCcd.Customers.AddRange(newCustomers);
            await dbCcd.SaveChangesAsync();
        }

        var allCustomers = await dbCcd.Customers.OrderBy(c => c.CustomerId).ToListAsync();

        // 2. Ensure 200 Renovation Projects
        int existingProjects = await dbCcd.Projects.CountAsync();
        if (existingProjects < 200)
        {
            string[] renovationTypes = {
                "Modern Minimalist Kitchen Remodel & Custom Island",
                "Luxury Master Bathroom & Walk-in Shower Makeover",
                "2-Bedroom Condominium Full Interior Renovation",
                "Living & Dining Room Open-Concept Transformation",
                "Commercial Boutique Office Interior Fit-Out",
                "Outdoor Patio & Lanai Timber Decking Upgrade",
                "Custom Wardrobes & Master Bedroom Suite Renovation",
                "Loft Apartment Mezzanine & Space Optimization",
                "Whole-House Modern Acoustic Ceiling & Lighting Overhaul",
                "Cafe & Bakery Interior Renovation & Bespoke Cabinetry",
                "Home Theater & Entertainment Lounge Acoustic Fit-Out",
                "Executive Penthouse Luxury Renovation & Tile Works"
            };

            var newProjects = new List<Project>();
            for (int i = existingProjects + 1; i <= 200; i++)
            {
                var cust = allCustomers[(i - 1) % allCustomers.Count];
                string reno = renovationTypes[rng.Next(renovationTypes.Length)];
                int daysAgo = rng.Next(5, 340);
                DateTime start = DateTime.UtcNow.AddDays(-daysAgo);

                int roll = rng.Next(100);
                string status = roll < 70 ? "Completed" : (roll < 90 ? "In Progress" : "Planning");
                int progress = status == "Completed" ? 100 : (status == "In Progress" ? rng.Next(35, 85) : 15);

                newProjects.Add(new Project
                {
                    CompanyId = companyId,
                    CustomerId = cust.CustomerId,
                    ProjectCode = $"CCD-REN-{i:D4}",
                    ProjectName = $"{cust.LastName} Residence - {reno}",
                    ProjectType = "Interior Renovation & Remodeling",
                    Location = cust.Address.Contains("Lanang") ? "Lanang Design Hub" : "Matina Studio",
                    Description = $"{reno} for {cust.FirstName} {cust.LastName}. Client category: {cust.CustomerType}",
                    StartDate = start,
                    TargetEndDate = start.AddDays(rng.Next(20, 90)),
                    Status = status,
                    DesignStage = status == "Completed" ? ProjectDesignStage.Completed : ProjectDesignStage.InProgress,
                    ProgressPercentage = progress,
                    DesignStartDate = start,
                    DesignCompletionDate = status == "Completed" ? start.AddDays(rng.Next(15, 60)) : null,
                    IsActive = true,
                    CreatedAt = start.AddDays(-2)
                });
            }

            dbCcd.Projects.AddRange(newProjects);
            await dbCcd.SaveChangesAsync();
        }

        var allProjects = await dbCcd.Projects.OrderBy(p => p.ProjectId).ToListAsync();

        // 3. Ensure 200 Renovation Quotations
        int existingQuotes = await dbCcd.Quotations.CountAsync();
        if (existingQuotes < 200)
        {
            var newQuotes = new List<Quotation>();
            for (int i = existingQuotes + 1; i <= 200; i++)
            {
                var proj = allProjects[(i - 1) % allProjects.Count];
                var cust = allCustomers.FirstOrDefault(c => c.CustomerId == proj.CustomerId) ?? allCustomers[0];

                decimal subtotal = rng.Next(75, 850) * 1000m; // 75,000 to 850,000
                decimal discount = rng.Next(4) == 0 ? rng.Next(10, 40) * 1000m : 0m;
                decimal total = Math.Max(50000m, subtotal - discount);

                string qStatus = proj.Status == "Completed" ? QuotationStatus.Accepted : (proj.Status == "In Progress" ? QuotationStatus.Accepted : QuotationStatus.Issued);
                string pStatus = proj.Status == "Completed" ? PaymentStatus.FullyPaid : (proj.Status == "In Progress" ? PaymentStatus.DepositReceived : PaymentStatus.Pending);
                decimal paid = pStatus == PaymentStatus.FullyPaid ? total : (pStatus == PaymentStatus.DepositReceived ? Math.Round(total * 0.5m, 2) : 0m);

                newQuotes.Add(new Quotation
                {
                    CompanyId = companyId,
                    QuotationNumber = $"CCD-Q-2026-{i:D4}",
                    ProjectId = proj.ProjectId,
                    CustomerId = cust.CustomerId,
                    QuotationDate = proj.StartDate ?? DateTime.UtcNow.AddDays(-rng.Next(5, 320)),
                    Subtotal = subtotal,
                    Discount = discount,
                    TotalAmount = total,
                    Status = qStatus,
                    PaymentStatus = pStatus,
                    AmountPaid = paid,
                    DepositRequired = Math.Round(total * 0.3m, 2),
                    PaymentMethod = pStatus != PaymentStatus.Pending ? (rng.Next(2) == 0 ? "Bank Transfer (BDO Davao)" : "Manager Check") : "Pending",
                    Notes = $"Renovation estimate for {proj.ProjectName}",
                    CreatedAt = proj.CreatedAt
                });
            }

            dbCcd.Quotations.AddRange(newQuotes);
            await dbCcd.SaveChangesAsync();
        }

        // 4. Ensure 25+ Promotional Renovation Packages
        if (await dbCcd.Promotions.CountAsync() < 25)
        {
            var promos = new List<Promotion>
            {
                new() { CompanyId = companyId, Name = "Full Kitchen Cabinetry 10% Bundle Discount", Code = "CCD-KIT10", OfferType = "Percentage", OfferValue = 10m, TargetSegment = "All Clients", Description = "10% discount on complete kitchen modular cabinetry fit-outs.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-30), ValidUntil = DateTime.UtcNow.AddMonths(3) },
                new() { CompanyId = companyId, Name = "Condo Interior Fit-Out Package ₱25,000 Off", Code = "CCD-CONDO25", OfferType = "FixedAmount", OfferValue = 25000m, TargetSegment = "Loyal", Description = "Exclusive ₱25,000 discount voucher on condominium turnkey renovations.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-20), ValidUntil = DateTime.UtcNow.AddMonths(2) },
                new() { CompanyId = companyId, Name = "Free 3D Architectural Visualization & Spatial Design", Code = "CCD-FREE3D", OfferType = "FreeService", OfferValue = 18000m, TargetSegment = "Champion", Description = "Complimentary high-definition 3D rendering package with approved quote.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-15), ValidUntil = DateTime.UtcNow.AddMonths(3) },
                new() { CompanyId = companyId, Name = "Master Bathroom Tile & Waterproofing ₱15,000 Off", Code = "CCD-BATH15", OfferType = "FixedAmount", OfferValue = 15000m, TargetSegment = "At Risk", Description = "₱15,000 subsidy on comprehensive bathroom renovation and waterproofing.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-10), ValidUntil = DateTime.UtcNow.AddMonths(1) },
                new() { CompanyId = companyId, Name = "Commercial Office Fit-Out ₱50,000 Rebate", Code = "CCD-CORP50", OfferType = "FixedAmount", OfferValue = 50000m, TargetSegment = "Champion", Description = "₱50,000 rebate on commercial fit-outs exceeding ₱500,000.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-40), ValidUntil = DateTime.UtcNow.AddMonths(6) },
                new() { CompanyId = companyId, Name = "Whole-Home Custom Wardrobes 12% Off", Code = "CCD-WARD12", OfferType = "Percentage", OfferValue = 12m, TargetSegment = "Loyal", Description = "12% off built-in master wardrobes and walk-in closet configurations.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-5), ValidUntil = DateTime.UtcNow.AddMonths(2) },
                new() { CompanyId = companyId, Name = "Lanai & Decking Summer Renovation Special", Code = "CCD-DECK20", OfferType = "Percentage", OfferValue = 15m, TargetSegment = "Promising", Description = "15% discount on composite timber decking and outdoor pergolas.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-12), ValidUntil = DateTime.UtcNow.AddMonths(2) },
                new() { CompanyId = companyId, Name = "Early Bird Renovation Booking ₱20,000 Voucher", Code = "CCD-EARLY20", OfferType = "FixedAmount", OfferValue = 20000m, TargetSegment = "All Clients", Description = "₱20,000 gift voucher for renovations scheduled 60 days in advance.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-25), ValidUntil = DateTime.UtcNow.AddMonths(4) }
            };
            dbCcd.Promotions.AddRange(promos);
            await dbCcd.SaveChangesAsync();
        }

        // 5. Ensure 60+ Customer Retention Actions (RFM Segments)
        if (await dbCcd.RetentionActions.CountAsync() < 60)
        {
            string[] segments = { "Champion", "Loyal", "Promising", "At Risk", "Dormant" };
            string[] rStatuses = { "Logged", "Contacted", "Redeemed", "Pending" };
            var newRetentions = new List<RetentionAction>();

            for (int i = 1; i <= 60; i++)
            {
                var cust = allCustomers[rng.Next(allCustomers.Count)];
                string seg = segments[rng.Next(segments.Length)];
                string st = rStatuses[rng.Next(rStatuses.Length)];

                newRetentions.Add(new RetentionAction
                {
                    CompanyId = companyId,
                    CustomerId = cust.CustomerId,
                    Segment = seg,
                    OfferType = (i % 2 == 0) ? "FixedAmount" : "Percentage",
                    OfferValue = (i % 2 == 0) ? 15000m : 10m,
                    OfferDescription = $"Exclusive {seg} homeowner reward: " + ((i % 2 == 0) ? "₱15,000 renovation voucher" : "10% discount on next room remodel"),
                    Notes = $"Post-renovation warranty & follow-up for {cust.LastName} residence. Status: {st}",
                    Status = st,
                    CreatedByUserId = "System",
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 120))
                });
            }

            dbCcd.RetentionActions.AddRange(newRetentions);
            await dbCcd.SaveChangesAsync();
        }
    }

    private static async Task SeedInventoryAndSuppliersAsync(TenantErpDbContext db, int companyId, string companyCode)
    {
        try
        {
            if (!await db.Suppliers.AnyAsync())
            {
                if (companyCode.Equals("GILBB", StringComparison.OrdinalIgnoreCase))
                {
                    db.Suppliers.AddRange(
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-GLI-01", SupplierName = "Davao Steel & Rebar Corp", ContactPerson = "Engr. Ramon Dela Cruz", ContactNumber = "+63 82 291 0011", EmailAddress = "sales@davaosteel.ph", Address = "Panacan, Davao City", IsActive = true },
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-GLI-02", SupplierName = "Mindanao Ready-Mix Concrete Solutions", ContactPerson = "Marco Gutierrez", ContactNumber = "+63 82 291 0022", EmailAddress = "orders@mindanaoconcrete.ph", Address = "Buhangin, Davao City", IsActive = true },
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-GLI-03", SupplierName = "Apo Cement & Aggregates Supply", ContactPerson = "Grace Tan", ContactNumber = "+63 82 291 0033", EmailAddress = "sales@apocement.ph", Address = "Toril, Davao City", IsActive = true },
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-GLI-04", SupplierName = "Pioneer Hardware & Heavy Lumber", ContactPerson = "David Sy", ContactNumber = "+63 82 291 0044", EmailAddress = "orders@pioneerhardware.ph", Address = "Agdao, Davao City", IsActive = true }
                    );
                }
                else if (companyCode.Equals("CCDAVAO", StringComparison.OrdinalIgnoreCase))
                {
                    db.Suppliers.AddRange(
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-CCD-01", SupplierName = "Matina Woodworks & Architectural Timber", ContactPerson = "Nestor Alvarez", ContactNumber = "+63 82 299 1101", EmailAddress = "sales@matinawood.ph", Address = "Matina, Davao City", IsActive = true },
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-CCD-02", SupplierName = "Davao Granite, Quartz & Tile Depot", ContactPerson = "Elena Lim", ContactNumber = "+63 82 299 1102", EmailAddress = "orders@davaotiles.ph", Address = "Lanang, Davao City", IsActive = true },
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-CCD-03", SupplierName = "Southern Finish Paints & Premium Coatings", ContactPerson = "Arthur Santos", ContactNumber = "+63 82 299 1103", EmailAddress = "info@southernfinish.ph", Address = "Bajada, Davao City", IsActive = true },
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-CCD-04", SupplierName = "Craftsman Cabinet Fittings & Hardware", ContactPerson = "Lito Reyes", ContactNumber = "+63 82 299 1104", EmailAddress = "sales@craftsmanhardware.ph", Address = "Ecoland, Davao City", IsActive = true }
                    );
                }
                else
                {
                    db.Suppliers.AddRange(
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-FTO-01", SupplierName = "Makati Designer Fabric & Drapery Hub", ContactPerson = "Cecilia Soriano", ContactNumber = "+63 2 8888 2001", EmailAddress = "sales@makatifabrics.ph", Address = "Pasong Tamo, Makati City", IsActive = true },
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-FTO-02", SupplierName = "BGC Luxury Architectural Lighting", ContactPerson = "Victor Cheng", ContactNumber = "+63 2 8888 2002", EmailAddress = "projects@bgclighting.ph", Address = "Bonifacio Global City, Taguig", IsActive = true },
                        new Supplier { CompanyId = companyId, SupplierCode = "SUP-FTO-03", SupplierName = "Metro Architectural Glass & Mirror", ContactPerson = "Anthony Go", ContactNumber = "+63 2 8888 2003", EmailAddress = "info@metroglass.ph", Address = "Ortigas Center, Pasig City", IsActive = true }
                    );
                }
                await db.SaveChangesAsync();
            }

            if (!await db.Inventories.AnyAsync())
            {
                if (companyCode.Equals("GILBB", StringComparison.OrdinalIgnoreCase))
                {
                    var p1 = new Product { CompanyId = companyId, ProductCode = "MAT-CMT-01", ProductName = "Portland Cement Type 1 (40kg bag)", UnitPrice = 245.00m };
                    var p2 = new Product { CompanyId = companyId, ProductCode = "MAT-RBR-01", ProductName = "Deformed Rebar 12mm x 6m Grade 40", UnitPrice = 285.00m };
                    var p3 = new Product { CompanyId = companyId, ProductCode = "MAT-CHB-01", ProductName = "Concrete Hollow Blocks 4-inch Standard", UnitPrice = 18.50m };
                    var p4 = new Product { CompanyId = companyId, ProductCode = "MAT-SND-01", ProductName = "Washed Aggregates Sand & Gravel (cu.m)", UnitPrice = 1200.00m };
                    var p5 = new Product { CompanyId = companyId, ProductCode = "MAT-STL-01", ProductName = "Structural Steel I-Beam 6x4 (per length)", UnitPrice = 4500.00m };
                    db.Products.AddRange(p1, p2, p3, p4, p5);
                    await db.SaveChangesAsync();

                    db.Inventories.AddRange(
                        new Inventory { ProductId = p1.ProductId, QuantityOnHand = 450, ReorderLevel = 100, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p2.ProductId, QuantityOnHand = 320, ReorderLevel = 80, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p3.ProductId, QuantityOnHand = 2500, ReorderLevel = 500, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p4.ProductId, QuantityOnHand = 75, ReorderLevel = 20, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p5.ProductId, QuantityOnHand = 40, ReorderLevel = 15, LastUpdatedAt = DateTime.UtcNow }
                    );
                }
                else if (companyCode.Equals("CCDAVAO", StringComparison.OrdinalIgnoreCase))
                {
                    var p1 = new Product { CompanyId = companyId, ProductCode = "RNV-PLY-01", ProductName = "Marine Plywood 3/4-inch 4x8 Hardwood", UnitPrice = 1350.00m };
                    var p2 = new Product { CompanyId = companyId, ProductCode = "RNV-LAM-01", ProductName = "High-Pressure Laminate Sheets (Teak finish)", UnitPrice = 980.00m };
                    var p3 = new Product { CompanyId = companyId, ProductCode = "RNV-PNT-01", ProductName = "Semi-Gloss Interior Acrylic Latex (16L)", UnitPrice = 3200.00m };
                    var p4 = new Product { CompanyId = companyId, ProductCode = "RNV-HNG-01", ProductName = "Soft-Close Cabinet Concealed Hinges (pair)", UnitPrice = 180.00m };
                    var p5 = new Product { CompanyId = companyId, ProductCode = "RNV-TLE-01", ProductName = "Polished Porcelain Floor Tiles 60x60 (box)", UnitPrice = 850.00m };
                    db.Products.AddRange(p1, p2, p3, p4, p5);
                    await db.SaveChangesAsync();

                    db.Inventories.AddRange(
                        new Inventory { ProductId = p1.ProductId, QuantityOnHand = 85, ReorderLevel = 25, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p2.ProductId, QuantityOnHand = 60, ReorderLevel = 15, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p3.ProductId, QuantityOnHand = 35, ReorderLevel = 10, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p4.ProductId, QuantityOnHand = 180, ReorderLevel = 50, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p5.ProductId, QuantityOnHand = 240, ReorderLevel = 60, LastUpdatedAt = DateTime.UtcNow }
                    );
                }
                else
                {
                    var p1 = new Product { CompanyId = companyId, ProductCode = "DSG-LED-01", ProductName = "Recessed Dimmable LED Spotlights 12W", UnitPrice = 750.00m };
                    var p2 = new Product { CompanyId = companyId, ProductCode = "DSG-WLL-01", ProductName = "Acoustic Wall Paneling (Walnut Veneer)", UnitPrice = 2800.00m };
                    var p3 = new Product { CompanyId = companyId, ProductCode = "DSG-MRB-01", ProductName = "Italian Calacatta Marble Tiles 80x80", UnitPrice = 3500.00m };
                    db.Products.AddRange(p1, p2, p3);
                    await db.SaveChangesAsync();

                    db.Inventories.AddRange(
                        new Inventory { ProductId = p1.ProductId, QuantityOnHand = 120, ReorderLevel = 30, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p2.ProductId, QuantityOnHand = 50, ReorderLevel = 15, LastUpdatedAt = DateTime.UtcNow },
                        new Inventory { ProductId = p3.ProductId, QuantityOnHand = 90, ReorderLevel = 20, LastUpdatedAt = DateTime.UtcNow }
                    );
                }
                await db.SaveChangesAsync();
            }
        }
        catch { }
    }
}
