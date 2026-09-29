using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class BranchDialog : CrmModalDialog
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly int? _branchId;
    private readonly bool _isEdit;

    private readonly TextBox _txtBranchCode;
    private readonly TextBox _txtBranchName;
    private readonly TextBox _txtAddress;
    private readonly TextBox _txtContact;
    private readonly TextBox _txtEmail;
    private readonly CheckBox _chkIsMain;
    private readonly CheckBox _chkIsActive;

    public BranchDialog(string apiUrl, HttpClient http, JsonElement? branchToEdit = null)
        : base(
            title: branchToEdit.HasValue ? "Edit Branch Details" : "Create New Branch",
            subtitle: "Configure branch identity, geographical location, and headquarters designation.",
            actionText: branchToEdit.HasValue ? "Save Changes" : "Create Branch",
            iconSymbol: "🏢",
            dialogWidth: 560)
    {
        _apiUrl = apiUrl;
        _http = http;
        _isEdit = branchToEdit.HasValue;

        if (_isEdit)
        {
            var b = branchToEdit!.Value;
            if (b.TryGetProperty("branchId", out var bid) || b.TryGetProperty("BranchId", out bid))
                _branchId = bid.GetInt32();
        }

        AddTwoTextFields(
            "Branch Code *", "e.g. FUERTO-HQ, LRS-SM", out _txtBranchCode,
            "Branch Name *", "e.g. Makati Main Showroom", out _txtBranchName,
            req1: true, req2: true);

        AddTwoTextFields(
            "Address / Location", "e.g. 123 Ayala Ave, Makati City", out _txtAddress,
            "Contact Number", "e.g. +63 2 8123 4567", out _txtContact);

        _txtEmail = AddTextField("Branch Email Address", "branch@company.com");

        _chkIsMain = AddCheckboxField("Designate as Main Branch / Headquarters", false);
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

        SubmitButton.Click += async (_, _) => await SaveAsync();
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
            IsActive = _chkIsActive.Checked
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
