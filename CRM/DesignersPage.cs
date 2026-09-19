using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class DesignersPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly DataGridView _grid;
    private readonly Label _lblStatus;
    private readonly Button _btnRefresh;
    private readonly Button _btnView;

    private List<JsonElement> _designers = new();

    public DesignersPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Padding = new Padding(32, 20, 32, 32);

        // ---- Toolbar ----
        var header = new Panel { Dock = DockStyle.Top, Height = 56 };
        Controls.Add(header);

        _lblStatus = new Label
        {
            Text = "Loading...",
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            Location = new Point(0, 16)
        };
        header.Controls.Add(_lblStatus);

        _btnRefresh = new Button
        {
            Text = "↻  Refresh",
            Left = 140,
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
        _btnRefresh.Click += async (_, _) => await LoadAsync();
        header.Controls.Add(_btnRefresh);

        _btnView = new Button
        {
            Text = "👁  View Details",
            Left = 260,
            Top = 8,
            Width = 150,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(255, 245, 225),
            ForeColor = Color.FromArgb(160, 95, 0),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnView.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        _btnView.Click += async (_, _) => await OpenDetailAsync();
        header.Controls.Add(_btnView);

        // ---- Grid ----
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 42
        };
        _grid.RowTemplate.Height = 52;

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

        _grid.Columns.Add("fullName", "DESIGNER");
        _grid.Columns.Add("email", "EMAIL");
        _grid.Columns.Add("totalProjects", "TOTAL PROJECTS");
        _grid.Columns.Add("activeProjects", "ACTIVE");
        _grid.Columns.Add("completedProjects", "COMPLETED");
        _grid.Columns.Add("openIssues", "OPEN ISSUES");
        _grid.Columns.Add("feedbackCount", "RATINGS");
        _grid.Columns.Add("rating", "AVG RATING");

        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenDetailAsync();
        };

        Controls.Add(_grid);
        _grid.BringToFront();

    }

    public async Task LoadAsync()
    {
        _lblStatus.Text = "Loading designers...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);
        _grid.Rows.Clear();

        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/designers";
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
            _designers = doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();

            foreach (var d in _designers)
            {
                var stars = Stars(GetDouble(d, "overallScore"));
                var avgText = GetDouble(d, "overallScore") is double s
                    ? $"{stars}  {s:F2}"
                    : "— no ratings —";

                _grid.Rows.Add(
                    GetStr(d, "fullName"),
                    GetStr(d, "email"),
                    GetInt(d, "totalProjects"),
                    GetInt(d, "activeProjects"),
                    GetInt(d, "completedProjects"),
                    GetInt(d, "openIssues"),
                    GetInt(d, "feedbackCount"),
                    avgText);
            }

            _lblStatus.Text = $"{_designers.Count} designer{(_designers.Count == 1 ? "" : "s")}";
            _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
        }
    }

    private async Task OpenDetailAsync()
    {
        if (_grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Select a designer first.", "Designer");
            return;
        }

        var idx = _grid.SelectedRows[0].Index;
        if (idx < 0 || idx >= _designers.Count) return;

        var designer = _designers[idx];
        var designerId = GetStr(designer, "userId");
        if (string.IsNullOrWhiteSpace(designerId)) return;

        using var dlg = new DesignerDetailDialog(_apiUrl, _http, designerId);
        dlg.ShowDialog(FindForm());
    }

    // ---- JSON helpers ----
    private static string GetStr(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return "";
        return p.ValueKind switch
        {
            JsonValueKind.Null => "",
            JsonValueKind.String => p.GetString() ?? "",
            _ => p.ToString()
        };
    }

    private static int GetInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0;
        if (p.ValueKind == JsonValueKind.Number) return (int)p.GetDouble();
        return 0;
    }

    private static double? GetDouble(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Null) return null;
        if (p.ValueKind == JsonValueKind.Number) return p.GetDouble();
        return null;
    }

    private static string Stars(double? rating)
    {
        if (!rating.HasValue) return "";
        int full = (int)Math.Round(rating.Value);
        if (full < 0) full = 0;
        if (full > 5) full = 5;
        return new string('★', full) + new string('☆', 5 - full);
    }
}