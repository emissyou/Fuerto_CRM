using System.Net.Http.Headers;
using System.Text.Json;
using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class BiDashboardPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private Panel _kpiPanel = null!;
    private Panel _chartPanel = null!;
    private Panel _retentionPanel = null!;
    private Panel _designerPanel = null!;
    private Label _lblStatus = null!;
    private Button _btnRefresh = null!;

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

        // ---- Header ----
        var header = new Panel { Dock = DockStyle.Top, Height = 56 };
        Controls.Add(header);

        _lblStatus = new Label
        {
            Text = "Loading dashboard...",
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            Location = new Point(0, 16)
        };
        header.Controls.Add(_lblStatus);

        _btnRefresh = new Button
        {
            Text = "↻  Refresh",
            Left = 0,
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
        _btnRefresh.Left = 200;

        // ---- KPI row ----
        _kpiPanel = new Panel { Dock = DockStyle.Top, Height = 220 };
        Controls.Add(_kpiPanel);

        // ---- Charts row ----
        _chartPanel = new Panel { Dock = DockStyle.Top, Height = 340 };
        Controls.Add(_chartPanel);

        // ---- Retention row ----
        _retentionPanel = new Panel { Dock = DockStyle.Top, Height = 340 };
        Controls.Add(_retentionPanel);

        // ---- Designer row ----
        _designerPanel = new Panel { Dock = DockStyle.Top, Height = 240 };
        Controls.Add(_designerPanel);
    }

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
            BuildRetention();
            BuildDesigners();

            _lblStatus.Text = $"Last refreshed: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
        }
    }

    // =========================================================
    // KPI CARDS
    // =========================================================
    private void BuildKpis()
    {
        _kpiPanel.Controls.Clear();

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(0, 8, 0, 8)
        };
        for (int i = 0; i < 4; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        _kpiPanel.Controls.Add(grid);

        grid.Controls.Add(MakeKpiCard("LEAD CONVERSION", $"{GetDouble(_kpis, "leadConversionRate"):F1}%", "Leads → Customers", "●"), 0, 0);
        grid.Controls.Add(MakeKpiCard("REPEAT RATE", $"{GetDouble(_kpis, "repeatRate"):F1}%", "Returning customers", "♻"), 1, 0);
        grid.Controls.Add(MakeKpiCard("CHURN RATE", $"{GetDouble(_kpis, "churnRate"):F1}%", "No purchase in 12 mo", "⚠"), 2, 0);
        grid.Controls.Add(MakeKpiCard("AVG RATING", $"{GetDouble(_kpis, "avgRating"):F2}★", $"Rec. rate {GetDouble(_kpis, "recommendRate"):F0}%", "★"), 3, 0);

        grid.Controls.Add(MakeKpiCard("REVENUE · 30D", $"₱{GetDecimal(_kpis, "revenueLast30Days") / 1_000_000m:F1}M", "Last 30 days", "₱"), 0, 1);
        grid.Controls.Add(MakeKpiCard("REVENUE · 90D", $"₱{GetDecimal(_kpis, "revenueLast90Days") / 1_000_000m:F1}M", "Last 90 days", "₱"), 1, 1);
        grid.Controls.Add(MakeKpiCard("AVG PROJECT VALUE", $"₱{GetDecimal(_kpis, "avgProjectValue") / 1000:N0}K", "Per accepted quote", "◆"), 2, 1);
        grid.Controls.Add(MakeKpiCard("AVG COMPLETION", $"{GetDouble(_kpis, "avgDaysToComplete"):F0} days", $"Accept: {GetDouble(_kpis, "avgDaysToAccept"):F1} days", "▣"), 3, 1);
    }

    private Panel MakeKpiCard(string title, string value, string subtitle, string icon)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(6)
        };

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 235, 240), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            using var accent = new SolidBrush(Color.FromArgb(255, 168, 0));
            e.Graphics.FillRectangle(accent, 0, 0, card.Width, 3);
        };

        card.Controls.Add(new Label
        {
            Text = icon,
            ForeColor = Color.FromArgb(255, 168, 0),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(14, 10)
        });
        card.Controls.Add(new Label
        {
            Text = title,
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(38, 14)
        });
        card.Controls.Add(new Label
        {
            Text = value,
            ForeColor = Color.FromArgb(28, 32, 40),
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(14, 36)
        });
        card.Controls.Add(new Label
        {
            Text = subtitle,
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 8f),
            AutoSize = true,
            Location = new Point(16, 74)
        });

        return card;
    }

    // =========================================================
    // CHARTS — Revenue + Segment Distribution
    // =========================================================
    private void BuildCharts()
    {
        _chartPanel.Controls.Clear();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 8, 0, 8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
        _chartPanel.Controls.Add(layout);

        // ---- Revenue bar chart ----
        var revenueCard = MakeCard("Revenue Trend", "Last 12 months (paid)");
        revenueCard.Dock = DockStyle.Fill;
        revenueCard.Margin = new Padding(6);
        layout.Controls.Add(revenueCard, 0, 0);

        var chartArea = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 60, 12, 12) };
        revenueCard.Controls.Add(chartArea);
        chartArea.BringToFront();

        chartArea.Paint += (s, e) =>
        {
            if (_revenue.Count == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int left = 60, right = 20, top = 20, bottom = 50;
            int w = chartArea.Width - left - right;
            int h = chartArea.Height - top - bottom;

            var values = _revenue.Select(r => (double)GetDecimal(r, "revenue")).ToList();
            var labels = _revenue.Select(r => GetStr(r, "label")).ToList();
            double max = Math.Max(1, values.Max());

            // Y-axis labels
            using var axisFont = new Font("Segoe UI", 7.5f);
            using var gridPen = new Pen(Color.FromArgb(238, 240, 244));
            for (int i = 0; i <= 4; i++)
            {
                int y = top + (int)(h - (h * i / 4.0));
                double val = max * i / 4.0;
                g.DrawLine(gridPen, left, y, left + w, y);
                var txt = val >= 1_000_000 ? $"₱{val / 1_000_000:F1}M"
                        : val >= 1_000 ? $"₱{val / 1_000:F0}K"
                        : $"₱{val:F0}";
                g.DrawString(txt, axisFont, Brushes.Gray, 4, y - 7);
            }

            // Bars
            int slot = Math.Max(20, w / Math.Max(1, values.Count));
            int barW = Math.Min(40, slot - 12);

            using var brush = new SolidBrush(Color.FromArgb(255, 168, 0));
            using var brushSoft = new SolidBrush(Color.FromArgb(255, 232, 180));

            for (int i = 0; i < values.Count; i++)
            {
                int x = left + i * slot + (slot - barW) / 2;
                int bh = (int)(values[i] / max * (h - 6));
                int y = top + h - bh;

                g.FillRectangle(i == values.Count - 1 ? brush : brushSoft, x, y, barW, bh);

                // Month label
                var label = labels[i].Split(' ').FirstOrDefault() ?? "";
                g.DrawString(label, axisFont, Brushes.Gray, x - 4, top + h + 8);
            }
        };

        // ---- Retention segment distribution ----
        var segCard = MakeCard("Retention Segments", "Customer distribution");
        segCard.Dock = DockStyle.Fill;
        segCard.Margin = new Padding(6);
        layout.Controls.Add(segCard, 1, 0);

        var segList = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 60, 12, 12), AutoScroll = true };
        segCard.Controls.Add(segList);
        segList.BringToFront();

        var groups = _retention
            .GroupBy(r => GetStr(r, "segment"))
            .OrderBy(g => SegmentPriority(g.Key))
            .ToList();

        int y = 0;
        foreach (var g in groups)
        {
            var row = new Panel
            {
                Left = 0,
                Top = y,
                Width = segList.Width - 20,
                Height = 34,
                BackColor = Color.White,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            segList.Controls.Add(row);

            var dot = new Panel
            {
                Left = 0,
                Top = 13,
                Width = 10,
                Height = 10,
                BackColor = SegmentColor(g.Key)
            };
            row.Controls.Add(dot);

            row.Controls.Add(new Label
            {
                Text = g.Key,
                Left = 20,
                Top = 8,
                Width = 150,
                Height = 20,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 32, 40)
            });

            row.Controls.Add(new Label
            {
                Text = g.Count().ToString(),
                Left = row.Width - 60,
                Top = 6,
                Width = 50,
                Height = 24,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 32, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            });

            y += 38;
        }
    }

    // =========================================================
    // RETENTION ACTIONS
    // =========================================================
    private void BuildRetention()
    {
        _retentionPanel.Controls.Clear();

        var card = MakeCard("⚠  Retention Actions", "Priority-ranked — action recommended per segment");
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(6);
        _retentionPanel.Controls.Add(card);

        var viewAll = new Button
        {
            Text = "View All →",
            Top = 18,
            Height = 26,
            Width = 100,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 95, 0),
            BackColor = Color.FromArgb(255, 245, 225),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        viewAll.Left = card.Width - 124;
        viewAll.FlatAppearance.BorderSize = 0;
        viewAll.Click += (_, _) =>
        {
            // Navigate to Retention page
            var form = FindForm();
            if (form is Form1 f1) f1.NavigateTo("Retention");
        };
        card.Controls.Add(viewAll);
        card.Resize += (_, _) => viewAll.Left = card.Width - 124;

        var grid = new DataGridView
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
            ColumnHeadersHeight = 34
        };
        grid.RowTemplate.Height = 40;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(249, 250, 252),
            ForeColor = Color.FromArgb(85, 93, 106),
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(28, 32, 40),
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(10, 0, 10, 0),
            SelectionBackColor = Color.FromArgb(255, 245, 225),
            SelectionForeColor = Color.FromArgb(28, 32, 40)
        };

        grid.Columns.Add("segment", "SEGMENT");
        grid.Columns.Add("name", "CUSTOMER");
        grid.Columns.Add("basis", "BASIS");
        grid.Columns.Add("action", "RECOMMENDED ACTION");

        // Top 8 by priority (excluding Active)
        var top = _retention
            .Where(r => GetStr(r, "segment") != "Active")
            .OrderBy(r => GetInt(r, "priority"))
            .ThenByDescending(r => GetInt(r, "daysSinceLastProject"))
            .Take(8);

        foreach (var r in top)
        {
            grid.Rows.Add(
                GetStr(r, "segment"),
                GetStr(r, "fullName"),
                GetStr(r, "basis"),
                GetStr(r, "action"));
        }

        // Color-code segment cells
        grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex == 0 && e.Value is string seg)
            {
                e.CellStyle.ForeColor = SegmentColor(seg);
                e.CellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            }
        };

        // Double-click opens action dialog
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];
            var name = row.Cells["name"].Value?.ToString() ?? "";
            var seg = row.Cells["segment"].Value?.ToString() ?? "";
            var basis = row.Cells["basis"].Value?.ToString() ?? "";
            var action = row.Cells["action"].Value?.ToString() ?? "";

            // Find full row in _retention
            var match = _retention.FirstOrDefault(r => GetStr(r, "fullName") == name);
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

        card.Controls.Add(grid);
        grid.BringToFront();
    }

    // =========================================================
    // DESIGNER LEADERBOARD
    // =========================================================
    private void BuildDesigners()
    {
        _designerPanel.Controls.Clear();

        var card = MakeCard("👤  Designer Leaderboard", "Ranked by overall rating & volume");
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(6);
        _designerPanel.Controls.Add(card);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 34
        };
        grid.RowTemplate.Height = 42;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(249, 250, 252),
            ForeColor = Color.FromArgb(85, 93, 106),
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(28, 32, 40),
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(10, 0, 10, 0),
            SelectionBackColor = Color.FromArgb(255, 245, 225),
            SelectionForeColor = Color.FromArgb(28, 32, 40)
        };

        grid.Columns.Add("rank", "RANK");
        grid.Columns.Add("name", "DESIGNER");
        grid.Columns.Add("score", "SCORE");
        grid.Columns.Add("total", "TOTAL");
        grid.Columns.Add("completed", "COMPLETED");
        grid.Columns.Add("avgDays", "AVG DAYS");
        grid.Columns.Add("rating", "RATINGS");
        grid.Columns.Add("recommend", "RECOMMEND");
        grid.Columns.Add("issues", "ISSUES");

        int rank = 1;
        foreach (var d in _designers)
        {
            var score = GetDouble(d, "overallScore");
            var scoreText = score > 0 ? $"{score:F2}★" : "—";
            var rec = GetDouble(d, "recommendRate");
            var recText = rec > 0 ? $"{rec:F0}%" : "—";

            grid.Rows.Add(
                $"#{rank++}",
                GetStr(d, "designerName"),
                scoreText,
                GetInt(d, "totalProjects"),
                GetInt(d, "completedProjects"),
                $"{GetDouble(d, "avgCompletionDays"):F1}",
                GetInt(d, "feedbackCount"),
                recText,
                $"{GetInt(d, "openIssues")}/{GetInt(d, "totalIssues")}");
        }

        card.Controls.Add(grid);
        grid.BringToFront();
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private Panel MakeCard(string title, string subtitle)
    {
        var card = new Panel { BackColor = Color.White, Padding = new Padding(16) };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 235, 240), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        card.Controls.Add(new Label
        {
            Text = title,
            ForeColor = Color.FromArgb(28, 32, 40),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 14)
        });
        card.Controls.Add(new Label
        {
            Text = subtitle,
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Location = new Point(16, 38)
        });
        return card;
    }

    private static Color SegmentColor(string segment) => segment switch
    {
        "Champion" => Color.FromArgb(34, 140, 78),
        "Loyal" => Color.FromArgb(80, 140, 200),
        "Promising" => Color.FromArgb(160, 130, 60),
        "Detractor" => Color.FromArgb(200, 55, 55),
        "At Risk" => Color.FromArgb(220, 120, 30),
        "Dormant" => Color.FromArgb(140, 140, 140),
        "Lost" => Color.FromArgb(110, 110, 110),
        _ => Color.FromArgb(110, 118, 132)
    };

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