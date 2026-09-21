using System.Drawing.Printing;
using System.Net.Http.Headers;
using System.Text.Json;
using ClosedXML.Excel;

namespace CRM_DesignServices.winforms;

public class ReportsPage : Panel
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;

    private TabControl _tabs = null!;
    private Label _lblStatus = null!;
    private Button _btnRefresh = null!;
    private Button _btnPrint = null!;
    private Button _btnExport = null!;

    // ---- Data caches ----
    private JsonElement _kpis;
    private List<JsonElement> _revenue = new();
    private List<JsonElement> _retention = new();
    private List<JsonElement> _designers = new();
    private List<JsonElement> _customers = new();
    private List<JsonElement> _quotations = new();

    // ---- Panels ----
    private Panel _pExecutive = null!;
    private Panel _pRevenue = null!;
    private Panel _pDesigners = null!;

    // ---- Print support ----
    private Bitmap? _printBuffer;
    private string _printTitle = "CRM Report";

    // ---- Pagination — Customer Revenue ----
    private const int PageSize = 17;
    private DataGridView _gridRevenue = null!;
    private List<JsonElement> _revenueFiltered = new();
    private int _revenueCurrentPage = 1;
    private Label _revPageInfo = null!;
    private Button _revPrev = null!;
    private Button _revNext = null!;

    // ---- Pagination — Designer Performance ----
    private DataGridView _gridDesigners = null!;
    private int _designerCurrentPage = 1;
    private Label _desPageInfo = null!;
    private Button _desPrev = null!;
    private Button _desNext = null!;

    public ReportsPage(string apiUrl, HttpClient http)
    {
        _apiUrl = apiUrl;
        _http = http;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Padding = new Padding(32, 20, 32, 32);

        // ---- Toolbar ----
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 60 };
        Controls.Add(toolbar);

        toolbar.Controls.Add(new Label
        {
            Text = "Reports",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(0, 0)
        });

        toolbar.Controls.Add(new Label
        {
            Text = "Printable reports and Excel exports",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(2, 32)
        });

        _btnRefresh = MakeButton("↻  Refresh", 110);
        _btnRefresh.Left = 320;
        _btnRefresh.Click += async (_, _) => await LoadAllAsync();
        toolbar.Controls.Add(_btnRefresh);

        _btnPrint = MakeButton("🖨  Print", 110);
        _btnPrint.Left = 440;
        _btnPrint.Click += (_, _) => PrintCurrentView();
        toolbar.Controls.Add(_btnPrint);

        _btnExport = MakeButton("📊  Export Excel", 160);
        _btnExport.Left = 560;
        _btnExport.Click += (_, _) => ExportCurrentViewToExcel();
        toolbar.Controls.Add(_btnExport);

        _lblStatus = new Label
        {
            Left = 740,
            Top = 20,
            Width = 400,
            Height = 22,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            Text = "Loading..."
        };
        toolbar.Controls.Add(_lblStatus);

        // ---- Tabs ----
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_tabs);
        _tabs.BringToFront();

        // Tab 1: Executive Summary
        var tabExec = new TabPage("  Executive Summary  ") { BackColor = Color.White };
        _tabs.TabPages.Add(tabExec);
        _pExecutive = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(24)
        };
        tabExec.Controls.Add(_pExecutive);

        // Tab 2: Customer Revenue
        var tabRev = new TabPage("  Customer Revenue  ") { BackColor = Color.White };
        _tabs.TabPages.Add(tabRev);
        _pRevenue = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(24) };
        tabRev.Controls.Add(_pRevenue);

        // Tab 3: Designer Performance
        var tabDes = new TabPage("  Designer Performance  ") { BackColor = Color.White };
        _tabs.TabPages.Add(tabDes);
        _pDesigners = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(24) };
        tabDes.Controls.Add(_pDesigners);
    }

    private static Button MakeButton(string text, int width) => new Button
    {
        Text = text,
        Top = 12,
        Width = width,
        Height = 36,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        ForeColor = Color.FromArgb(28, 32, 40),
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Cursor = Cursors.Hand
    };

    // =========================================================
    // LOAD ALL
    // =========================================================
    public async Task LoadAllAsync()
    {
        _lblStatus.Text = "Loading report data...";
        _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);

        try
        {
            var kpisTask = GetObjectAsync("bi/kpis");
            var revTask = GetArrayAsync("bi/revenue");
            var retTask = GetArrayAsync("bi/retention");
            var desTask = GetArrayAsync("bi/designers");
            var custTask = GetArrayAsync("customers");
            var quotTask = GetArrayAsync("quotations");

            await Task.WhenAll(kpisTask, revTask, retTask, desTask, custTask, quotTask);

            _kpis = await kpisTask;
            _revenue = await revTask;
            _retention = await retTask;
            _designers = await desTask;
            _customers = await custTask;
            _quotations = await quotTask;

            BuildExecutiveSummary();
            BuildCustomerRevenue();
            BuildDesignerPerformance();

            _lblStatus.Text = $"Updated {DateTime.Now:HH:mm:ss}  ·  {_customers.Count} customers  ·  {_quotations.Count} quotations";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
        }
    }

    // =========================================================
    // TAB 1 — EXECUTIVE SUMMARY (unchanged)
    // =========================================================
    private void BuildExecutiveSummary()
    {
        _pExecutive.Controls.Clear();

        int y = 0;

        AddHeader(_pExecutive, "Executive Summary", "Company performance at a glance", ref y);

        AddKpiRow(_pExecutive, y, new[]
        {
            ("LEAD CONV.", $"{GetDouble(_kpis, "leadConversionRate"):F1}%"),
            ("REPEAT RATE", $"{GetDouble(_kpis, "repeatRate"):F1}%"),
            ("CHURN RATE", $"{GetDouble(_kpis, "churnRate"):F1}%"),
            ("AVG RATING", $"{GetDouble(_kpis, "avgRating"):F2}★"),
        });
        y += 110;

        AddKpiRow(_pExecutive, y, new[]
        {
            ("REVENUE 30D", $"₱{GetDecimal(_kpis, "revenueLast30Days") / 1_000_000m:F1}M"),
            ("REVENUE 90D", $"₱{GetDecimal(_kpis, "revenueLast90Days") / 1_000_000m:F1}M"),
            ("AVG PROJECT", $"₱{GetDecimal(_kpis, "avgProjectValue") / 1000:N0}K"),
            ("AVG DAYS", $"{GetDouble(_kpis, "avgDaysToComplete"):F0}"),
        });
        y += 130;

        AddSectionTitle(_pExecutive, "Revenue — Last 12 Months", ref y);

        var chart = new Panel
        {
            Left = 0,
            Top = y,
            Width = _pExecutive.ClientSize.Width - 20,
            Height = 220,
            BackColor = Color.White,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        _pExecutive.Controls.Add(chart);
        chart.Paint += (s, e) => DrawRevenueChart(chart, e.Graphics);
        y += 240;

        var grid = new TableLayoutPanel
        {
            Left = 0,
            Top = y,
            Width = _pExecutive.ClientSize.Width - 20,
            Height = 320,
            ColumnCount = 2,
            RowCount = 1,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _pExecutive.Controls.Add(grid);

        var desCard = MakeSimpleCard("🏆  Top Designers", "By overall rating");
        desCard.Dock = DockStyle.Fill;
        desCard.Margin = new Padding(0, 0, 10, 0);
        grid.Controls.Add(desCard, 0, 0);

        var desList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = false,
            GridLines = false,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        desList.Columns.Add("DESIGNER", 180);
        desList.Columns.Add("RATING", 80);
        desList.Columns.Add("PROJECTS", 80);

        foreach (var d in _designers.Take(5))
        {
            var item = new ListViewItem(GetStr(d, "designerName"));
            item.SubItems.Add($"{GetDouble(d, "overallScore"):F2}★");
            item.SubItems.Add(GetInt(d, "totalProjects").ToString());
            desList.Items.Add(item);
        }
        desCard.Controls.Add(desList);
        desList.BringToFront();

        var custCard = MakeSimpleCard("💎  Top Customers", "By lifetime revenue");
        custCard.Dock = DockStyle.Fill;
        custCard.Margin = new Padding(10, 0, 0, 0);
        grid.Controls.Add(custCard, 1, 0);

        var custList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = false,
            GridLines = false,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        custList.Columns.Add("CUSTOMER", 180);
        custList.Columns.Add("REVENUE", 120);
        custList.Columns.Add("SEGMENT", 100);

        foreach (var r in _retention.OrderByDescending(x => GetDecimal(x, "totalRevenue")).Take(5))
        {
            var item = new ListViewItem(GetStr(r, "fullName"));
            item.SubItems.Add($"₱{GetDecimal(r, "totalRevenue"):N0}");
            item.SubItems.Add(GetStr(r, "segment"));
            custList.Items.Add(item);
        }
        custCard.Controls.Add(custList);
        custList.BringToFront();
        y += 340;

        AddSectionTitle(_pExecutive, "Retention Segments", ref y);

        var segGroups = _retention
            .GroupBy(r => GetStr(r, "segment"))
            .OrderBy(g => SegmentPriority(g.Key))
            .ToList();

        var segPanel = new Panel
        {
            Left = 0,
            Top = y,
            Width = _pExecutive.ClientSize.Width - 20,
            Height = 60 + segGroups.Count * 34,
            BackColor = Color.FromArgb(250, 251, 253),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        _pExecutive.Controls.Add(segPanel);

        int yy = 12;
        foreach (var g in segGroups)
        {
            var dot = new Panel { Left = 16, Top = yy + 8, Width = 10, Height = 10, BackColor = SegmentColor(g.Key) };
            segPanel.Controls.Add(dot);

            segPanel.Controls.Add(new Label
            {
                Text = g.Key,
                Left = 36,
                Top = yy,
                Width = 180,
                Height = 24,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 32, 40)
            });

            segPanel.Controls.Add(new Label
            {
                Text = $"{g.Count()} customers",
                Left = 220,
                Top = yy,
                Width = 200,
                Height = 24,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(110, 118, 132)
            });

            yy += 34;
        }
    }

    // =========================================================
    // TAB 2 — CUSTOMER REVENUE  (WITH PAGINATION)
    // =========================================================
    private void BuildCustomerRevenue()
    {
        _pRevenue.Controls.Clear();

        _pRevenue.Controls.Add(new Label
        {
            Text = "Customer Revenue Report",
            Left = 0,
            Top = 0,
            Width = 600,
            Height = 32,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40)
        });

        _pRevenue.Controls.Add(new Label
        {
            Text = $"All customers sorted by lifetime revenue · {PageSize} rows per page",
            Left = 2,
            Top = 34,
            Width = 600,
            Height = 22,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132)
        });

        // Prepare sorted data
        _revenueFiltered = _retention
            .OrderByDescending(r => GetDecimal(r, "totalRevenue"))
            .ToList();
        _revenueCurrentPage = 1;

        // Grid
        _gridRevenue = MakeReportGrid();
        _gridRevenue.Dock = DockStyle.Fill;
        _gridRevenue.Columns.Add("customerId", "#");
        _gridRevenue.Columns.Add("fullName", "CUSTOMER");
        _gridRevenue.Columns.Add("email", "EMAIL");
        _gridRevenue.Columns.Add("phone", "PHONE");
        _gridRevenue.Columns.Add("projectCount", "PROJECTS");
        _gridRevenue.Columns.Add("avgRating", "AVG ★");
        _gridRevenue.Columns.Add("totalRevenue", "LIFETIME REVENUE");
        _gridRevenue.Columns.Add("segment", "SEGMENT");
        _gridRevenue.Columns.Add("daysSinceLastProject", "LAST PROJECT");

        // Wrap grid + pagination bar
        var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 60, 0, 0) };
        _pRevenue.Controls.Add(wrapper);

        // Pagination bar (bottom)
        var pager = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.FromArgb(250, 251, 253)
        };
        wrapper.Controls.Add(pager);

        _revPrev = new Button
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
        _revPrev.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        _revPrev.Click += (_, _) =>
        {
            if (_revenueCurrentPage > 1)
            {
                _revenueCurrentPage--;
                RenderRevenuePage();
            }
        };
        pager.Controls.Add(_revPrev);

        _revPageInfo = new Label
        {
            Left = 120,
            Top = 18,
            Width = 420,
            Height = 24,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40)
        };
        pager.Controls.Add(_revPageInfo);

        _revNext = new Button
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
        _revNext.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        _revNext.Click += (_, _) =>
        {
            if (_revenueCurrentPage < TotalRevenuePages)
            {
                _revenueCurrentPage++;
                RenderRevenuePage();
            }
        };
        pager.Controls.Add(_revNext);

        // Grid fills the rest
        _gridRevenue.Dock = DockStyle.Fill;
        wrapper.Controls.Add(_gridRevenue);
        _gridRevenue.BringToFront();

        // Initial render
        RenderRevenuePage();
    }

    private int TotalRevenuePages =>
        _revenueFiltered.Count == 0 ? 1
        : (int)Math.Ceiling(_revenueFiltered.Count / (double)PageSize);

    private void RenderRevenuePage()
    {
        _gridRevenue.Rows.Clear();

        int start = (_revenueCurrentPage - 1) * PageSize;
        int end = Math.Min(start + PageSize, _revenueFiltered.Count);

        for (int i = start; i < end; i++)
        {
            var r = _revenueFiltered[i];
            var rating = GetDouble(r, "avgRating");
            var ratingText = rating > 0 ? $"{rating:F1}★" : "—";
            var days = GetInt(r, "daysSinceLastProject");
            var daysText = days > 0 ? $"{days} days ago" : "—";

            _gridRevenue.Rows.Add(
                i + 1,
                GetStr(r, "fullName"),
                GetStr(r, "email"),
                GetStr(r, "phone"),
                GetInt(r, "projectCount"),
                ratingText,
                $"₱{GetDecimal(r, "totalRevenue"):N0}",
                GetStr(r, "segment"),
                daysText);
        }

        _revPageInfo.Text = $"Page {_revenueCurrentPage} of {TotalRevenuePages}   ·   " +
                           $"Showing {start + 1}–{end} of {_revenueFiltered.Count}";

        _revPrev.Enabled = _revenueCurrentPage > 1;
        _revNext.Enabled = _revenueCurrentPage < TotalRevenuePages;
        _revPrev.ForeColor = _revPrev.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);
        _revNext.ForeColor = _revNext.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);
    }

    // =========================================================
    // TAB 3 — DESIGNER PERFORMANCE  (WITH PAGINATION)
    // =========================================================
    private void BuildDesignerPerformance()
    {
        _pDesigners.Controls.Clear();

        _pDesigners.Controls.Add(new Label
        {
            Text = "Designer Performance Report",
            Left = 0,
            Top = 0,
            Width = 600,
            Height = 32,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40)
        });

        _pDesigners.Controls.Add(new Label
        {
            Text = $"Ranked by overall rating · {PageSize} rows per page",
            Left = 2,
            Top = 34,
            Width = 600,
            Height = 22,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132)
        });

        _designerCurrentPage = 1;

        _gridDesigners = MakeReportGrid();
        _gridDesigners.Dock = DockStyle.Fill;
        _gridDesigners.Columns.Add("rank", "#");
        _gridDesigners.Columns.Add("name", "DESIGNER");
        _gridDesigners.Columns.Add("score", "SCORE");
        _gridDesigners.Columns.Add("total", "TOTAL");
        _gridDesigners.Columns.Add("completed", "COMPLETED");
        _gridDesigners.Columns.Add("active", "ACTIVE");
        _gridDesigners.Columns.Add("avgDays", "AVG DAYS");
        _gridDesigners.Columns.Add("rating", "RATINGS");
        _gridDesigners.Columns.Add("recommend", "RECOMMEND");
        _gridDesigners.Columns.Add("issues", "ISSUES");

        var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 60, 0, 0) };
        _pDesigners.Controls.Add(wrapper);

        // Pagination bar
        var pager = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.FromArgb(250, 251, 253)
        };
        wrapper.Controls.Add(pager);

        _desPrev = new Button
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
        _desPrev.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        _desPrev.Click += (_, _) =>
        {
            if (_designerCurrentPage > 1)
            {
                _designerCurrentPage--;
                RenderDesignerPage();
            }
        };
        pager.Controls.Add(_desPrev);

        _desPageInfo = new Label
        {
            Left = 120,
            Top = 18,
            Width = 420,
            Height = 24,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40)
        };
        pager.Controls.Add(_desPageInfo);

        _desNext = new Button
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
        _desNext.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        _desNext.Click += (_, _) =>
        {
            if (_designerCurrentPage < TotalDesignerPages)
            {
                _designerCurrentPage++;
                RenderDesignerPage();
            }
        };
        pager.Controls.Add(_desNext);

        _gridDesigners.Dock = DockStyle.Fill;
        wrapper.Controls.Add(_gridDesigners);
        _gridDesigners.BringToFront();

        RenderDesignerPage();
    }

    private int TotalDesignerPages =>
        _designers.Count == 0 ? 1
        : (int)Math.Ceiling(_designers.Count / (double)PageSize);

    private void RenderDesignerPage()
    {
        _gridDesigners.Rows.Clear();

        int start = (_designerCurrentPage - 1) * PageSize;
        int end = Math.Min(start + PageSize, _designers.Count);

        for (int i = start; i < end; i++)
        {
            var d = _designers[i];
            var score = GetDouble(d, "overallScore");
            var rec = GetDouble(d, "recommendRate");

            _gridDesigners.Rows.Add(
                i + 1,
                GetStr(d, "designerName"),
                score > 0 ? $"{score:F2}★" : "—",
                GetInt(d, "totalProjects"),
                GetInt(d, "completedProjects"),
                GetInt(d, "activeProjects"),
                $"{GetDouble(d, "avgCompletionDays"):F1}",
                GetInt(d, "feedbackCount"),
                rec > 0 ? $"{rec:F0}%" : "—",
                $"{GetInt(d, "openIssues")}/{GetInt(d, "totalIssues")}");
        }

        _desPageInfo.Text = $"Page {_designerCurrentPage} of {TotalDesignerPages}   ·   " +
                           $"Showing {start + 1}–{end} of {_designers.Count}";

        _desPrev.Enabled = _designerCurrentPage > 1;
        _desNext.Enabled = _designerCurrentPage < TotalDesignerPages;
        _desPrev.ForeColor = _desPrev.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);
        _desNext.ForeColor = _desNext.Enabled ? Color.FromArgb(28, 32, 40) : Color.FromArgb(180, 186, 196);
    }

    // =========================================================
    // HELPERS — UI
    // =========================================================
    private void AddHeader(Panel p, string title, string subtitle, ref int y)
    {
        p.Controls.Add(new Label
        {
            Text = title,
            Left = 0,
            Top = y,
            Width = 600,
            Height = 32,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40)
        });
        y += 32;

        p.Controls.Add(new Label
        {
            Text = subtitle,
            Left = 2,
            Top = y,
            Width = 600,
            Height = 22,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132)
        });
        y += 34;
    }

    private void AddSectionTitle(Panel p, string text, ref int y)
    {
        p.Controls.Add(new Label
        {
            Text = text,
            Left = 0,
            Top = y,
            Width = 600,
            Height = 26,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40)
        });
        y += 32;
    }

    private void AddKpiRow(Panel p, int top, (string Label, string Value)[] items)
    {
        int cardW = (_pExecutive.ClientSize.Width - 60) / 4;
        for (int i = 0; i < items.Length; i++)
        {
            var card = new Panel
            {
                Left = i * (cardW + 10),
                Top = top,
                Width = cardW,
                Height = 100,
                BackColor = Color.White,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
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
                Text = items[i].Label,
                Left = 16,
                Top = 16,
                Width = cardW - 32,
                Height = 18,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(110, 118, 132)
            });
            card.Controls.Add(new Label
            {
                Text = items[i].Value,
                Left = 16,
                Top = 40,
                Width = cardW - 32,
                Height = 50,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 32, 40)
            });

            _pExecutive.Controls.Add(card);
        }
    }

    private Panel MakeSimpleCard(string title, string subtitle)
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
            Left = 16,
            Top = 14,
            Width = 300,
            Height = 24,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40)
        });
        card.Controls.Add(new Label
        {
            Text = subtitle,
            Left = 16,
            Top = 36,
            Width = 300,
            Height = 18,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(110, 118, 132)
        });
        return card;
    }

    private void DrawRevenueChart(Panel chart, Graphics g)
    {
        if (_revenue.Count == 0) return;

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int left = 60, right = 20, top = 20, bottom = 50;
        int w = chart.Width - left - right;
        int h = chart.Height - top - bottom;

        var values = _revenue.Select(r => (double)GetDecimal(r, "revenue")).ToList();
        var labels = _revenue.Select(r => GetStr(r, "label")).ToList();
        double max = Math.Max(1, values.Max());

        using var axisFont = new Font("Segoe UI", 8f);
        using var gridPen = new Pen(Color.FromArgb(238, 240, 244));

        for (int i = 0; i <= 4; i++)
        {
            int yy = top + (int)(h - (h * i / 4.0));
            g.DrawLine(gridPen, left, yy, left + w, yy);
            var val = max * i / 4.0;
            var txt = val >= 1_000_000 ? $"₱{val / 1_000_000:F1}M"
                    : val >= 1_000 ? $"₱{val / 1_000:F0}K"
                    : $"₱{val:F0}";
            g.DrawString(txt, axisFont, Brushes.Gray, 4, yy - 7);
        }

        int slot = Math.Max(20, w / Math.Max(1, values.Count));
        int barW = Math.Min(45, slot - 12);

        using var brush = new SolidBrush(Color.FromArgb(255, 168, 0));
        using var brushSoft = new SolidBrush(Color.FromArgb(255, 232, 180));

        for (int i = 0; i < values.Count; i++)
        {
            int x = left + i * slot + (slot - barW) / 2;
            int bh = (int)(values[i] / max * (h - 6));
            int yy = top + h - bh;

            g.FillRectangle(i == values.Count - 1 ? brush : brushSoft, x, yy, barW, bh);

            var label = labels[i].Split(' ').FirstOrDefault() ?? "";
            g.DrawString(label, axisFont, Brushes.Gray, x - 4, top + h + 8);
        }
    }

    private DataGridView MakeReportGrid() => new DataGridView
    {
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 42,
        RowTemplate = { Height = 40 },
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(249, 250, 252),
            ForeColor = Color.FromArgb(85, 93, 106),
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0)
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(28, 32, 40),
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(10, 0, 10, 0),
            SelectionBackColor = Color.FromArgb(255, 245, 225),
            SelectionForeColor = Color.FromArgb(28, 32, 40)
        },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 251, 253)
        }
    };

    // =========================================================
    // PRINT (unchanged)
    // =========================================================
    private void PrintCurrentView()
    {
        var currentTab = _tabs.SelectedTab;
        if (currentTab is null) return;

        var panel = currentTab.Controls.Count > 0 ? currentTab.Controls[0] : null;
        if (panel is null) return;

        try
        {
            _printTitle = currentTab.Text.Trim();
            var bmp = new Bitmap(Math.Max(panel.Width, 1000), Math.Max(panel.Height, 700));
            panel.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
            _printBuffer = bmp;

            using var pd = new PrintDocument();
            pd.DocumentName = $"Fuerto CRM — {_printTitle}";
            pd.PrintPage += PrintPageHandler;

            using var dlg = new PrintDialog { Document = pd, UseEXDialog = true };
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            {
                pd.Print();
                _lblStatus.Text = "Sent to printer.";
                _lblStatus.ForeColor = Color.FromArgb(34, 140, 78);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Print failed: " + ex.Message, "Print");
        }
    }

    private void PrintPageHandler(object sender, PrintPageEventArgs e)
    {
        if (_printBuffer is null || e.Graphics is null) return;
        var g = e.Graphics;
        var bounds = e.MarginBounds;

        using var headerFont = new Font("Segoe UI", 16f, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 9f);
        g.DrawString("FUERTO Interior Design Services", headerFont, Brushes.Black, bounds.Left, bounds.Top);
        g.DrawString($"Report: {_printTitle}  ·  Generated: {DateTime.Now:MMM dd, yyyy HH:mm}", subFont, Brushes.Gray, bounds.Left, bounds.Top + 32);

        var contentTop = bounds.Top + 60;
        var contentHeight = bounds.Height - 80;

        var ratio = Math.Min(
            (double)bounds.Width / _printBuffer.Width,
            (double)contentHeight / _printBuffer.Height);
        var w = (int)(_printBuffer.Width * ratio);
        var h = (int)(_printBuffer.Height * ratio);
        var x = bounds.Left + (bounds.Width - w) / 2;

        g.DrawImage(_printBuffer, x, contentTop, w, h);

        using var footerFont = new Font("Segoe UI", 8f);
        g.DrawString($"Fuerto CRM  ·  Page 1  ·  Confidential", footerFont, Brushes.Gray, bounds.Left, bounds.Bottom + 10);

        e.HasMorePages = false;
    }

    // =========================================================
    // EXPORT EXCEL (unchanged — exports ALL rows, not just current page)
    // =========================================================
    private void ExportCurrentViewToExcel()
    {
        var tabName = _tabs.SelectedTab?.Text.Trim() ?? "Report";

        using var sfd = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"FuertoCRM_{tabName.Replace(" ", "")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            using var wb = new XLWorkbook();

            if (tabName.Contains("Executive")) ExportExecutiveSheet(wb);
            else if (tabName.Contains("Customer")) ExportCustomerRevenueSheet(wb);
            else if (tabName.Contains("Designer")) ExportDesignerSheet(wb);

            wb.SaveAs(sfd.FileName);

            _lblStatus.Text = $"Exported to {System.IO.Path.GetFileName(sfd.FileName)}";
            _lblStatus.ForeColor = Color.FromArgb(34, 140, 78);

            if (MessageBox.Show($"Report exported.\n\nOpen file now?", "Export Successful",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = sfd.FileName,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Excel export failed: " + ex.Message, "Export Error");
        }
    }

    private void ExportExecutiveSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Executive Summary");
        ws.Cell("A1").Value = "FUERTO Interior Design Services";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 18;
        ws.Cell("A2").Value = "Executive Summary";
        ws.Cell("A2").Style.Font.FontSize = 14;
        ws.Cell("A3").Value = $"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}";
        ws.Cell("A3").Style.Font.Italic = true;
        ws.Cell("A3").Style.Font.FontColor = XLColor.Gray;

        int row = 5;
        ws.Cell(row, 1).Value = "KPI";
        ws.Cell(row, 2).Value = "Value";
        ws.Range(row, 1, row, 2).Style.Font.Bold = true;
        ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE8B4");
        row++;

        var kpis = new (string Label, string Value)[]
        {
            ("Lead Conversion Rate", $"{GetDouble(_kpis, "leadConversionRate"):F2}%"),
            ("Repeat Rate", $"{GetDouble(_kpis, "repeatRate"):F2}%"),
            ("Churn Rate", $"{GetDouble(_kpis, "churnRate"):F2}%"),
            ("Avg Rating", $"{GetDouble(_kpis, "avgRating"):F2}"),
            ("Recommend Rate", $"{GetDouble(_kpis, "recommendRate"):F2}%"),
            ("Avg Project Value", $"₱{GetDecimal(_kpis, "avgProjectValue"):N2}"),
            ("Avg Days to Accept", $"{GetDouble(_kpis, "avgDaysToAccept"):F1}"),
            ("Avg Days to Complete", $"{GetDouble(_kpis, "avgDaysToComplete"):F1}"),
            ("Revenue Last 30 Days", $"₱{GetDecimal(_kpis, "revenueLast30Days"):N2}"),
            ("Revenue Last 90 Days", $"₱{GetDecimal(_kpis, "revenueLast90Days"):N2}"),
            ("Revenue Last 365 Days", $"₱{GetDecimal(_kpis, "revenueLast365Days"):N2}"),
            ("Total Customers", GetInt(_kpis, "totalCustomers").ToString()),
            ("Total Projects", GetInt(_kpis, "totalProjects").ToString()),
            ("Open Issues", GetInt(_kpis, "openIssues").ToString()),
        };
        foreach (var (label, value) in kpis)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 2).Value = value;
            row++;
        }

        row += 2;
        ws.Cell(row, 1).Value = "Top Designers";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        row++;
        ws.Cell(row, 1).Value = "Designer";
        ws.Cell(row, 2).Value = "Rating";
        ws.Cell(row, 3).Value = "Projects";
        ws.Range(row, 1, row, 3).Style.Font.Bold = true;
        row++;
        foreach (var d in _designers.Take(5))
        {
            ws.Cell(row, 1).Value = GetStr(d, "designerName");
            ws.Cell(row, 2).Value = GetDouble(d, "overallScore");
            ws.Cell(row, 3).Value = GetInt(d, "totalProjects");
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void ExportCustomerRevenueSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Customer Revenue");
        ws.Cell("A1").Value = "Customer Revenue Report";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Cell("A2").Value = $"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}";
        ws.Cell("A2").Style.Font.Italic = true;

        int row = 4;
        var headers = new[] { "#", "Customer", "Email", "Phone", "Projects", "Avg Rating", "Lifetime Revenue", "Segment", "Last Project (days)" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
        ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE8B4");
        row++;

        int idx = 1;
        decimal total = 0;
        foreach (var r in _revenueFiltered)
        {
            var rev = GetDecimal(r, "totalRevenue");
            total += rev;
            ws.Cell(row, 1).Value = idx++;
            ws.Cell(row, 2).Value = GetStr(r, "fullName");
            ws.Cell(row, 3).Value = GetStr(r, "email");
            ws.Cell(row, 4).Value = GetStr(r, "phone");
            ws.Cell(row, 5).Value = GetInt(r, "projectCount");
            ws.Cell(row, 6).Value = GetDouble(r, "avgRating");
            ws.Cell(row, 7).Value = rev;
            ws.Cell(row, 8).Value = GetStr(r, "segment");
            ws.Cell(row, 9).Value = GetInt(r, "daysSinceLastProject");
            row++;
        }

        row++;
        ws.Cell(row, 6).Value = "TOTAL";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = total;
        ws.Cell(row, 7).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private void ExportDesignerSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Designer Performance");
        ws.Cell("A1").Value = "Designer Performance Report";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Cell("A2").Value = $"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}";
        ws.Cell("A2").Style.Font.Italic = true;

        int row = 4;
        var headers = new[] { "Rank", "Designer", "Score", "Total Projects", "Completed", "Active", "Avg Days", "Ratings", "Recommend %", "Issues" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
        ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE8B4");
        row++;

        int rank = 1;
        foreach (var d in _designers)
        {
            ws.Cell(row, 1).Value = rank++;
            ws.Cell(row, 2).Value = GetStr(d, "designerName");
            ws.Cell(row, 3).Value = GetDouble(d, "overallScore");
            ws.Cell(row, 4).Value = GetInt(d, "totalProjects");
            ws.Cell(row, 5).Value = GetInt(d, "completedProjects");
            ws.Cell(row, 6).Value = GetInt(d, "activeProjects");
            ws.Cell(row, 7).Value = GetDouble(d, "avgCompletionDays");
            ws.Cell(row, 8).Value = GetInt(d, "feedbackCount");
            ws.Cell(row, 9).Value = GetDouble(d, "recommendRate");
            ws.Cell(row, 10).Value = $"{GetInt(d, "openIssues")}/{GetInt(d, "totalIssues")}";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    // =========================================================
    // HELPERS — Data
    // =========================================================
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

    private static int SegmentPriority(string segment) => segment switch
    {
        "Champion" => 1,
        "Loyal" => 2,
        "Detractor" => 3,
        "Promising" => 4,
        "At Risk" => 5,
        "Dormant" => 6,
        "Lost" => 7,
        _ => 99
    };

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
}