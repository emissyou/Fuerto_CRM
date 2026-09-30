using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_DesignServices.winforms;

public class NewUserDialog : CrmModalDialog
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private readonly TextBox _txtEmail;
    private readonly TextBox _txtPassword;
    private readonly TextBox _txtFullName;
    private readonly ComboBox _cmbRole;
    private readonly ComboBox? _cmbBranch;

    public class UserBranchItem
    {
        public int? BranchId { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    public NewUserDialog(string apiUrl, HttpClient http)
        : base(
            title: (Session.Roles?.Contains("Super Admin") ?? false) ? "Create Company Admin" : "Create New User",
            subtitle: "Register credentials, assign roles, and grant permissions for this team account.",
            actionText: (Session.Roles?.Contains("Super Admin") ?? false) ? "Create Admin" : "Create User",
            iconSymbol: "👤",
            dialogWidth: 540)
    {
        _apiUrl = apiUrl;
        _http = http;

        var roles = Session.Roles ?? new List<string>();
        bool isSuperAdmin = roles.Contains("Super Admin");
        bool isAdmin = roles.Contains("Admin");

        AddTwoTextFields(
            "Full Name *", "e.g. Jane Doe", out _txtFullName,
            "Email Address *", "name@company.com", out _txtEmail,
            req1: true, req2: true);

        _txtPassword = AddTextField("Temporary Password (min. 6 characters) *", "••••••••", required: true);
        _txtPassword.UseSystemPasswordChar = true;

        var availableRoles = isSuperAdmin ? new object[] { "Admin" }
                           : isAdmin ? new object[] { "Manager", "Staff" }
                           : new object[] { "Staff" };

        _cmbRole = AddDropdownField("Account Role *", availableRoles, required: true);

        if (!isSuperAdmin && Session.HasModule("Branching"))
        {
            _cmbBranch = AddDropdownField("Assigned Branch", new object[]
            {
                new UserBranchItem { BranchId = null, Name = "— (All Branches / HQ) —" }
            });
            _ = LoadBranchesAsync();
        }

        SubmitButton.Click += async (_, _) => await CreateAsync();
    }

    private async Task LoadBranchesAsync()
    {
        if (_cmbBranch == null) return;
        int companyId = Session.CompanyId ?? 1;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_apiUrl}/tenant/{companyId}/branches");
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
                        items.Add(new UserBranchItem { BranchId = id, Name = disp });
                    }
                    _cmbBranch.Items.Clear();
                    _cmbBranch.Items.AddRange(items.ToArray());
                    _cmbBranch.SelectedIndex = 0;
                }
            }
        }
        catch { }
    }

    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtFullName.Text) || string.IsNullOrWhiteSpace(_txtEmail.Text))
        {
            MessageBox.Show("Full Name and Email Address are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        if (_txtPassword.Text.Length < 6)
        {
            MessageBox.Show("Password must be at least 6 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        SubmitButton.Enabled = false;
        SubmitButton.Text = "Creating...";

        try
        {
            int? branchId = (_cmbBranch?.SelectedItem as UserBranchItem)?.BranchId;

            var body = new
            {
                email = _txtEmail.Text.Trim(),
                password = _txtPassword.Text,
                fullName = _txtFullName.Text.Trim(),
                role = _cmbRole.SelectedItem?.ToString() ?? "Staff",
                branchId = branchId
            };

            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/users";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
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
            MessageBox.Show("Error creating account: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SubmitButton.Enabled = true;
            SubmitButton.Text = "Create User";
            DialogResult = DialogResult.None;
        }
    }
}