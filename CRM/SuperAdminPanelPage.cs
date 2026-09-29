using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Super Admin – Tenant & Subscription Management (Admin Panel).
/// Highly functional, robust row selection, quick actions, context menu.
/// Controls added in reverse order for accurate top-to-bottom rendering.
/// </summary>
public class SuperAdminPanelPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly Action<string>? _navigateCallback;

    private DataGridView _grid          = null!;
    private Label        _lblStatus     = null!;
    private Button       _btnNewCompany = null!;
    private Button       _btnEditSub    = null!;
    private Button       _btnEditComp   = null!;
    private Button       _btnRefresh    = null!;
    private CrmFilterBar _filterBar     = null!;
    private Label        _lblSelectionHint = null!;

    private Label _lblTotalComp = null!;
    private Label _lblActiveSub = null!;
    private Label _lblBillings  = null!;

    private List<JsonElement> _allCompanies      = new();
    private List<JsonElement> _filteredCompanies = new();
    private JsonElement?      _selectedRow;

    private static readonly Color CBg     = Color.FromArgb(245, 247, 250);
    private static readonly Color CCard   = Color.White;
    private static readonly Color CBorder = Color.FromArgb(226, 230, 236);
    private static readonly Color CText   = Color.FromArgb(15, 23, 42);
    private static readonly Color CMuted  = Color.FromArgb(100, 116, 139);
    private static readonly Color CAccent = Color.FromArgb(255, 168, 0);
    private static readonly Color CGreen  = Color.FromArgb(22, 163, 74);
    private static readonly Color CBlue   = Color.FromArgb(59, 130, 246);

    public SuperAdminPanelPage(string apiUrl, HttpClient http, Action<string>? navigateCallback = null)
    {
        _apiUrl           = apiUrl;
        _http             = http;
        _navigateCallback = navigateCallback;

        Dock      = DockStyle.Fill;
        BackColor = CBg;
        Padding   = new Padding(28, 14, 28, 28);

        BuildInterface();
        _ = LoadCompaniesAsync();
    }

    private void BuildInterface()
    {
        Controls.Clear();

        // ── ADD IN REVERSE ORDER: last added = topmost displayed ──────────

        // [BOTTOM] Tenant grid card
        var gridCard = new Panel { Dock = DockStyle.Fill, BackColor = CCard, Padding = new Padding(14, 8, 14, 10) };
        gridCard.Paint += (_, pe) => pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, gridCard.Width - 1, gridCard.Height - 1);
        Controls.Add(gridCard);

        var gridHeader = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
        gridCard.Controls.Add(gridHeader);
        gridHeader.Controls.Add(new Label
        {
            Text = "REGISTERED TENANT COMPANIES  (Double-click any row to edit)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = CMuted,
            AutoSize = false,
            Width = 500,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft
        });

        _grid = new DataGridView
        {
            Dock            = DockStyle.Fill,
            BackgroundColor = CCard,
            BorderStyle     = BorderStyle.None,
            SelectionMode   = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect     = false,
            ReadOnly        = true
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyId",      HeaderText = "ID",               FillWeight = 38  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code",           HeaderText = "CODE",             FillWeight = 75  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",           HeaderText = "COMPANY NAME",     FillWeight = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Plan",           HeaderText = "PLAN",             FillWeight = 90  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fee",            HeaderText = "MONTHLY FEE",      FillWeight = 85  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",         HeaderText = "STATUS",           FillWeight = 68  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvailedModules", HeaderText = "AVAILED MODULES",  FillWeight = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Database",       HeaderText = "DATABASE",         FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Created",        HeaderText = "JOINED",           FillWeight = 80  });

        CrmTableStyler.Apply(_grid, "Plan", "Status");

        // Wire selection events
        _grid.SelectionChanged += (_, _) => UpdateSelectionState();
        _grid.CellClick        += (_, e) => { if (e.RowIndex >= 0) UpdateSelectionState(); };
        _grid.RowEnter         += (_, e) => { if (e.RowIndex >= 0) UpdateSelectionState(); };
        _grid.CellDoubleClick  += async (_, e) =>
        {
            if (e.RowIndex >= 0) await OpenEditCompanyDialogAsync();
        };

        // Context Menu
        var ctx = new ContextMenuStrip();
        var mnuEditComp = new ToolStripMenuItem("✎ Edit Company", null, async (_, _) => await OpenEditCompanyDialogAsync());
        var mnuEditSub  = new ToolStripMenuItem("⚡ Manage Subscription", null, async (_, _) => await OpenSubscriptionDialogAsync());
        var mnuAdmins   = new ToolStripMenuItem("👥 View Admins", null, (_, _) => _navigateCallback?.Invoke("Company Admins"));
        var mnuBranches = new ToolStripMenuItem("🏢 View Branches", null, (_, _) => _navigateCallback?.Invoke("Branches"));
        ctx.Items.AddRange(new ToolStripItem[] { mnuEditComp, mnuEditSub, new ToolStripSeparator(), mnuAdmins, mnuBranches });
        _grid.ContextMenuStrip = ctx;

        gridCard.Controls.Add(_grid);
        _grid.BringToFront();

        // [MIDDLE] Filter bar
        _filterBar = new CrmFilterBar("Search by company code or name…");
        _filterBar.FiltersChanged += (_, _) => ApplyFilters();
        _filterBar.AddFilter("plan",   "Plan",   "Enterprise", "Professional", "Starter");
        _filterBar.AddFilter("status", "Status", "Active", "Trial", "Inactive");
        Controls.Add(_filterBar);

        // [MIDDLE] Quick action toolbar
        var actionStrip = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 46,
            BackColor = Color.FromArgb(249, 250, 252),
            Padding   = new Padding(10, 6, 10, 6)
        };
        actionStrip.Paint += (_, pe) =>
        {
            pe.Graphics.DrawLine(new Pen(CBorder, 1), 0, 0, actionStrip.Width, 0);
            pe.Graphics.DrawLine(new Pen(CBorder, 1), 0, actionStrip.Height - 1, actionStrip.Width, actionStrip.Height - 1);
        };
        Controls.Add(actionStrip);

        int ax = 10;

        _btnEditComp = MakeActionBtn("✎  Edit Company", 140, CAccent, solid: true);
        _btnEditComp.Left    = ax; ax += 148;
        _btnEditComp.Enabled = false;
        _btnEditComp.Click  += async (_, _) => await OpenEditCompanyDialogAsync();
        actionStrip.Controls.Add(_btnEditComp);

        _btnEditSub = MakeActionBtn("⚡  Manage Subscription", 180, Color.FromArgb(37, 99, 235), solid: true);
        _btnEditSub.Left    = ax; ax += 188;
        _btnEditSub.Enabled = false;
        _btnEditSub.Click  += async (_, _) => await OpenSubscriptionDialogAsync();
        actionStrip.Controls.Add(_btnEditSub);

        var btnAdmins = MakeActionBtn("👥  Company Admins", 145, CCard, solid: false);
        btnAdmins.Left   = ax; ax += 153;
        btnAdmins.Click += (_, _) => _navigateCallback?.Invoke("Company Admins");
        actionStrip.Controls.Add(btnAdmins);

        var btnBranches = MakeActionBtn("🏢  Branches", 110, CCard, solid: false);
        btnBranches.Left   = ax; ax += 118;
        btnBranches.Click += (_, _) => _navigateCallback?.Invoke("Branches");
        actionStrip.Controls.Add(btnBranches);

        _lblSelectionHint = new Label
        {
            Text      = "ℹ Select a company to edit or manage subscription",
            ForeColor = CMuted,
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            AutoSize  = true,
            Anchor    = AnchorStyles.Right | AnchorStyles.Top,
            Top       = 13
        };
        _lblSelectionHint.Left = actionStrip.Width - _lblSelectionHint.PreferredWidth - 14;
        actionStrip.Resize += (_, _) => _lblSelectionHint.Left = actionStrip.Width - _lblSelectionHint.PreferredWidth - 14;
        actionStrip.Controls.Add(_lblSelectionHint);

        // [TOP-ish] Primary toolbar
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        Controls.Add(toolbar);

        int left = 0;

        _btnNewCompany = MakeBtn("＋  Register New Company", 190, true);
        _btnNewCompany.Left   = left; left += 200;
        _btnNewCompany.Click += async (_, _) => await OpenNewCompanyDialogAsync();
        toolbar.Controls.Add(_btnNewCompany);

        _btnRefresh = MakeBtn("↻  Refresh", 100, false);
        _btnRefresh.Left   = left; left += 110;
        _btnRefresh.Click += async (_, _) => await LoadCompaniesAsync();
        toolbar.Controls.Add(_btnRefresh);

        _lblStatus = new Label
        {
            Left      = left,
            Top       = 16,
            AutoSize  = true,
            ForeColor = CMuted,
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic)
        };
        toolbar.Controls.Add(_lblStatus);

        // [TOP] KPI cards row
        var kpiRow = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };
        Controls.Add(kpiRow);

        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent };
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        kpiRow.Controls.Add(tbl);

        tbl.Controls.Add(MakeKpi("🏢  REGISTERED TENANTS",  CBlue,   out _lblTotalComp), 0, 0);
        tbl.Controls.Add(MakeKpi("✅  ACTIVE SUBSCRIBERS",  CGreen,  out _lblActiveSub), 1, 0);
        tbl.Controls.Add(MakeKpi("₱  MONTHLY BILLINGS",    CAccent, out _lblBillings),  2, 0);
    }

    private void UpdateSelectionState()
    {
        var sel = GetSelectedCompany();
        if (sel.HasValue)
        {
            _selectedRow = sel;
            _btnEditComp.Enabled = true;
            _btnEditSub.Enabled  = true;
            string compName = GP(sel.Value, "companyName", "CompanyName");
            _lblSelectionHint.Text = $"Selected: {compName}";
            _lblSelectionHint.ForeColor = Color.FromArgb(22, 101, 52);
        }
        else
        {
            _selectedRow = null;
            _btnEditComp.Enabled = false;
            _btnEditSub.Enabled  = false;
            _lblSelectionHint.Text = "ℹ Select a company to edit or manage subscription";
            _lblSelectionHint.ForeColor = CMuted;
        }
    }

    private JsonElement? GetSelectedCompany()
    {
        if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0)
        {
            if (_grid.CurrentRow.Tag is JsonElement el) return el;

            // Fallback: lookup by ID column
            var idCell = _grid.CurrentRow.Cells["CompanyId"]?.Value;
            if (idCell != null && int.TryParse(idCell.ToString(), out int cid))
            {
                var match = _allCompanies.FirstOrDefault(c =>
                {
                    if (c.TryGetProperty("companyId", out var idProp) || c.TryGetProperty("CompanyId", out idProp))
                        return idProp.GetInt32() == cid;
                    return false;
                });
                if (match.ValueKind != JsonValueKind.Undefined) return match;
            }
        }

        if (_selectedRow.HasValue) return _selectedRow;
        return null;
    }

    // ── KPI card factory ─────────────────────────────────────────────────
    private Panel MakeKpi(string title, Color accent, out Label val)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = CCard, Margin = new Padding(0, 0, 12, 0) };
        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
            using var b = new SolidBrush(accent);
            pe.Graphics.FillRectangle(b, 0, 0, card.Width, 4);
        };
        card.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = CMuted, AutoSize = false, Width = 300, Height = 20, Location = new Point(14, 16), TextAlign = ContentAlignment.MiddleLeft });
        val = new Label { Text = "—", Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = CText, AutoSize = false, Width = 300, Height = 40, Location = new Point(14, 38), TextAlign = ContentAlignment.MiddleLeft };
        card.Controls.Add(val);
        return card;
    }

    private static Button MakeBtn(string text, int width, bool primary)
    {
        var btn = new Button
        {
            Text      = text,
            Width     = width,
            Height    = 36,
            Top       = 8,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? CAccent : CCard,
            ForeColor = primary ? Color.White : Color.FromArgb(55, 65, 81),
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize  = primary ? 0 : 1;
        btn.FlatAppearance.BorderColor = CBorder;
        return btn;
    }

    private static Button MakeActionBtn(string text, int width, Color bg, bool solid = true)
    {
        var btn = new Button
        {
            Text      = text,
            Width     = width,
            Height    = 32,
            Top       = 7,
            FlatStyle = FlatStyle.Flat,
            BackColor = solid ? bg : CCard,
            ForeColor = solid ? Color.White : Color.FromArgb(55, 65, 81),
            Font      = new Font("Segoe UI", 8.8f, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize  = 1;
        btn.FlatAppearance.BorderColor = solid ? bg : CBorder;
        return btn;
    }

    // ── Data loading ─────────────────────────────────────────────────────
    public async Task LoadCompaniesAsync()
    {
        _lblStatus.Text = "Loading tenants…";
        _lblStatus.ForeColor = CMuted;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_apiUrl}/superadmin/companies");
            if (!string.IsNullOrWhiteSpace(Session.Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var json      = await res.Content.ReadAsStringAsync();
                _allCompanies = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
                UpdateKpis();
                ApplyFilters();
                _lblStatus.Text = $"✓ {_allCompanies.Count} tenants loaded";
                _lblStatus.ForeColor = CGreen;
            }
            else
            {
                _lblStatus.Text = $"⚠ HTTP {(int)res.StatusCode}";
                _lblStatus.ForeColor = Color.Firebrick;
            }
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"⚠ {ex.Message}";
            _lblStatus.ForeColor = Color.Firebrick;
        }
    }

    private void UpdateKpis()
    {
        int total  = _allCompanies.Count;
        int active = _allCompanies.Count(c =>
        {
            if (c.TryGetProperty("isActive", out var ia) || c.TryGetProperty("IsActive", out ia))
                return ia.GetBoolean();
            return false;
        });

        decimal billing = 0;
        foreach (var c in _allCompanies)
            if (c.TryGetProperty("subscription", out var s) || c.TryGetProperty("Subscription", out s))
                if (s.ValueKind == JsonValueKind.Object)
                    if (s.TryGetProperty("monthlyFee", out var f) || s.TryGetProperty("MonthlyFee", out f))
                        billing += f.GetDecimal();

        _lblTotalComp.Text = total.ToString();
        _lblActiveSub.Text = active.ToString();
        _lblBillings.Text  = $"₱{billing:N0}";
    }

    private void ApplyFilters()
    {
        string q     = _filterBar.SearchText.ToLowerInvariant();
        string? plan = _filterBar.GetFilterValue("plan");
        string? stat = _filterBar.GetFilterValue("status");

        _filteredCompanies = _allCompanies.Where(c =>
        {
            string code = GP(c, "companyCode", "CompanyCode").ToLowerInvariant();
            string name = GP(c, "companyName", "CompanyName").ToLowerInvariant();
            if (!string.IsNullOrEmpty(q) && !code.Contains(q) && !name.Contains(q)) return false;
            if (!string.IsNullOrEmpty(plan) || !string.IsNullOrEmpty(stat))
            {
                if (c.TryGetProperty("subscription", out var s) || c.TryGetProperty("Subscription", out s))
                    if (s.ValueKind == JsonValueKind.Object)
                    {
                        if (!string.IsNullOrEmpty(plan) && !GP(s, "planName", "PlanName").Equals(plan, StringComparison.OrdinalIgnoreCase)) return false;
                        if (!string.IsNullOrEmpty(stat) && !GP(s, "status",   "Status").Equals(stat,   StringComparison.OrdinalIgnoreCase)) return false;
                    }
            }
            return true;
        }).ToList();

        _filterBar.SetRecordCount(_filteredCompanies.Count, _allCompanies.Count);
        RenderGrid();
    }

    private void RenderGrid()
    {
        _grid.Rows.Clear();
        foreach (var c in _filteredCompanies)
        {
            int     id      = 0;
            string  code    = GP(c, "companyCode", "CompanyCode");
            string  name    = GP(c, "companyName", "CompanyName");
            string  plan    = "—", status = "—", modules = "—", db = "—";
            decimal fee     = 0;

            if (c.TryGetProperty("companyId", out var cid) || c.TryGetProperty("CompanyId", out cid)) id = cid.GetInt32();
            if (c.TryGetProperty("subscription", out var s) || c.TryGetProperty("Subscription", out s))
                if (s.ValueKind == JsonValueKind.Object)
                {
                    plan    = GP(s, "planName", "PlanName");
                    status  = GP(s, "status",   "Status");
                    modules = GP(s, "availedModules", "AvailedModules");
                    if (s.TryGetProperty("monthlyFee", out var f) || s.TryGetProperty("MonthlyFee", out f)) fee = f.GetDecimal();
                }

            if (c.TryGetProperty("databases", out var dbs) || c.TryGetProperty("Databases", out dbs))
                if (dbs.ValueKind == JsonValueKind.Array && dbs.GetArrayLength() > 0)
                    db = GP(dbs[0], "databaseName", "DatabaseName");

            string cr = GP(c, "createdAt", "CreatedAt");
            string dt = DateTime.TryParse(cr, out var d) ? d.ToString("MMM d, yyyy") : "—";

            int rowIdx = _grid.Rows.Add(id, code, name, plan, $"₱{fee:N0}", status, modules, db, dt);
            _grid.Rows[rowIdx].Tag = c;
        }

        if (_grid.Rows.Count > 0)
        {
            _grid.Rows[0].Selected = true;
            UpdateSelectionState();
        }
        else
        {
            UpdateSelectionState();
        }
    }

    // ── Dialogs ──────────────────────────────────────────────────────────
    private async Task OpenNewCompanyDialogAsync()
    {
        using var dlg = new CompanyDialog(_apiUrl, _http);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadCompaniesAsync();
    }

    private async Task OpenEditCompanyDialogAsync()
    {
        var sel = GetSelectedCompany();
        if (!sel.HasValue)
        {
            MessageBox.Show("Please select a company row from the table first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new CompanyDialog(_apiUrl, _http, sel.Value);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadCompaniesAsync();
    }

    private async Task OpenSubscriptionDialogAsync()
    {
        var sel = GetSelectedCompany();
        if (!sel.HasValue)
        {
            MessageBox.Show("Please select a company row from the table first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var c = sel.Value;
        int     id      = 0;
        string  name    = GP(c, "companyName", "CompanyName");
        string  code    = GP(c, "companyCode", "CompanyCode");
        string  plan    = "Professional";
        string  status  = "Active";
        decimal fee     = 2499m;
        string  modules = "Main Transaction,Data Collection";

        if (c.TryGetProperty("companyId", out var cid) || c.TryGetProperty("CompanyId", out cid)) id = cid.GetInt32();
        if (c.TryGetProperty("subscription", out var s) || c.TryGetProperty("Subscription", out s))
            if (s.ValueKind == JsonValueKind.Object)
            {
                plan    = GP(s, "planName", "PlanName");
                status  = GP(s, "status",   "Status");
                modules = GP(s, "availedModules", "AvailedModules");
                if (s.TryGetProperty("monthlyFee", out var f) || s.TryGetProperty("MonthlyFee", out f)) fee = f.GetDecimal();
            }

        bool isCompActive = true;
        if (c.TryGetProperty("isActive", out var act) || c.TryGetProperty("IsActive", out act)) isCompActive = act.GetBoolean();

        using var dlg = new CompanySubscriptionDialog(_apiUrl, _http, id, name, code, plan, status, fee, modules, isCompActive);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadCompaniesAsync();
    }

    private static string GP(JsonElement el, params string[] names)
    {
        foreach (var n in names)
            if (el.TryGetProperty(n, out var v) && v.ValueKind != JsonValueKind.Null)
                return v.ToString();
        return "";
    }
}
