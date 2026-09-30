using System.Text.Json;

namespace CRM.api.Services;

public class PlatformTermsModel
{
    public string Version { get; set; } = "v1.0-2026";
    public string Title { get; set; } = "FUERTO CRM ENTERPRISE PLATFORM - SOFTWARE LICENSE & MASTER TERMS OF SERVICE";
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "Platform Super Admin";
    public bool RequireExplicitAcceptance { get; set; } = true;
    public string Content { get; set; } = string.Empty;
}

public class TermsAcceptanceRecord
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string AcceptedByName { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public string TermsVersion { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class PlatformTermsManager
{
    private static readonly object _lock = new();
    private readonly string _storageDir;
    private readonly string _termsFile;
    private readonly string _acceptanceFile;

    public PlatformTermsManager()
    {
        _storageDir = Path.Combine(AppContext.BaseDirectory, "App_Data");
        Directory.CreateDirectory(_storageDir);

        _termsFile = Path.Combine(_storageDir, "PlatformTerms.json");
        _acceptanceFile = Path.Combine(_storageDir, "TermsAcceptance.json");

        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        lock (_lock)
        {
            if (!File.Exists(_termsFile))
            {
                var defaultTerms = new PlatformTermsModel
                {
                    Version = "v1.0-2026",
                    Title = "FUERTO CRM ENTERPRISE PLATFORM - SOFTWARE LICENSE & MASTER TERMS OF SERVICE",
                    EffectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LastUpdated = DateTime.UtcNow,
                    UpdatedBy = "Super Administrator",
                    RequireExplicitAcceptance = true,
                    Content = GetDefaultTermsText()
                };

                var json = JsonSerializer.Serialize(defaultTerms, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_termsFile, json);
            }

            if (!File.Exists(_acceptanceFile))
            {
                var list = new List<TermsAcceptanceRecord>();
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_acceptanceFile, json);
            }
        }
    }

    public PlatformTermsModel GetTerms()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_termsFile))
                {
                    var json = File.ReadAllText(_termsFile);
                    var model = JsonSerializer.Deserialize<PlatformTermsModel>(json);
                    if (model != null) return model;
                }
            }
            catch { }

            return new PlatformTermsModel
            {
                Version = "v1.0-2026",
                Title = "FUERTO CRM ENTERPRISE PLATFORM - SOFTWARE LICENSE & MASTER TERMS OF SERVICE",
                Content = GetDefaultTermsText()
            };
        }
    }

    public void UpdateTerms(string title, string content, string version, bool forceReacceptance, string updatedByEmail)
    {
        lock (_lock)
        {
            var terms = GetTerms();
            terms.Title = string.IsNullOrWhiteSpace(title) ? terms.Title : title;
            terms.Content = string.IsNullOrWhiteSpace(content) ? terms.Content : content;
            terms.Version = string.IsNullOrWhiteSpace(version) ? terms.Version : version;
            terms.LastUpdated = DateTime.UtcNow;
            terms.UpdatedBy = updatedByEmail;

            var json = JsonSerializer.Serialize(terms, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_termsFile, json);

            if (forceReacceptance)
            {
                // Clear all previous acceptances so every company must re-accept
                File.WriteAllText(_acceptanceFile, JsonSerializer.Serialize(new List<TermsAcceptanceRecord>(), new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }

    public bool HasAccepted(int? companyId, string? userEmail, out TermsAcceptanceRecord? record)
    {
        record = null;
        if (!companyId.HasValue || companyId.Value <= 0) return true; // Super admin / platform accounts bypass

        var currentTerms = GetTerms();
        if (!currentTerms.RequireExplicitAcceptance) return true;

        lock (_lock)
        {
            try
            {
                var list = LoadAcceptances();
                // Check if this company has accepted the current version
                var match = list.FirstOrDefault(a =>
                    a.CompanyId == companyId.Value &&
                    string.Equals(a.TermsVersion, currentTerms.Version, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    record = match;
                    return true;
                }
            }
            catch { }

            return false;
        }
    }

    public TermsAcceptanceRecord RecordAcceptance(int companyId, string companyCode, string companyName, string userEmail, string fullName, string ipAddress = "")
    {
        lock (_lock)
        {
            var currentTerms = GetTerms();
            var list = LoadAcceptances();

            // Remove previous acceptance for this company and version if any
            list.RemoveAll(a => a.CompanyId == companyId && string.Equals(a.TermsVersion, currentTerms.Version, StringComparison.OrdinalIgnoreCase));

            var rec = new TermsAcceptanceRecord
            {
                CompanyId = companyId,
                CompanyCode = companyCode,
                CompanyName = companyName,
                UserEmail = userEmail,
                AcceptedByName = fullName,
                AcceptedAt = DateTime.UtcNow,
                TermsVersion = currentTerms.Version,
                IpAddress = ipAddress
            };

            list.Add(rec);

            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_acceptanceFile, json);

            return rec;
        }
    }

    public List<TermsAcceptanceRecord> GetAllAcceptances()
    {
        lock (_lock)
        {
            return LoadAcceptances();
        }
    }

    public bool RevokeAcceptance(int companyId)
    {
        lock (_lock)
        {
            var list = LoadAcceptances();
            int removed = list.RemoveAll(a => a.CompanyId == companyId);
            if (removed > 0)
            {
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_acceptanceFile, json);
                return true;
            }
            return false;
        }
    }

    public void RevokeAllAcceptances()
    {
        lock (_lock)
        {
            var list = new List<TermsAcceptanceRecord>();
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_acceptanceFile, json);
        }
    }

    public void ResetToDefaultTerms(string updatedByEmail)
    {
        lock (_lock)
        {
            var defaultTerms = new PlatformTermsModel
            {
                Version = "v1.0-2026",
                Title = "FUERTO CRM ENTERPRISE PLATFORM - SOFTWARE LICENSE & MASTER TERMS OF SERVICE",
                EffectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LastUpdated = DateTime.UtcNow,
                UpdatedBy = string.IsNullOrWhiteSpace(updatedByEmail) ? "Super Administrator" : updatedByEmail,
                RequireExplicitAcceptance = true,
                Content = GetDefaultTermsText()
            };

            var json = JsonSerializer.Serialize(defaultTerms, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_termsFile, json);

            RevokeAllAcceptances();
        }
    }

    private List<TermsAcceptanceRecord> LoadAcceptances()
    {
        if (!File.Exists(_acceptanceFile)) return new List<TermsAcceptanceRecord>();
        var json = File.ReadAllText(_acceptanceFile);
        return JsonSerializer.Deserialize<List<TermsAcceptanceRecord>>(json) ?? new List<TermsAcceptanceRecord>();
    }

    private static string GetDefaultTermsText()
    {
        return
@"FUERTO CRM ENTERPRISE PLATFORM
END USER LICENSE AGREEMENT & MASTER TERMS OF SERVICE (EULA)
Issued by: Platform Super Administrator
Governance Policy: Enterprise Multi-Tenant SaaS Compliance

================================================================================
IMPORTANT - READ CAREFULLY BEFORE ACCESSING OR USING THIS SOFTWARE
================================================================================

This End User License Agreement & Master Terms of Service (""Agreement"") constitutes a legally binding contract between the Platform Super Administrator (""Licensor"") and the licensed Company / Tenant Organization (""Licensee"", ""Company"", or ""You""), represented by its designated Administrator and authorized users.

BY CLICKING ""ACCEPT & CONTINUE"", OR BY ACCESSING OR USING ANY PORTION OF THE FUERTO CRM SYSTEM, YOU EXPLICITLY ACKNOWLEDGE THAT YOU HAVE READ, UNDERSTOOD, AND AGREE TO BE BOUND BY ALL TERMS, CONDITIONS, AND POLICIES SET FORTH HEREIN. 

IF YOU DO NOT AGREE TO THESE TERMS, YOU MUST CLICK ""REJECT & EXIT"". ACCESS TO THE APPLICATION AND ASSOCIATED CLOUD SERVICES WILL BE IMMEDIATELY DENIED.

--------------------------------------------------------------------------------
1. GRANT OF LICENSE & AUTHORIZED USAGE
--------------------------------------------------------------------------------
1.1 Licensor grants Company a non-exclusive, non-transferable, revocable license to access and use the Fuerto CRM Platform strictly for internal commercial operations, customer relationship management, sales pipeline tracking, quotation generation, and project management.

1.2 Scope of Permitted Use:
(a) Access is authorized solely for registered employees and credentialed personnel of the Licensee.
(b) Sharing login credentials across unverified third parties or distributing accounts outside the tenant organization is strictly prohibited.
(c) Decompilation, reverse engineering, unauthorized modification, or extraction of platform source code or proprietary architecture is expressly forbidden.

--------------------------------------------------------------------------------
2. SUPER ADMINISTRATOR AUTHORITY & TENANT GOVERNANCE
--------------------------------------------------------------------------------
2.1 The Platform Super Administrator maintains supreme administrative authority over platform infrastructure, multi-tenant databases, global security policies, and subscription provisioning.

2.2 The Super Administrator reserves the right to:
(a) Audit company user accounts, subscription tiers, and system activity logs for compliance and security assurance.
(b) Suspend, deactivate, or restrict access for any tenant organization that fails to maintain active subscription standing or violates governance policies.
(c) Reset administrative credentials, force security policy updates, or require re-acceptance of platform terms upon system version upgrades.

--------------------------------------------------------------------------------
3. MULTI-TENANT DATA PRIVACY & CONFIDENTIALITY
--------------------------------------------------------------------------------
3.1 Data Isolation: Licensor warrants that Company's business records, customer contact databases, project files, and financial quotations are logically isolated within dedicated tenant schemas and accessible only by authorized Licensee users.

3.2 Privacy Compliance: Both Licensor and Licensee agree to adhere strictly to the Data Privacy Act of 2012 (Republic Act No. 10173) and applicable international data protection standards. Personal data collected from leads and customers must be processed lawfully, fairly, and transparently.

3.3 Non-Disclosure: Licensor agrees not to sell, rent, commercialize, or disclose Company's confidential business metrics or customer lists to any third party.

--------------------------------------------------------------------------------
4. HYBRID LOCAL AND CLOUD DATA SYNCHRONIZATION
--------------------------------------------------------------------------------
4.1 The Fuerto CRM system incorporates dual-tier local caching and automated cloud synchronization architecture.

4.2 Offline Operations: When operating in offline mode, data entries and transactions are recorded locally on the authorized workstation. Company acknowledges that local storage security is the responsibility of the Licensee.

4.3 Cloud Convergence: When an active internet connection is re-established, all pending local modifications automatically synchronize to the central cloud repository. Licensee agrees to allow background synchronization to preserve data integrity and prevent operational divergence.

--------------------------------------------------------------------------------
5. BILLING, SUBSCRIPTIONS, AND ACCOUNT SUSPENSION
--------------------------------------------------------------------------------
5.1 Companies operate under assigned subscription plans configured by the Super Administrator (Starter, Professional, or Enterprise).

5.2 In the event of subscription delinquency, non-payment, or administrative revocation:
(a) The Super Administrator may transition company status to ""Suspended"" or ""Inactive"".
(b) Upon deactivation, user access to CRM modules will be locked, and offline write operations will be disabled.

--------------------------------------------------------------------------------
6. ACCEPTABLE USE & PROHIBITED CONDUCT
--------------------------------------------------------------------------------
Company and its users shall NOT:
(a) Transmit malicious code, viruses, or disruptive scripts through CRM communication channels.
(b) Utilize the CRM platform for sending unsolicited mass spam, unlawful marketing, or fraudulent campaigns.
(c) Attempt unauthorized cross-tenant data access or exploit platform APIs.

--------------------------------------------------------------------------------
7. DISCLAIMER OF WARRANTIES & LIMITATION OF LIABILITY
--------------------------------------------------------------------------------
7.1 The CRM platform is provided on an ""AS IS"" and ""AS AVAILABLE"" basis. While Licensor makes best efforts to ensure 99.9% uptime, Licensor does not warrant that system operation will be entirely uninterrupted or error-free.

7.2 In no event shall Licensor be liable for indirect, incidental, consequential, or punitive damages resulting from user workstation compromise, unauthorized credential sharing, or external internet outages.

--------------------------------------------------------------------------------
8. MANDATORY ACCEPTANCE & ACCESS RESTRICTION
--------------------------------------------------------------------------------
8.1 Initial Acceptance: All newly provisioned company credentials generated by the Super Administrator require explicit initial review and acceptance of these Terms before CRM workspace access is granted.

8.2 Rejection Policy: Rejecting this agreement immediately revokes the authenticated session. The user is logged out, and no customer, sales, or project records may be viewed, entered, or exported.

================================================================================
Platform Licensor: Fuerto CRM Platform Engineering & Super Administration
System Build: 2026.1 Enterprise Release
================================================================================";
    }
}
