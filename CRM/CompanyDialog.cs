using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_DesignServices.winforms;

public class CompanyDialog : Form
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly int? _companyId;
    private readonly bool _isEdit;

    private TextBox _txtCode = null!;
    private TextBox _txtName = null!;
    private ComboBox _cmbPlan = null!;
    private TextBox _txtFee = null!;
    private CheckBox _chkMainTx = null!;
    private CheckBox _chkDataCol = null!;
    private CheckBox _chkBi = null!;
    private CheckBox _chkAction = null!;
    private CheckBox _chkBranching = null!;
    private CheckBox _chkSupport = null!;
    private CheckBox _chkInventory = null!;
    private CheckBox _chkAudit = null!;
    private CheckBox _chkIsActive = null!;
    private Label _lblStatus = null!;
    private Button _btnSave = null!;

    public CompanyDialog(string apiUrl, HttpClient http, JsonElement? companyToEdit = null)
    {
        _apiUrl = apiUrl;
        _http = http;
        _isEdit = companyToEdit.HasValue;

        if (_isEdit)
        {
            var c = companyToEdit!.Value;
            if (c.TryGetProperty("companyId", out var cid) || c.TryGetProperty("CompanyId", out cid))
                _companyId = cid.GetInt32();
        }

        Text = _isEdit ? "Edit Company & Subscription" : "Register New Tenant Company";
        Size = new Size(540, 680);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        BuildInterface(companyToEdit);
    }

    private void BuildInterface(JsonElement? company)
    {
        Controls.Add(new Label
        {
            Text = _isEdit ? "Edit Company & Subscription Entitlements" : "Create New Tenant Company",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 18)
        });

        int y = 56;

        AddLabel("Company Code * (e.g. GLIBB, CCDAVAL, FUERTO)", y); y += 22;
        _txtCode = AddText(y); y += 40;

        AddLabel("Company Name * (e.g. GLI Bahay Builds, Custom Crafters Davao)", y); y += 22;
        _txtName = AddText(y); y += 40;

        AddLabel("Subscription Plan", y); y += 22;
        _cmbPlan = new ComboBox
        {
            Left = 24,
            Top = y,
            Width = 470,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbPlan.Items.AddRange(new object[] { "Enterprise", "Professional", "Starter", "Custom" });
        _cmbPlan.SelectedIndex = 1;
        Controls.Add(_cmbPlan);
        y += 40;

        AddLabel("Monthly Fee (PHP)", y); y += 22;
        _txtFee = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 470,
            Height = 28,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            Text = "2499.00"
        };
        Controls.Add(_txtFee);
        y += 44;

        var grp = new GroupBox
        {
            Text = "Availed Modules (SaaS Feature Entitlements Reflecting to Company)",
            Left = 24,
            Top = y,
            Width = 490,
            Height = 175,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 45, 55)
        };
        Controls.Add(grp);

        _chkMainTx    = new CheckBox { Text = "Main Transaction", Left = 16, Top = 22, Width = 210, Font = new Font("Segoe UI", 9f), Checked = true };
        _chkDataCol   = new CheckBox { Text = "Data Collection", Left = 16, Top = 48, Width = 210, Font = new Font("Segoe UI", 9f), Checked = true };
        _chkBi        = new CheckBox { Text = "Business Intelligence", Left = 16, Top = 74, Width = 210, Font = new Font("Segoe UI", 9f) };
        _chkAction    = new CheckBox { Text = "Action & Retention", Left = 16, Top = 100, Width = 210, Font = new Font("Segoe UI", 9f) };

        _chkBranching = new CheckBox { Text = "Branching & Locations", Left = 240, Top = 22, Width = 230, Font = new Font("Segoe UI", 9f), Checked = false };
        _chkSupport   = new CheckBox { Text = "Customer Support & Issues", Left = 240, Top = 48, Width = 230, Font = new Font("Segoe UI", 9f) };
        _chkInventory = new CheckBox { Text = "Inventory & Supplies", Left = 240, Top = 74, Width = 230, Font = new Font("Segoe UI", 9f) };
        _chkAudit     = new CheckBox { Text = "Audit & Compliance", Left = 240, Top = 100, Width = 230, Font = new Font("Segoe UI", 9f) };

        var btnSelectAll = new LinkLabel { Text = "Select All", Left = 16, Top = 138, AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        btnSelectAll.LinkClicked += (_, _) =>
        {
            _chkMainTx.Checked = _chkDataCol.Checked = _chkBi.Checked = _chkAction.Checked =
            _chkBranching.Checked = _chkSupport.Checked = _chkInventory.Checked = _chkAudit.Checked = true;
        };

        var btnClearAll = new LinkLabel { Text = "Clear All", Left = 90, Top = 138, AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        btnClearAll.LinkClicked += (_, _) =>
        {
            _chkMainTx.Checked = _chkDataCol.Checked = _chkBi.Checked = _chkAction.Checked =
            _chkBranching.Checked = _chkSupport.Checked = _chkInventory.Checked = _chkAudit.Checked = false;
        };

        grp.Controls.Add(_chkMainTx);
        grp.Controls.Add(_chkDataCol);
        grp.Controls.Add(_chkBi);
        grp.Controls.Add(_chkAction);
        grp.Controls.Add(_chkBranching);
        grp.Controls.Add(_chkSupport);
        grp.Controls.Add(_chkInventory);
        grp.Controls.Add(_chkAudit);
        grp.Controls.Add(btnSelectAll);
        grp.Controls.Add(btnClearAll);
        y += 185;

        _chkIsActive = new CheckBox
        {
            Text = "Company Active & Operational (Permits Tenant Logins)",
            Left = 24,
            Top = y,
            Width = 470,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 45, 55),
            Checked = true,
            AutoSize = true
        };
        Controls.Add(_chkIsActive);
        y += 36;

        _lblStatus = new Label
        {
            Left = 24,
            Top = y,
            Width = 470,
            Height = 28,
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8.5f)
        };
        Controls.Add(_lblStatus);
        y += 32;

        _btnSave = new Button
        {
            Text = _isEdit ? "SAVE CHANGES" : "REGISTER COMPANY",
            Left = 24,
            Top = y,
            Width = 200,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.FromArgb(17, 24, 39),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += async (_, _) => await SaveAsync();
        Controls.Add(_btnSave);

        var btnCancel = new Button
        {
            Text = "CANCEL",
            Left = 234,
            Top = y,
            Width = 100,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(240, 242, 246),
            ForeColor = Color.FromArgb(70, 78, 92),
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnCancel);

        // Pre-populate if editing
        if (_isEdit && company.HasValue)
        {
            var c = company.Value;
            _txtCode.Text = GetProp(c, "companyCode", "CompanyCode");
            _txtName.Text = GetProp(c, "companyName", "CompanyName");

            if (bool.TryParse(GetProp(c, "isActive", "IsActive"), out var act))
                _chkIsActive.Checked = act;

            if (c.TryGetProperty("subscription", out var s) || c.TryGetProperty("Subscription", out s))
            {
                if (s.ValueKind == JsonValueKind.Object)
                {
                    string plan = GetProp(s, "planName", "PlanName");
                    var matchPlan = _cmbPlan.Items.Cast<string>().FirstOrDefault(p => p.Equals(plan, StringComparison.OrdinalIgnoreCase));
                    if (matchPlan != null) _cmbPlan.SelectedItem = matchPlan;

                    if (s.TryGetProperty("monthlyFee", out var feeProp) && feeProp.ValueKind == JsonValueKind.Number)
                    {
                        _txtFee.Text = feeProp.GetDecimal().ToString("F2");
                    }

                    string mods = GetProp(s, "availedModules", "AvailedModules");
                    if (!string.IsNullOrWhiteSpace(mods))
                    {
                        bool all = mods.Equals("All", StringComparison.OrdinalIgnoreCase);
                        var modSet = new HashSet<string>(mods.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase);

                        _chkMainTx.Checked = all || modSet.Contains("Main Transaction");
                        _chkDataCol.Checked = all || modSet.Contains("Data Collection");
                        _chkBi.Checked = all || modSet.Contains("Business Intelligence");
                        _chkAction.Checked = all || modSet.Contains("Action") || modSet.Contains("Action & Loyalty");
                        _chkBranching.Checked = all || modSet.Contains("Branching & Locations") || modSet.Contains("Branching");
                        _chkSupport.Checked = all || modSet.Contains("Customer Support & Issues") || modSet.Contains("Customer Support");
                        _chkInventory.Checked = all || modSet.Contains("Inventory & Supplies") || modSet.Contains("Inventory");
                        _chkAudit.Checked = all || modSet.Contains("Audit & Compliance") || modSet.Contains("Audit");
                    }
                }
            }
        }
    }

    private void AddLabel(string text, int top)
    {
        Controls.Add(new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(24, top)
        });
    }

    private TextBox AddText(int top)
    {
        var txt = new TextBox
        {
            Left = 24,
            Top = top,
            Width = 470,
            Height = 28,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(txt);
        return txt;
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
        string code = _txtCode.Text.Trim();
        string name = _txtName.Text.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            _lblStatus.ForeColor = Color.Firebrick;
            _lblStatus.Text = "Company code and company name are required.";
            return;
        }

        var selectedMods = new List<string>();
        if (_chkMainTx.Checked)    selectedMods.Add("Main Transaction");
        if (_chkDataCol.Checked)   selectedMods.Add("Data Collection");
        if (_chkBi.Checked)        selectedMods.Add("Business Intelligence");
        if (_chkAction.Checked)    selectedMods.Add("Action");
        if (_chkBranching.Checked) selectedMods.Add("Branching & Locations");
        if (_chkSupport.Checked)   selectedMods.Add("Customer Support & Issues");
        if (_chkInventory.Checked) selectedMods.Add("Inventory & Supplies");
        if (_chkAudit.Checked)     selectedMods.Add("Audit & Compliance");

        if (selectedMods.Count == 0)
        {
            _lblStatus.ForeColor = Color.Firebrick;
            _lblStatus.Text = "Please select at least one availed module.";
            return;
        }

        decimal.TryParse(_txtFee.Text.Trim(), out var fee);
        if (fee <= 0) fee = 2499m;

        string mods = (selectedMods.Count == 8) ? "All" : string.Join(",", selectedMods);
        string plan = _cmbPlan.SelectedItem?.ToString() ?? "Professional";
        bool isActive = _chkIsActive.Checked;

        _btnSave.Enabled = false;
        _lblStatus.ForeColor = Color.FromArgb(70, 78, 92);
        _lblStatus.Text = "Saving company and module entitlements...";

        try
        {
            if (_isEdit)
            {
                // 1. Update basic company info
                var compPayload = new
                {
                    CompanyCode = code.ToUpperInvariant(),
                    CompanyName = name,
                    IsActive = isActive
                };
                using var compReq = new HttpRequestMessage(HttpMethod.Put, $"{_apiUrl}/superadmin/companies/{_companyId}")
                {
                    Content = new StringContent(JsonSerializer.Serialize(compPayload), Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrWhiteSpace(Session.Token))
                    compReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

                var compRes = await _http.SendAsync(compReq);
                if (!compRes.IsSuccessStatusCode)
                {
                    var body = await compRes.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Company update failed: {body}");
                }

                // 2. Update subscription and modules
                var subPayload = new
                {
                    PlanName = plan,
                    Status = isActive ? "Active" : "Inactive",
                    MonthlyFee = fee,
                    AvailedModules = mods,
                    EndDate = (DateTime?)null,
                    IsActive = isActive
                };
                using var subReq = new HttpRequestMessage(HttpMethod.Put, $"{_apiUrl}/superadmin/companies/{_companyId}/subscription")
                {
                    Content = new StringContent(JsonSerializer.Serialize(subPayload), Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrWhiteSpace(Session.Token))
                    subReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

                var subRes = await _http.SendAsync(subReq);
                if (!subRes.IsSuccessStatusCode)
                {
                    var body = await subRes.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Subscription update failed: {body}");
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                // Create new company with subscription
                var payload = new
                {
                    CompanyCode = code.ToUpperInvariant(),
                    CompanyName = name,
                    PlanName = plan,
                    MonthlyFee = fee,
                    AvailedModules = mods
                };
                using var req = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/superadmin/companies")
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
                    _lblStatus.ForeColor = Color.Firebrick;
                    _lblStatus.Text = $"Failed to register company: {body}";
                    _btnSave.Enabled = true;
                }
            }
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor = Color.Firebrick;
            _lblStatus.Text = $"Error: {ex.Message}";
            _btnSave.Enabled = true;
        }
    }
}
