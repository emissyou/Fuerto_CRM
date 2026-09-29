namespace CRM.domain.Models;

/// <summary>
/// Defines the storage strategy for enterprise data and documents.
/// </summary>
public enum HybridStorageMode
{
    /// <summary>
    /// Stores data locally first for high performance and offline durability,
    /// then asynchronously or synchronously replicates to the cloud.
    /// </summary>
    LocalThenCloud,

    /// <summary>
    /// Stores only on local storage/local database.
    /// </summary>
    LocalOnly,

    /// <summary>
    /// Stores directly into the remote cloud vault.
    /// </summary>
    CloudOnly
}

/// <summary>
/// Result of storing a document, file, or export in the hybrid vault.
/// </summary>
public class CloudStoreResult
{
    public string FileId { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string TenantCode { get; set; } = "SYSTEM";
    public string LocalPath { get; set; } = string.Empty;
    public string CloudUri { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public bool StoredLocally { get; set; }
    public bool StoredInCloud { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public DateTime StoredAtUtc { get; set; } = DateTime.UtcNow;
    public string StorageMode { get; set; } = "LocalThenCloud";
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Status of the hybrid Local-then-Cloud storage infrastructure.
/// </summary>
public class CloudSyncStatus
{
    public string StorageMode { get; set; } = "Local then Cloud (Dual Storage)";
    public bool IsLocalAvailable { get; set; } = true;
    public bool IsCloudAvailable { get; set; } = true;
    public string LocalStoragePath { get; set; } = string.Empty;
    public string CloudStorageEndpoint { get; set; } = string.Empty;
    public string CloudContainer { get; set; } = "fuerto-cloud-vault";
    public DateTime? LastSyncedAtUtc { get; set; }
    public int TotalSyncedEntities { get; set; }
    public int PendingUploads { get; set; }
    public string OverallHealth { get; set; } = "Healthy - Local & Cloud Synchronized";
    public List<TenantCloudSyncDetail> Tenants { get; set; } = new();
}

/// <summary>
/// Tenant-level sync status and entity counts between local and cloud.
/// </summary>
public class TenantCloudSyncDetail
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string LocalDatabase { get; set; } = string.Empty;
    public string CloudStorageVault { get; set; } = string.Empty;
    public int CustomersCount { get; set; }
    public int ProjectsCount { get; set; }
    public int QuotationsCount { get; set; }
    public int LeadsCount { get; set; }
    public int TotalEntitiesCount { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }
    public string SyncStatus { get; set; } = "In Sync";
    public string StoragePipeline { get; set; } = "Local ➔ Cloud Replicated";
}

/// <summary>
/// Outcome of a tenant data sync operation.
/// </summary>
public class CloudSyncResult
{
    public bool Success { get; set; }
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public int RecordsProcessed { get; set; }
    public string CloudVaultPath { get; set; } = string.Empty;
    public string LocalSnapshotPath { get; set; } = string.Empty;
    public string ChecksumSha256 { get; set; } = string.Empty;
    public DateTime SyncedAtUtc { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Outcome of a complete system cloud backup.
/// </summary>
public class CloudBackupResult
{
    public bool Success { get; set; }
    public string BackupFileName { get; set; } = string.Empty;
    public string LocalBackupPath { get; set; } = string.Empty;
    public string CloudBackupUri { get; set; } = string.Empty;
    public long BackupSizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ChecksumSha256 { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
