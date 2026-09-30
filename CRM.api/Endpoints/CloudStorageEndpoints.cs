using CRM.domain.Models;
using CRM.infrastructure.Services;

namespace CRM.api.Endpoints;

public static class CloudStorageEndpoints
{
    public record StoreFileRequest(string TenantCode, string Category, string FileName, string Base64Content);

    public static void MapCloudStorageEndpoints(this WebApplication app)
    {
        // 1. GET STORAGE STATUS & METRICS
        app.MapGet("/cloud/status", async (IHybridStorageService storage) =>
        {
            var status = await storage.GetStorageStatusAsync();
            return Results.Ok(status);
        })
        .RequireAuthorization();

        // 2. TRIGGER SYNC FOR A SINGLE TENANT (Local DB ➔ Cloud Vault)
        app.MapPost("/cloud/sync/{companyId:int}", async (int companyId, IHybridStorageService storage, ITenantDatabaseSyncService dbSync) =>
        {
            try { await dbSync.SyncCompanyAsync(companyId); } catch { }
            var result = await storage.SyncTenantToCloudAsync(companyId);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization();

        // 3. TRIGGER SYNC FOR ALL ACTIVE TENANTS
        app.MapPost("/cloud/sync-all", async (IHybridStorageService storage, ITenantDatabaseSyncService dbSync) =>
        {
            try { await dbSync.SyncAllTenantsAsync(); } catch { }
            var results = await storage.SyncAllTenantsToCloudAsync();
            return Results.Ok(new
            {
                message = "Local-to-Cloud synchronization triggered for all active tenants.",
                totalTenants = results.Count,
                successful = results.Count(r => r.Success),
                details = results
            });
        })
        .RequireAuthorization();

        // 4. TRIGGER FULL PLATFORM BACKUP (Local First ➔ Cloud Vault)
        app.MapPost("/cloud/backup", async (IHybridStorageService storage) =>
        {
            var result = await storage.BackupAllToCloudAsync();
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Super Admin"));

        // 5. LIST HYBRID STORED FILES
        app.MapGet("/cloud/files", async (string? tenant, string? category, IHybridStorageService storage) =>
        {
            var files = await storage.ListStoredFilesAsync(tenant, category);
            return Results.Ok(files);
        })
        .RequireAuthorization();

        // 6. STORE FILE VIA LOCAL-THEN-CLOUD PIPELINE
        app.MapPost("/cloud/store", async (StoreFileRequest req, IHybridStorageService storage) =>
        {
            if (string.IsNullOrWhiteSpace(req.FileName) || string.IsNullOrWhiteSpace(req.Base64Content))
            {
                return Results.BadRequest(new { message = "FileName and Base64Content are required." });
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(req.Base64Content);
                var result = await storage.StoreAsync(req.TenantCode, req.Category, req.FileName, bytes);
                return Results.Ok(result);
            }
            catch (FormatException)
            {
                return Results.BadRequest(new { message = "Invalid Base64 payload." });
            }
        })
        .RequireAuthorization();

        // 7. RETRIEVE FILE (Local First, Falling back to Cloud)
        app.MapGet("/cloud/retrieve/{tenantCode}/{category}/{fileName}", async (
            string tenantCode,
            string category,
            string fileName,
            IHybridStorageService storage) =>
        {
            var bytes = await storage.RetrieveAsync(tenantCode, category, fileName);
            if (bytes == null || bytes.Length == 0)
            {
                return Results.NotFound(new { message = $"File '{fileName}' not found in local or cloud storage." });
            }

            return Results.File(bytes, "application/octet-stream", fileDownloadName: fileName);
        })
        .RequireAuthorization();
    }
}
