using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class BranchesPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private DataGridView _grid = null!;
    private Label _lblStatus = null!;
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnToggle = null!;
    private Button _btnDelete = null!;
    private Button _btnRefresh = null!;
    private CrmFilterBar _filterBar = null!;

    private Label _lblTotalCount = null!;
    private Label _lblActiveCount = null!;
    private Label _lblMainBranchName = null!;

    private List<JsonElement> _allBranches = new();
    private List<JsonElement> _filteredBranches = new();

    public BranchesPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Padding = new Padding(32, 20, 32, 32);

        BuildInterface();
        _ = LoadBranchesAsync();
    }

    private void BuildInterface()
    {
        Controls.Clear();

        // 1. KPI MINI SUMMARY CARDS AT TOP
        var kpiPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.Transparent,
            WrapContents = false
        };
        Controls.Add(kpiPanel);

        var card1 = CreateMiniKpi("TOTAL BRANCHES", out _lblTotalCount, Color.FromArgb(255, 168, 0));
        var card2 = CreateMiniKpi("ACTIVE OPERATIONAL", out _lblActiveCount, Color.FromArgb(34, 140, 78));
        var card3 = CreateMiniKpi("HEADQUARTERS / MAIN", out _lblMainBranchName, Color.FromArgb(59, 130, 246), 280);

        kpiPanel.Controls.Add(card1);
        kpiPanel.Controls.Add(card2);
        kpiPanel.Controls.Add(card3);

        // 2. TOOLBAR
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.Transparent
        };
        Controls.Add(toolbar);

        int left = 0;
        _btnNew = MakeButton("＋  New Branch", 130, true);
        _btnNew.Left = left;
        _btnNew.Click += async (_, _) => await OpenNewBranchDialogAsync();
        toolbar.Controls.Add(_btnNew);
        left += 140;

        _btnEdit = MakeButton("✎  Edit Branch", 120, false);
        _btnEdit.Left = left;
        _btnEdit.Click += async (_, _) => await OpenEditBranchDialogAsync();
        toolbar.Controls.Add(_btnEdit);
        left += 130;

        _btnToggle = MakeButton("⏻  Toggle Status", 130, false);
        _btnToggle.Left = left;
        _btnToggle.Click += async (_, _) => await ToggleStatusAsync();
        toolbar.Controls.Add(_btnToggle);
        left += 140;

        _btnDelete = MakeButton("🗑  Delete", 95, false);
        _btnDelete.Left = left;
        _btnDelete.Click += async (_, _) => await DeleteBranchAsync();
        toolbar.Controls.Add(_btnDelete);
        left += 105;

        _btnRefresh = MakeButton("↻  Refresh", 100, false);
        _btnRefresh.Left = left;
        _btnRefresh.Click += async (_, _) => await LoadBranchesAsync();
        toolbar.Controls.Add(_btnRefresh);

        _lblStatus = new Label
        {
            Left = left + 115,
            Top = 14,
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 9f)
        };
        toolbar.Controls.Add(_lblStatus);

        // 3. FILTER BAR
        _filterBar = new CrmFilterBar("Search branches by code, name, address...");
        _filterBar.FiltersChanged += (_, _) => ApplyFilters();
        _filterBar.AddFilter("status", "Status", "Active", "Inactive", "Main Branch");
        Controls.Add(_filterBar);

        // 4. DATA TABLE CONTAINER
        var tableCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12)
        };
        Controls.Add(tableCard);
        tableCard.BringToFront();

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        _grid.Columns.Add("BranchId", "ID");
        _grid.Columns["BranchId"]!.Visible = false;

        _grid.Columns.Add("Code", "BRANCH CODE");
        _grid.Columns["Code"]!.FillWeight = 85;

        _grid.Columns.Add("Name", "BRANCH NAME");
        _grid.Columns["Name"]!.FillWeight = 160;

        _grid.Columns.Add("Address", "LOCATION / ADDRESS");
        _grid.Columns["Address"]!.FillWeight = 170;

        _grid.Columns.Add("Contact", "CONTACT NUMBER");
        _grid.Columns["Contact"]!.FillWeight = 100;

        _grid.Columns.Add("Email", "EMAIL");
        _grid.Columns["Email"]!.FillWeight = 110;

        _grid.Columns.Add("Manager", "ASSIGNED MANAGER");
        _grid.Columns["Manager"]!.FillWeight = 140;

        _grid.Columns.Add("Type", "TYPE");
        _grid.Columns["Type"]!.FillWeight = 80;

        _grid.Columns.Add("Status", "STATUS");
        _grid.Columns["Status"]!.FillWeight = 75;

        _grid.Columns.Add("CreatedAt", "CREATED");
        _grid.Columns["CreatedAt"]!.FillWeight = 80;

        CrmTableStyler.Apply(_grid, "Type", "Status");

        _grid.CellDoubleClick += async (_, _) => await OpenEditBranchDialogAsync();

        tableCard.Controls.Add(_grid);
    }

    private Panel CreateMiniKpi(string title, out Label valueLabel, Color accentColor, int width = 200)
    {
        var panel = new Panel
        {
            Width = width,
            Height = 72,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 16, 0),
            Padding = new Padding(14, 10, 14, 8)
        };

        var border = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accentColor };
        panel.Controls.Add(border);

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 16
        };
        panel.Controls.Add(lblTitle);

        valueLabel = new Label
        {
            Text = "-",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(valueLabel);
        valueLabel.BringToFront();

        return panel;
    }

    private Button MakeButton(string text, int width, bool isPrimary)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = 36,
            Top = 6,
            FlatStyle = FlatStyle.Flat,
            BackColor = isPrimary ? Color.FromArgb(255, 168, 0) : Color.White,
            ForeColor = isPrimary ? Color.White : Color.FromArgb(70, 78, 92),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = isPrimary ? 0 : 1;
        btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        return btn;
    }

    public async Task LoadBranchesAsync()
    {
        _lblStatus.Text = "Loading branches...";
        _lblStatus.ForeColor = Color.FromArgb(100, 116, 139);

        int companyId = Session.CompanyId ?? 1;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_apiUrl}/tenant/{companyId}/branches");
            if (!string.IsNullOrWhiteSpace(Session.Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                var rawBranches = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
                _allBranches = CrmTableStyler.SortNewestFirst(rawBranches);
                UpdateKpiSummaries();
                ApplyFilters();
                _lblStatus.Text = $"Loaded {_allBranches.Count} branches.";
                _lblStatus.ForeColor = Color.FromArgb(34, 140, 78);
            }
            else
            {
                _lblStatus.Text = $"Failed to load branches (HTTP {(int)res.StatusCode})";
                _lblStatus.ForeColor = Color.Firebrick;
            }
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
            _lblStatus.ForeColor = Color.Firebrick;
        }
    }

    private void UpdateKpiSummaries()
    {
        int total = _allBranches.Count;
        int active = _allBranches.Count(b =>
        {
            if (b.TryGetProperty("isActive", out var ia) || b.TryGetProperty("IsActive", out ia))
                return ia.GetBoolean();
            return false;
        });

        var main = _allBranches.FirstOrDefault(b =>
        {
            if (b.TryGetProperty("isMainBranch", out var imb) || b.TryGetProperty("IsMainBranch", out imb))
                return imb.GetBoolean();
            return false;
        });

        string mainName = "-";
        if (main.ValueKind == JsonValueKind.Object)
        {
            mainName = GetProp(main, "branchName", "BranchName");
            if (mainName.Length > 24) mainName = mainName.Substring(0, 22) + "...";
        }

        _lblTotalCount.Text = total.ToString();
        _lblActiveCount.Text = active.ToString();
        _lblMainBranchName.Text = string.IsNullOrEmpty(mainName) ? "None" : mainName;
    }

    private void ApplyFilters()
    {
        string query = _filterBar.SearchText.ToLowerInvariant();
        string? statusFilter = _filterBar.GetFilterValue("status");

        _filteredBranches = _allBranches.Where(b =>
        {
            string code = GetProp(b, "branchCode", "BranchCode").ToLowerInvariant();
            string name = GetProp(b, "branchName", "BranchName").ToLowerInvariant();
            string address = GetProp(b, "address", "Address").ToLowerInvariant();

            bool matchesQuery = string.IsNullOrEmpty(query) ||
                                code.Contains(query) ||
                                name.Contains(query) ||
                                address.Contains(query);

            if (!matchesQuery) return false;

            bool isActive = false;
            if (b.TryGetProperty("isActive", out var ia) || b.TryGetProperty("IsActive", out ia))
                isActive = ia.GetBoolean();

            bool isMain = false;
            if (b.TryGetProperty("isMainBranch", out var imb) || b.TryGetProperty("IsMainBranch", out imb))
                isMain = imb.GetBoolean();

            if (statusFilter == "Active" && !isActive) return false;
            if (statusFilter == "Inactive" && isActive) return false;
            if (statusFilter == "Main Branch" && !isMain) return false;

            return true;
        }).ToList();

        _filterBar.SetRecordCount(_filteredBranches.Count, _allBranches.Count);
        RenderGrid();
    }

    private void RenderGrid()
    {
        _grid.Rows.Clear();

        foreach (var b in _filteredBranches)
        {
            int id = 0;
            if (b.TryGetProperty("branchId", out var bid) || b.TryGetProperty("BranchId", out bid))
                id = bid.GetInt32();

            string code = GetProp(b, "branchCode", "BranchCode");
            string name = GetProp(b, "branchName", "BranchName");
            string address = GetProp(b, "address", "Address");
            string contact = GetProp(b, "contactNumber", "ContactNumber");
            string email = GetProp(b, "email", "Email");

            bool isMain = false;
            if (b.TryGetProperty("isMainBranch", out var imb) || b.TryGetProperty("IsMainBranch", out imb))
                isMain = imb.GetBoolean();

            bool isActive = false;
            if (b.TryGetProperty("isActive", out var ia) || b.TryGetProperty("IsActive", out ia))
                isActive = ia.GetBoolean();

            string createdAtStr = GetProp(b, "createdAt", "CreatedAt");
            string createdDisplay = DateTime.TryParse(createdAtStr, out var dt) ? dt.ToString("yyyy-MM-dd") : "-";

            string typeDisplay = isMain ? "Main Branch" : "Satellite";
            string statusDisplay = isActive ? "Active" : "Inactive";

            string mgrName = GetProp(b, "managerName", "ManagerName");
            string mgrEmail = GetProp(b, "managerEmail", "ManagerEmail");
            string managerDisplay = !string.IsNullOrWhiteSpace(mgrName) ? $"{mgrName} ({mgrEmail})" :
                                    !string.IsNullOrWhiteSpace(mgrEmail) ? mgrEmail : "— (Unassigned)";

            int rowIndex = _grid.Rows.Add(
                id,
                code,
                name,
                address,
                contact,
                email,
                managerDisplay,
                typeDisplay,
                statusDisplay,
                createdDisplay
            );

            _grid.Rows[rowIndex].Tag = b;
        }
    }

    private async Task OpenNewBranchDialogAsync()
    {
        using var dlg = new BranchDialog(_apiUrl, _http);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await LoadBranchesAsync();
        }
    }

    private async Task OpenEditBranchDialogAsync()
    {
        if (_grid.CurrentRow?.Tag is not JsonElement selected)
        {
            MessageBox.Show("Please select a branch to edit.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new BranchDialog(_apiUrl, _http, selected);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await LoadBranchesAsync();
        }
    }

    private async Task ToggleStatusAsync()
    {
        if (_grid.CurrentRow?.Tag is not JsonElement selected)
        {
            MessageBox.Show("Please select a branch to toggle.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int id = 0;
        if (selected.TryGetProperty("branchId", out var bid) || selected.TryGetProperty("BranchId", out bid))
            id = bid.GetInt32();

        bool isActive = false;
        if (selected.TryGetProperty("isActive", out var ia) || selected.TryGetProperty("IsActive", out ia))
            isActive = ia.GetBoolean();

        bool newStatus = !isActive;
        int companyId = Session.CompanyId ?? 1;

        var payload = new
        {
            BranchId = id,
            CompanyId = companyId,
            BranchCode = GetProp(selected, "branchCode", "BranchCode"),
            BranchName = GetProp(selected, "branchName", "BranchName"),
            Address = GetProp(selected, "address", "Address"),
            ContactNumber = GetProp(selected, "contactNumber", "ContactNumber"),
            Email = GetProp(selected, "email", "Email"),
            IsMainBranch = selected.TryGetProperty("isMainBranch", out var imb) && imb.GetBoolean(),
            IsActive = newStatus
        };

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, $"{_apiUrl}/tenant/{companyId}/branches/{id}")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(Session.Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                await LoadBranchesAsync();
            }
            else
            {
                MessageBox.Show("Failed to update branch status.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteBranchAsync()
    {
        if (_grid.CurrentRow?.Tag is not JsonElement selected)
        {
            MessageBox.Show("Please select a branch to delete.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int id = 0;
        if (selected.TryGetProperty("branchId", out var bid) || selected.TryGetProperty("BranchId", out bid))
            id = bid.GetInt32();

        string name = GetProp(selected, "branchName", "BranchName");

        var confirm = MessageBox.Show(
            $"Are you sure you want to delete branch \"{name}\"?\nThis action cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        int companyId = Session.CompanyId ?? 1;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"{_apiUrl}/tenant/{companyId}/branches/{id}");
            if (!string.IsNullOrWhiteSpace(Session.Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                await LoadBranchesAsync();
            }
            else
            {
                MessageBox.Show("Failed to delete branch.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
}
