using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class UsersPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private DataGridView _grid = null!;
    private Label _lblStatus = null!;
    private Button _btnRefresh = null!;
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnResetPwd = null!;
    private Button _btnDelete = null!;
    private TextBox _searchBox = null!;
    private ComboBox _cmbRoleFilter = null!;

    private List<JsonElement> _all = new();
    private List<JsonElement> _filtered = new();

    private const int PageSize = 17;
    private int _currentPage = 1;
    private Label _lblPageInfo = null!;
    private Button _btnPrev = null!;
    private Button _btnNext = null!;

    public UsersPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Padding = new Padding(32, 20, 32, 32);

        // =========================================================
        // HEADER (toolbar)
        // =========================================================
        var header = new Panel { Dock = DockStyle.Top, Height = 56 };
        Controls.Add(header);

        _lblStatus = new Label
        {
            Text = "Loading users...",
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            Location = new Point(0, 16)
        };
        header.Controls.Add(_lblStatus);

        _btnNew = MakeButton("＋  New User", 140, true);
        _btnNew.Left = 220;
        _btnNew.Click += async (_, _) => await OpenNewUserDialogAsync();
        header.Controls.Add(_btnNew);

        _btnRefresh = MakeButton("↻  Refresh", 110, false);
        _btnRefresh.Left = 368;
        _btnRefresh.Click += async (_, _) => await LoadAsync();
        header.Controls.Add(_btnRefresh);

        _btnEdit = MakeButton("✎  Edit", 90, false);
        _btnEdit.Left = 486;
        _btnEdit.Click += async (_, _) => await OpenEditDialogAsync();
        header.Controls.Add(_btnEdit);

        _btnResetPwd = MakeButton("🔑  Reset Pwd", 130, false);
        _btnResetPwd.Left = 584;
        _btnResetPwd.Click += async (_, _) => await OpenResetPasswordAsync();
        header.Controls.Add(_btnResetPwd);

        _btnDelete = MakeButton("🗑  Delete", 110, false);
        _btnDelete.Left = 722;
        _btnDelete.Click += async (_, _) => await DeleteSelectedAsync();
        header.Controls.Add(_btnDelete);

        // Search + filter on the right
        _cmbRoleFilter = new ComboBox
        {
            Left = 900,
            Top = 12,
            Width = 160,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _cmbRoleFilter.Items.AddRange(new object[]
        {
            "All Roles", "Admin", "Manager", "Staff", "Designer"
        });
        _cmbRoleFilter.SelectedIndex = 0;
        _cmbRoleFilter.SelectedIndexChanged += (_, _) =>
        {
            _currentPage = 1;
            ApplyFilterAndRender();
        };
        header.Controls.Add(_cmbRoleFilter);

        _searchBox = new TextBox
        {
            Left = 1070,
            Top = 10,
            Width = 260,
            Height = 34,
            PlaceholderText = "Search name or email...",
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _searchBox.TextChanged += (_, _) =>
        {
            _currentPage = 1;
            ApplyFilterAndRender();
        };
        header.Controls.Add(_searchBox);

        Resize += (_, _) =>
        {
            if (_cmbRoleFilter.IsDisposed) return;
            int w = ClientSize.Width;
            _searchBox.Left = Math.Max(900, w - 300);
            _cmbRoleFilter.Left = Math.Max(700, w - 480);
        };

        // =========================================================
        // GRID
        // =========================================================
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 42
        };
        _grid.RowTemplate.Height = 46;

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(249, 250, 252),
            ForeColor = Color.FromArgb(85, 93, 106),
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
            Padding = new Padding(12, 0, 12, 0),
            SelectionBackColor = Color.FromArgb(249, 250, 252),
            SelectionForeColor = Color.FromArgb(85, 93, 106)
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(28, 32, 40),
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(12, 0, 12, 0),
            SelectionBackColor = Color.FromArgb(255, 245, 225),
            SelectionForeColor = Color.FromArgb(28, 32, 40)
        };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 251, 253)
        };

        _grid.Columns.Add("fullName", "FULL NAME");
        _grid.Columns.Add("email", "EMAIL");
        _grid.Columns.Add("role", "ROLE");
        _grid.Columns.Add("status", "STATUS");
        _grid.Columns.Add("userId", "USER ID");
        _grid.Columns["userId"].Visible = false;

        _grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex == _grid.Columns["role"].Index && e.Value is string role)
            {
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                e.CellStyle.ForeColor = role switch
                {
                    "Admin" => Color.FromArgb(200, 55, 55),
                    "Manager" => Color.FromArgb(160, 95, 0),
                    "Staff" => Color.FromArgb(80, 140, 200),
                    "Designer" => Color.FromArgb(34, 140, 78),
                    _ => Color.FromArgb(110, 118, 132)
                };
            }
            if (e.ColumnIndex == _grid.Columns["status"].Index && e.Value is string status)
            {
                e.CellStyle.ForeColor = status == "Active"
                    ? Color.FromArgb(34, 140, 78)
                    : Color.FromArgb(200, 55, 55);
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
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
            Height = 52,
            BackColor = Color.FromArgb(250, 251, 253)
        };
        Controls.Add(pager);

        _btnPrev = new Button
        {
            Text = "◀  Prev",
            Left = 12,
            Top = 8,
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
            Left = 120,
            Top = 18,
            Width = 420,
            Height = 24,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40)
        };
        pager.Controls.Add(_lblPageInfo);

        _btnNext = new Button
        {
            Text = "Next  ▶",
            Left = 560,
            Top = 8,
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

    public async Task LoadAsync()
    {
        _lblStatus.Text = "Loading users...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/users";
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
        var search = _searchBox.Text.Trim().ToLowerInvariant();
        var roleFilter = _cmbRoleFilter.SelectedItem?.ToString() ?? "All Roles";

        _filtered = _all.Where(u =>
        {
            var role = GetStr(u, "role");
            var name = GetStr(u, "fullName");
            var email = GetStr(u, "email");

            if (roleFilter != "All Roles" && role != roleFilter) return false;

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

        _lblStatus.Text = $"{_filtered.Count} user{(_filtered.Count == 1 ? "" : "s")}";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);
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
        if (selected is null) { MessageBox.Show("Select a user first.", "Edit User"); return; }

        using var dlg = new EditUserDialog(_apiUrl, _http, selected.Value);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await LoadAsync();
        }
    }

    private async Task OpenResetPasswordAsync()
    {
        var selected = GetSelected();
        if (selected is null) { MessageBox.Show("Select a user first.", "Reset Password"); return; }

        using var dlg = new ResetPasswordDialog(_apiUrl, _http, selected.Value);
        dlg.ShowDialog(FindForm());
    }

    private async Task DeleteSelectedAsync()
    {
        var selected = GetSelected();
        if (selected is null) { MessageBox.Show("Select a user first.", "Delete User"); return; }

        var user = selected.Value;
        var email = GetStr(user, "email");
        var userId = GetStr(user, "userId");

        var confirm = MessageBox.Show(
            $"Delete user: {email}?\n\nThis cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/users/{userId}";
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

            MessageBox.Show("User deleted.", "Success");
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