using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

        Text = _isEdit ? "Edit Company" : "Register New Company";
        Size = new Size(560, 680);
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
            Text = _isEdit ? "Edit Company Information" : "Create New Tenant Company",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 18)
        });

        int y = 56;

        AddLabel("Company Code * (e.g. LRSALON, MRDONUT, FUERTO)", y); y += 22;
        _txtCode = AddText(y); y += 40;

        AddLabel("Company Name * (e.g. Leo Revita Salon, Mister Donut)", y); y += 22;
        _txtName = AddText(y); y += 40;

        if (!_isEdit)
        {
            AddLabel("Initial Subscription Plan", y); y += 22;
            _cmbPlan = new ComboBox
            {
                Left = 24,
                Top = y,
                Width = 470,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cmbPlan.Items.AddRange(new object[] { "Enterprise", "Professional", "Starter" });
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
                Text = "Initial Availed Modules (SaaS Feature Entitlements)",
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
            _chkAction    = new CheckBox { Text = "Action & Loyalty", Left = 16, Top = 100, Width = 210, Font = new Font("Segoe UI", 9f) };

            _chkBranching = new CheckBox { Text = "Branching & Locations", Left = 240, Top = 22, Width = 230, Font = new Font("Segoe UI", 9f), Checked = true };
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
        }
        else
        {
            _chkIsActive = new CheckBox
            {
                Text = "Company Active & Operational",
                Left = 24,
                Top = y,
                Width = 300,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 45, 55),
                Checked = true,
                AutoSize = true
            };
            Controls.Add(_chkIsActive);
            y += 36;
        }

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
            Text = _isEdit ? "SAVE CHANGES" : "CREATE COMPANY",
            Left = 24,
            Top = y,
            Width = 190,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += async (_, _) => await SaveAsync();
        Controls.Add(_btnSave);

        var btnCancel = new Button
        {
            Text = "CANCEL",
            Left = 224,
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

        if (company.HasValue)
        {
            var c = company.Value;
            _txtCode.Text = GetProp(c, "companyCode", "CompanyCode");
            _txtName.Text = GetProp(c, "companyName", "CompanyName");
            if (_chkIsActive != null)
            {
                if (c.TryGetProperty("isActive", out var ia) || c.TryGetProperty("IsActive", out ia))
                    _chkIsActive.Checked = ia.GetBoolean();
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

        _btnSave.Enabled = false;
        _lblStatus.ForeColor = Color.FromArgb(70, 78, 92);
        _lblStatus.Text = "Saving company...";

        try
        {
            HttpRequestMessage req;
            if (_isEdit)
            {
                var payload = new
                {
                    CompanyCode = code.ToUpperInvariant(),
                    CompanyName = name,
                    IsActive = _chkIsActive?.Checked ?? true
                };
                req = new HttpRequestMessage(HttpMethod.Put, $"{_apiUrl}/superadmin/companies/{_companyId}")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
            }
            else
            {
                var selectedMods = new List<string>();
                if (_chkMainTx?.Checked == true)    selectedMods.Add("Main Transaction");
                if (_chkDataCol?.Checked == true)   selectedMods.Add("Data Collection");
                if (_chkBi?.Checked == true)        selectedMods.Add("Business Intelligence");
                if (_chkAction?.Checked == true)    selectedMods.Add("Action");
                if (_chkBranching?.Checked == true) selectedMods.Add("Branching & Locations");
                if (_chkSupport?.Checked == true)   selectedMods.Add("Customer Support & Issues");
                if (_chkInventory?.Checked == true) selectedMods.Add("Inventory & Supplies");
                if (_chkAudit?.Checked == true)     selectedMods.Add("Audit & Compliance");

                decimal.TryParse(_txtFee?.Text.Trim(), out var fee);
                if (fee <= 0) fee = 2499m;

                string mods = (selectedMods.Count == 8) ? "All" : string.Join(",", selectedMods);
                if (string.IsNullOrWhiteSpace(mods)) mods = "Main Transaction,Data Collection";

                var payload = new
                {
                    CompanyCode = code.ToUpperInvariant(),
                    CompanyName = name,
                    PlanName = _cmbPlan?.SelectedItem?.ToString() ?? "Professional",
                    MonthlyFee = fee,
                    AvailedModules = mods
                };
                req = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/superadmin/companies")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
            }

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
                _lblStatus.Text = $"Failed to save: {body}";
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
