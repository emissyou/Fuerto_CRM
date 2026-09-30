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

        var salon = await masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyCode == "LRSALON");
        if (salon == null)
        {
            salon = new Company
            {
                CompanyCode = "LRSALON",
                CompanyName = "Leo Revita Salon",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            masterDb.Companies.Add(salon);
            await masterDb.SaveChangesAsync();
        }

        var donut = await masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyCode == "MRDONUT");
        if (donut == null)
        {
            donut = new Company
            {
                CompanyCode = "MRDONUT",
                CompanyName = "Mister Donut",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            masterDb.Companies.Add(donut);
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
        await EnsureCompanyDb(salon.CompanyId, "CRM_LRSalon", "db70838.public.databaseasp.net", "db70838", "LeoRevita");
        await EnsureCompanyDb(donut.CompanyId, "CRM_MrDonut", "db70839.public.databaseasp.net", "db70839", "MisterDonut");

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
            else if (sub.AvailedModules != modules)
            {
                sub.AvailedModules = modules;
                sub.PlanName = plan;
                await masterDb.SaveChangesAsync();
            }
        }

        // Fuerto: All modules + Branching
        await EnsureSubscription(fuerto.CompanyId, "Enterprise", 4999m, "All");

        // Leo Revita Salon: Main Transaction + Data Collection
        await EnsureSubscription(salon.CompanyId, "Professional", 2499m, "Main Transaction,Data Collection");

        // Mister Donut: Business Intelligence + Action
        await EnsureSubscription(donut.CompanyId, "Professional", 2999m, "Business Intelligence,Action");

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

        await EnsureAdminUser("admin@fuerto.com", "Fuerto Admin", fuerto.CompanyId);
        await EnsureAdminUser("admin@leorevita.local", "Leo Revita Salon Admin", salon.CompanyId);
        await EnsureAdminUser("admin@misterdonut.local", "Mister Donut Admin", donut.CompanyId);

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
                        IsActive = true
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
        }
        catch { }

        // -------------------------------------------------------------
        // 6. SEED LEO REVITA SALON (200 Records)
        // -------------------------------------------------------------
        try
        {
            await using var dbSalon = await tenantFactory.CreateAsync(salon.CompanyId);
            await dbSalon.Database.EnsureCreatedAsync();
            await CRM.api.Endpoints.BranchEndpoints.EnsureBranchesTableExistsAsync(dbSalon);

            if (!await dbSalon.Branches.AnyAsync())
            {
                dbSalon.Branches.AddRange(
                    new Branch
                    {
                        CompanyId = salon.CompanyId,
                        BranchCode = "LRS-SM",
                        BranchName = "Leo Revita Salon - SM North EDSA",
                        Address = "3F Main Mall, SM North EDSA, Quezon City",
                        ContactNumber = "+63 2 8920 3344",
                        Email = "smnorth@leorevita.local",
                        IsMainBranch = true,
                        IsActive = true
                    },
                    new Branch
                    {
                        CompanyId = salon.CompanyId,
                        BranchCode = "LRS-MEGA",
                        BranchName = "Leo Revita Salon - Megamall",
                        Address = "4F Bldg B, SM Megamall, Mandaluyong City",
                        ContactNumber = "+63 2 8633 1122",
                        Email = "megamall@leorevita.local",
                        IsMainBranch = false,
                        IsActive = true
                    }
                );
                await dbSalon.SaveChangesAsync();
            }

            await SeedSalon200Async(dbSalon, salon.CompanyId);
        }
        catch { }

        // -------------------------------------------------------------
        // 7. SEED MISTER DONUT (200 Records)
        // -------------------------------------------------------------
        try
        {
            await using var dbDonut = await tenantFactory.CreateAsync(donut.CompanyId);
            await dbDonut.Database.EnsureCreatedAsync();
            await CRM.api.Endpoints.BranchEndpoints.EnsureBranchesTableExistsAsync(dbDonut);

            if (!await dbDonut.Branches.AnyAsync())
            {
                dbDonut.Branches.AddRange(
                    new Branch
                    {
                        CompanyId = donut.CompanyId,
                        BranchCode = "MD-GH",
                        BranchName = "Mister Donut - Greenhills Store",
                        Address = "Ortigas Ave, Greenhills Shopping Center, San Juan",
                        ContactNumber = "+63 2 8721 5566",
                        Email = "greenhills@misterdonut.local",
                        IsMainBranch = true,
                        IsActive = true
                    },
                    new Branch
                    {
                        CompanyId = donut.CompanyId,
                        BranchCode = "MD-ATC",
                        BranchName = "Mister Donut - Alabang Town Center",
                        Address = "Alabang-Zapote Rd, Muntinlupa City",
                        ContactNumber = "+63 2 8807 9988",
                        Email = "alabang@misterdonut.local",
                        IsMainBranch = false,
                        IsActive = true
                    }
                );
                await dbDonut.SaveChangesAsync();
            }

            await SeedDonut200Async(dbDonut, donut.CompanyId);
        }
        catch { }
    }

    // =========================================================================
    // SEED LEO REVITA SALON (200 Customers, 200 Projects, 200 Appointments/Quotes)
    // =========================================================================
    private static async Task SeedSalon200Async(TenantErpDbContext dbSalon, int companyId)
    {
        int existingCount = await dbSalon.Customers.CountAsync();
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
            string[] types = { "VIP", "Regular", "Regular", "Regular", "Corporate", "Walk-in" };

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
                    Notes = cType == "VIP" ? "VIP member - prefers Senior Stylist" : "Preferred branch: SM North / Megamall"
                });
            }

            dbSalon.Customers.AddRange(newCustomers);
            await dbSalon.SaveChangesAsync();
        }

        var allCustomers = await dbSalon.Customers.OrderBy(c => c.CustomerId).ToListAsync();

        // 2. Ensure 200 Salon Services / Projects
        int existingProjects = await dbSalon.Projects.CountAsync();
        if (existingProjects < 200)
        {
            string[] servicePackages = {
                "Full Balayage & Ash Blonde Toner",
                "Keratin Brazilian Blowout Therapy",
                "Deluxe Bridal Hair & HD Makeup Package",
                "Japanese Silk Rebonding & Moisture Lock",
                "Scalp Detox & Hair Revitalization Spa",
                "Signature Precision Layer Cut & Blowdry",
                "Deep Conditioning Botanical Hair Spa",
                "Korean C-Curl Digital Wave Perm",
                "Color Correction & Platinum Highlights",
                "Olaplex Complete Hair Bond Reconstruction",
                "Express Gel Spa Manicure & Pedicure",
                "Men's Executive Fade & Beard Grooming",
                "Pastel Fashion Hair Color Transformation",
                "Anti-Frizz Hair Botox Deep Treatment",
                "Scalp Exfoliation & Anti-Dandruff Treatment"
            };

            var newProjects = new List<Project>();
            for (int i = existingProjects + 1; i <= 200; i++)
            {
                var cust = allCustomers[(i - 1) % allCustomers.Count];
                string srv = servicePackages[rng.Next(servicePackages.Length)];
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
                    compDate = start.AddDays(rng.Next(1, 4));
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
                    ProjectCode = $"LRS-SRV-{i:D4}",
                    ProjectName = $"{cust.FirstName} {cust.LastName} - {srv}",
                    ProjectType = "Salon Service",
                    Location = cust.Address.Contains("Quezon") ? "SM North EDSA Branch" : "Megamall Branch",
                    Description = $"{srv} requested by {cust.FirstName}. Customer type: {cust.CustomerType}",
                    StartDate = start,
                    TargetEndDate = start.AddDays(7),
                    Status = status,
                    DesignStage = stage,
                    ProgressPercentage = progress,
                    DesignStartDate = start,
                    DesignCompletionDate = compDate,
                    IsActive = true,
                    CreatedAt = start.AddDays(-2)
                });
            }

            dbSalon.Projects.AddRange(newProjects);
            await dbSalon.SaveChangesAsync();
        }

        var allProjects = await dbSalon.Projects.OrderBy(p => p.ProjectId).ToListAsync();

        // 3. Ensure 200 Appointments / Quotations
        int existingQuotes = await dbSalon.Quotations.CountAsync();
        if (existingQuotes < 200)
        {
            var newQuotes = new List<Quotation>();
            for (int i = existingQuotes + 1; i <= 200; i++)
            {
                var proj = allProjects[(i - 1) % allProjects.Count];
                var cust = allCustomers.FirstOrDefault(c => c.CustomerId == proj.CustomerId) ?? allCustomers[0];

                decimal subtotal = rng.Next(18, 280) * 100m; // 1,800 to 28,000
                decimal discount = rng.Next(10) == 0 ? 500m : (rng.Next(8) == 0 ? 1000m : 0m);
                decimal total = Math.Max(1200m, subtotal - discount);

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
                    QuotationNumber = $"QUO-LR-2026-{i:D4}",
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
                    PaymentMethod = pStatus != PaymentStatus.Pending ? (rng.Next(2) == 0 ? "GCash / Maya" : "Credit Card") : "Pending",
                    Notes = $"Appointment booking for {proj.ProjectName}",
                    CreatedAt = proj.CreatedAt
                });
            }

            dbSalon.Quotations.AddRange(newQuotes);
            await dbSalon.SaveChangesAsync();
        }

        // 4. Ensure 50+ Feedback & Reviews
        if (await dbSalon.ProjectFeedbacks.CountAsync() < 50)
        {
            string[] comments = {
                "Loved the balayage color! Looked exactly like my Pinterest reference.",
                "Super friendly senior stylists and very clean salon tools.",
                "Keratin treatment made my frizzy hair so manageable and shiny.",
                "Fast booking and no waiting in line at SM North branch.",
                "The scalp massage was heaven! Definitely booking again next month.",
                "Great service from start to finish. Highly recommended!",
                "Haircut was very neat and professional. 5 stars!",
                "Value for money package. Very satisfied with the result."
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
            dbSalon.ProjectFeedbacks.AddRange(newFeedbacks);
            await dbSalon.SaveChangesAsync();
        }

        // 5. Ensure 30+ Leads
        if (await dbSalon.Leads.CountAsync() < 30)
        {
            string[] sources = { "Instagram", "Facebook Ads", "TikTok", "Referral", "Walk-In", "Google Search" };
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
                    Notes = "Interested in Bridal Hair & Makeup package / Balayage promo",
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 90))
                });
            }
            dbSalon.Leads.AddRange(newLeads);
            await dbSalon.SaveChangesAsync();
        }
    }

    // =========================================================================
    // SEED MISTER DONUT (200 Customers, 200 Supply Contracts/Orders, 200 Invoices)
    // =========================================================================
    private static async Task SeedDonut200Async(TenantErpDbContext dbDonut, int companyId)
    {
        int existingCount = await dbDonut.Customers.CountAsync();
        var rng = new Random(202);

        if (existingCount < 200)
        {
            string[] corporateClients = {
                "San Miguel Foods Corp", "SM Megamall Supermarket", "Metro Bakeshop Distribution", "Robinsons Retail Stores",
                "Sunrise Coffee Chain Hub", "Ateneo Campus Canteen", "BGC Tech Park Catering", "Shell Select North Kiosk",
                "Caltex Star Mart Ortigas", "Greenhills Canteen Concessions", "Puregold Supermarket Hub", "7-Eleven Consignment Partner",
                "Mini Stop Bulk Distribution", "UST Student Canteen Services", "Ayala Malls Event Center", "Makati Medical Center Cafe",
                "St. Luke's Hospital Pantry", "Toyota Motors Employee Lounge", "Jollibee Corp Commissary", "San Miguel Brewery Canteen",
                "BDO Unibank Tower Pantry", "Accenture BGC Employee Events", "Convergys Eastwood Cafeteria", "Teleperformance Ortigas Hub",
                "Philippine Airlines Catering", "Globe Telecom Corporate Events", "PLDT Head Office Pantry", "De La Salle Greenhills Canteen",
                "Megaworld Lifestyle Malls", "Filinvest Alabang Corporate Kiosk", "Universal Robina Commissary", "Monde Nissin Event Partners",
                "Golden Arches Employee Hub", "Shopee Philippines Logistics Canteen", "Lazada Pasig Sorting Hub Cafe", "Grab Philippines HQ Pantry",
                "Asian Hospital Wellness Cafe", "Medical City Pasig Food Court", "Unilever BGC Office Pantry", "Nestle Philippines Event Catering"
            };

            string[] filipinoFirst = {
                "Vicente", "Joey", "Vic", "Tito", "Francis", "Eduardo", "Ramon", "Jaime", "Manuel", "Antonio",
                "Carlos", "Felipe", "Lorenzo", "Gabriel", "Mateo", "Enrico", "Dante", "Arthur", "Rolando", "Danilo",
                "Maria", "Teresa", "Lourdes", "Carmela", "Corazon", "Esperanza", "Leticia", "Rosario", "Divina", "Josefina"
            };

            string[] filipinoLast = {
                "Sotto", "De Leon", "Concepcion", "Zobel", "Ayala", "Cojuangco", "Gokongwei", "Sy", "Tan", "Lucio",
                "Pangilinan", "Villar", "Razon", "Aboitiz", "Consunji", "Lopez", "Ortigas", "Araneta", "Tuason", "Roxas"
            };

            string[] areas = { "Greenhills, San Juan", "Ortigas Center, Pasig", "Alabang Town Center", "BGC, Taguig", "Makati CBD", "Quezon City Hub", "Muntinlupa", "Mandaluyong", "Pasay", "Paranaque" };

            var newCustomers = new List<Customer>();
            for (int i = existingCount + 1; i <= 200; i++)
            {
                string fn, ln, email, phone, cType;
                if (i <= 110)
                {
                    // Corporate / Wholesale partners
                    string corp = corporateClients[(i - 1) % corporateClients.Length];
                    fn = corp;
                    ln = "Accounts";
                    email = $"orders.{corp.ToLower().Replace(" ", "").Replace(".", "")}{i}@corp.ph";
                    phone = $"0917{rng.Next(1000000, 9999999)}";
                    cType = (i % 2 == 0) ? "Corporate" : "Wholesale";
                }
                else
                {
                    // Franchisees / VIP retail bulk clients
                    fn = filipinoFirst[rng.Next(filipinoFirst.Length)];
                    ln = filipinoLast[rng.Next(filipinoLast.Length)];
                    email = $"{fn.ToLower()}.{ln.ToLower()}{i}@donutvip.ph";
                    phone = $"0918{rng.Next(1000000, 9999999)}";
                    cType = (i % 3 == 0) ? "Franchisee" : ((i % 2 == 0) ? "VIP" : "Regular");
                }

                string area = areas[rng.Next(areas.Length)];
                newCustomers.Add(new Customer
                {
                    CompanyId = companyId,
                    FirstName = fn,
                    LastName = ln,
                    Email = email,
                    Phone = phone,
                    CustomerType = cType,
                    Address = $"{rng.Next(10, 500)} Commercial Blvd, {area}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(15, 360)),
                    Notes = cType == "Corporate" ? "Recurring weekly delivery contract" : "Preferred branch: Greenhills / Alabang"
                });
            }

            dbDonut.Customers.AddRange(newCustomers);
            await dbDonut.SaveChangesAsync();
        }

        var allCustomers = await dbDonut.Customers.OrderBy(c => c.CustomerId).ToListAsync();

        // 2. Ensure 200 Wholesale Contracts / Catering Projects
        int existingProjects = await dbDonut.Projects.CountAsync();
        if (existingProjects < 200)
        {
            string[] orderTypes = {
                "Bulk Bavarian Box (100 Dozens) Supply",
                "Choco Butternut Holiday Box Bulk Consignment",
                "Corporate Employee Appreciation Donut Tower (300 Pax)",
                "Daily Campus Canteen Fresh Donut Consignment",
                "Supermarket Kiosk Restocking & Display Batch",
                "Smidgets Donut Bites Party Platters (1,500 pcs)",
                "Piping Hot Brewed Coffee & Donut Pairing Breakfast",
                "Custom Logo Branded Corporate Anniversary Donuts",
                "Franchise Store Weekly Bavarian & Choco Restock",
                "Weekend Mall Pop-up Kiosk Donut Supply Batch",
                "School Graduation Donut Treat Boxes (500 units)",
                "Office Monthly Birthday Donut Celebration Package"
            };

            var newProjects = new List<Project>();
            for (int i = existingProjects + 1; i <= 200; i++)
            {
                var cust = allCustomers[(i - 1) % allCustomers.Count];
                string ord = orderTypes[rng.Next(orderTypes.Length)];
                int daysAgo = rng.Next(2, 340);
                DateTime start = DateTime.UtcNow.AddDays(-daysAgo);

                int roll = rng.Next(100);
                string status = roll < 78 ? "Completed" : (roll < 92 ? "In Progress" : "Planning");
                int progress = status == "Completed" ? 100 : (status == "In Progress" ? rng.Next(40, 85) : 20);

                newProjects.Add(new Project
                {
                    CompanyId = companyId,
                    CustomerId = cust.CustomerId,
                    ProjectCode = $"MD-ORD-{i:D4}",
                    ProjectName = $"{cust.FirstName} - {ord}",
                    ProjectType = "Donut Wholesale & Catering",
                    Location = cust.Address.Contains("Greenhills") ? "Greenhills Hub" : "Alabang Hub",
                    Description = $"{ord} for {cust.FirstName}. Account type: {cust.CustomerType}",
                    StartDate = start,
                    TargetEndDate = start.AddDays(rng.Next(1, 5)),
                    Status = status,
                    DesignStage = status == "Completed" ? ProjectDesignStage.Completed : ProjectDesignStage.InProgress,
                    ProgressPercentage = progress,
                    DesignStartDate = start,
                    DesignCompletionDate = status == "Completed" ? start.AddDays(1) : null,
                    IsActive = true,
                    CreatedAt = start.AddDays(-1)
                });
            }

            dbDonut.Projects.AddRange(newProjects);
            await dbDonut.SaveChangesAsync();
        }

        var allProjects = await dbDonut.Projects.OrderBy(p => p.ProjectId).ToListAsync();

        // 3. Ensure 200 Wholesale Invoices / Quotations
        int existingQuotes = await dbDonut.Quotations.CountAsync();
        if (existingQuotes < 200)
        {
            var newQuotes = new List<Quotation>();
            for (int i = existingQuotes + 1; i <= 200; i++)
            {
                var proj = allProjects[(i - 1) % allProjects.Count];
                var cust = allCustomers.FirstOrDefault(c => c.CustomerId == proj.CustomerId) ?? allCustomers[0];

                decimal subtotal = rng.Next(45, 950) * 100m; // 4,500 to 95,000
                decimal discount = rng.Next(5) == 0 ? rng.Next(5, 30) * 100m : 0m;
                decimal total = Math.Max(3500m, subtotal - discount);

                string qStatus = proj.Status == "Completed" ? QuotationStatus.Accepted : (proj.Status == "In Progress" ? QuotationStatus.Accepted : QuotationStatus.Issued);
                string pStatus = proj.Status == "Completed" ? PaymentStatus.FullyPaid : (proj.Status == "In Progress" ? PaymentStatus.DepositReceived : PaymentStatus.Pending);
                decimal paid = pStatus == PaymentStatus.FullyPaid ? total : (pStatus == PaymentStatus.DepositReceived ? Math.Round(total * 0.5m, 2) : 0m);

                newQuotes.Add(new Quotation
                {
                    CompanyId = companyId,
                    QuotationNumber = $"MD-INV-2026-{i:D4}",
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
                    PaymentMethod = pStatus != PaymentStatus.Pending ? (rng.Next(2) == 0 ? "Bank Transfer (BDO)" : "Corporate Check") : "Pending",
                    Notes = $"Wholesale invoice for {proj.ProjectName}",
                    CreatedAt = proj.CreatedAt
                });
            }

            dbDonut.Quotations.AddRange(newQuotes);
            await dbDonut.SaveChangesAsync();
        }

        // 4. Ensure 25+ Promotional Campaigns
        if (await dbDonut.Promotions.CountAsync() < 25)
        {
            var promos = new List<Promotion>
            {
                new() { CompanyId = companyId, Name = "Bavarian Donut Buy 1 Take 1", Code = "MD-BAV2026", OfferType = "Percentage", OfferValue = 50m, TargetSegment = "All Clients", Description = "Buy 1 dozen Bavarian and get 1 dozen free.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-30), ValidUntil = DateTime.UtcNow.AddMonths(3) },
                new() { CompanyId = companyId, Name = "20% Off Bavarian Box of 12", Code = "MD-DOZEN20", OfferType = "Percentage", OfferValue = 20m, TargetSegment = "Loyal", Description = "Exclusive 20% discount coupon for loyal customers.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-20), ValidUntil = DateTime.UtcNow.AddMonths(2) },
                new() { CompanyId = companyId, Name = "Free Brewed Coffee with 6 Donuts", Code = "MD-FREECOFFEE", OfferType = "FreeService", OfferValue = 85m, TargetSegment = "Champion", Description = "Complimentary piping hot brewed coffee on every half-dozen box.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-15), ValidUntil = DateTime.UtcNow.AddMonths(3) },
                new() { CompanyId = companyId, Name = "Weekend Donut Party Pack ₱150 Off", Code = "MD-PARTY150", OfferType = "FixedAmount", OfferValue = 150m, TargetSegment = "At Risk", Description = "Re-engagement offer for dormant accounts: ₱150 off on Party Pack.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-10), ValidUntil = DateTime.UtcNow.AddMonths(1) },
                new() { CompanyId = companyId, Name = "Corporate Catering 15% Rebate", Code = "MD-CORP15", OfferType = "Percentage", OfferValue = 15m, TargetSegment = "Champion", Description = "15% rebate on all bulk corporate catering exceeding ₱20,000.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-40), ValidUntil = DateTime.UtcNow.AddMonths(6) },
                new() { CompanyId = companyId, Name = "Choco Butternut Holiday Box ₱200 Off", Code = "MD-CHOCO200", OfferType = "FixedAmount", OfferValue = 200m, TargetSegment = "Loyal", Description = "₱200 voucher on holiday premium Choco Butternut tins.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-5), ValidUntil = DateTime.UtcNow.AddMonths(2) },
                new() { CompanyId = companyId, Name = "Smidgets Bite-Sized Party 25% Off", Code = "MD-SMIDGETS25", OfferType = "Percentage", OfferValue = 25m, TargetSegment = "Promising", Description = "25% discount on 100-pc Smidgets party buckets.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-12), ValidUntil = DateTime.UtcNow.AddMonths(2) },
                new() { CompanyId = companyId, Name = "Campus Wholesale Special ₱300 Off", Code = "MD-CAMPUS300", OfferType = "FixedAmount", OfferValue = 300m, TargetSegment = "All Clients", Description = "₱300 subsidy on campus canteen wholesale consignments.", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-25), ValidUntil = DateTime.UtcNow.AddMonths(4) }
            };
            dbDonut.Promotions.AddRange(promos);
            await dbDonut.SaveChangesAsync();
        }

        // 5. Ensure 60+ Customer Retention Actions (RFM Segments)
        if (await dbDonut.RetentionActions.CountAsync() < 60)
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
                    OfferValue = (i % 2 == 0) ? 200m : 15m,
                    OfferDescription = $"Exclusive {seg} client reward: " + ((i % 2 == 0) ? "₱200 voucher" : "15% discount on bulk re-order"),
                    Notes = $"Retention initiative for {cust.FirstName}. Status: {st}",
                    Status = st,
                    CreatedByUserId = "System",
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 120))
                });
            }

            dbDonut.RetentionActions.AddRange(newRetentions);
            await dbDonut.SaveChangesAsync();
        }
    }
}
