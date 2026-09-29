using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class EditUserDialog : CrmModalDialog
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly string _userId;

    private readonly TextBox _txtFullName;
    private readonly TextBox _txtEmail;
    private readonly ComboBox _cmbRole;
    private readonly CheckBox _chkActive;

    public EditUserDialog(string apiUrl, HttpClient http, JsonElement user)
        : base(
            title: (Session.Roles?.Contains("Super Admin") ?? false) ? "Edit Company Admin" : "Edit User Account",
            subtitle: "Update account profile details, system permissions, and active status.",
            actionText: "Save Changes",
            iconSymbol: "✎",
            dialogWidth: 540)
    {
        _apiUrl = apiUrl;
        _http = http;
        _userId = GetStr(user, "userId");

        var actorRoles = Session.Roles ?? new List<string>();
        bool actorIsSuperAdmin = actorRoles.Contains("Super Admin");
        bool actorIsCompanyAdmin = actorRoles.Contains("Admin");

        AddTwoTextFields(
            "Full Name *", "e.g. Jane Doe", out _txtFullName,
            "Email Address *", "name@company.com", out _txtEmail,
            req1: true, req2: true);

        _txtFullName.Text = GetStr(user, "fullName");
        _txtEmail.Text = GetStr(user, "email");

        var currentRole = GetStr(user, "role");
        var availableRoles = actorIsSuperAdmin ? new object[] { "Admin" }
                           : actorIsCompanyAdmin ? new object[] { "Manager", "Staff" }
                           : new object[] { "Staff" };

        _cmbRole = AddDropdownField("Account Role *", availableRoles, required: true);
        var idx = _cmbRole.Items.IndexOf(currentRole);
        if (idx >= 0) _cmbRole.SelectedIndex = idx;
        else _cmbRole.SelectedIndex = 0;

        _chkActive = AddCheckboxField("Active Account Status", GetBool(user, "isActive"));

        SubmitButton.Click += async (_, _) => await SaveAsync();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtFullName.Text) || string.IsNullOrWhiteSpace(_txtEmail.Text))
        {
            MessageBox.Show("Full Name and Email Address are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        SubmitButton.Enabled = false;
        SubmitButton.Text = "Saving...";

        try
        {
            var body = new
            {
                email = _txtEmail.Text.Trim(),
                fullName = _txtFullName.Text.Trim(),
                role = _cmbRole.SelectedItem?.ToString() ?? "Staff",
                isActive = _chkActive.Checked
            };

            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/users/{_userId}";
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

    private static string GetStr(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return "";
        return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.ToString();
    }

    private static bool GetBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return false;
        return p.ValueKind == JsonValueKind.True;
    }
}