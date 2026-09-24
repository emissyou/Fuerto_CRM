using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class BiDashboardPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private Label _lblStatus = null!;
    private Button _btnRefresh = null!;
    private Panel _contentArea = null!;

    private JsonElement _kpis;
    private List<JsonElement> _revenue = new();
    private List<JsonElement> _retention = new();
    private List<JsonElement> _designers = new();

    public BiDashboardPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        AutoScroll = true;
        Padding = new Padding(32, 20, 32, 32);

        // =========================================================
        // Header
        // =========================================================
        var header = new Panel { Dock = DockStyle.Top, Height = 60 };
        Controls.Add(header);

        header.Controls.Add(new Label
        {
            Text = "Analytics",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(0, 0)
        });

        header.Controls.Add(new Label
        {
            Text = "KPIs, trends, and retention intelligence",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(2, 34)
        });

        _btnRefresh = new Button
        {
            Text = "↻  Refresh",
            Left = 320,
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

        _lblStatus = new Label
        {
            Left = 450,
            Top = 18,
            Width = 500,
            Height = 22,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            Text = "Loading..."
        };
        header.Controls.Add(_lblStatus);

        // =========================================================
        // Content area
        // =========================================================
        _contentArea = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(245, 247, 250),
            Padding = new Padding(0, 8, 0, 0)
        };
        Controls.Add(_contentArea);
        _contentArea.BringToFront();
    }

    // =========================================================
    // LOAD
    // =========================================================
    public async Task LoadAsync()
    {
        _lblStatus.Text = "Loading...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var kpisTask = GetObjectAsync("bi/kpis");
            var revTask = GetArrayAsync("bi/revenue");
            var retTask = GetArrayAsync("bi/retention");
            var desTask = GetArrayAsync("bi/designers");

            await Task.WhenAll(kpisTask, revTask, retTask, desTask);

            _kpis = await kpisTask;
            _revenue = await revTask;
            _retention = await retTask;
            _designers = await desTask;

            BuildKpis();
            BuildCharts();
            BuildRetentionAndDesigners();

            _lblStatus.Text = $"Updated {DateTime.Now:HH:mm:ss}  ·  {_designers.Count} designers  ·  {_retention.Count} customers";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
        }
    }

    // =========================================================
    // SECTION 1 — KPI CARDS (clickable)
    // =========================================================
    private void BuildKpis()
    {
        // Clear only KPI cards
        var oldKpis = _contentArea.Controls.OfType<CrmKpiCard>().ToList();
        foreach (var c in oldKpis) c.Dispose();

        int availableWidth = Math.Max(1200, _contentArea.ClientSize.Width - 20);
        int y = 4;

        int cardW = (availableWidth - 60) / 4;
        int cardH = 130;

        // =========================================================
        // ROW 1 — KPI cards
        // =========================================================
        var kpiData = new (string Label, string Value, string Delta, bool Positive, string Icon, string TargetPage, Color Accent)[]
        {
            ("LEAD CONVERSION", $"{GetDouble(_kpis, "leadConversionRate"):F1}%", "+12.4%", true, "●",
                "Analytics", Color.FromArgb(255, 168, 0)),
            ("REPEAT RATE", $"{GetDouble(_kpis, "repeatRate"):F1}%", "+8.2%", true, "♻",
                "Retention", Color.FromArgb(80, 140, 200)),
            ("CHURN RATE", $"{GetDouble(_kpis, "churnRate"):F1}%", "-3.5%", false, "⚠",
                "Retention", Color.FromArgb(200, 55, 55)),
            ("AVG RATING", $"{GetDouble(_kpis, "avgRating"):F2}★", $"{GetDouble(_kpis, "recommendRate"):F0}% rec.", true, "★",
                "Designers", Color.FromArgb(34, 140, 78)),
        };

        var trends = new[]
        {
            new double[] { 60, 62, 65, 68, 70, 71, 71.4 },
            new double[] { 30, 32, 34, 36, 37, 38, 38.8 },
            new double[] { 40, 38, 36, 34, 33, 32, 32.0 },
            new double[] { 3.8, 3.9, 4.0, 4.05, 4.1, 4.12, 4.14 },
        };

        for (int i = 0; i < kpiData.Length; i++)
        {
            var kpi = kpiData[i];
            var card = new CrmKpiCard
            {
                Label = kpi.Label,
                Value = kpi.Value,
                DeltaText = kpi.Delta,
                DeltaPositive = kpi.Positive,
                Icon = kpi.Icon,
                AccentColor = kpi.Accent,
                Location = new Point(i * (cardW + 15), y),
                Size = new Size(cardW, cardH),
                TrendValues = trends[i].ToList(),
                TargetPage = kpi.TargetPage
            };

            card.NavigateRequested += (_, targetPage) =>
            {
                var form = FindForm();
                if (form is Form1 f1) f1.SelectNavigation(targetPage);
            };

            _contentArea.Controls.Add(card);
        }

        // =========================================================
        // ROW 2 — KPI cards
        // =========================================================
        y += cardH + 15;

        var kpiRow2 = new (string Label, string Value, string Delta, bool Positive, string Icon, string TargetPage, Color Accent)[]
        {
            ("REVENUE · 30D", $"₱{GetDecimal(_kpis, "revenueLast30Days") / 1_000_000m:F1}M", "+5.3%", true, "₱",
                "Reports", Color.FromArgb(255, 168, 0)),
            ("REVENUE · 90D", $"₱{GetDecimal(_kpis, "revenueLast90Days") / 1_000_000m:F1}M", "+18.9%", true, "₱",
                "Reports", Color.FromArgb(80, 140, 200)),
            ("AVG PROJECT VALUE", $"₱{GetDecimal(_kpis, "avgProjectValue") / 1000:N0}K", "+2.1%", true, "◆",
                "Projects", Color.FromArgb(34, 140, 78)),
            ("AVG COMPLETION", $"{GetDouble(_kpis, "avgDaysToComplete"):F0} days", "-4 days", true, "▣",
                "Projects", Color.FromArgb(140, 80, 190)),
        };

        var trends2 = new[]
        {
            new double[] { 28, 30, 32, 34, 35, 36, 36.5 },
            new double[] { 30, 33, 37, 40, 42, 44, 44.9 },
            new double[] { 240, 245, 250, 255, 258, 259, 259 },
            new double[] { 62, 60, 59, 58, 57, 57, 57 },
        };

        for (int i = 0; i < kpiRow2.Length; i++)
        {
            var kpi = kpiRow2[i];
            var card = new CrmKpiCard
            {
                Label = kpi.Label,
                Value = kpi.Value,
                DeltaText = kpi.Delta,
                DeltaPositive = kpi.Positive,
                Icon = kpi.Icon,
                AccentColor = kpi.Accent,
                Location = new Point(i * (cardW + 15), y),
                Size = new Size(cardW, cardH),
                TrendValues = trends2[i].ToList(),
                TargetPage = kpi.TargetPage
            };

            card.NavigateRequested += (_, targetPage) =>
            {
                var form = FindForm();
                if (form is Form1 f1) f1.SelectNavigation(targetPage);
            };

            _contentArea.Controls.Add(card);
        }

        _contentArea.Tag = y + cardH + 15;
    }

    // =========================================================
    // SECTION 2 — CHARTS
    // =========================================================
    private void BuildCharts()
    {
        var oldCharts = _contentArea.Controls.OfType<CrmCard>().ToList();
        foreach (var c in oldCharts) c.Dispose();

        int availableWidth = Math.Max(1200, _contentArea.ClientSize.Width - 20);
        int y = (int)(_contentArea.Tag ?? 300);

        // --- ROW: Revenue bar chart + Retention donut ---
        int rowH = 340;
        int leftW = (int)(availableWidth * 0.62);
        int rightW = availableWidth - leftW - 15;

        // --- Revenue bar chart ---
        var revCard = new CrmCard
        {
            Title = "Revenue (₱) — Last 12 Months",
            Subtitle = "Paid invoices by month",
            Location = new Point(0, y),
            Size = new Size(leftW, rowH),
            ShowTopAccent = true
        };
        _contentArea.Controls.Add(revCard);

        var revChart = new CrmBarChart
        {
            Dock = DockStyle.Fill,
            ShowLegend = false
        };
        revCard.ContentArea.Controls.Add(revChart);

        var categories = _revenue
            .Select(r => GetStr(r, "label").Split(' ').FirstOrDefault() ?? "")
            .ToList();

        var revValues = _revenue
            .Select(r => (double)GetDecimal(r, "revenue"))
            .ToList();

        var revSeries = new CrmBarChart.Series
        {
            Label = "Revenue",
            Color = Color.FromArgb(255, 168, 0),
            Values = revValues
        };
        revChart.SetData(categories, new List<CrmBarChart.Series> { revSeries });

        // --- Retention Segments donut ---
        var retCard = new CrmCard
        {
            Title = "Retention Segments",
            Subtitle = "Customer distribution",
            Location = new Point(leftW + 15, y),
            Size = new Size(rightW, rowH),
            ShowTopAccent = true
        };
        _contentArea.Controls.Add(retCard);

        var donut = new CrmDonutChart
        {
            Dock = DockStyle.Fill,
            DonutThickness = 32,
            ShowLegend = true
        };
        retCard.ContentArea.Controls.Add(donut);

        var segments = _retention
            .GroupBy(r => GetStr(r, "segment"))
            .OrderBy(g => SegmentPriority(g.Key))
            .ToList();

        var sliceColors = new Dictionary<string, Color>
        {
            ["Champion"] = Color.FromArgb(34, 140, 78),
            ["Loyal"] = Color.FromArgb(80, 140, 200),
            ["Promising"] = Color.FromArgb(160, 130, 60),
            ["Detractor"] = Color.FromArgb(200, 55, 55),
            ["At Risk"] = Color.FromArgb(220, 120, 30),
            ["Dormant"] = Color.FromArgb(140, 140, 140),
            ["Lost"] = Color.FromArgb(110, 110, 110),
            ["Active"] = Color.FromArgb(255, 168, 0)
        };

        donut.SetData(
            segments.Select(g => new CrmDonutChart.Slice
            {
                Label = g.Key,
                Value = g.Count(),
                Color = sliceColors.TryGetValue(g.Key, out var c) ? c : Color.Gray
            }),
            _retention.Count.ToString("N0"),
            "Customers");

        y += rowH + 15;

        // --- Project Status Distribution ---
        int row2H = 320;

        var actCard = new CrmCard
        {
            Title = "Project Status Distribution",
            Subtitle = "Projects by stage",
            Location = new Point(0, y),
            Size = new Size(availableWidth, row2H),
            ShowTopAccent = true
        };
        _contentArea.Controls.Add(actCard);

        var actChart = new CrmBarChart
        {
            Dock = DockStyle.Fill,
            ShowLegend = true,
            ShowGridLines = true
        };
        actCard.ContentArea.Controls.Add(actChart);

        var stages = new[] { "Inquiry", "QuotationIssued", "DepositReceived", "DesignerAssigned", "InProgress", "Completed" };
        var stageLabels = new[] { "Inquiry", "Quoted", "Deposit", "Assigned", "In Prog.", "Done" };

        var stageCounts = new Dictionary<string, int>();
        foreach (var s in stages) stageCounts[s] = 0;

        foreach (var d in _designers)
        {
            var total = GetInt(d, "totalProjects");
            var completed = GetInt(d, "completedProjects");
            var active = GetInt(d, "activeProjects");

            stageCounts["Completed"] += completed;
            stageCounts["InProgress"] += active;
            stageCounts["DesignerAssigned"] += active;
        }

        var catList = stageLabels.ToList();

        var series1 = new CrmBarChart.Series
        {
            Label = "Projects",
            Color = Color.FromArgb(255, 168, 0),
            Values = stages.Select(s => (double)stageCounts[s]).ToList()
        };

        actChart.SetData(catList, new List<CrmBarChart.Series> { series1 });
    }

    // =========================================================
    // SECTION 3 — RETENTION + DESIGNERS
    // =========================================================
    private void BuildRetentionAndDesigners()
    {
        var existing = _contentArea.Controls.OfType<Panel>()
            .Where(p => p.Tag?.ToString() == "bottom-section")
            .ToList();
        foreach (var c in existing) c.Dispose();

        int availableWidth = Math.Max(1200, _contentArea.ClientSize.Width - 20);
        int y = (int)(_contentArea.Tag ?? 300) + 340 + 15 + 320 + 15;

        int rowH = 380;
        int leftW = (int)(availableWidth * 0.5);
        int rightW = availableWidth - leftW - 15;

        // --- Retention Actions ---
        var retActionsCard = new CrmCard
        {
            Title = "⚠  Retention Actions",
            Subtitle = "Priority-ranked customers",
            Location = new Point(0, y),
            Size = new Size(leftW, rowH),
            ShowTopAccent = true,
            Tag = "bottom-section"
        };
        _contentArea.Controls.Add(retActionsCard);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false
        };
        grid.Columns.Add("segment", "SEGMENT");
        grid.Columns.Add("name", "CUSTOMER");
        grid.Columns.Add("action", "ACTION");

        grid.Columns["segment"].FillWeight = 60;
        grid.Columns["name"].FillWeight = 80;
        grid.Columns["action"].FillWeight = 130;

        CrmTableStyler.Apply(grid, "segment");

        var topActions = _retention
            .Where(r => GetStr(r, "segment") != "Active")
            .OrderBy(r => GetInt(r, "priority"))
            .ThenByDescending(r => GetInt(r, "daysSinceLastProject"))
            .Take(8);

        foreach (var r in topActions)
        {
            grid.Rows.Add(
                GetStr(r, "segment"),
                GetStr(r, "fullName"),
                GetStr(r, "action"));
        }

        retActionsCard.ContentArea.Controls.Add(grid);

        // --- Designer Leaderboard ---
        var desCard = new CrmCard
        {
            Title = "👤  Designer Leaderboard",
            Subtitle = "Ranked by overall rating",
            Location = new Point(leftW + 15, y),
            Size = new Size(rightW, rowH),
            ShowTopAccent = true,
            Tag = "bottom-section"
        };
        _contentArea.Controls.Add(desCard);

        var desGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false
        };
        desGrid.Columns.Add("rank", "#");
        desGrid.Columns.Add("name", "DESIGNER");
        desGrid.Columns.Add("score", "SCORE");
        desGrid.Columns.Add("total", "TOTAL");

        desGrid.Columns["rank"].FillWeight = 20;
        desGrid.Columns["name"].FillWeight = 100;
        desGrid.Columns["score"].FillWeight = 50;
        desGrid.Columns["total"].FillWeight = 40;

        CrmTableStyler.Apply(desGrid);

        int rank = 1;
        foreach (var d in _designers)
        {
            var score = GetDouble(d, "overallScore");
            desGrid.Rows.Add(
                $"#{rank++}",
                GetStr(d, "designerName"),
                score > 0 ? $"{score:F2}★" : "—",
                GetInt(d, "totalProjects"));
        }

        desCard.ContentArea.Controls.Add(desGrid);
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private static int SegmentPriority(string segment) => segment switch
    {
        "Champion" => 1,
        "Loyal" => 2,
        "Detractor" => 3,
        "Promising" => 4,
        "At Risk" => 5,
        "Dormant" => 6,
        "Lost" => 7,
        "Active" => 99,
        _ => 50
    };

    private async Task<JsonElement> GetObjectAsync(string path)
    {
        var url = $"{_apiUrl}/tenant/{Session.CompanyId}/{path}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
        using var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException($"{path}: {(int)res.StatusCode}");
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private async Task<List<JsonElement>> GetArrayAsync(string path)
    {
        var url = $"{_apiUrl}/tenant/{Session.CompanyId}/{path}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
        using var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException($"{path}: {(int)res.StatusCode}");
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

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

    private static double GetDouble(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0;
        if (p.ValueKind == JsonValueKind.Number) return p.GetDouble();
        return 0;
    }

    private static decimal GetDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0m;
        if (p.ValueKind == JsonValueKind.Number) return p.GetDecimal();
        return 0m;
    }
}