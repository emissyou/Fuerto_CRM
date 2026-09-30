using CRM.domain.Entities;
using CRM.infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.infrastructure.Services;

public interface ITenantDatabaseSyncService
{
    Task SyncAllTenantsAsync(CancellationToken ct = default);
    Task SyncCompanyAsync(int companyId, CancellationToken ct = default);
    Task MirrorCustomerAsync(Customer customer, CancellationToken ct = default);
    Task MirrorProjectAsync(Project project, CancellationToken ct = default);
    Task MirrorLeadAsync(Lead lead, CancellationToken ct = default);
    Task MirrorQuotationAsync(Quotation quotation, CancellationToken ct = default);
    Task MirrorActivityAsync(Activity activity, CancellationToken ct = default);
}

public class TenantDatabaseSyncService : ITenantDatabaseSyncService
{
    private readonly ITenantDbContextFactory _factory;
    private readonly ILogger<TenantDatabaseSyncService> _logger;
    private static readonly SemaphoreSlim _syncLock = new(1, 1);

    public TenantDatabaseSyncService(
        ITenantDbContextFactory factory,
        ILogger<TenantDatabaseSyncService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task SyncAllTenantsAsync(CancellationToken ct = default)
    {
        if (!TenantDbContextFactory.IsCloudReachable) return;

        if (!await _syncLock.WaitAsync(0, ct))
        {
            // Another sync is currently running, skip
            return;
        }

        try
        {
            int[] companyIds = { 1, 2, 3 };
            foreach (var cid in companyIds)
            {
                if (ct.IsCancellationRequested) break;
                await SyncCompanyAsync(cid, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during full tenant database synchronization.");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task SyncCompanyAsync(int companyId, CancellationToken ct = default)
    {
        try
        {
            await using var cloudDb = await _factory.CreateCloudAsync(companyId);
            if (cloudDb == null) return;

            await using var localDb = await _factory.CreateLocalAsync(companyId);
            if (localDb == null) return;

            await SyncCustomersInternalAsync(companyId, cloudDb, localDb, ct);
            await SyncLeadsInternalAsync(companyId, cloudDb, localDb, ct);
            await SyncProjectsInternalAsync(companyId, cloudDb, localDb, ct);
            await SyncQuotationsInternalAsync(companyId, cloudDb, localDb, ct);
            await SyncActivitiesInternalAsync(companyId, cloudDb, localDb, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SyncCompanyAsync for Company {CompanyId} encountered an error: {Message}", companyId, ex.Message);
        }
    }

    // =========================================================================
    // 1. CUSTOMERS SYNC
    // =========================================================================
    private async Task SyncCustomersInternalAsync(int companyId, TenantErpDbContext cloudDb, TenantErpDbContext localDb, CancellationToken ct)
    {
        var cloudCusts = await cloudDb.Customers.AsNoTracking().Where(c => c.CompanyId == companyId).ToListAsync(ct);
        var localCusts = await localDb.Customers.AsNoTracking().Where(c => c.CompanyId == companyId).ToListAsync(ct);

        var localMap = localCusts.ToDictionary(c => c.CustomerId);
        var cloudMap = cloudCusts.ToDictionary(c => c.CustomerId);

        // 1a. Missing in Local -> Copy from Cloud to Local
        var missingInLocal = cloudCusts.Where(c => !localMap.ContainsKey(c.CustomerId)).ToList();
        foreach (var c in missingInLocal)
        {
            await InsertCustomerWithIdentityAsync(localDb, c);
        }

        // 1b. Missing in Cloud -> Copy from Local to Cloud
        var missingInCloud = localCusts.Where(c => !cloudMap.ContainsKey(c.CustomerId)).ToList();
        foreach (var c in missingInCloud)
        {
            await InsertCustomerWithIdentityAsync(cloudDb, c);
        }
    }

    private static async Task InsertCustomerWithIdentityAsync(TenantErpDbContext targetDb, Customer c)
    {
        try
        {
            await targetDb.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT Customers ON; " +
                "INSERT INTO Customers (CompanyId, CustomerId, FirstName, LastName, Email, Phone, Address, CustomerType, IsActive, CreatedAt, Notes) " +
                "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}); " +
                "SET IDENTITY_INSERT Customers OFF;",
                c.CompanyId, c.CustomerId, c.FirstName, c.LastName, c.Email ?? string.Empty, c.Phone ?? string.Empty,
                c.Address ?? string.Empty, c.CustomerType, c.IsActive, c.CreatedAt, c.Notes ?? string.Empty);
        }
        catch { /* Ignore duplicate or constraint conflicts */ }
    }

    // =========================================================================
    // 2. LEADS SYNC
    // =========================================================================
    private async Task SyncLeadsInternalAsync(int companyId, TenantErpDbContext cloudDb, TenantErpDbContext localDb, CancellationToken ct)
    {
        var cloudLeads = await cloudDb.Leads.AsNoTracking().Where(l => l.CompanyId == companyId).ToListAsync(ct);
        var localLeads = await localDb.Leads.AsNoTracking().Where(l => l.CompanyId == companyId).ToListAsync(ct);

        var localMap = localLeads.ToDictionary(l => l.LeadId);
        var cloudMap = cloudLeads.ToDictionary(l => l.LeadId);

        foreach (var l in cloudLeads.Where(l => !localMap.ContainsKey(l.LeadId)))
        {
            try
            {
                await localDb.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Leads ON; " +
                    "INSERT INTO Leads (CompanyId, LeadId, FirstName, LastName, Email, Phone, Status, LeadSource, Notes, CreatedAt) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}); " +
                    "SET IDENTITY_INSERT Leads OFF;",
                    l.CompanyId, l.LeadId, l.FirstName, l.LastName, l.Email ?? string.Empty, l.Phone ?? string.Empty,
                    l.Status, l.LeadSource, l.Notes ?? string.Empty, l.CreatedAt);
            }
            catch { }
        }

        foreach (var l in localLeads.Where(l => !cloudMap.ContainsKey(l.LeadId)))
        {
            try
            {
                await cloudDb.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Leads ON; " +
                    "INSERT INTO Leads (CompanyId, LeadId, FirstName, LastName, Email, Phone, Status, LeadSource, Notes, CreatedAt) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}); " +
                    "SET IDENTITY_INSERT Leads OFF;",
                    l.CompanyId, l.LeadId, l.FirstName, l.LastName, l.Email ?? string.Empty, l.Phone ?? string.Empty,
                    l.Status, l.LeadSource, l.Notes ?? string.Empty, l.CreatedAt);
            }
            catch { }
        }
    }

    // =========================================================================
    // 3. PROJECTS SYNC
    // =========================================================================
    private async Task SyncProjectsInternalAsync(int companyId, TenantErpDbContext cloudDb, TenantErpDbContext localDb, CancellationToken ct)
    {
        var cloudProjects = await cloudDb.Projects.AsNoTracking().Where(p => p.CompanyId == companyId).ToListAsync(ct);
        var localProjects = await localDb.Projects.AsNoTracking().Where(p => p.CompanyId == companyId).ToListAsync(ct);

        var localMap = localProjects.ToDictionary(p => p.ProjectId);
        var cloudMap = cloudProjects.ToDictionary(p => p.ProjectId);

        foreach (var p in cloudProjects.Where(p => !localMap.ContainsKey(p.ProjectId)))
        {
            try
            {
                await localDb.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Projects ON; " +
                    "INSERT INTO Projects (CompanyId, ProjectId, ProjectName, CustomerId, ProjectType, Status, DesignStage, ProgressPercentage, CreatedAt) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}); " +
                    "SET IDENTITY_INSERT Projects OFF;",
                    p.CompanyId, p.ProjectId, p.ProjectName, p.CustomerId, p.ProjectType ?? string.Empty, p.Status ?? string.Empty, p.DesignStage ?? string.Empty, p.ProgressPercentage, p.CreatedAt);
            }
            catch { }
        }

        foreach (var p in localProjects.Where(p => !cloudMap.ContainsKey(p.ProjectId)))
        {
            try
            {
                await cloudDb.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Projects ON; " +
                    "INSERT INTO Projects (CompanyId, ProjectId, ProjectName, CustomerId, ProjectType, Status, DesignStage, ProgressPercentage, CreatedAt) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}); " +
                    "SET IDENTITY_INSERT Projects OFF;",
                    p.CompanyId, p.ProjectId, p.ProjectName, p.CustomerId, p.ProjectType ?? string.Empty, p.Status ?? string.Empty, p.DesignStage ?? string.Empty, p.ProgressPercentage, p.CreatedAt);
            }
            catch { }
        }
    }

    // =========================================================================
    // 4. QUOTATIONS SYNC
    // =========================================================================
    private async Task SyncQuotationsInternalAsync(int companyId, TenantErpDbContext cloudDb, TenantErpDbContext localDb, CancellationToken ct)
    {
        var cloudQuotes = await cloudDb.Quotations.AsNoTracking().Where(q => q.CompanyId == companyId).ToListAsync(ct);
        var localQuotes = await localDb.Quotations.AsNoTracking().Where(q => q.CompanyId == companyId).ToListAsync(ct);

        var localMap = localQuotes.ToDictionary(q => q.QuotationId);
        var cloudMap = cloudQuotes.ToDictionary(q => q.QuotationId);

        foreach (var q in cloudQuotes.Where(q => !localMap.ContainsKey(q.QuotationId)))
        {
            try
            {
                await localDb.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Quotations ON; " +
                    "INSERT INTO Quotations (CompanyId, QuotationId, QuotationNumber, ProjectId, CustomerId, QuotationDate, Subtotal, Discount, TotalAmount, Status, AmountPaid, DepositRequired, PaymentStatus, CreatedAt, ApprovalStatus) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}); " +
                    "SET IDENTITY_INSERT Quotations OFF;",
                    q.CompanyId, q.QuotationId, q.QuotationNumber, q.ProjectId, q.CustomerId, q.QuotationDate, q.Subtotal, q.Discount, q.TotalAmount, q.Status, q.AmountPaid, q.DepositRequired, q.PaymentStatus, q.CreatedAt, q.ApprovalStatus);
            }
            catch { }
        }

        foreach (var q in localQuotes.Where(q => !cloudMap.ContainsKey(q.QuotationId)))
        {
            try
            {
                await cloudDb.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Quotations ON; " +
                    "INSERT INTO Quotations (CompanyId, QuotationId, QuotationNumber, ProjectId, CustomerId, QuotationDate, Subtotal, Discount, TotalAmount, Status, AmountPaid, DepositRequired, PaymentStatus, CreatedAt, ApprovalStatus) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}); " +
                    "SET IDENTITY_INSERT Quotations OFF;",
                    q.CompanyId, q.QuotationId, q.QuotationNumber, q.ProjectId, q.CustomerId, q.QuotationDate, q.Subtotal, q.Discount, q.TotalAmount, q.Status, q.AmountPaid, q.DepositRequired, q.PaymentStatus, q.CreatedAt, q.ApprovalStatus);
            }
            catch { }
        }
    }

    // =========================================================================
    // 5. ACTIVITIES SYNC
    // =========================================================================
    private async Task SyncActivitiesInternalAsync(int companyId, TenantErpDbContext cloudDb, TenantErpDbContext localDb, CancellationToken ct)
    {
        var cloudActs = await cloudDb.Activities.AsNoTracking().Where(a => a.CompanyId == companyId).ToListAsync(ct);
        var localActs = await localDb.Activities.AsNoTracking().Where(a => a.CompanyId == companyId).ToListAsync(ct);

        var localMap = localActs.ToDictionary(a => a.ActivityId);
        var cloudMap = cloudActs.ToDictionary(a => a.ActivityId);

        foreach (var a in cloudActs.Where(a => !localMap.ContainsKey(a.ActivityId)))
        {
            try
            {
                await localDb.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Activities ON; " +
                    "INSERT INTO Activities (CompanyId, ActivityId, CustomerId, ProjectId, ActivityType, Subject, Description, ActivityDate, Status, Notes) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}); " +
                    "SET IDENTITY_INSERT Activities OFF;",
                    a.CompanyId, a.ActivityId, a.CustomerId, a.ProjectId, a.ActivityType ?? "Other", a.Subject ?? string.Empty, a.Description ?? string.Empty, a.ActivityDate, a.Status ?? "Completed", a.Notes ?? string.Empty);
            }
            catch { }
        }

        foreach (var a in localActs.Where(a => !cloudMap.ContainsKey(a.ActivityId)))
        {
            try
            {
                await cloudDb.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Activities ON; " +
                    "INSERT INTO Activities (CompanyId, ActivityId, CustomerId, ProjectId, ActivityType, Subject, Description, ActivityDate, Status, Notes) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}); " +
                    "SET IDENTITY_INSERT Activities OFF;",
                    a.CompanyId, a.ActivityId, a.CustomerId, a.ProjectId, a.ActivityType ?? "Other", a.Subject ?? string.Empty, a.Description ?? string.Empty, a.ActivityDate, a.Status ?? "Completed", a.Notes ?? string.Empty);
            }
            catch { }
        }
    }

    // =========================================================================
    // REAL-TIME INSTANT MIRRORS (Dual-Write at save time)
    // =========================================================================
    public async Task MirrorCustomerAsync(Customer c, CancellationToken ct = default)
    {
        // Try writing to both Local and Cloud
        await MirrorToAlternateDbAsync(c.CompanyId, async db =>
        {
            var exists = await db.Customers.AnyAsync(x => x.CustomerId == c.CustomerId, ct);
            if (!exists)
            {
                await InsertCustomerWithIdentityAsync(db, c);
            }
            else
            {
                var existing = await db.Customers.FirstOrDefaultAsync(x => x.CustomerId == c.CustomerId, ct);
                if (existing != null)
                {
                    existing.FirstName = c.FirstName;
                    existing.LastName = c.LastName;
                    existing.Email = c.Email;
                    existing.Phone = c.Phone;
                    existing.Address = c.Address;
                    existing.CustomerType = c.CustomerType;
                    existing.IsActive = c.IsActive;
                    existing.Notes = c.Notes;
                    await db.SaveChangesAsync(ct);
                }
            }
        });
    }

    public async Task MirrorProjectAsync(Project p, CancellationToken ct = default)
    {
        await MirrorToAlternateDbAsync(p.CompanyId, async db =>
        {
            var exists = await db.Projects.AnyAsync(x => x.ProjectId == p.ProjectId, ct);
            if (!exists)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Projects ON; " +
                    "INSERT INTO Projects (CompanyId, ProjectId, ProjectName, CustomerId, ProjectType, Status, DesignStage, ProgressPercentage, CreatedAt) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}); " +
                    "SET IDENTITY_INSERT Projects OFF;",
                    p.CompanyId, p.ProjectId, p.ProjectName, p.CustomerId, p.ProjectType ?? string.Empty, p.Status ?? string.Empty, p.DesignStage ?? string.Empty, p.ProgressPercentage, p.CreatedAt);
            }
            else
            {
                var existing = await db.Projects.FirstOrDefaultAsync(x => x.ProjectId == p.ProjectId, ct);
                if (existing != null)
                {
                    existing.ProjectName = p.ProjectName;
                    existing.Status = p.Status;
                    existing.DesignStage = p.DesignStage;
                    existing.ProgressPercentage = p.ProgressPercentage;
                    await db.SaveChangesAsync(ct);
                }
            }
        });
    }

    public async Task MirrorLeadAsync(Lead l, CancellationToken ct = default)
    {
        await MirrorToAlternateDbAsync(l.CompanyId, async db =>
        {
            var exists = await db.Leads.AnyAsync(x => x.LeadId == l.LeadId, ct);
            if (!exists)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Leads ON; " +
                    "INSERT INTO Leads (CompanyId, LeadId, FirstName, LastName, Email, Phone, Status, LeadSource, Notes, CreatedAt) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}); " +
                    "SET IDENTITY_INSERT Leads OFF;",
                    l.CompanyId, l.LeadId, l.FirstName, l.LastName, l.Email ?? string.Empty, l.Phone ?? string.Empty,
                    l.Status, l.LeadSource, l.Notes ?? string.Empty, l.CreatedAt);
            }
            else
            {
                var existing = await db.Leads.FirstOrDefaultAsync(x => x.LeadId == l.LeadId, ct);
                if (existing != null)
                {
                    existing.FirstName = l.FirstName;
                    existing.LastName = l.LastName;
                    existing.Email = l.Email;
                    existing.Phone = l.Phone;
                    existing.Status = l.Status;
                    existing.Notes = l.Notes;
                    await db.SaveChangesAsync(ct);
                }
            }
        });
    }

    public async Task MirrorQuotationAsync(Quotation q, CancellationToken ct = default)
    {
        await MirrorToAlternateDbAsync(q.CompanyId, async db =>
        {
            var exists = await db.Quotations.AnyAsync(x => x.QuotationId == q.QuotationId, ct);
            if (!exists)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Quotations ON; " +
                    "INSERT INTO Quotations (CompanyId, QuotationId, QuotationNumber, ProjectId, CustomerId, QuotationDate, Subtotal, Discount, TotalAmount, Status, AmountPaid, DepositRequired, PaymentStatus, CreatedAt, ApprovalStatus) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}); " +
                    "SET IDENTITY_INSERT Quotations OFF;",
                    q.CompanyId, q.QuotationId, q.QuotationNumber, q.ProjectId, q.CustomerId, q.QuotationDate, q.Subtotal, q.Discount, q.TotalAmount, q.Status, q.AmountPaid, q.DepositRequired, q.PaymentStatus, q.CreatedAt, q.ApprovalStatus);
            }
            else
            {
                var existing = await db.Quotations.FirstOrDefaultAsync(x => x.QuotationId == q.QuotationId, ct);
                if (existing != null)
                {
                    existing.Status = q.Status;
                    existing.AmountPaid = q.AmountPaid;
                    existing.PaymentStatus = q.PaymentStatus;
                    existing.ApprovalStatus = q.ApprovalStatus;
                    await db.SaveChangesAsync(ct);
                }
            }
        });
    }

    public async Task MirrorActivityAsync(Activity a, CancellationToken ct = default)
    {
        await MirrorToAlternateDbAsync(a.CompanyId, async db =>
        {
            var exists = await db.Activities.AnyAsync(x => x.ActivityId == a.ActivityId, ct);
            if (!exists)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT Activities ON; " +
                    "INSERT INTO Activities (CompanyId, ActivityId, CustomerId, ProjectId, ActivityType, Subject, Description, ActivityDate, Status, Notes) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}); " +
                    "SET IDENTITY_INSERT Activities OFF;",
                    a.CompanyId, a.ActivityId, a.CustomerId, a.ProjectId, a.ActivityType ?? "Other", a.Subject ?? string.Empty, a.Description ?? string.Empty, a.ActivityDate, a.Status ?? "Completed", a.Notes ?? string.Empty);
            }
        });
    }

    private async Task MirrorToAlternateDbAsync(int companyId, Func<TenantErpDbContext, Task> action)
    {
        // 1. Mirror to LocalDB
        try
        {
            await using var localDb = await _factory.CreateLocalAsync(companyId);
            if (localDb != null)
            {
                await action(localDb);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Mirror to LocalDB for Company {CompanyId} skipped: {Message}", companyId, ex.Message);
        }

        // 2. Mirror to Cloud DB if reachable
        if (TenantDbContextFactory.IsCloudReachable)
        {
            try
            {
                await using var cloudDb = await _factory.CreateCloudAsync(companyId);
                if (cloudDb != null)
                {
                    await action(cloudDb);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Mirror to Cloud DB for Company {CompanyId} skipped: {Message}", companyId, ex.Message);
            }
        }
    }
}
