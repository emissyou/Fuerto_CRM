using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class DesignersPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly DataGridView _grid;
    private readonly Button _btnRefresh;
    private readonly Button _btnView;
    private readonly CrmFilterBar _filterBar;

    private List<JsonElement> _designers = new();
    private List<JsonElement> _filteredDesigners = new();

    public DesignersPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Padding = new Padding(32, 20, 32, 32);

        // ---- Toolbar ----
        var header = new Panel { Dock = DockStyle.Top, Height = 50 };
        Controls.Add(header);

        _btnRefresh = new Button
        {
            Text = "↻  Refresh",
            Left = 0,
            Top = 7,
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
            Left = 118,
            Top = 7,
            Width = 140,
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

        // ---- Filter Bar ----
        _filterBar = new CrmFilterBar("Search designers by name, email...");
        _filterBar.AddFilter("Tier", "Tier", "Top Rated (≥ 4.0 ★)", "Good (3.0 - 3.9 ★)", "Needs Review (< 3.0 ★)");
        _filterBar.AddFilter("Workload", "Workload", "Active Projects (> 0)", "Available (0 Active)", "Has Open Issues");
        _filterBar.FiltersChanged += (_, _) => ApplyFilters();
        Controls.Add(_filterBar);
        _filterBar.BringToFront();

        // ---- Card & Grid ----
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };
        Controls.Add(card);
        card.BringToFront();

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false
        };

        _grid.Columns.Add("fullName", "DESIGNER");
        _grid.Columns.Add("email", "EMAIL");
        _grid.Columns.Add("totalProjects", "TOTAL PROJECTS");
        _grid.Columns.Add("activeProjects", "ACTIVE");
        _grid.Columns.Add("completedProjects", "COMPLETED");
        _grid.Columns.Add("openIssues", "OPEN ISSUES");
        _grid.Columns.Add("feedbackCount", "RATINGS");
        _grid.Columns.Add("rating", "AVG RATING");

        _grid.Columns["fullName"].FillWeight = 130;
        _grid.Columns["email"].FillWeight = 140;
        _grid.Columns["totalProjects"].FillWeight = 70;
        _grid.Columns["activeProjects"].FillWeight = 60;
        _grid.Columns["completedProjects"].FillWeight = 70;
        _grid.Columns["openIssues"].FillWeight = 65;
        _grid.Columns["feedbackCount"].FillWeight = 65;
        _grid.Columns["rating"].FillWeight = 100;

        CrmTableStyler.Apply(_grid);

        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenDetailAsync();
        };

        card.Controls.Add(_grid);
    }

    public async Task LoadAsync()
    {
        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/designers";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            using var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _filterBar.SetRecordCount(0, 0);
                return;
            }

            using var doc = JsonDocument.Parse(json);
            _designers = doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
            ApplyFilters();
        }
        catch
        {
            _filterBar.SetRecordCount(0, 0);
        }
    }

    private void ApplyFilters()
    {
        var search = _filterBar.SearchText;
        var tier = _filterBar.GetFilterValue("Tier");
        var workload = _filterBar.GetFilterValue("Workload");

        _filteredDesigners = _designers.Where(d =>
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var name = GetStr(d, "fullName");
                var email = GetStr(d, "email");
                if (!name.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !email.Contains(search, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            var score = GetDouble(d, "overallScore");
            if (tier != null)
            {
                if (tier.StartsWith("Top") && (score == null || score < 4.0)) return false;
                if (tier.StartsWith("Good") && (score == null || score < 3.0 || score >= 4.0)) return false;
                if (tier.StartsWith("Needs") && (score == null || score >= 3.0)) return false;
            }

            if (workload != null)
            {
                var active = GetInt(d, "activeProjects");
                var issues = GetInt(d, "openIssues");
                if (workload.StartsWith("Active") && active == 0) return false;
                if (workload.StartsWith("Available") && active > 0) return false;
                if (workload.StartsWith("Has") && issues == 0) return false;
            }

            return true;
        }).ToList();

        RenderGrid();
        _filterBar.SetRecordCount(_filteredDesigners.Count, _designers.Count);
    }

    private void RenderGrid()
    {
        _grid.Rows.Clear();

        foreach (var d in _filteredDesigners)
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
    }

    private async Task OpenDetailAsync()
    {
        if (_grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Select a designer first.", "Designer");
            return;
        }

        var idx = _grid.SelectedRows[0].Index;
        if (idx < 0 || idx >= _filteredDesigners.Count) return;

        var designer = _filteredDesigners[idx];
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