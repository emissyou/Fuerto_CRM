using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class UsersPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly bool _isSuperAdmin;
    private readonly bool _isCompanyAdmin;
    private readonly bool _isManager;

    private DataGridView _grid = null!;
    private Label _lblStatus = null!;
    private Button _btnRefresh = null!;
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnResetPwd = null!;
    private Button _btnDelete = null!;
    private CrmFilterBar _filterBar = null!;

    private Label? _lblCompany;
    private ComboBox? _cmbCompany;

    private List<JsonElement> _all = new();
    private List<JsonElement> _filtered = new();

    private const int PageSize = 17;
    private int _currentPage = 1;
    private Label _lblPageInfo = null!;
    private Button _btnPrev = null!;
    private Button _btnNext = null!;

    private class CompanyItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    public UsersPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        var roles = Session.Roles ?? new List<string>();
        _isSuperAdmin = roles.Contains("Super Admin");
        _isCompanyAdmin = roles.Contains("Admin");
        _isManager = roles.Contains("Manager");

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Padding = new Padding(32, 20, 32, 32);

        // =========================================================
        // HEADER (toolbar)
        // =========================================================
        var header = new Panel { Dock = DockStyle.Top, Height = 56 };
        Controls.Add(header);

        int currentLeft = 0;

        if (_isSuperAdmin)
        {
            _lblCompany = new Label
            {
                Text = "Company:",
                ForeColor = Color.FromArgb(70, 78, 92),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 16)
            };
            header.Controls.Add(_lblCompany);

            _cmbCompany = new ComboBox
            {
                Left = 72,
                Top = 12,
                Width = 200,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cmbCompany.SelectedIndexChanged += async (_, _) =>
            {
                if (_cmbCompany.SelectedItem is CompanyItem ci)
                {
                    Session.CompanyId = ci.Id;
                    await LoadAsync();
                }
            };
            header.Controls.Add(_cmbCompany);

            currentLeft = 284;
        }

        _btnNew = MakeButton(_isSuperAdmin ? "＋  New Admin" : "＋  New User", _isSuperAdmin ? 140 : 130, true);
        _btnNew.Left = currentLeft;
        _btnNew.Click += async (_, _) => await OpenNewUserDialogAsync();
        header.Controls.Add(_btnNew);
        currentLeft += _btnNew.Width + 8;

        _btnRefresh = MakeButton("↻  Refresh", 100, false);
        _btnRefresh.Left = currentLeft;
        _btnRefresh.Click += async (_, _) => await LoadAsync();
        header.Controls.Add(_btnRefresh);
        currentLeft += _btnRefresh.Width + 8;

        _btnEdit = MakeButton("✎  Edit", 85, false);
        _btnEdit.Left = currentLeft;
        _btnEdit.Click += async (_, _) => await OpenEditDialogAsync();
        header.Controls.Add(_btnEdit);
        currentLeft += _btnEdit.Width + 8;

        _btnResetPwd = MakeButton("🔑  Reset Pwd", 120, false);
        _btnResetPwd.Left = currentLeft;
        _btnResetPwd.Click += async (_, _) => await OpenResetPasswordAsync();
        header.Controls.Add(_btnResetPwd);
        currentLeft += _btnResetPwd.Width + 8;

        _btnDelete = MakeButton("🗑  Delete", 95, false);
        _btnDelete.Left = currentLeft;
        _btnDelete.Click += async (_, _) => await DeleteSelectedAsync();
        header.Controls.Add(_btnDelete);
        currentLeft += _btnDelete.Width + 8;

        // ---- Status message safely positioned on right side ----
        _lblStatus = new Label
        {
            Text = _isSuperAdmin ? "Loading admins..." : "Loading users...",
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Width = 320,
            Height = 26,
            Location = new Point(Math.Max(currentLeft, header.ClientSize.Width - 330), 13)
        };
        header.Controls.Add(_lblStatus);
        header.Resize += (_, _) => _lblStatus.Left = Math.Max(currentLeft, header.ClientSize.Width - 330);

        // Filter bar with Role & Status
        _filterBar = new CrmFilterBar("Search name or email...");
        if (_isSuperAdmin)
        {
            _filterBar.AddFilter("Role", "Role", "Admin");
        }
        else if (_isCompanyAdmin)
        {
            _filterBar.AddFilter("Role", "Role", "Manager", "Staff");
        }
        else
        {
            _filterBar.AddFilter("Role", "Role", "Staff");
        }

        _filterBar.AddFilter("Status", "Status", "Active", "Inactive");
        _filterBar.FiltersChanged += (_, _) =>
        {
            _currentPage = 1;
            ApplyFilterAndRender();
        };
        Controls.Add(_filterBar);
        _filterBar.BringToFront();

        // =========================================================
        // GRID
        // =========================================================
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false
        };

        _grid.Columns.Add("fullName", "FULL NAME");
        _grid.Columns.Add("email", "EMAIL");
        _grid.Columns.Add("role", "ROLE");
        _grid.Columns.Add("status", "STATUS");
        _grid.Columns.Add("userId", "USER ID");
        _grid.Columns["userId"].Visible = false;

        // Column weight tuning
        _grid.Columns["fullName"].FillWeight = 100;
        _grid.Columns["email"].FillWeight = 130;
        _grid.Columns["role"].FillWeight = 70;
        _grid.Columns["status"].FillWeight = 60;

        // =========================================================
        // MODERN TABLE STYLING
        // =========================================================
        CrmTableStyler.Apply(_grid, "role", "status");

        _grid.RowTemplate.Height = 56;

        _grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex == _grid.Columns["role"].Index ||
                e.ColumnIndex == _grid.Columns["status"].Index)
            {
                e.CellStyle.ForeColor = Color.Transparent;
            }
        };

        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0) await OpenEditDialogAsync();
        };

        // =========================================================
        // PAGER
        // =========================================================
        var pager = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 10)
        };
        Controls.Add(pager);

        pager.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 235, 240), 1);
            e.Graphics.DrawLine(pen, 0, 0, pager.Width, 0);
        };

        _btnPrev = new Button
        {
            Text = "◀  Prev",
            Left = 16,
            Top = 10,
            Width = 90,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnPrev.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        _btnPrev.Click += (_, _) =>
        {
            if (_currentPage > 1) { _currentPage--; RenderPage(); }
        };
        pager.Controls.Add(_btnPrev);

        _lblPageInfo = new Label
        {
            Left = 118,
            Top = 20,
            Width = 420,
            Height = 24,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40)
        };
        pager.Controls.Add(_lblPageInfo);

        _btnNext = new Button
        {
            Text = "Next  ▶",
            Left = 550,
            Top = 10,
            Width = 90,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnNext.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        _btnNext.Click += (_, _) =>
        {
            int total = TotalPages;
            if (_currentPage < total) { _currentPage++; RenderPage(); }
        };
        pager.Controls.Add(_btnNext);

        Controls.Add(_grid);
        _grid.BringToFront();
    }

    private static Button MakeButton(string text, int width, bool primary) => new Button
    {
        Text = text,
        Top = 8,
        Width = width,
        Height = 36,
        FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Color.FromArgb(255, 168, 0) : Color.White,
        ForeColor = primary ? Color.White : Color.FromArgb(28, 32, 40),
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Cursor = Cursors.Hand
    };

    private int TotalPages =>
        _filtered.Count == 0 ? 1
        : (int)Math.Ceiling(_filtered.Count / (double)PageSize);

    private async Task LoadCompaniesAsync()
    {
        if (_cmbCompany == null) return;
        try
        {
            var url = $"{_apiUrl}/companies";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            using var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                _cmbCompany.Items.Clear();
                int selectIdx = 0;
                int currentId = Session.CompanyId ?? 1;

                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    int id = el.GetProperty("companyId").GetInt32();
                    string name = el.TryGetProperty("companyName", out var np) ? np.GetString() ?? $"Company {id}" : $"Company {id}";
                    var item = new CompanyItem { Id = id, Name = name };
                    int idx = _cmbCompany.Items.Add(item);
                    if (id == currentId) selectIdx = idx;
                }

                if (_cmbCompany.Items.Count > 0)
                {
                    _cmbCompany.SelectedIndex = selectIdx;
                }
            }
        }
        catch { }
    }

    public async Task LoadAsync()
    {
        if (_isSuperAdmin && _cmbCompany != null && _cmbCompany.Items.Count == 0)
        {
            await LoadCompaniesAsync();
        }

        int targetCompanyId = Session.CompanyId ?? 1;

        _lblStatus.Text = _isSuperAdmin ? "Loading admins..." : "Loading users...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var url = $"{_apiUrl}/tenant/{targetCompanyId}/users";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            using var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _lblStatus.Text = $"Failed: {(int)res.StatusCode} {res.ReasonPhrase}";
                _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
                return;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _all.Clear();
            if (root.TryGetProperty("users", out var usersProp) &&
                usersProp.ValueKind == JsonValueKind.Array)
            {
                _all = usersProp.EnumerateArray().Select(e => e.Clone()).ToList();
            }

            _currentPage = 1;
            ApplyFilterAndRender();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
        }
    }

    private void ApplyFilterAndRender()
    {
        var search = _filterBar.SearchText.ToLowerInvariant();
        var roleFilter = _filterBar.GetFilterValue("Role");
        var statusFilter = _filterBar.GetFilterValue("Status");

        _filtered = _all.Where(u =>
        {
            var role = GetStr(u, "role");
            var name = GetStr(u, "fullName");
            var email = GetStr(u, "email");
            var isActive = GetBool(u, "isActive");

            if (roleFilter != null && !role.Equals(roleFilter, StringComparison.OrdinalIgnoreCase)) return false;

            if (statusFilter != null)
            {
                if (statusFilter.Equals("Active", StringComparison.OrdinalIgnoreCase) && !isActive) return false;
                if (statusFilter.Equals("Inactive", StringComparison.OrdinalIgnoreCase) && isActive) return false;
            }

            if (!string.IsNullOrEmpty(search))
            {
                if (!name.ToLowerInvariant().Contains(search) &&
                    !email.ToLowerInvariant().Contains(search))
                    return false;
            }
            return true;
        }).ToList();

        if (_currentPage > TotalPages) _currentPage = TotalPages;
        if (_currentPage < 1) _currentPage = 1;

        _filterBar.SetRecordCount(_filtered.Count, _all.Count);
        RenderPage();
    }

    private void RenderPage()
    {
        _grid.Rows.Clear();

        int start = (_currentPage - 1) * PageSize;
        int end = Math.Min(start + PageSize, _filtered.Count);

        for (int i = start; i < end; i++)
        {
            var u = _filtered[i];
            var isActive = GetBool(u, "isActive");
            _grid.Rows.Add(
                GetStr(u, "fullName"),
                GetStr(u, "email"),
                GetStr(u, "role"),
                isActive ? "Active" : "Inactive",
                GetStr(u, "userId"));
        }

        _lblPageInfo.Text = $"Page {_currentPage} of {TotalPages}   ·   " +
                           $"Showing {start + 1}–{end} of {_filtered.Count}";

        _btnPrev.Enabled = _currentPage > 1;
        _btnNext.Enabled = _currentPage < TotalPages;
        _btnPrev.ForeColor = _btnPrev.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);
        _btnNext.ForeColor = _btnNext.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);

        _filterBar.SetRecordCount(_filtered.Count, _all.Count);
        _lblStatus.Text = "";
    }

    private JsonElement? GetSelected()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        var visibleIdx = _grid.SelectedRows[0].Index;
        if (visibleIdx < 0 || visibleIdx >= _grid.Rows.Count) return null;

        var userId = _grid.Rows[visibleIdx].Cells["userId"].Value?.ToString();
        if (string.IsNullOrEmpty(userId)) return null;

        var match = _filtered.FirstOrDefault(u => GetStr(u, "userId") == userId);
        return match.ValueKind == JsonValueKind.Undefined ? null : match;
    }

    private async Task OpenNewUserDialogAsync()
    {
        using var dlg = new NewUserDialog(_apiUrl, _http);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await LoadAsync();
        }
    }

    private async Task OpenEditDialogAsync()
    {
        var selected = GetSelected();
        if (selected is null)
        {
            MessageBox.Show(_isSuperAdmin ? "Select an admin first." : "Select a user first.", "Edit User");
            return;
        }

        using var dlg = new EditUserDialog(_apiUrl, _http, selected.Value);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await LoadAsync();
        }
    }

    private async Task OpenResetPasswordAsync()
    {
        var selected = GetSelected();
        if (selected is null)
        {
            MessageBox.Show(_isSuperAdmin ? "Select an admin first." : "Select a user first.", "Reset Password");
            return;
        }

        using var dlg = new ResetPasswordDialog(_apiUrl, _http, selected.Value);
        dlg.ShowDialog(FindForm());
    }

    private async Task DeleteSelectedAsync()
    {
        var selected = GetSelected();
        if (selected is null)
        {
            MessageBox.Show(_isSuperAdmin ? "Select an admin first." : "Select a user first.", "Delete User");
            return;
        }

        var user = selected.Value;
        var email = GetStr(user, "email");
        var userId = GetStr(user, "userId");

        var confirm = MessageBox.Show(
            $"Delete {(_isSuperAdmin ? "admin" : "user")}: {email}?\n\nThis cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            int targetCompanyId = Session.CompanyId ?? 1;
            var url = $"{_apiUrl}/tenant/{targetCompanyId}/users/{userId}";
            using var req = new HttpRequestMessage(HttpMethod.Delete, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
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

            MessageBox.Show(_isSuperAdmin ? "Admin deleted." : "User deleted.", "Success");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Delete failed:\n\n" + ex.Message, "Error");
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