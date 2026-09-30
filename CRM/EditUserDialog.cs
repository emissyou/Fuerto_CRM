using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_DesignServices.winforms;

public class EditUserDialog : CrmModalDialog
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly string _userId;
    private readonly int _targetCompanyId;
    private readonly int? _initialBranchId;

    private readonly TextBox _txtFullName;
    private readonly TextBox _txtEmail;
    private readonly TextBox _txtPassword;
    private readonly ComboBox _cmbRole;
    private readonly ComboBox? _cmbBranch;
    private readonly CheckBox _chkActive;

    public class UserBranchItem
    {
        public int? BranchId { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    public EditUserDialog(string apiUrl, HttpClient http, JsonElement user)
        : base(
            title: (Session.Roles?.Contains("Super Admin") ?? false) ? "Edit Company Admin Credentials" : "Edit User Account",
            subtitle: (Session.Roles?.Contains("Super Admin") ?? false) 
                ? "Update administrator full name, login email, password, and operational status."
                : "Update account profile details, branch assignment, system permissions, and active status.",
            actionText: "Save Changes",
            iconSymbol: (Session.Roles?.Contains("Super Admin") ?? false) ? "🔑" : "✎",
            dialogWidth: 540)
    {
        _apiUrl = apiUrl;
        _http = http;
        _userId = GetStr(user, "userId");

        // Resolve target company ID from user object (essential when Super Admin manages tenant users)
        if (user.TryGetProperty("companyId", out var cid) && cid.ValueKind == JsonValueKind.Number)
        {
            _targetCompanyId = cid.GetInt32();
        }
        else if (int.TryParse(GetStr(user, "companyId", "CompanyId"), out var parsedCid))
        {
            _targetCompanyId = parsedCid;
        }
        else
        {
            _targetCompanyId = Session.CompanyId ?? 1;
        }

        if (user.TryGetProperty("branchId", out var bid) && bid.ValueKind == JsonValueKind.Number)
        {
            _initialBranchId = bid.GetInt32();
        }

        var actorRoles = Session.Roles ?? new List<string>();
        bool actorIsSuperAdmin = actorRoles.Contains("Super Admin");
        bool actorIsCompanyAdmin = actorRoles.Contains("Admin");

        AddTwoTextFields(
            "Full Name *", "e.g. Jane Doe", out _txtFullName,
            "Email Address *", "name@company.com", out _txtEmail,
            req1: true, req2: true);

        _txtFullName.Text = GetStr(user, "fullName");
        _txtEmail.Text = GetStr(user, "email");

        // Direct Password Reset / Update Field
        _txtPassword = AddTextField(
            actorIsSuperAdmin ? "New Password (leave blank to keep current) *" : "Update Password (leave blank to keep current)",
            "•••••••• (min. 6 characters)");
        _txtPassword.UseSystemPasswordChar = true;

        var currentRole = GetStr(user, "role");
        var availableRoles = actorIsSuperAdmin ? new object[] { "Admin" }
                           : actorIsCompanyAdmin ? new object[] { "Manager", "Staff" }
                           : new object[] { "Staff" };

        _cmbRole = AddDropdownField("Account Role *", availableRoles, required: true);
        var idx = _cmbRole.Items.IndexOf(currentRole);
        if (idx >= 0) _cmbRole.SelectedIndex = idx;
        else _cmbRole.SelectedIndex = 0;

        if (actorIsSuperAdmin)
        {
            // Super Admin only manages Company Admins
            _cmbRole.Enabled = false;
        }

        if (!actorIsSuperAdmin && Session.HasModule("Branching"))
        {
            _cmbBranch = AddDropdownField("Assigned Branch", new object[]
            {
                new UserBranchItem { BranchId = null, Name = "— (All Branches / HQ) —" }
            });
            _ = LoadBranchesAsync();
        }

        _chkActive = AddCheckboxField("Active Account Status (Permits Login)", GetBool(user, "isActive"));

        SubmitButton.Click += async (_, _) => await SaveAsync();
    }

    private async Task LoadBranchesAsync()
    {
        if (_cmbBranch == null) return;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_apiUrl}/tenant/{_targetCompanyId}/branches");
            if (!string.IsNullOrWhiteSpace(Session.Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var items = new List<UserBranchItem>
                    {
                        new UserBranchItem { BranchId = null, Name = "— (All Branches / HQ) —" }
                    };

                    UserBranchItem? matched = null;
                    foreach (var b in doc.RootElement.EnumerateArray())
                    {
                        int id = 0;
                        if (b.TryGetProperty("branchId", out var bid) || b.TryGetProperty("BranchId", out bid))
                            id = bid.GetInt32();
                        string name = "";
                        if (b.TryGetProperty("branchName", out var bn) || b.TryGetProperty("BranchName", out bn))
                            name = bn.GetString() ?? "";
                        bool isMain = false;
                        if (b.TryGetProperty("isMainBranch", out var imb) || b.TryGetProperty("IsMainBranch", out imb))
                            isMain = imb.GetBoolean();

                        string disp = isMain ? $"★ {name} (HQ)" : name;
                        var item = new UserBranchItem { BranchId = id, Name = disp };
                        items.Add(item);

                        if (_initialBranchId.HasValue && id == _initialBranchId.Value)
                        {
                            matched = item;
                        }
                    }

                    _cmbBranch.Items.Clear();
                    _cmbBranch.Items.AddRange(items.ToArray());
                    if (matched != null) _cmbBranch.SelectedItem = matched;
                    else _cmbBranch.SelectedIndex = 0;
                }
            }
        }
        catch { }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtFullName.Text) || string.IsNullOrWhiteSpace(_txtEmail.Text))
        {
            MessageBox.Show("Full Name and Email Address are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        string newPassword = _txtPassword.Text.Trim();
        if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < 6)
        {
            MessageBox.Show("New password must be at least 6 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        SubmitButton.Enabled = false;
        SubmitButton.Text = "Saving...";

        try
        {
            int? branchId = (_cmbBranch?.SelectedItem as UserBranchItem)?.BranchId;

            var body = new
            {
                email = _txtEmail.Text.Trim(),
                fullName = _txtFullName.Text.Trim(),
                role = _cmbRole.SelectedItem?.ToString() ?? "Staff",
                isActive = _chkActive.Checked,
                branchId = branchId,
                newPassword = string.IsNullOrEmpty(newPassword) ? null : newPassword
            };

            var url = $"{_apiUrl}/tenant/{_targetCompanyId}/users/{_userId}";
            using var req = new HttpRequestMessage(HttpMethod.Put, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            req.Content = new StringContent(
                JsonSerializer.Serialize(body),
                System.Text.Encoding.UTF8,
                "application/json");

            using var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                string msg = json;
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("message", out var m))
                        msg = m.GetString() ?? json;
                }
                catch { }
                throw new HttpRequestException(msg);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error updating account: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SubmitButton.Enabled = true;
            SubmitButton.Text = "Save Changes";
            DialogResult = DialogResult.None;
        }
    }

    private static string GetStr(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (el.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null)
                return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.ToString();
        }
        return "";
    }

    private static bool GetBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return false;
        return p.ValueKind == JsonValueKind.True;
    }
}