using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class RetentionPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private DataGridView _grid = null!;
    private ComboBox _cmbFilter = null!;
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
        var header = new Panel { Dock = DockStyle.Top, Height = 60 };
        Controls.Add(header);

        _lblStatus = new Label
        {
            Text = "Loading...",
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            Location = new Point(0, 18)
        };
        header.Controls.Add(_lblStatus);

        // ---- Refresh ----
        _btnRefresh = new Button
        {
            Text = "↻  Refresh",
            Left = 220,
            Top = 8,
            Width = 110,
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

        // ---- NEW: Retain Any Customer ----
        _btnRetainAny = new Button
        {
            Text = "＋  Retain Any Customer",
            Left = 340,
            Top = 8,
            Width = 210,
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
                // Reload in case anything changed
                await LoadAsync();
            }
        };
        header.Controls.Add(_btnRetainAny);

        // ---- Filter label ----
        header.Controls.Add(new Label
        {
            Text = "Filter:",
            Left = 570,
            Top = 18,
            Width = 50,
            Height = 20,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(110, 118, 132)
        });

        _cmbFilter = new ComboBox
        {
            Left = 620,
            Top = 12,
            Width = 180,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbFilter.Items.Add("All Segments");
        _cmbFilter.Items.AddRange(new object[]
        {
            "Champion", "Loyal", "Promising", "Detractor",
            "At Risk", "Dormant", "Lost", "Active"
        });
        _cmbFilter.SelectedIndex = 0;
        _cmbFilter.SelectedIndexChanged += (_, _) =>
        {
            _currentPage = 1;
            ApplyFilterAndPaginate();
        };
        header.Controls.Add(_cmbFilter);

        // ---- Per page ----
        header.Controls.Add(new Label
        {
            Text = "Per page:",
            Left = 820,
            Top = 18,
            Width = 70,
            Height = 20,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(110, 118, 132)
        });

        _cmbPageSize = new ComboBox
        {
            Left = 895,
            Top = 12,
            Width = 80,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
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

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];
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
                dlg.ShowDialog(FindForm());
            }
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
            _all = doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();

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
        var filter = _cmbFilter.SelectedItem?.ToString() ?? "All Segments";

        var filtered = filter == "All Segments"
            ? _all
            : _all.Where(r => GetStr(r, "segment") == filter).ToList();

        _filtered = filtered
            .OrderBy(r => GetInt(r, "priority"))
            .ThenByDescending(r => GetInt(r, "daysSinceLastProject"))
            .ToList();

        if (_currentPage > TotalPages) _currentPage = TotalPages;
        if (_currentPage < 1) _currentPage = 1;

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

        var filter = _cmbFilter.SelectedItem?.ToString() ?? "All Segments";
        _lblPageInfo.Text = $"Page {_currentPage} of {TotalPages}   ·   " +
                           $"Showing {start + 1}–{end} of {_filtered.Count}";

        _btnPrev.Enabled = _currentPage > 1;
        _btnNext.Enabled = _currentPage < TotalPages;
        _btnPrev.ForeColor = _btnPrev.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);
        _btnNext.ForeColor = _btnNext.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);

        _lblStatus.Text = $"{_filtered.Count} customer{(_filtered.Count == 1 ? "" : "s")}   ·   Filter: {filter}";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);
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