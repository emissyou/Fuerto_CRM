using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class CompanySubscriptionDialog : Form
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly int _companyId;
    private readonly string _companyName;
    private readonly string _companyCode;

    private ComboBox _cmbPlan = null!;
    private ComboBox _cmbStatus = null!;
    private CheckBox _chkCompanyActive = null!;
    private TextBox _txtFee = null!;
    private CheckBox _chkMainTx = null!;
    private CheckBox _chkDataCol = null!;
    private CheckBox _chkBi = null!;
    private CheckBox _chkAction = null!;
    private CheckBox _chkBranching = null!;
    private CheckBox _chkSupport = null!;
    private CheckBox _chkInventory = null!;
    private CheckBox _chkAudit = null!;
    private Label _lblStatus = null!;
    private Button _btnSave = null!;

    public CompanySubscriptionDialog(
        string apiUrl,
        HttpClient http,
        int companyId,
        string companyName,
        string companyCode,
        string currentPlan,
        string currentStatus,
        decimal currentFee,
        string currentModules,
        bool currentCompanyActive = true)
    {
        _apiUrl = apiUrl;
        _http = http;
        _companyId = companyId;
        _companyName = companyName;
        _companyCode = companyCode;

        Text = $"Manage Subscription & Status - {_companyName}";
        Size = new Size(560, 715);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        BuildInterface(currentPlan, currentStatus, currentFee, currentModules, currentCompanyActive);
    }

    private void BuildInterface(string currentPlan, string currentStatus, decimal currentFee, string currentModules, bool currentCompanyActive)
    {
        Controls.Add(new Label
        {
            Text = "Company Subscription & Operational Status",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 18)
        });

        var lblSubtitle = new Label
        {
            Text = $"{_companyName} ({_companyCode}) · Tenant #{_companyId}",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(24, 44)
        };
        Controls.Add(lblSubtitle);

        int y = 74;

        // Plan
        AddLabel("Subscription Plan", y); y += 22;
        _cmbPlan = new ComboBox
        {
            Left = 24,
            Top = y,
            Width = 490,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbPlan.Items.AddRange(new object[] { "Enterprise", "Professional", "Starter", "Custom" });
        _cmbPlan.SelectedItem = _cmbPlan.Items.Cast<string>().FirstOrDefault(p => p.Equals(currentPlan, StringComparison.OrdinalIgnoreCase)) ?? "Professional";
        Controls.Add(_cmbPlan);
        y += 38;

        // Subscription Status
        AddLabel("Subscription Billing Status", y); y += 22;
        _cmbStatus = new ComboBox
        {
            Left = 24,
            Top = y,
            Width = 490,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbStatus.Items.AddRange(new object[] { "Active", "Trial", "Suspended", "Cancelled", "Inactive", "Expired" });
        _cmbStatus.SelectedItem = _cmbStatus.Items.Cast<string>().FirstOrDefault(s => s.Equals(currentStatus, StringComparison.OrdinalIgnoreCase)) ?? "Active";
        Controls.Add(_cmbStatus);
        y += 38;

        // Company Operational Active Status (Permits login)
        _chkCompanyActive = new CheckBox
        {
            Text = "Company Active & Operational (Permits Tenant Logins & Access)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Checked = currentCompanyActive,
            AutoSize = true,
            Left = 24,
            Top = y,
            Cursor = Cursors.Hand
        };
        Controls.Add(_chkCompanyActive);
        y += 34;

        // Keep Subscription Status & Company Active synchronized
        _cmbStatus.SelectedIndexChanged += (_, _) =>
        {
            string s = _cmbStatus.SelectedItem?.ToString() ?? "";
            if (s.Equals("Suspended", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Inactive", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Expired", StringComparison.OrdinalIgnoreCase))
            {
                _chkCompanyActive.Checked = false;
            }
            else if (s.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                     s.Equals("Trial", StringComparison.OrdinalIgnoreCase))
            {
                _chkCompanyActive.Checked = true;
            }
        };

        // Monthly Fee
        AddLabel("Monthly Fee (PHP)", y); y += 22;
        _txtFee = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 490,
            Height = 28,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            Text = currentFee.ToString("F2")
        };
        Controls.Add(_txtFee);
        y += 42;

        // Availed Modules Group
        var grpModules = new GroupBox
        {
            Text = "Availed Modules (SaaS Feature Entitlements)",
            Left = 24,
            Top = y,
            Width = 490,
            Height = 180,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 45, 55)
        };
        Controls.Add(grpModules);

        bool isAll = currentModules.Equals("All", StringComparison.OrdinalIgnoreCase);

        _chkMainTx = new CheckBox
        {
            Text = "Main Transaction",
            Left = 16, Top = 22, Width = 210, Font = new Font("Segoe UI", 9f),
            Checked = isAll || currentModules.Contains("Main Transaction", StringComparison.OrdinalIgnoreCase)
        };
        _chkDataCol = new CheckBox
        {
            Text = "Data Collection",
            Left = 16, Top = 48, Width = 210, Font = new Font("Segoe UI", 9f),
            Checked = isAll || currentModules.Contains("Data Collection", StringComparison.OrdinalIgnoreCase)
        };
        _chkBi = new CheckBox
        {
            Text = "Business Intelligence",
            Left = 16, Top = 74, Width = 210, Font = new Font("Segoe UI", 9f),
            Checked = isAll || currentModules.Contains("Business Intelligence", StringComparison.OrdinalIgnoreCase)
        };
        _chkAction = new CheckBox
        {
            Text = "Action & Loyalty",
            Left = 16, Top = 100, Width = 210, Font = new Font("Segoe UI", 9f),
            Checked = isAll || currentModules.Contains("Action", StringComparison.OrdinalIgnoreCase)
        };

        _chkBranching = new CheckBox
        {
            Text = "Branching & Locations",
            Left = 240, Top = 22, Width = 230, Font = new Font("Segoe UI", 9f),
            Checked = isAll || currentModules.Contains("Branching", StringComparison.OrdinalIgnoreCase) || currentModules.Contains("Team & Branching", StringComparison.OrdinalIgnoreCase)
        };
        _chkSupport = new CheckBox
        {
            Text = "Customer Support & Issues",
            Left = 240, Top = 48, Width = 230, Font = new Font("Segoe UI", 9f),
            Checked = isAll || currentModules.Contains("Support", StringComparison.OrdinalIgnoreCase) || currentModules.Contains("Issues", StringComparison.OrdinalIgnoreCase)
        };
        _chkInventory = new CheckBox
        {
            Text = "Inventory & Supplies",
            Left = 240, Top = 74, Width = 230, Font = new Font("Segoe UI", 9f),
            Checked = isAll || currentModules.Contains("Inventory", StringComparison.OrdinalIgnoreCase)
        };
        _chkAudit = new CheckBox
        {
            Text = "Audit & Compliance",
            Left = 240, Top = 100, Width = 230, Font = new Font("Segoe UI", 9f),
            Checked = isAll || currentModules.Contains("Audit", StringComparison.OrdinalIgnoreCase)
        };

        var btnSelectAll = new LinkLabel { Text = "Select All", Left = 16, Top = 142, AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        btnSelectAll.LinkClicked += (_, _) =>
        {
            _chkMainTx.Checked = _chkDataCol.Checked = _chkBi.Checked = _chkAction.Checked =
            _chkBranching.Checked = _chkSupport.Checked = _chkInventory.Checked = _chkAudit.Checked = true;
        };

        var btnClearAll = new LinkLabel { Text = "Clear All", Left = 90, Top = 142, AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        btnClearAll.LinkClicked += (_, _) =>
        {
            _chkMainTx.Checked = _chkDataCol.Checked = _chkBi.Checked = _chkAction.Checked =
            _chkBranching.Checked = _chkSupport.Checked = _chkInventory.Checked = _chkAudit.Checked = false;
        };

        grpModules.Controls.Add(_chkMainTx);
        grpModules.Controls.Add(_chkDataCol);
        grpModules.Controls.Add(_chkBi);
        grpModules.Controls.Add(_chkAction);
        grpModules.Controls.Add(_chkBranching);
        grpModules.Controls.Add(_chkSupport);
        grpModules.Controls.Add(_chkInventory);
        grpModules.Controls.Add(_chkAudit);
        grpModules.Controls.Add(btnSelectAll);
        grpModules.Controls.Add(btnClearAll);

        y += 190;

        _lblStatus = new Label
        {
            Left = 24,
            Top = y,
            Width = 490,
            Height = 28,
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8.5f)
        };
        Controls.Add(_lblStatus);
        y += 30;

        _btnSave = new Button
        {
            Text = "SAVE SUBSCRIPTION & STATUS",
            Left = 24,
            Top = y,
            Width = 240,
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
            Left = 274,
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

    private async Task SaveAsync()
    {
        if (!decimal.TryParse(_txtFee.Text.Trim(), out var fee) || fee < 0)
        {
            _lblStatus.ForeColor = Color.Firebrick;
            _lblStatus.Text = "Please enter a valid monthly fee.";
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

        string plan = _cmbPlan.SelectedItem?.ToString() ?? "Professional";
        string status = _cmbStatus.SelectedItem?.ToString() ?? "Active";
        string modulesStr = (selectedMods.Count == 8) ? "All" : string.Join(",", selectedMods);
        bool isActive = _chkCompanyActive.Checked;

        var payload = new
        {
            PlanName = plan,
            Status = status,
            MonthlyFee = fee,
            AvailedModules = modulesStr,
            EndDate = (DateTime?)null,
            IsActive = isActive
        };

        _btnSave.Enabled = false;
        _lblStatus.ForeColor = Color.FromArgb(70, 78, 92);
        _lblStatus.Text = "Updating subscription & company status...";

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, $"{_apiUrl}/superadmin/companies/{_companyId}/subscription")
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
                _lblStatus.Text = $"Failed: {body}";
                _btnSave.Enabled = true;
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
