using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Model for a queued offline modification waiting to be synced to the cloud.
/// </summary>
public class OfflineSyncItem
{
    public string QueueId { get; set; } = Guid.NewGuid().ToString("N");
    public int CompanyId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST"; // POST, PUT, DELETE
    public string? RecordId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; } = 0;
}

/// <summary>
/// Cached credentials for offline login verification.
/// </summary>
public class OfflineAuthCache
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Token { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyCode { get; set; }
    public string? AvailedModules { get; set; }
    public string? SubscriptionStatus { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool HasAcceptedTerms { get; set; } = false;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Dual-Storage Offline Engine & Cloud Synchronization Manager:
/// - Provides instant local read/write capabilities when disconnected from internet or API.
/// - Automatically detects online connectivity restoration via background heartbeat.
/// - Automatically flushes pending queue and syncs local updates to Cloud API.
/// - Maintains full cached datasets for all modules locally.
/// </summary>
public static class OfflineSyncManager
{
    private static HttpClient? _http;
    private static string _apiUrl = "http://127.0.0.1:5068";
    private static System.Windows.Forms.Timer? _heartbeatTimer;
    private static bool _isSyncing = false;
    private static readonly object _lock = new();

    public static bool IsOnline { get; private set; } = true;
    public static int PendingSyncCount => GetQueueCount();

    public static event EventHandler<bool>? ConnectivityChanged;
    public static event EventHandler<int>? PendingQueueChanged;
    public static event EventHandler<string>? SyncStatusChanged;
    public static event EventHandler? SyncCompleted;

    private static string BaseStoragePath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "OfflineStorage");

    public static void Initialize(HttpClient httpClient, string apiUrl)
    {
        _http = httpClient;
        _apiUrl = apiUrl.TrimEnd('/');

        Directory.CreateDirectory(BaseStoragePath);

        // Instant network availability change detection (WiFi toggle off/on)
        System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += (s, e) =>
        {
            if (!e.IsAvailable)
            {
                SetOnlineStatus(false);
            }
            else
            {
                _ = Task.Run(async () => await CheckConnectivityAndSyncAsync());
            }
        };

        System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += (s, e) =>
        {
            bool hasNet = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (!hasNet)
            {
                SetOnlineStatus(false);
            }
            else
            {
                _ = Task.Run(async () => await CheckConnectivityAndSyncAsync());
            }
        };

        if (_heartbeatTimer == null)
        {
            _heartbeatTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000 // check every 3 seconds
            };
            _heartbeatTimer.Tick += async (_, _) =>
            {
                try { await CheckConnectivityAndSyncAsync(); } catch { }
            };
            _heartbeatTimer.Start();
        }

        // Trigger immediate connectivity check
        _ = Task.Run(async () =>
        {
            try { await CheckConnectivityAndSyncAsync(); } catch { }
        });
    }

    // =========================================================
    // 1. CONNECTIVITY DETECTION & AUTO-SYNC TRIGGER
    // =========================================================

    public static async Task<bool> CheckConnectivityAsync()
    {
        // 1. Instant local hardware check (if no active network adapter, instantly offline)
        if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            SetOnlineStatus(false);
            return false;
        }

        if (_http == null) return false;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var resp = await _http.GetAsync($"{_apiUrl}/health", cts.Token);

            if (resp.IsSuccessStatusCode)
            {
                // Check cloudConnected flag — if false, local API is up but cloud DB is offline
                try
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("cloudConnected", out var cloudProp))
                    {
                        bool cloudOnline = cloudProp.GetBoolean();
                        SetOnlineStatus(cloudOnline);
                        return cloudOnline;
                    }
                }
                catch { /* ignore parse failures, fall through to true */ }

                SetOnlineStatus(true);
                return true;
            }

            SetOnlineStatus(false);
            return false;
        }
        catch
        {
            SetOnlineStatus(false);
            return false;
        }
    }

    private static void SetOnlineStatus(bool online)
    {
        if (IsOnline != online)
        {
            IsOnline = online;
            ConnectivityChanged?.Invoke(null, online);
            SyncStatusChanged?.Invoke(null, online ? "🟢 Online · Cloud Connected" : "🟡 Offline Mode · Changes Stored Locally");
        }
    }

    public static async Task CheckConnectivityAndSyncAsync()
    {
        bool online = await CheckConnectivityAsync();
        if (online && PendingSyncCount > 0 && !_isSyncing)
        {
            await ProcessPendingQueueAsync();
        }
    }

    // =========================================================
    // 2. LOCAL CACHING (OFFLINE DATA STORAGE)
    // =========================================================

    private static string GetCompanyFolder(int companyId)
    {
        var path = Path.Combine(BaseStoragePath, companyId.ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    public static void SaveCache(int companyId, string endpoint, List<JsonElement> records)
    {
        try
        {
            var folder = GetCompanyFolder(companyId);
            var file = Path.Combine(folder, $"{endpoint.ToLowerInvariant()}.json");
            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            lock (_lock)
            {
                File.WriteAllText(file, json);
            }
        }
        catch { }
    }

    public static List<JsonElement>? LoadCache(int companyId, string endpoint)
    {
        try
        {
            var folder = GetCompanyFolder(companyId);
            var file = Path.Combine(folder, $"{endpoint.ToLowerInvariant()}.json");
            if (!File.Exists(file)) return null;

            string json;
            lock (_lock)
            {
                json = File.ReadAllText(file);
            }

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
            }
        }
        catch { }
        return null;
    }

    // =========================================================
    // 3. OFFLINE RECORD MODIFICATIONS (LOCAL IMMEDIATE WRITE)
    // =========================================================

    public static JsonElement ApplyOfflineCreate(int companyId, string endpoint, Dictionary<string, object?> body, string description)
    {
        var existing = LoadCache(companyId, endpoint) ?? new List<JsonElement>();

        // Generate temporary offline integer ID
        int tempId = Math.Abs((int)(DateTime.UtcNow.Ticks % 900000) + 100000);
        string idProp = GetPrimaryIdPropertyName(endpoint);

        body[idProp] = tempId;
        body["Id"] = tempId;
        body["isOfflinePending"] = true;
        body["offlineCreatedDate"] = DateTime.UtcNow.ToString("o");

        var recordJson = JsonSerializer.Serialize(body);
        using var doc = JsonDocument.Parse(recordJson);
        var newElement = doc.RootElement.Clone();

        existing.Insert(0, newElement);
        SaveCache(companyId, endpoint, existing);

        // Enqueue sync operation
        EnqueueSyncItem(new OfflineSyncItem
        {
            CompanyId = companyId,
            Endpoint = endpoint,
            HttpMethod = "POST",
            RecordId = tempId.ToString(),
            PayloadJson = recordJson,
            Description = $"Create {endpoint}: {description}"
        });

        return newElement;
    }

    public static void ApplyOfflineUpdate(int companyId, string endpoint, string recordId, Dictionary<string, object?> body, string description)
    {
        var existing = LoadCache(companyId, endpoint) ?? new List<JsonElement>();
        string idProp = GetPrimaryIdPropertyName(endpoint);

        var updatedList = new List<JsonElement>();
        foreach (var item in existing)
        {
            var currentId = GetPropString(item, idProp) ?? GetPropString(item, "Id");
            if (string.Equals(currentId, recordId, StringComparison.OrdinalIgnoreCase))
            {
                // Merge updated fields
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (item.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in item.EnumerateObject())
                        dict[p.Name] = p.Value.Clone();
                }

                foreach (var kvp in body)
                    dict[kvp.Key] = kvp.Value;

                dict["isOfflinePending"] = true;
                dict["offlineUpdatedDate"] = DateTime.UtcNow.ToString("o");

                var mergedJson = JsonSerializer.Serialize(dict);
                using var doc = JsonDocument.Parse(mergedJson);
                updatedList.Add(doc.RootElement.Clone());
            }
            else
            {
                updatedList.Add(item);
            }
        }

        SaveCache(companyId, endpoint, updatedList);

        EnqueueSyncItem(new OfflineSyncItem
        {
            CompanyId = companyId,
            Endpoint = endpoint,
            HttpMethod = "PUT",
            RecordId = recordId,
            PayloadJson = JsonSerializer.Serialize(body),
            Description = $"Update {endpoint} #{recordId}: {description}"
        });
    }

    public static void ApplyOfflineDelete(int companyId, string endpoint, string recordId, string description)
    {
        var existing = LoadCache(companyId, endpoint) ?? new List<JsonElement>();
        string idProp = GetPrimaryIdPropertyName(endpoint);

        var filtered = existing.Where(item =>
        {
            var currentId = GetPropString(item, idProp) ?? GetPropString(item, "Id");
            return !string.Equals(currentId, recordId, StringComparison.OrdinalIgnoreCase);
        }).ToList();

        SaveCache(companyId, endpoint, filtered);

        EnqueueSyncItem(new OfflineSyncItem
        {
            CompanyId = companyId,
            Endpoint = endpoint,
            HttpMethod = "DELETE",
            RecordId = recordId,
            PayloadJson = "{}",
            Description = $"Delete {endpoint} #{recordId}: {description}"
        });
    }

    // =========================================================
    // 4. OFFLINE SYNC QUEUE MANAGEMENT
    // =========================================================

    private static string GetQueueFilePath(int? companyId = null)
    {
        int cid = companyId ?? Session.CompanyId ?? 0;
        return Path.Combine(GetCompanyFolder(cid), "sync_queue.json");
    }

    public static List<OfflineSyncItem> GetPendingQueue(int? companyId = null)
    {
        try
        {
            var file = GetQueueFilePath(companyId);
            if (!File.Exists(file)) return new List<OfflineSyncItem>();

            string json;
            lock (_lock)
            {
                json = File.ReadAllText(file);
            }

            return JsonSerializer.Deserialize<List<OfflineSyncItem>>(json) ?? new List<OfflineSyncItem>();
        }
        catch
        {
            return new List<OfflineSyncItem>();
        }
    }

    public static int GetQueueCount(int? companyId = null)
    {
        return GetPendingQueue(companyId).Count;
    }

    private static void EnqueueSyncItem(OfflineSyncItem item)
    {
        try
        {
            lock (_lock)
            {
                var queue = GetPendingQueue(item.CompanyId);
                queue.Add(item);
                var json = JsonSerializer.Serialize(queue, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetQueueFilePath(item.CompanyId), json);
            }

            PendingQueueChanged?.Invoke(null, GetQueueCount(item.CompanyId));
            SyncStatusChanged?.Invoke(null, $"🟡 Offline Mode ({GetQueueCount(item.CompanyId)} change(s) stored locally)");
        }
        catch { }
    }

    // =========================================================
    // 5. AUTOMATIC CLOUD SYNCHRONIZATION EXECUTION
    // =========================================================

    public static async Task<bool> ProcessPendingQueueAsync()
    {
        if (_isSyncing) return false;
        if (_http == null) return false;

        int companyId = Session.CompanyId ?? 0;
        if (companyId <= 0) return false;

        var queue = GetPendingQueue(companyId);
        if (queue.Count == 0) return true;

        _isSyncing = true;
        SyncStatusChanged?.Invoke(null, $"🔄 Syncing {queue.Count} local update(s) to Cloud Storage...");

        var remaining = new List<OfflineSyncItem>();
        int syncedSuccess = 0;

        foreach (var item in queue)
        {
            try
            {
                string url = $"{_apiUrl}/tenant/{item.CompanyId}/{item.Endpoint}";
                if (item.HttpMethod is "PUT" or "DELETE" && !string.IsNullOrWhiteSpace(item.RecordId))
                {
                    url = $"{url}/{item.RecordId}";
                }

                using var req = new HttpRequestMessage(new HttpMethod(item.HttpMethod), url);
                if (!string.IsNullOrWhiteSpace(Session.Token))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
                }

                if (item.HttpMethod is "POST" or "PUT")
                {
                    // For POST (create), strip temporary offline ID and metadata
                    // so the server generates a real ID and doesn't receive stale offline flags.
                    string payloadToSend = item.PayloadJson;
                    if (item.HttpMethod == "POST")
                    {
                        try
                        {
                            using var pdoc = JsonDocument.Parse(item.PayloadJson);
                            var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                            foreach (var prop in pdoc.RootElement.EnumerateObject())
                                dict[prop.Name] = prop.Value;

                            // Remove temporary / offline-only fields
                            dict.Remove("Id");
                            dict.Remove("id");
                            dict.Remove("isOfflinePending");
                            dict.Remove("offlineCreatedDate");

                            // Also remove any domain-specific primary key that matches the tempId
                            string idProp = GetPrimaryIdPropertyName(item.Endpoint);
                            if (!string.Equals(idProp, "Id", StringComparison.OrdinalIgnoreCase))
                                dict.Remove(idProp);

                            payloadToSend = JsonSerializer.Serialize(dict);
                        }
                        catch { /* if parse fails, send original */ }
                    }
                    req.Content = new StringContent(payloadToSend, Encoding.UTF8, "application/json");
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                var res = await _http.SendAsync(req, cts.Token);

                if (res.IsSuccessStatusCode)
                {
                    syncedSuccess++;

                    // For POST (create): server returns the record with a real DB ID.
                    // Replace the temp-ID entry in the local cache with the real record.
                    if (item.HttpMethod == "POST")
                    {
                        try
                        {
                            var responseBody = await res.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(responseBody))
                            {
                                using var serverDoc = JsonDocument.Parse(responseBody);
                                var serverRecord = serverDoc.RootElement.Clone();

                                var cache = LoadCache(item.CompanyId, item.Endpoint) ?? new List<JsonElement>();
                                string idProp = GetPrimaryIdPropertyName(item.Endpoint);
                                string tempId = item.RecordId ?? "";

                                // Replace the temp-ID entry
                                var updated = new List<JsonElement>();
                                bool replaced = false;
                                foreach (var entry in cache)
                                {
                                    var entryId = GetPropString(entry, idProp) ?? GetPropString(entry, "Id") ?? "";
                                    if (!replaced && string.Equals(entryId, tempId, StringComparison.OrdinalIgnoreCase))
                                    {
                                        updated.Add(serverRecord);
                                        replaced = true;
                                    }
                                    else
                                    {
                                        updated.Add(entry);
                                    }
                                }
                                if (!replaced) updated.Insert(0, serverRecord); // add if not found
                                SaveCache(item.CompanyId, item.Endpoint, updated);
                            }
                        }
                        catch { /* ignore – cache will refresh on next load */ }
                    }
                }
                else
                {
                    // Server responded with error, but connection exists
                    item.RetryCount++;
                    if (item.RetryCount < 3)
                        remaining.Add(item);
                }
            }
            catch (Exception)
            {
                // Network dropped again
                item.RetryCount++;
                remaining.Add(item);
                SetOnlineStatus(false);
                break;
            }
        }

        lock (_lock)
        {
            var json = JsonSerializer.Serialize(remaining, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetQueueFilePath(companyId), json);
        }

        _isSyncing = false;
        PendingQueueChanged?.Invoke(null, remaining.Count);

        if (remaining.Count == 0)
        {
            SyncStatusChanged?.Invoke(null, "🟢 Online · Cloud Storage Synced");
            SyncCompleted?.Invoke(null, EventArgs.Empty);
            return true;
        }
        else
        {
            SyncStatusChanged?.Invoke(null, $"🟡 Offline Mode ({remaining.Count} pending cloud sync)");
            return false;
        }
    }

    // =========================================================
    // 6. CACHED AUTHENTICATION FOR OFFLINE SIGN-IN
    // =========================================================

    public static void CacheAuth(
        string email,
        string password,
        string token,
        int? companyId,
        string? companyName,
        string? companyCode,
        string? availedModules,
        string? subscriptionStatus,
        List<string> roles,
        bool hasAcceptedTerms = true)
    {
        try
        {
            var authFile = Path.Combine(BaseStoragePath, "auth_cache.json");
            var cache = new OfflineAuthCache
            {
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = HashPasswordSimple(password),
                Token = token,
                CompanyId = companyId,
                CompanyName = companyName,
                CompanyCode = companyCode,
                AvailedModules = availedModules,
                SubscriptionStatus = subscriptionStatus,
                Roles = roles,
                HasAcceptedTerms = hasAcceptedTerms,
                CachedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
            lock (_lock)
            {
                File.WriteAllText(authFile, json);
            }
        }
        catch { }
    }

    public static bool TryOfflineLogin(
        string email,
        string password,
        out string? token,
        out int? companyId,
        out string? companyName,
        out string? companyCode,
        out string? availedModules,
        out string? subscriptionStatus,
        out List<string>? roles,
        out bool hasAcceptedTerms)
    {
        token = null; companyId = null; companyName = null; companyCode = null;
        availedModules = null; subscriptionStatus = null; roles = null; hasAcceptedTerms = false;

        try
        {
            var authFile = Path.Combine(BaseStoragePath, "auth_cache.json");
            if (!File.Exists(authFile)) return false;

            string json;
            lock (_lock)
            {
                json = File.ReadAllText(authFile);
            }

            var cache = JsonSerializer.Deserialize<OfflineAuthCache>(json);
            if (cache == null) return false;

            if (string.Equals(cache.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) &&
                cache.PasswordHash == HashPasswordSimple(password))
            {
                token = cache.Token;
                companyId = cache.CompanyId;
                companyName = cache.CompanyName;
                companyCode = cache.CompanyCode;
                availedModules = cache.AvailedModules;
                subscriptionStatus = cache.SubscriptionStatus;
                roles = cache.Roles;
                hasAcceptedTerms = cache.HasAcceptedTerms;
                return true;
            }
        }
        catch { }

        return false;
    }

    public static bool TryOfflineLogin(
        string email,
        string password,
        out string? token,
        out int? companyId,
        out string? companyName,
        out string? companyCode,
        out string? availedModules,
        out string? subscriptionStatus,
        out List<string>? roles)
    {
        return TryOfflineLogin(email, password, out token, out companyId, out companyName, out companyCode, out availedModules, out subscriptionStatus, out roles, out _);
    }

    private static string HashPasswordSimple(string pwd)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(pwd + "_FuertoSalt_2026"));
        return Convert.ToBase64String(bytes);
    }

    private static string GetPrimaryIdPropertyName(string endpoint)
    {
        return endpoint.ToLowerInvariant() switch
        {
            "customers" => "customerId",
            "leads" => "leadId",
            "projects" => "projectId",
            "quotations" => "quotationId",
            "activities" => "activityId",
            "issues" => "projectIssueId",
            "suppliers" => "supplierId",
            _ => "id"
        };
    }

    private static string? GetPropString(JsonElement elem, string prop)
    {
        if (elem.ValueKind != JsonValueKind.Object) return null;
        if (elem.TryGetProperty(prop, out var p))
        {
            if (p.ValueKind == JsonValueKind.String) return p.GetString();
            if (p.ValueKind == JsonValueKind.Number) return p.GetInt64().ToString();
        }
        // Try case-insensitive
        foreach (var item in elem.EnumerateObject())
        {
            if (string.Equals(item.Name, prop, StringComparison.OrdinalIgnoreCase))
            {
                if (item.Value.ValueKind == JsonValueKind.String) return item.Value.GetString();
                if (item.Value.ValueKind == JsonValueKind.Number) return item.Value.GetInt64().ToString();
            }
        }
        return null;
    }
}
