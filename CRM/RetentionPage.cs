using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class RetentionPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private DataGridView _grid = null!;
    private CrmFilterBar _filterBar = null!;
    private Label _lblStatus = null!;
    private Button _btnRefresh = null!;
    private Button _btnRetainAny = null!;
    private Button _btnPrev = null!;
    private Button _btnNext = null!;
    private Label _lblPageInfo = null!;
    private ComboBox _cmbPageSize = null!;

    private List<JsonElement> _all = new();
    private List<JsonElement> _filtered = new();

    private const int DefaultPageSize = 10;
    private int _pageSize = DefaultPageSize;
    private int _currentPage = 1;

    public RetentionPage(string apiUrl, HttpClient http)
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

        // ---- NEW: Retain Any Customer / Add Member (Primary Action) ----
        _btnRetainAny = new Button
        {
            Text = CompanyTerminology.BtnNewRetention,
            Left = 0,
            Top = 8,
            Width = 190,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRetainAny.FlatAppearance.BorderSize = 0;
        _btnRetainAny.Click += async (_, _) =>
        {
            using var dlg = new RetainCustomerDialog(_apiUrl, _http);
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            {
                await LoadAsync();
            }
        };
        header.Controls.Add(_btnRetainAny);

        // ---- Send via Email button ----
        var btnSendEmail = new Button
        {
            Text = "✉  Send via Email",
            Left = 196,
            Top = 8,
            Width = 140,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSendEmail.FlatAppearance.BorderSize = 0;
        btnSendEmail.Click += async (_, _) =>
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0)
            {
                int rIdx = _grid.CurrentRow.Index;
                var row = _grid.Rows[rIdx];
                var name = row.Cells["name"].Value?.ToString() ?? "";
                var seg = row.Cells["segment"].Value?.ToString() ?? "";
                var basis = row.Cells["basis"].Value?.ToString() ?? "";
                var action = row.Cells["action"].Value?.ToString() ?? "";

                var match = _filtered.FirstOrDefault(r => GetStr(r, "fullName") == name);
                if (match.ValueKind != JsonValueKind.Undefined)
                {
                    var custId = GetInt(match, "customerId");
                    using var dlg = new ActionTemplateDialog(
                        _apiUrl, _http,
                        custId, name, seg, action, basis,
                        GetStr(match, "email"),
                        GetStr(match, "phone"),
                        GetDecimal(match, "totalRevenue"));
                    if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    {
                        await LoadAsync();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a customer from the table first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
        header.Controls.Add(btnSendEmail);

        // ---- Change Customer Status / Tier button ----
        var btnChangeStatus = new Button
        {
            Text = "★  Change Status",
            Left = 342,
            Top = 8,
            Width = 145,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnChangeStatus.FlatAppearance.BorderSize = 0;
        btnChangeStatus.Click += async (_, _) => await PromptChangeStatusForSelectedAsync();
        header.Controls.Add(btnChangeStatus);

        // ---- Refresh ----
        _btnRefresh = new Button
        {
            Text = "↻  Refresh",
            Left = 494,
            Top = 8,
            Width = 90,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(28, 32, 40),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        _btnRefresh.Click += async (_, _) =>
        {
            _currentPage = 1;
            await LoadAsync();
        };
        header.Controls.Add(_btnRefresh);

        // ---- Per page ----
        header.Controls.Add(new Label
        {
            Text = "Per page:",
            Left = 592,
            Top = 16,
            Width = 58,
            Height = 24,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132)
        });

        _cmbPageSize = new ComboBox
        {
            Left = 652,
            Top = 12,
            Width = 65,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        _cmbPageSize.Items.AddRange(new object[] { "10", "25", "50", "100" });
        _cmbPageSize.SelectedIndex = 0;
        _cmbPageSize.SelectedIndexChanged += (_, _) =>
        {
            if (int.TryParse(_cmbPageSize.SelectedItem?.ToString(), out var n))
            {
                _pageSize = n;
                _currentPage = 1;
                ApplyFilterAndPaginate();
            }
        };
        header.Controls.Add(_cmbPageSize);

        // ---- Status message safely positioned on right side ----
        _lblStatus = new Label
        {
            Text = "Loading...",
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Width = 320,
            Height = 26,
            Location = new Point(Math.Max(485, header.ClientSize.Width - 330), 13)
        };
        header.Controls.Add(_lblStatus);
        header.Resize += (_, _) => _lblStatus.Left = Math.Max(485, header.ClientSize.Width - 330);

        // ---- Filter bar ----
        _filterBar = new CrmFilterBar("Search customer, action, or basis...");
        _filterBar.AddFilter("Segment", "Segment", "Champion", "Loyal", "Promising", "Active", "At Risk", "Detractor", "Dormant", "Lost", "VIP", "Regular");
        _filterBar.AddFilter("Urgency", "Priority", "High Priority", "Medium Priority", "Low Priority");
        _filterBar.FiltersChanged += (_, _) =>
        {
            _currentPage = 1;
            ApplyFilterAndPaginate();
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

        _grid.Columns.Add("rowNum", "#");
        _grid.Columns.Add("segment", "SEGMENT");
        _grid.Columns.Add("name", "CUSTOMER");
        _grid.Columns.Add("projects", "PROJECTS");
        _grid.Columns.Add("rating", "AVG ★");
        _grid.Columns.Add("days", "DAYS SINCE");
        _grid.Columns.Add("revenue", "REVENUE");
        _grid.Columns.Add("basis", "BASIS");
        _grid.Columns.Add("action", "RECOMMENDED ACTION");

        // Column weight tuning
        _grid.Columns["rowNum"].FillWeight = 18;
        _grid.Columns["segment"].FillWeight = 65;
        _grid.Columns["name"].FillWeight = 90;
        _grid.Columns["projects"].FillWeight = 35;
        _grid.Columns["rating"].FillWeight = 35;
        _grid.Columns["days"].FillWeight = 55;
        _grid.Columns["revenue"].FillWeight = 60;
        _grid.Columns["basis"].FillWeight = 140;
        _grid.Columns["action"].FillWeight = 140;

        // ---- Apply modern card-row styling with pills ----
        CrmTableStyler.Apply(_grid, "segment");
        _grid.RowTemplate.Height = 60;

        _grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex == _grid.Columns["rowNum"].Index)
            {
                e.CellStyle.ForeColor = Color.FromArgb(160, 168, 180);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                e.CellStyle.Font = new Font("Segoe UI", 8.5f);
            }

            if (e.ColumnIndex == _grid.Columns["segment"].Index && e.Value is string)
            {
                // Suppress default text rendering — pill paints itself
                e.CellStyle.ForeColor = Color.Transparent;
            }
        };

        // ---- Context Menu for fast loyalty and status changes ----
        var contextMenu = new ContextMenuStrip();
        var itemChampion = new ToolStripMenuItem("★  Make Champion", null, async (_, _) => await QuickSetStatusForSelectedAsync("Champion"));
        var itemLoyal = new ToolStripMenuItem("💎  Make Loyal", null, async (_, _) => await QuickSetStatusForSelectedAsync("Loyal"));
        var itemPromising = new ToolStripMenuItem("⚡  Make Promising", null, async (_, _) => await QuickSetStatusForSelectedAsync("Promising"));
        var itemActive = new ToolStripMenuItem("🟢  Set as Active", null, async (_, _) => await QuickSetStatusForSelectedAsync("Active"));
        var itemAtRisk = new ToolStripMenuItem("⚠️  Set as At Risk", null, async (_, _) => await QuickSetStatusForSelectedAsync("At Risk"));
        var itemDetractor = new ToolStripMenuItem("🔴  Set as Detractor", null, async (_, _) => await QuickSetStatusForSelectedAsync("Detractor"));
        var itemChangeTier = new ToolStripMenuItem("✎  Change Status / Tier...", null, async (_, _) => await PromptChangeStatusForSelectedAsync());
        var itemEmail = new ToolStripMenuItem("✉  Send Retention Email", null, async (_, _) => await OpenEmailDialogForSelectedAsync());

        contextMenu.Items.AddRange(new ToolStripItem[]
        {
            itemChampion,
            itemLoyal,
            itemPromising,
            itemActive,
            itemAtRisk,
            itemDetractor,
            new ToolStripSeparator(),
            itemChangeTier,
            new ToolStripSeparator(),
            itemEmail
        });

        _grid.ContextMenuStrip = contextMenu;
        _grid.CellMouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                _grid.ClearSelection();
                _grid.Rows[e.RowIndex].Selected = true;
                _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex >= 0 ? e.ColumnIndex : 0];
            }
        };

        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex < 0) return;
            await OpenEmailDialogForSelectedAsync();
        };

        // =========================================================
        // PAGINATION BAR
        // =========================================================
        var pager = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.White
        };
        Controls.Add(pager);

        pager.Paint += (_, e) =>
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
            if (_currentPage < TotalPages) { _currentPage++; RenderPage(); }
        };
        pager.Controls.Add(_btnNext);

        Controls.Add(_grid);
        _grid.BringToFront();
    }

    // =========================================================
    // LOAD
    // =========================================================
    public async Task LoadAsync()
    {
        _lblStatus.Text = "Loading retention data...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/bi/retention";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            using var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _lblStatus.Text = $"Failed: {(int)res.StatusCode}";
                _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
                return;
            }

            using var doc = JsonDocument.Parse(json);
            _all = CrmTableStyler.SortNewestFirst(doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList());

            _currentPage = 1;
            ApplyFilterAndPaginate();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
        }
    }

    // =========================================================
    // PAGINATION
    // =========================================================
    private int TotalPages =>
        _filtered.Count == 0 ? 1 : (int)Math.Ceiling(_filtered.Count / (double)_pageSize);

    private void ApplyFilterAndPaginate()
    {
        var search = _filterBar.SearchText.ToLowerInvariant();
        var segFilter = _filterBar.GetFilterValue("Segment");
        var urgencyFilter = _filterBar.GetFilterValue("Urgency");

        var filtered = _all.Where(r =>
        {
            var seg = GetStr(r, "segment");
            var name = GetStr(r, "fullName");
            var basis = GetStr(r, "basis");
            var action = GetStr(r, "recommendedAction");
            var priority = GetInt(r, "priority");

            if (segFilter != null && !seg.Equals(segFilter, StringComparison.OrdinalIgnoreCase)) return false;

            if (urgencyFilter != null)
            {
                if (urgencyFilter.StartsWith("High") && priority > 2) return false;
                if (urgencyFilter.StartsWith("Medium") && (priority < 3 || priority > 4)) return false;
                if (urgencyFilter.StartsWith("Low") && priority < 5) return false;
            }

            if (!string.IsNullOrEmpty(search))
            {
                if (!name.ToLowerInvariant().Contains(search) &&
                    !basis.ToLowerInvariant().Contains(search) &&
                    !action.ToLowerInvariant().Contains(search) &&
                    !seg.ToLowerInvariant().Contains(search))
                    return false;
            }
            return true;
        }).ToList();

        _filtered = filtered
            .OrderBy(r => GetInt(r, "priority"))
            .ThenByDescending(r => GetInt(r, "daysSinceLastProject"))
            .ToList();

        if (_currentPage > TotalPages) _currentPage = TotalPages;
        if (_currentPage < 1) _currentPage = 1;

        _filterBar.SetRecordCount(_filtered.Count, _all.Count);
        RenderPage();
    }

    private void RenderPage()
    {
        _grid.Rows.Clear();

        int start = (_currentPage - 1) * _pageSize;
        int end = Math.Min(start + _pageSize, _filtered.Count);

        for (int i = start; i < end; i++)
        {
            var r = _filtered[i];
            var rating = GetDouble(r, "avgRating");
            var ratingText = rating > 0 ? $"{rating:F1}★" : "—";

            _grid.Rows.Add(
                (i + 1).ToString(),
                GetStr(r, "segment"),
                GetStr(r, "fullName"),
                GetInt(r, "projectCount"),
                ratingText,
                $"{GetInt(r, "daysSinceLastProject")} days",
                $"₱{GetDecimal(r, "totalRevenue") / 1000:N0}K",
                GetStr(r, "basis"),
                GetStr(r, "action"));
        }

        var filter = _filterBar.GetFilterValue("Segment") ?? "All Segments";
        _lblPageInfo.Text = $"Page {_currentPage} of {TotalPages}   ·   " +
                           $"Showing {start + 1}–{end} of {_filtered.Count}";

        _btnPrev.Enabled = _currentPage > 1;
        _btnNext.Enabled = _currentPage < TotalPages;
        _btnPrev.ForeColor = _btnPrev.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);
        _btnNext.ForeColor = _btnNext.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);

        _filterBar.SetRecordCount(_filtered.Count, _all.Count);
        _lblStatus.Text = "";
    }

    // =========================================================
    // STATUS & EMAIL ACTIONS
    // =========================================================
    private async Task OpenEmailDialogForSelectedAsync()
    {
        if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0)
        {
            MessageBox.Show("Please select a customer from the table first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int rIdx = _grid.CurrentRow.Index;
        var row = _grid.Rows[rIdx];
        var name = row.Cells["name"].Value?.ToString() ?? "";
        var seg = row.Cells["segment"].Value?.ToString() ?? "";
        var basis = row.Cells["basis"].Value?.ToString() ?? "";
        var action = row.Cells["action"].Value?.ToString() ?? "";

        var match = _filtered.FirstOrDefault(r => GetStr(r, "fullName") == name);
        if (match.ValueKind != JsonValueKind.Undefined)
        {
            var custId = GetInt(match, "customerId");
            using var dlg = new ActionTemplateDialog(
                _apiUrl, _http,
                custId, name, seg, action, basis,
                GetStr(match, "email"),
                GetStr(match, "phone"),
                GetDecimal(match, "totalRevenue"));
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            {
                await LoadAsync();
            }
        }
    }

    private async Task QuickSetStatusForSelectedAsync(string newStatus)
    {
        if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0)
        {
            MessageBox.Show("Please select a customer from the table first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var name = _grid.Rows[_grid.CurrentRow.Index].Cells["name"].Value?.ToString() ?? "";
        var match = _filtered.FirstOrDefault(r => GetStr(r, "fullName") == name);
        if (match.ValueKind != JsonValueKind.Undefined)
        {
            var custId = GetInt(match, "customerId");
            await SetCustomerStatusAsync(custId, name, newStatus);
        }
    }

    private async Task PromptChangeStatusForSelectedAsync()
    {
        if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0)
        {
            MessageBox.Show("Please select a customer from the table first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var name = _grid.Rows[_grid.CurrentRow.Index].Cells["name"].Value?.ToString() ?? "";
        var match = _filtered.FirstOrDefault(r => GetStr(r, "fullName") == name);
        if (match.ValueKind == JsonValueKind.Undefined) return;

        var custId = GetInt(match, "customerId");
        var currentStatus = GetStr(match, "segment");

        using var dlg = new CrmModalDialog(
            "Change Customer Loyalty & Status",
            $"Update customer tier and retention segment for {name}",
            "★",
            "Update Status",
            480);

        var txtCust = dlg.AddTextField("Customer", "", $"{name} (ID: {custId})", true);
        txtCust.ReadOnly = true;

        var cmbStatus = dlg.AddDropdownField("New Loyalty Status / Tier *", new object[]
        {
            "Champion",
            "Loyal",
            "Promising",
            "Active",
            "At Risk",
            "Detractor",
            "Dormant",
            "Lost",
            "VIP",
            "Regular"
        }, currentStatus, true);

        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            var chosen = cmbStatus.SelectedItem?.ToString() ?? "Loyal";
            await SetCustomerStatusAsync(custId, name, chosen);
        }
    }

    private async Task SetCustomerStatusAsync(int custId, string custName, string newStatus)
    {
        try
        {
            _lblStatus.Text = $"Updating {custName} to {newStatus}...";
            _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

            var payload = new { Status = newStatus };
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/customers/{custId}/status";

            using var req = new HttpRequestMessage(HttpMethod.Put, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            using var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                MessageBox.Show($"Failed to update customer status: {err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show($"Customer {custName} has been successfully updated to '{newStatus}'!", "Status Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error updating status: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private static string GetStr(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return "";
        return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.ToString();
    }

    private static int GetInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0;
        return p.ValueKind == JsonValueKind.Number ? (int)p.GetDouble() : 0;
    }

    private static double GetDouble(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0;
        return p.ValueKind == JsonValueKind.Number ? p.GetDouble() : 0;
    }

    private static decimal GetDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0m;
        return p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : 0m;
    }
}