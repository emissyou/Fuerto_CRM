using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Super Admin – Centralized Company Administrators & System Users Page.
/// Permits editing and updating Company Administrator credentials (including passwords)
/// across all tenants. Staff & Managers are managed by their respective Company Admins.
/// </summary>
public class SuperAdminSystemUsersPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private DataGridView _grid      = null!;
    private Label        _lblStatus = null!;
    private CrmFilterBar _filterBar = null!;

    private Label _lblTotal   = null!;
    private Label _lblAdmins  = null!;
    private Label _lblTenants = null!;
    private Label _lblActive  = null!;

    private Button _btnNewUser   = null!;
    private Button _btnEditAdmin = null!;
    private Button _btnResetPwd  = null!;
    private Button _btnRefresh   = null!;

    private List<JsonElement> _all      = new();
    private List<JsonElement> _filtered = new();

    private static readonly Color CBg     = Color.FromArgb(245, 247, 250);
    private static readonly Color CCard   = Color.White;
    private static readonly Color CBorder = Color.FromArgb(226, 230, 236);
    private static readonly Color CText   = Color.FromArgb(15, 23, 42);
    private static readonly Color CMuted  = Color.FromArgb(100, 116, 139);
    private static readonly Color CAccent = Color.FromArgb(255, 168, 0);
    private static readonly Color CGreen  = Color.FromArgb(22, 163, 74);
    private static readonly Color CBlue   = Color.FromArgb(59, 130, 246);
    private static readonly Color CPurple = Color.FromArgb(139, 92, 246);

    public SuperAdminSystemUsersPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http   = http;

        Dock      = DockStyle.Fill;
        BackColor = CBg;
        Padding   = new Padding(28, 14, 28, 28);

        BuildUI();
        _ = LoadAsync();
    }

    private void BuildUI()
    {
        Controls.Clear();

        // ── Grid card ──
        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = CCard,
            Padding   = new Padding(14, 8, 14, 10)
        };
        card.Paint += (_, pe) => pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
        Controls.Add(card);

        var gridHeader = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
        card.Controls.Add(gridHeader);
        gridHeader.Controls.Add(new Label
        {
            Text = "TENANT USER ACCOUNTS & COMPANY ADMINISTRATORS",
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Company", HeaderText = "COMPANY",      FillWeight = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",    HeaderText = "FULL NAME",    FillWeight = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email",   HeaderText = "EMAIL / LOGIN", FillWeight = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Role",    HeaderText = "ROLE",         FillWeight = 85  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",  HeaderText = "STATUS",       FillWeight = 75  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Created", HeaderText = "MANAGEMENT SCOPE", FillWeight = 120 });

        CrmTableStyler.Apply(_grid, "Role", "Status");

        _grid.SelectionChanged += (_, _) => UpdateSelectionState();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenEditAdminDialogAsync();
        };

        card.Controls.Add(_grid);
        _grid.BringToFront();

        // ── Filter bar ──
        _filterBar = new CrmFilterBar("Search users by email, name, or company…");
        _filterBar.FiltersChanged += (_, _) => Apply();
        _filterBar.AddFilter("company", "Company", "Fuerto", "GLI Bahay Builds", "Custom Crafters Davao");
        _filterBar.AddFilter("role",    "Role",    "Admin", "Manager", "Staff");
        _filterBar.AddFilter("status",  "Status",  "Active", "Inactive");
        Controls.Add(_filterBar);

        // ── Action Toolbar ──
        var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        Controls.Add(bar);

        int left = 0;

        _btnNewUser = MakeBtn("＋  Register Admin / User", 190, true);
        _btnNewUser.Left   = left; left += 200;
        _btnNewUser.Click += async (_, _) => await OpenNewUserDialogAsync();
        bar.Controls.Add(_btnNewUser);

        _btnEditAdmin = MakeBtn("✎  Edit Admin Credentials", 195, false);
        _btnEditAdmin.Left = left; left += 205;
        _btnEditAdmin.Enabled = false;
        _btnEditAdmin.Click += async (_, _) => await OpenEditAdminDialogAsync();
        bar.Controls.Add(_btnEditAdmin);

        _btnResetPwd = MakeBtn("🔑  Reset Admin Password", 185, false);
        _btnResetPwd.Left = left; left += 195;
        _btnResetPwd.Enabled = false;
        _btnResetPwd.Click += async (_, _) => await OpenEditAdminDialogAsync();
        bar.Controls.Add(_btnResetPwd);

        _btnRefresh = MakeBtn("↻  Refresh", 95, false);
        _btnRefresh.Left   = left; left += 105;
        _btnRefresh.Click += async (_, _) => await LoadAsync();
        bar.Controls.Add(_btnRefresh);

        _lblStatus = new Label
        {
            Left      = left,
            Top       = 16,
            AutoSize  = true,
            ForeColor = CMuted,
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic)
        };
        bar.Controls.Add(_lblStatus);

        // ── KPI cards row (4 cards) ──
        var kpiRow = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };
        Controls.Add(kpiRow);

        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.Transparent };
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++) tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        kpiRow.Controls.Add(tbl);

        tbl.Controls.Add(MakeKpi("👥  TOTAL SYSTEM USERS",    CBlue,   out _lblTotal),   0, 0);
        tbl.Controls.Add(MakeKpi("🔑  COMPANY ADMINISTRATORS", CPurple, out _lblAdmins),  1, 0);
        tbl.Controls.Add(MakeKpi("🏢  TENANT COMPANIES",      CAccent, out _lblTenants), 2, 0);
        tbl.Controls.Add(MakeKpi("✅  ACTIVE STATUS",          CGreen,  out _lblActive),  3, 0);
    }

    public async Task LoadAsync()
    {
        _lblStatus.Text = "Loading company admins and users…";
        _lblStatus.ForeColor = CMuted;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_apiUrl}/superadmin/users");
            if (!string.IsNullOrWhiteSpace(Session.Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                _all     = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
                UpdateKpis();
                Apply();
                _lblStatus.Text      = $"✓ {_all.Count} accounts loaded across all tenants";
                _lblStatus.ForeColor = CGreen;
            }
            else
            {
                _lblStatus.Text      = $"⚠ HTTP {(int)res.StatusCode}";
                _lblStatus.ForeColor = Color.Firebrick;
            }
        }
        catch (Exception ex)
        {
            _lblStatus.Text      = $"⚠ {ex.Message}";
            _lblStatus.ForeColor = Color.Firebrick;
        }
    }

    private void UpdateKpis()
    {
        int total  = _all.Count;
        int active = _all.Count(u =>
        {
            if (u.TryGetProperty("isActive", out var a) || u.TryGetProperty("IsActive", out a))
                return a.GetBoolean();
            return false;
        });

        int admins = _all.Count(u =>
        {
            string r = GP(u, "role", "Role").ToLowerInvariant();
            string rs = GP(u, "roles", "Roles").ToLowerInvariant();
            return r.Contains("admin") || rs.Contains("admin");
        });

        int tenantCount = _all.Select(u => GP(u, "companyId", "CompanyId")).Where(id => !string.IsNullOrEmpty(id)).Distinct().Count();
        if (tenantCount == 0) tenantCount = 3;

        _lblTotal.Text   = total.ToString();
        _lblAdmins.Text  = admins.ToString();
        _lblTenants.Text = $"{tenantCount} Tenants";
        _lblActive.Text  = total > 0 ? $"{(int)Math.Round(active * 100.0 / total)}% Active" : "100%";
    }

    private void Apply()
    {
        string q       = _filterBar.SearchText.ToLowerInvariant();
        string? comp   = _filterBar.GetFilterValue("company");
        string? role   = _filterBar.GetFilterValue("role");
        string? status = _filterBar.GetFilterValue("status");

        _filtered = _all.Where(u =>
        {
            string email = GP(u, "email",       "Email").ToLowerInvariant();
            string fname = GP(u, "fullName",    "FullName").ToLowerInvariant();
            string cname = GP(u, "companyName", "CompanyName").ToLowerInvariant();

            if (!string.IsNullOrEmpty(q) && !email.Contains(q) && !fname.Contains(q) && !cname.Contains(q))
                return false;

            if (!string.IsNullOrEmpty(comp) && !cname.Contains(comp.ToLowerInvariant()))
                return false;

            if (!string.IsNullOrEmpty(role))
            {
                string r  = GP(u, "role",  "Role");
                string rs = GP(u, "roles", "Roles");
                if (!r.Equals(role, StringComparison.OrdinalIgnoreCase) &&
                    !rs.Contains(role, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool active = false;
                if (u.TryGetProperty("isActive", out var ia) || u.TryGetProperty("IsActive", out ia))
                    active = ia.GetBoolean();

                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase) && !active) return false;
                if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase) && active) return false;
            }

            return true;
        }).ToList();

        _filterBar.SetRecordCount(_filtered.Count, _all.Count);
        _grid.Rows.Clear();

        foreach (var u in _filtered)
        {
            string company = GP(u, "companyName", "CompanyName");
            string email   = GP(u, "email",       "Email");
            string fn      = GP(u, "fullName",    "FullName");
            string role2   = GP(u, "role",        "Role");
            if (string.IsNullOrEmpty(role2)) role2 = GP(u, "roles", "Roles");

            bool active2 = false;
            if (u.TryGetProperty("isActive", out var ia) || u.TryGetProperty("IsActive", out ia))
                active2 = ia.GetBoolean();

            string acctType = role2.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                ? "👑 Super Admin Managed"
                : "Tenant Managed";

            int rowIdx = _grid.Rows.Add(company, fn, email, role2, active2 ? "Active" : "Inactive", acctType);
            _grid.Rows[rowIdx].Tag = u;
        }

        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        var sel = GetSelectedUser();
        if (sel.HasValue)
        {
            string role = GP(sel.Value, "role", "Role");
            bool isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            _btnEditAdmin.Enabled = isAdmin;
            _btnResetPwd.Enabled = isAdmin;

            if (isAdmin)
            {
                string fn = GP(sel.Value, "fullName", "FullName");
                string comp = GP(sel.Value, "companyName", "CompanyName");
                _lblStatus.Text = $"Selected Admin: {fn} ({comp}) · Ready to update credentials & password";
                _lblStatus.ForeColor = Color.FromArgb(22, 101, 52);
            }
            else
            {
                _lblStatus.Text = "ℹ Super Admin can only edit and manage Company Administrator accounts.";
                _lblStatus.ForeColor = CMuted;
            }
        }
        else
        {
            _btnEditAdmin.Enabled = false;
            _btnResetPwd.Enabled = false;
        }
    }

    private JsonElement? GetSelectedUser()
    {
        if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0)
        {
            if (_grid.CurrentRow.Tag is JsonElement el) return el;
        }
        return null;
    }

    private async Task OpenNewUserDialogAsync()
    {
        using var dlg = new NewUserDialog(_apiUrl, _http);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadAsync();
    }

    private async Task OpenEditAdminDialogAsync()
    {
        var sel = GetSelectedUser();
        if (!sel.HasValue)
        {
            MessageBox.Show("Please select an administrator account from the table first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string role = GP(sel.Value, "role", "Role");
        if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "Super Admin can only edit and update Company Administrator credentials.\n\n" +
                "Staff and Manager accounts are managed by their respective Company Admins.",
                "Restricted Action",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new EditUserDialog(_apiUrl, _http, sel.Value);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await LoadAsync();
        }
    }

    private Panel MakeKpi(string title, Color accent, out Label val)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = CCard, Margin = new Padding(0, 0, 10, 0) };
        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
            using var b = new SolidBrush(accent);
            pe.Graphics.FillRectangle(b, 0, 0, card.Width, 4);
        };
        card.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 7.8f, FontStyle.Bold), ForeColor = CMuted, AutoSize = false, Width = 260, Height = 20, Location = new Point(14, 16), TextAlign = ContentAlignment.MiddleLeft });
        val = new Label { Text = "—", Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = CText, AutoSize = false, Width = 260, Height = 38, Location = new Point(14, 38), TextAlign = ContentAlignment.MiddleLeft };
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

    private static string GP(JsonElement el, params string[] names)
    {
        foreach (var n in names)
            if (el.TryGetProperty(n, out var v) && v.ValueKind != JsonValueKind.Null)
                return v.ToString();
        return "";
    }
}
