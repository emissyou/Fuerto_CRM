using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class PromotionsPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private DataGridView _grid = null!;
    private Label _lblStatus = null!;
    private Button _btnNew = null!;
    private Button _btnRefresh = null!;
    private Button _btnEdit = null!;
    private Button _btnToggle = null!;
    private Button _btnDelete = null!;
    private CrmFilterBar _filterBar = null!;

    private List<JsonElement> _all = new();
    private List<JsonElement> _filtered = new();

    private const int PageSize = 17;
    private int _currentPage = 1;
    private Label _lblPageInfo = null!;
    private Button _btnPrev = null!;
    private Button _btnNext = null!;

    public PromotionsPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Padding = new Padding(32, 20, 32, 32);

        // ---- HEADER ----
        var header = new Panel { Dock = DockStyle.Top, Height = 60 };
        Controls.Add(header);

        // ---- Status message safely positioned on right side ----
        _lblStatus = new Label
        {
            Text = "Loading promotions...",
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Width = 320,
            Height = 26,
            Location = new Point(Math.Max(615, header.ClientSize.Width - 330), 13)
        };
        header.Controls.Add(_lblStatus);
        header.Resize += (_, _) => _lblStatus.Left = Math.Max(615, header.ClientSize.Width - 330);

        int pLeft = 0;
        _btnNew = MakeButton(CompanyTerminology.BtnNewPromotion, 180, true);
        _btnNew.Left = pLeft;
        _btnNew.Click += async (_, _) => await OpenNewDialogAsync();
        header.Controls.Add(_btnNew);
        pLeft += _btnNew.Width + 8;

        _btnRefresh = MakeButton("↻  Refresh", 100, false);
        _btnRefresh.Left = pLeft;
        _btnRefresh.Click += async (_, _) => await LoadAsync();
        header.Controls.Add(_btnRefresh);
        pLeft += _btnRefresh.Width + 8;

        _btnEdit = MakeButton("✎  Edit", 85, false);
        _btnEdit.Left = pLeft;
        _btnEdit.Click += async (_, _) => await OpenEditDialogAsync();
        header.Controls.Add(_btnEdit);
        pLeft += _btnEdit.Width + 8;

        _btnToggle = MakeButton("⏻  Toggle", 95, false);
        _btnToggle.Left = pLeft;
        _btnToggle.Click += async (_, _) => await ToggleSelectedAsync();
        header.Controls.Add(_btnToggle);
        pLeft += _btnToggle.Width + 8;

        _btnDelete = MakeButton("🗑  Delete", 95, false);
        _btnDelete.Left = pLeft;
        _btnDelete.Click += async (_, _) => await DeleteSelectedAsync();
        header.Controls.Add(_btnDelete);

        // ---- Filter bar ----
        _filterBar = new CrmFilterBar("Search promotion name, code, offer...");
        _filterBar.AddFilter("Type", "Type", "Percentage", "FixedAmount", "FreeService");
        _filterBar.AddFilter("Target", "Target", "All Clients", "Champion", "Loyal", "Promising", "Detractor", "At Risk", "Dormant");
        _filterBar.AddFilter("Status", "Status", "Active", "Inactive");
        _filterBar.FiltersChanged += (_, _) =>
        {
            _currentPage = 1;
            ApplyFilterAndRender();
        };
        Controls.Add(_filterBar);
        _filterBar.BringToFront();

        // ---- GRID ----
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false
        };

        _grid.Columns.Add("name", "NAME");
        _grid.Columns.Add("code", "CODE");
        _grid.Columns.Add("offer", "OFFER");
        _grid.Columns.Add("segment", "TARGET");
        _grid.Columns.Add("validity", "VALIDITY");
        _grid.Columns.Add("uses", "USES");
        _grid.Columns.Add("status", "STATUS");

        _grid.Columns["name"].FillWeight = 140;
        _grid.Columns["code"].FillWeight = 80;
        _grid.Columns["offer"].FillWeight = 110;
        _grid.Columns["segment"].FillWeight = 80;
        _grid.Columns["validity"].FillWeight = 130;
        _grid.Columns["uses"].FillWeight = 60;
        _grid.Columns["status"].FillWeight = 70;

        CrmTableStyler.Apply(_grid, "status", "segment");
        _grid.RowTemplate.Height = 56;

        _grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex == _grid.Columns["status"].Index ||
                e.ColumnIndex == _grid.Columns["segment"].Index)
            {
                e.CellStyle.ForeColor = Color.Transparent;
            }
        };

        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0) await OpenEditDialogAsync();
        };

        // ---- PAGER ----
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
        _lblStatus.Text = "Loading promotions...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/promotions";

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
            _all = CrmTableStyler.SortNewestFirst(doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList());

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
        var typeFilter = _filterBar.GetFilterValue("Type");
        var targetFilter = _filterBar.GetFilterValue("Target");
        var statusFilter = _filterBar.GetFilterValue("Status");

        _filtered = _all.Where(p =>
        {
            var name = GetStr(p, "name");
            var code = GetStr(p, "code");
            var offerType = GetStr(p, "offerType");
            var target = GetStr(p, "targetSegment");
            var isActive = GetBool(p, "isActive");

            if (typeFilter != null && !offerType.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (targetFilter != null && !target.Equals(targetFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (statusFilter != null)
            {
                if (statusFilter.Equals("Active", StringComparison.OrdinalIgnoreCase) && !isActive) return false;
                if (statusFilter.Equals("Inactive", StringComparison.OrdinalIgnoreCase) && isActive) return false;
            }

            if (!string.IsNullOrEmpty(search))
            {
                if (!name.ToLowerInvariant().Contains(search) &&
                    !code.ToLowerInvariant().Contains(search) &&
                    !target.ToLowerInvariant().Contains(search) &&
                    !offerType.ToLowerInvariant().Contains(search))
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
            var p = _filtered[i];

            var offerType = GetStr(p, "offerType");
            var offerValue = GetDecimal(p, "offerValue");
            var offerText = offerType switch
            {
                "Percentage" => $"{offerValue:F0}% off",
                "FixedAmount" => $"₱{offerValue:N0} off",
                "FreeService" => "Free service",
                _ => "Custom"
            };

            var validFrom = GetDate(p, "validFrom");
            var validUntil = GetDate(p, "validUntil");
            var validity = (validFrom, validUntil) switch
            {
                (null, null) => "No expiry",
                (null, _) => $"Until {validUntil:MMM dd, yyyy}",
                (_, null) => $"From {validFrom:MMM dd, yyyy}",
                _ => $"{validFrom:MMM dd} → {validUntil:MMM dd}"
            };

            var maxUses = GetInt(p, "maxUses");
            var used = GetInt(p, "usedCount") ?? 0;
            var usesText = maxUses.HasValue ? $"{used} / {maxUses}" : $"{used} / ∞";

            var isActive = GetBool(p, "isActive");
            var statusText = isActive ? "Active" : "Inactive";

            _grid.Rows.Add(
                GetStr(p, "name"),
                GetStr(p, "code"),
                offerText,
                GetStr(p, "targetSegment"),
                validity,
                usesText,
                statusText);
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

        var name = _grid.Rows[visibleIdx].Cells["name"].Value?.ToString();
        if (string.IsNullOrEmpty(name)) return null;

        var match = _filtered.FirstOrDefault(p => GetStr(p, "name") == name);
        return match.ValueKind == JsonValueKind.Undefined ? null : match;
    }

    private async Task OpenNewDialogAsync()
    {
        using var dlg = new PromotionDialog(_apiUrl, _http);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadAsync();
    }

    private async Task OpenEditDialogAsync()
    {
        var selected = GetSelected();
        if (selected is null)
        {
            MessageBox.Show("Select a promotion first.", "Edit Promotion");
            return;
        }

        using var dlg = new PromotionDialog(_apiUrl, _http, selected.Value);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadAsync();
    }

    private async Task ToggleSelectedAsync()
    {
        var selected = GetSelected();
        if (selected is null)
        {
            MessageBox.Show("Select a promotion first.", "Toggle Promotion");
            return;
        }

        var id = GetInt(selected.Value, "promotionId");
        if (!id.HasValue) return;

        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/promotions/{id.Value}/toggle";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
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

            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Toggle failed:\n\n" + ex.Message, "Error");
        }
    }

    private async Task DeleteSelectedAsync()
    {
        var selected = GetSelected();
        if (selected is null)
        {
            MessageBox.Show("Select a promotion first.", "Delete Promotion");
            return;
        }

        var id = GetInt(selected.Value, "promotionId");
        var name = GetStr(selected.Value, "name");
        if (!id.HasValue) return;

        var confirm = MessageBox.Show(
            $"Delete promotion: {name}?\n\nThis cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/promotions/{id.Value}";
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

            MessageBox.Show("Promotion deleted.", "Success");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Delete failed:\n\n" + ex.Message, "Error");
        }
    }

    // ---- JSON helpers ----
    private static string GetStr(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return "";
        return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.ToString();
    }

    private static int? GetInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Null) return null;
        return p.ValueKind == JsonValueKind.Number ? (int)p.GetDouble() : (int?)null;
    }

    private static decimal GetDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0m;
        if (p.ValueKind == JsonValueKind.Number) return p.GetDecimal();
        return 0m;
    }

    private static bool GetBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return false;
        return p.ValueKind == JsonValueKind.True;
    }

    private static DateTime? GetDate(JsonElement el, string name)
    {
        var s = GetStr(el, name);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, out var d) ? d : null;
    }
}