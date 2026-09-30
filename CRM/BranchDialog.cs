using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_DesignServices.winforms;

public class BranchDialog : CrmModalDialog
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly int? _branchId;
    private readonly bool _isEdit;
    private readonly string? _initialManagerUserId;
    private readonly string? _initialManagerEmail;

    private readonly TextBox _txtBranchCode;
    private readonly TextBox _txtBranchName;
    private readonly TextBox _txtAddress;
    private readonly TextBox _txtContact;
    private readonly TextBox _txtEmail;
    private readonly ComboBox _cmbManager;
    private readonly TextBox _txtNewMgrName;
    private readonly TextBox _txtNewMgrEmail;
    private readonly TextBox _txtNewMgrPassword;
    private readonly CheckBox _chkIsMain;
    private readonly CheckBox _chkIsActive;

    public class ManagerComboItem
    {
        public string? UserId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsCreateNew { get; set; }

        public override string ToString()
        {
            if (IsCreateNew) return "➕ [Create New Manager Account]";
            if (string.IsNullOrEmpty(UserId) && string.IsNullOrEmpty(Email)) return "— (Unassigned) —";
            return $"{Name} ({Email})";
        }
    }

    public BranchDialog(string apiUrl, HttpClient http, JsonElement? branchToEdit = null)
        : base(
            title: branchToEdit.HasValue ? "Edit Branch Details" : "Create New Branch",
            subtitle: "Configure branch identity, address, assigned branch manager, and headquarters status.",
            actionText: branchToEdit.HasValue ? "Save Changes" : "Create Branch",
            iconSymbol: "🏢",
            dialogWidth: 580)
    {
        _apiUrl = apiUrl;
        _http = http;
        _isEdit = branchToEdit.HasValue;

        if (_isEdit)
        {
            var b = branchToEdit!.Value;
            if (b.TryGetProperty("branchId", out var bid) || b.TryGetProperty("BranchId", out bid))
                _branchId = bid.GetInt32();

            _initialManagerUserId = GetProp(b, "managerUserId", "ManagerUserId");
            _initialManagerEmail = GetProp(b, "managerEmail", "ManagerEmail");
        }

        AddTwoTextFields(
            "Branch Code *", "e.g. FUERTO-HQ, FUERTO-MKT", out _txtBranchCode,
            "Branch Name *", "e.g. Makati Flagship Studio", out _txtBranchName,
            req1: true, req2: true);

        AddTwoTextFields(
            "Address / Location", "e.g. Ayala Avenue, Makati City", out _txtAddress,
            "Contact Number", "e.g. +63 2 8123 4567", out _txtContact);

        _txtEmail = AddTextField("Branch Email Address", "branch@fuerto.local");

        // Manager Assignment Dropdown
        _cmbManager = AddDropdownField("Assigned Branch Manager", new object[]
        {
            new ManagerComboItem { UserId = null, Name = "— (Unassigned) —" },
            new ManagerComboItem { IsCreateNew = true }
        });

        // New Manager Registration Fields (visible if creating new manager)
        AddTwoTextFields(
            "New Manager Full Name", "e.g. Roberto Ramos", out _txtNewMgrName,
            "New Manager Email", "makati.manager@fuerto.local", out _txtNewMgrEmail);

        _txtNewMgrPassword = AddTextField("New Manager Password (min 6 characters)", "••••••••");
        _txtNewMgrPassword.UseSystemPasswordChar = true;

        _chkIsMain = AddCheckboxField("Designate as Main Branch / Headquarters (Makati City)", false);
        _chkIsActive = AddCheckboxField("Active Operational Branch", true);

        if (_isEdit && branchToEdit.HasValue)
        {
            var b = branchToEdit.Value;
            _txtBranchCode.Text = GetProp(b, "branchCode", "BranchCode");
            _txtBranchName.Text = GetProp(b, "branchName", "BranchName");
            _txtAddress.Text    = GetProp(b, "address", "Address");
            _txtContact.Text    = GetProp(b, "contactNumber", "ContactNumber");
            _txtEmail.Text      = GetProp(b, "email", "Email");

            if (bool.TryParse(GetProp(b, "isMainBranch", "IsMainBranch"), out var mb))
                _chkIsMain.Checked = mb;

            if (bool.TryParse(GetProp(b, "isActive", "IsActive"), out var act))
                _chkIsActive.Checked = act;
        }

        _cmbManager.SelectedIndexChanged += (_, _) =>
        {
            bool isNew = _cmbManager.SelectedItem is ManagerComboItem { IsCreateNew: true };
            _txtNewMgrName.Enabled = isNew;
            _txtNewMgrEmail.Enabled = isNew;
            _txtNewMgrPassword.Enabled = isNew;

            if (isNew && string.IsNullOrWhiteSpace(_txtNewMgrEmail.Text))
            {
                var cleanCode = _txtBranchCode.Text.Trim().ToLowerInvariant().Replace("fuerto-", "").Replace(" ", "");
                if (!string.IsNullOrWhiteSpace(cleanCode))
                {
                    _txtNewMgrEmail.Text = $"{cleanCode}.manager@fuerto.local";
                }
            }
        };

        // Default state: disable new manager fields until "Create New Manager" is chosen
        _txtNewMgrName.Enabled = false;
        _txtNewMgrEmail.Enabled = false;
        _txtNewMgrPassword.Enabled = false;

        _ = LoadManagersAsync();

        SubmitButton.Click += async (_, _) => await SaveAsync();
    }

    private async Task LoadManagersAsync()
    {
        int companyId = Session.CompanyId ?? 1;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_apiUrl}/tenant/{companyId}/users");
            if (!string.IsNullOrWhiteSpace(Session.Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var items = new List<ManagerComboItem>
                    {
                        new ManagerComboItem { UserId = null, Name = "— (Unassigned) —" },
                        new ManagerComboItem { IsCreateNew = true }
                    };

                    ManagerComboItem? matchedItem = null;

                    foreach (var u in doc.RootElement.EnumerateArray())
                    {
                        string uid = GetProp(u, "userId", "UserId", "id", "Id");
                        string name = GetProp(u, "fullName", "FullName");
                        string email = GetProp(u, "email", "Email");
                        string role = GetProp(u, "role", "Role");

                        // Add users with Manager or Staff role (or any user in the company)
                        var item = new ManagerComboItem
                        {
                            UserId = uid,
                            Name = string.IsNullOrWhiteSpace(name) ? email : name,
                            Email = email
                        };
                        items.Add(item);

                        if (!string.IsNullOrWhiteSpace(_initialManagerUserId) && uid == _initialManagerUserId)
                        {
                            matchedItem = item;
                        }
                        else if (matchedItem == null && !string.IsNullOrWhiteSpace(_initialManagerEmail) &&
                                 email.Equals(_initialManagerEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedItem = item;
                        }
                    }

                    _cmbManager.Items.Clear();
                    _cmbManager.Items.AddRange(items.ToArray());

                    if (matchedItem != null)
                    {
                        _cmbManager.SelectedItem = matchedItem;
                    }
                    else
                    {
                        _cmbManager.SelectedIndex = 0;
                    }
                }
            }
        }
        catch { }
    }

    private static string GetProp(JsonElement el, params string[] names)
    {
        foreach (var n in names)
        {
            if (el.TryGetProperty(n, out var val) && val.ValueKind != JsonValueKind.Null)
                return val.ToString();
        }
        return "";
    }

    private async Task SaveAsync()
    {
        string code = _txtBranchCode.Text.Trim();
        string name = _txtBranchName.Text.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Branch Code and Branch Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        int companyId = Session.CompanyId ?? 1;

        string? managerUserId = null;
        string? managerName = null;
        string? managerEmail = null;

        var selectedManager = _cmbManager.SelectedItem as ManagerComboItem;

        // If creating a new manager account on the fly:
        if (selectedManager != null && selectedManager.IsCreateNew)
        {
            string newMgrName = _txtNewMgrName.Text.Trim();
            string newMgrEmail = _txtNewMgrEmail.Text.Trim();
            string newMgrPwd = _txtNewMgrPassword.Text;

            if (string.IsNullOrWhiteSpace(newMgrName) || string.IsNullOrWhiteSpace(newMgrEmail))
            {
                MessageBox.Show("Please enter the Full Name and Email Address for the new Branch Manager.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(newMgrPwd) || newMgrPwd.Length < 6)
            {
                MessageBox.Show("Branch Manager password must be at least 6 characters long.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            SubmitButton.Enabled = false;
            SubmitButton.Text = "Registering Manager...";

            try
            {
                var userPayload = new
                {
                    email = newMgrEmail,
                    fullName = newMgrName,
                    password = newMgrPwd,
                    role = "Manager"
                };

                using var userReq = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/tenant/{companyId}/users")
                {
                    Content = new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrWhiteSpace(Session.Token))
                    userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

                var userRes = await _http.SendAsync(userReq);
                var userJson = await userRes.Content.ReadAsStringAsync();

                if (!userRes.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Failed to register new manager account: {userJson}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SubmitButton.Enabled = true;
                    SubmitButton.Text = _isEdit ? "Save Changes" : "Create Branch";
                    DialogResult = DialogResult.None;
                    return;
                }

                using var userDoc = JsonDocument.Parse(userJson);
                managerUserId = GetProp(userDoc.RootElement, "userId", "UserId", "id", "Id");
                managerName = newMgrName;
                managerEmail = newMgrEmail;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error registering manager: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SubmitButton.Enabled = true;
                SubmitButton.Text = _isEdit ? "Save Changes" : "Create Branch";
                DialogResult = DialogResult.None;
                return;
            }
        }
        else if (selectedManager != null && !string.IsNullOrWhiteSpace(selectedManager.UserId))
        {
            managerUserId = selectedManager.UserId;
            managerName = selectedManager.Name;
            managerEmail = selectedManager.Email;
        }

        var payload = new
        {
            BranchId = _branchId ?? 0,
            CompanyId = companyId,
            BranchCode = code.ToUpperInvariant(),
            BranchName = name,
            Address = _txtAddress.Text.Trim(),
            ContactNumber = _txtContact.Text.Trim(),
            Email = _txtEmail.Text.Trim(),
            IsMainBranch = _chkIsMain.Checked,
            IsActive = _chkIsActive.Checked,
            ManagerUserId = managerUserId,
            ManagerName = managerName,
            ManagerEmail = managerEmail
        };

        SubmitButton.Enabled = false;
        SubmitButton.Text = "Saving...";

        try
        {
            using var req = new HttpRequestMessage(
                _isEdit ? HttpMethod.Put : HttpMethod.Post,
                _isEdit ? $"{_apiUrl}/tenant/{companyId}/branches/{_branchId}" : $"{_apiUrl}/tenant/{companyId}/branches")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(Session.Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                var body = await res.Content.ReadAsStringAsync();
                MessageBox.Show($"Failed to save branch: {body}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SubmitButton.Enabled = true;
                SubmitButton.Text = _isEdit ? "Save Changes" : "Create Branch";
                DialogResult = DialogResult.None;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SubmitButton.Enabled = true;
            SubmitButton.Text = _isEdit ? "Save Changes" : "Create Branch";
            DialogResult = DialogResult.None;
        }
    }
}
