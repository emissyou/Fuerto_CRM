using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Super Admin – Subscriptions & Billing management page.
/// Allows viewing, filtering, and directly changing Company operational status
/// and Subscription billing status across all tenant organizations.
/// </summary>
public class SuperAdminSubscriptionsPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private DataGridView _grid           = null!;
    private Label        _lblStatus      = null!;
    private CrmFilterBar _filterBar      = null!;
    private Label        _lblMrr         = null!;
    private Label        _lblActive      = null!;
    private Label        _lblExpiring    = null!;
    private Button       _btnEditStatus  = null!;
    private Button       _btnActivate    = null!;
    private Button       _btnSuspend     = null!;

    private List<JsonElement> _all       = new();
    private List<JsonElement> _filtered  = new();
    private JsonElement?      _selectedRow;

    private static readonly Color CBg     = Color.FromArgb(245, 247, 250);
    private static readonly Color CCard   = Color.White;
    private static readonly Color CBorder = Color.FromArgb(226, 230, 236);
    private static readonly Color CText   = Color.FromArgb(15, 23, 42);
    private static readonly Color CMuted  = Color.FromArgb(100, 116, 139);
    private static readonly Color CAccent = Color.FromArgb(255, 168, 0);
    private static readonly Color CGreen  = Color.FromArgb(22, 163, 74);
    private static readonly Color CBlue   = Color.FromArgb(59, 130, 246);
    private static readonly Color CRed    = Color.FromArgb(220, 38, 38);

    public SuperAdminSubscriptionsPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http   = http;
        Dock      = DockStyle.Fill;
        BackColor = CBg;
        Padding   = new Padding(28, 16, 28, 28);
        BuildUI();
        _ = LoadAsync();
    }

    private void BuildUI()
    {
        Controls.Clear();

        // 1. KPI Row
        var kpiRow = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };
        Controls.Add(kpiRow);

        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent };
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        kpiRow.Controls.Add(tbl);

        tbl.Controls.Add(MakeKpi("₱  TOTAL MONTHLY REVENUE",  CGreen,  out _lblMrr),     0, 0);
        tbl.Controls.Add(MakeKpi("✅  ACTIVE TENANTS",        CBlue,   out _lblActive),   1, 0);
        tbl.Controls.Add(MakeKpi("⚠  SUSPENDED / INACTIVE",  CRed,    out _lblExpiring), 2, 0);

        // 2. Toolbar with direct status change actions
        var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        Controls.Add(bar);

        int bx = 0;

        var btnRefresh = MakeBtn("↻  Refresh", 100, false, Color.FromArgb(241, 245, 249), CText);
        btnRefresh.Left = bx;
        btnRefresh.Click += async (_, _) => await LoadAsync();
        bar.Controls.Add(btnRefresh);
        bx += 108;

        _btnEditStatus = MakeBtn("✎  Change Status & Subscription", 240, true, CAccent, Color.FromArgb(17, 24, 39));
        _btnEditStatus.Left = bx;
        _btnEditStatus.Enabled = false;
        _btnEditStatus.Click += async (_, _) => await OpenSubscriptionDialogAsync();
        bar.Controls.Add(_btnEditStatus);
        bx += 248;

        _btnActivate = MakeBtn("🟢  Set Active", 124, false, Color.FromArgb(240, 253, 244), Color.FromArgb(22, 101, 52));
        _btnActivate.Left = bx;
        _btnActivate.Enabled = false;
        _btnActivate.Click += async (_, _) => await QuickSetStatusAsync("Active", true);
        bar.Controls.Add(_btnActivate);
        bx += 132;

        _btnSuspend = MakeBtn("🔴  Suspend Company", 150, false, Color.FromArgb(254, 242, 242), Color.FromArgb(185, 28, 28));
        _btnSuspend.Left = bx;
        _btnSuspend.Enabled = false;
        _btnSuspend.Click += async (_, _) => await QuickSetStatusAsync("Suspended", false);
        bar.Controls.Add(_btnSuspend);
        bx += 160;

        _lblStatus = new Label
        {
            Left = bx,
            Top = 14,
            AutoSize = true,
            ForeColor = CMuted,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Italic)
        };
        bar.Controls.Add(_lblStatus);

        // 3. Filter Bar
        _filterBar = new CrmFilterBar("Search companies or codes...");
        _filterBar.FiltersChanged += (_, _) => Apply();
        _filterBar.AddFilter("companyStatus", "Company Status", "Active", "Inactive");
        _filterBar.AddFilter("plan",          "Plan Tier",      "Enterprise", "Professional", "Starter");
        _filterBar.AddFilter("subStatus",     "Sub Status",     "Active", "Trial", "Suspended", "Cancelled", "Expired");
        Controls.Add(_filterBar);

        // 4. Grid Card
        var card = MakeCard();
        Controls.Add(card);
        card.BringToFront();

        var cardHeader = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
        card.Controls.Add(cardHeader);
        cardHeader.Controls.Add(new Label
        {
            Text = "TENANT SUBSCRIPTIONS & COMPANY ACCESS  (Double-click any row to change company status)",
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
            ForeColor = CMuted,
            AutoSize = false,
            Width = 650,
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code",          HeaderText = "CODE",            FillWeight = 65  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Company",       HeaderText = "COMPANY NAME",    FillWeight = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyStatus", HeaderText = "COMPANY ACCESS",  FillWeight = 85  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubStatus",     HeaderText = "BILLING STATUS",  FillWeight = 85  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Plan",          HeaderText = "PLAN TIER",       FillWeight = 90  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fee",           HeaderText = "MONTHLY FEE",     FillWeight = 80  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modules",       HeaderText = "AVAILED MODULES", FillWeight = 190 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Start",         HeaderText = "START DATE",      FillWeight = 75  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "End",           HeaderText = "END DATE",        FillWeight = 75  });

        CrmTableStyler.Apply(_grid, "CompanyStatus", "SubStatus", "Plan");

        // Wire Events
        _grid.SelectionChanged += (_, _) => UpdateSelectionState();
        _grid.CellClick        += (_, e) => { if (e.RowIndex >= 0) UpdateSelectionState(); };
        _grid.RowEnter         += (_, e) => { if (e.RowIndex >= 0) UpdateSelectionState(); };
        _grid.CellDoubleClick  += async (_, e) =>
        {
            if (e.RowIndex >= 0) await OpenSubscriptionDialogAsync();
        };

        // Context Menu
        var ctx = new ContextMenuStrip();
        var mnuManage = new ToolStripMenuItem("✎ Manage Status & Subscription...", null, async (_, _) => await OpenSubscriptionDialogAsync());
        var mnuActive = new ToolStripMenuItem("🟢 Set Status: Active", null, async (_, _) => await QuickSetStatusAsync("Active", true));
        var mnuTrial  = new ToolStripMenuItem("🟡 Set Status: Trial", null, async (_, _) => await QuickSetStatusAsync("Trial", true));
        var mnuSuspend= new ToolStripMenuItem("🔴 Set Status: Suspended", null, async (_, _) => await QuickSetStatusAsync("Suspended", false));
        var mnuCancel = new ToolStripMenuItem("⛔ Set Status: Cancelled / Inactive", null, async (_, _) => await QuickSetStatusAsync("Cancelled", false));

        ctx.Items.AddRange(new ToolStripItem[] { mnuManage, new ToolStripSeparator(), mnuActive, mnuTrial, mnuSuspend, mnuCancel });
        _grid.ContextMenuStrip = ctx;

        card.Controls.Add(_grid);
        _grid.BringToFront();
    }

    public async Task LoadAsync()
    {
        _lblStatus.Text = "Loading subscriptions…";
        _lblStatus.ForeColor = CMuted;
        try
        {
            EnsureAuthHeader();
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_apiUrl}/superadmin/companies");
            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                _all     = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
                UpdateKpis();
                Apply();
                _lblStatus.Text      = $"✓ {_all.Count} tenants loaded";
                _lblStatus.ForeColor = CGreen;
            }
            else
            {
                _lblStatus.Text      = $"⚠ HTTP {(int)res.StatusCode}";
                _lblStatus.ForeColor = CRed;
            }
        }
        catch (Exception ex)
        {
            _lblStatus.Text      = $"⚠ {ex.Message}";
            _lblStatus.ForeColor = CRed;
        }
    }

    private void UpdateSelectionState()
    {
        if (_grid.CurrentRow?.Tag is JsonElement el)
        {
            _selectedRow = el;
            _btnEditStatus.Enabled = true;

            bool isCompActive = true;
            if (el.TryGetProperty("isActive", out var act) || el.TryGetProperty("IsActive", out act))
                isCompActive = act.GetBoolean();

            _btnActivate.Enabled = !isCompActive;
            _btnSuspend.Enabled  = isCompActive;

            string cName = GP(el, "companyName", "CompanyName");
            _lblStatus.Text = $"Selected: {cName} ({(isCompActive ? "Active" : "Suspended")})";
            _lblStatus.ForeColor = CText;
        }
        else
        {
            _selectedRow = null;
            _btnEditStatus.Enabled = false;
            _btnActivate.Enabled   = false;
            _btnSuspend.Enabled    = false;
        }
    }

    private async Task OpenSubscriptionDialogAsync()
    {
        if (!_selectedRow.HasValue) return;

        var c = _selectedRow.Value;
        int     id      = 0;
        string  name    = GP(c, "companyName", "CompanyName");
        string  code    = GP(c, "companyCode", "CompanyCode");
        string  plan    = "Professional";
        string  status  = "Active";
        decimal fee     = 2499m;
        string  modules = "Main Transaction,Data Collection";
        bool    isCompActive = true;

        if (c.TryGetProperty("companyId", out var cid) || c.TryGetProperty("CompanyId", out cid)) id = cid.GetInt32();
        if (c.TryGetProperty("isActive", out var act) || c.TryGetProperty("IsActive", out act)) isCompActive = act.GetBoolean();

        if (c.TryGetProperty("subscription", out var s) || c.TryGetProperty("Subscription", out s))
        {
            if (s.ValueKind == JsonValueKind.Object)
            {
                plan    = GP(s, "planName", "PlanName");
                status  = GP(s, "status",   "Status");
                modules = GP(s, "availedModules", "AvailedModules");
                if (s.TryGetProperty("monthlyFee", out var f) || s.TryGetProperty("MonthlyFee", out f)) fee = f.GetDecimal();
            }
        }

        using var dlg = new CompanySubscriptionDialog(_apiUrl, _http, id, name, code, plan, status, fee, modules, isCompActive);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await LoadAsync();
        }
    }

    private async Task QuickSetStatusAsync(string newStatus, bool makeActive)
    {
        if (!_selectedRow.HasValue) return;

        var c = _selectedRow.Value;
        int id = 0;
        if (c.TryGetProperty("companyId", out var cid) || c.TryGetProperty("CompanyId", out cid)) id = cid.GetInt32();
        string name = GP(c, "companyName", "CompanyName");

        var confirm = MessageBox.Show(
            $"Change status for {name} to '{newStatus}' (Company Access: {(makeActive ? "ACTIVE" : "SUSPENDED")})?",
            "Confirm Status Change",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            EnsureAuthHeader();
            var payload = new { Status = newStatus, IsActive = makeActive };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _http.PutAsync($"{_apiUrl}/superadmin/companies/{id}/status", content);
            if (response.IsSuccessStatusCode)
            {
                _lblStatus.Text = $"✓ Status for {name} updated to {newStatus}.";
                _lblStatus.ForeColor = CGreen;
                await LoadAsync();
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Failed to change status: {body}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error updating status: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateKpis()
    {
        decimal mrr    = 0;
        int active     = 0;
        int inactive   = 0;

        foreach (var c in _all)
        {
            bool isCompActive = true;
            if (c.TryGetProperty("isActive", out var act) || c.TryGetProperty("IsActive", out act))
                isCompActive = act.GetBoolean();

            if (isCompActive) active++;
            else inactive++;

            if (c.TryGetProperty("subscription", out var sub) || c.TryGetProperty("Subscription", out sub))
            {
                if (sub.ValueKind == JsonValueKind.Object)
                {
                    if (sub.TryGetProperty("monthlyFee", out var f) || sub.TryGetProperty("MonthlyFee", out f))
                    {
                        if (isCompActive) mrr += f.GetDecimal();
                    }
                }
            }
        }

        _lblMrr.Text      = $"₱{mrr:N0}";
        _lblActive.Text   = active.ToString();
        _lblExpiring.Text = inactive.ToString();
    }

    private void Apply()
    {
        string q           = _filterBar.SearchText.ToLowerInvariant();
        string? plan       = _filterBar.GetFilterValue("plan");
        string? subStatus  = _filterBar.GetFilterValue("subStatus");
        string? compStatus = _filterBar.GetFilterValue("companyStatus");

        _filtered = _all.Where(c =>
        {
            string code = GP(c, "companyCode", "CompanyCode").ToLowerInvariant();
            string name = GP(c, "companyName", "CompanyName").ToLowerInvariant();
            if (!string.IsNullOrEmpty(q) && !code.Contains(q) && !name.Contains(q)) return false;

            bool isCompActive = true;
            if (c.TryGetProperty("isActive", out var act) || c.TryGetProperty("IsActive", out act))
                isCompActive = act.GetBoolean();

            if (!string.IsNullOrEmpty(compStatus))
            {
                if (compStatus.Equals("Active", StringComparison.OrdinalIgnoreCase) && !isCompActive) return false;
                if (compStatus.Equals("Inactive", StringComparison.OrdinalIgnoreCase) && isCompActive) return false;
            }

            if (!string.IsNullOrEmpty(plan) || !string.IsNullOrEmpty(subStatus))
            {
                if (c.TryGetProperty("subscription", out var sub) || c.TryGetProperty("Subscription", out sub))
                {
                    if (sub.ValueKind == JsonValueKind.Object)
                    {
                        if (!string.IsNullOrEmpty(plan)      && !GP(sub, "planName", "PlanName").Equals(plan, StringComparison.OrdinalIgnoreCase)) return false;
                        if (!string.IsNullOrEmpty(subStatus) && !GP(sub, "status",   "Status").Equals(subStatus, StringComparison.OrdinalIgnoreCase)) return false;
                    }
                }
            }
            return true;
        }).ToList();

        _filterBar.SetRecordCount(_filtered.Count, _all.Count);

        _grid.Rows.Clear();
        foreach (var c in _filtered)
        {
            string code = GP(c, "companyCode", "CompanyCode");
            string name = GP(c, "companyName", "CompanyName");

            bool isCompActive = true;
            if (c.TryGetProperty("isActive", out var act) || c.TryGetProperty("IsActive", out act))
                isCompActive = act.GetBoolean();

            string companyStatusText = isCompActive ? "Active" : "Inactive";

            string plan2 = "—", subStatusText = "—", mods = "—", start = "—", end = "—";
            decimal fee = 0;

            if (c.TryGetProperty("subscription", out var sub) || c.TryGetProperty("Subscription", out sub))
            {
                if (sub.ValueKind == JsonValueKind.Object)
                {
                    plan2          = GP(sub, "planName", "PlanName");
                    subStatusText  = GP(sub, "status",   "Status");
                    mods           = GP(sub, "availedModules", "AvailedModules");
                    if (sub.TryGetProperty("monthlyFee", out var f) || sub.TryGetProperty("MonthlyFee", out f)) fee = f.GetDecimal();
                    if (sub.TryGetProperty("startDate",  out var sd) || sub.TryGetProperty("StartDate", out sd))
                        start = DateTime.TryParse(sd.GetString(), out var dt1) ? dt1.ToString("MMM d, yyyy") : "—";
                    if (sub.TryGetProperty("endDate",    out var ed) || sub.TryGetProperty("EndDate",   out ed))
                        end = DateTime.TryParse(ed.GetString(), out var dt2) ? dt2.ToString("MMM d, yyyy") : "—";
                }
            }

            int row = _grid.Rows.Add(code, name, companyStatusText, subStatusText, plan2, $"₱{fee:N0}", mods, start, end);
            _grid.Rows[row].Tag = c;
        }

        UpdateSelectionState();
    }

    private Panel MakeCard()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = CCard, Padding = new Padding(14, 10, 14, 10) };
        card.Paint += (_, pe) => pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
        return card;
    }

    private Panel MakeKpi(string title, Color accent, out Label val)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = CCard, Margin = new Padding(0, 0, 12, 0) };
        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
            using var b = new SolidBrush(accent);
            pe.Graphics.FillRectangle(b, 0, 0, 4, card.Height);
        };
        card.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = CMuted, AutoSize = false, Width = 300, Height = 20, Location = new Point(18, 18), TextAlign = ContentAlignment.MiddleLeft });
        val = new Label { Text = "—", Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = CText, AutoSize = false, Width = 300, Height = 36, Location = new Point(18, 40), TextAlign = ContentAlignment.MiddleLeft };
        card.Controls.Add(val);
        return card;
    }

    private static Button MakeBtn(string text, int w, bool primary, Color bg, Color fg)
    {
        var btn = new Button
        {
            Text = text,
            Width = w,
            Height = 36,
            Top = 8,
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = fg,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = CBorder;
        return btn;
    }

    private void EnsureAuthHeader()
    {
        if (!string.IsNullOrEmpty(Session.Token) && _http.DefaultRequestHeaders.Authorization == null)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
        }
    }

    private static string GP(JsonElement el, params string[] names)
    {
        foreach (var n in names)
            if (el.TryGetProperty(n, out var v) && v.ValueKind != JsonValueKind.Null)
                return v.ToString();
        return "";
    }
}
