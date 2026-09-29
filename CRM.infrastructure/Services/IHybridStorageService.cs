using CRM.domain.Models;

namespace CRM.infrastructure.Services;

/// <summary>
/// Service managing dual-tier "Local then Cloud" storage for data, documents, and backups.
/// </summary>
public interface IHybridStorageService
{
    /// <summary>
    /// Stores content locally first for instant speed and offline resilience,
    /// then replicates/uploads to cloud storage.
    /// </summary>
    Task<CloudStoreResult> StoreAsync(string tenantCode, string category, string fileName, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Stores byte array content locally first, then replicates to cloud storage.
    /// </summary>
    Task<CloudStoreResult> StoreAsync(string tenantCode, string category, string fileName, byte[] content, CancellationToken ct = default);

    /// <summary>
    /// Retrieves content: checks Local storage first, falling back to Cloud storage if local is absent.
    /// </summary>
    Task<byte[]?> RetrieveAsync(string tenantCode, string category, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Synchronizes a tenant's complete dataset from the Local database into the Cloud storage vault/database.
    /// </summary>
    Task<CloudSyncResult> SyncTenantToCloudAsync(int companyId, CancellationToken ct = default);

    /// <summary>
    /// Synchronizes all active tenant datasets from Local to Cloud.
    /// </summary>
    Task<List<CloudSyncResult>> SyncAllTenantsToCloudAsync(CancellationToken ct = default);

    /// <summary>
    /// Performs a full system database and storage backup: stores locally, then uploads to cloud vault.
    /// </summary>
    Task<CloudBackupResult> BackupAllToCloudAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the current dual-tier storage status, connectivity, and tenant sync metrics.
    /// </summary>
    Task<CloudSyncStatus> GetStorageStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Lists all stored files and documents across local and cloud tiers.
    /// </summary>
    Task<List<CloudStoreResult>> ListStoredFilesAsync(string? tenantCode = null, string? category = null, CancellationToken ct = default);
}
