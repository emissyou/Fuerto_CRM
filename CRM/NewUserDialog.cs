using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class NewUserDialog : CrmModalDialog
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private readonly TextBox _txtEmail;
    private readonly TextBox _txtPassword;
    private readonly TextBox _txtFullName;
    private readonly ComboBox _cmbRole;

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

        SubmitButton.Click += async (_, _) => await CreateAsync();
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
            var body = new
            {
                email = _txtEmail.Text.Trim(),
                password = _txtPassword.Text,
                fullName = _txtFullName.Text.Trim(),
                role = _cmbRole.SelectedItem?.ToString() ?? "Staff"
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