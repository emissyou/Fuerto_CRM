using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public partial class Form1 : Form
{
    // =========================================================
    // API
    // =========================================================
    private const string ApiUrl = "http://localhost:5068";
    private readonly HttpClient _httpClient = new();

    // =========================================================
    // COLORS
    // =========================================================
    private readonly Color ColorAccent = Color.FromArgb(255, 168, 0);
    private readonly Color ColorAccentSoft = Color.FromArgb(255, 245, 225);
    private readonly Color ColorSidebar = Color.White;
    private readonly Color ColorSidebarHover = Color.FromArgb(255, 249, 240);
    private readonly Color ColorBackground = Color.FromArgb(245, 247, 250);
    private readonly Color ColorWhite = Color.White;
    private readonly Color ColorText = Color.FromArgb(28, 32, 40);
    private readonly Color ColorMuted = Color.FromArgb(110, 118, 132);
    private readonly Color ColorBorder = Color.FromArgb(226, 230, 236);
    private readonly Color ColorCardBorder = Color.FromArgb(232, 235, 240);
    private readonly Color ColorSuccess = Color.FromArgb(34, 140, 78);
    private readonly Color ColorDanger = Color.FromArgb(200, 55, 55);
    private readonly Color ColorHeaderBg = Color.FromArgb(249, 250, 252);

    // =========================================================
    // MAIN CONTROLS
    // =========================================================
    private Panel sidebar = null!;
    private Panel mainPanel = null!;
    private Panel contentPanel = null!;
    private Panel topGoldBar = null!;
    private Label lblPageTitle = null!;
    private Label lblPageSubtitle = null!;
    private Label lblUserName = null!;
    private Label lblUserRole = null!;
    private Label lblApiStatus = null!;
    private readonly Dictionary<string, Button> navigationButtons = new();

    public Form1()
    {
        BuildInterface();
        Shown += async (_, _) => await LoadDashboardAsync();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _httpClient.Dispose();
        base.OnFormClosed(e);
    }

    // =========================================================
    // BUILD INTERFACE
    // =========================================================
    private void BuildInterface()
    {
        Text = "Fuerto CRM";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1280, 780);
        BackColor = ColorBackground;
        Font = new Font("Segoe UI", 9.5f);
        Controls.Clear();

        BuildSidebar();
        BuildMainArea();
        SelectNavigation("Overview");
    }

    // =========================================================
    // SIDEBAR
    // =========================================================
    private void BuildSidebar()
    {
        sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 248,
            BackColor = ColorWhite
        };

        var border = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = ColorBorder };
        sidebar.Controls.Add(border);
        Controls.Add(sidebar);

        var brandPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 90,
            BackColor = ColorWhite
        };
        sidebar.Controls.Add(brandPanel);

        var logoImage = new PictureBox
        {
            Size = new Size(40, 40),
            Location = new Point(18, 20),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        TryLoadLogo(logoImage);
        brandPanel.Controls.Add(logoImage);

        var logo = new Label
        {
            Text = "FUERTO",
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(68, 18)
        };
        brandPanel.Controls.Add(logo);

        var companyText = new Label
        {
            Text = "Interior Design Services",
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 8f),
            AutoSize = true,
            Location = new Point(70, 48)
        };
        brandPanel.Controls.Add(companyText);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            Padding = new Padding(16, 8, 16, 10)
        };
        sidebar.Controls.Add(footer);

        lblApiStatus = new Label
        {
            Text = "●  Connecting...",
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 8.5f),
            Dock = DockStyle.Top,
            Height = 24
        };
        footer.Controls.Add(lblApiStatus);

        var footerText = new Label
        {
            Text = "FUERTO  ·  COMPANY CRM",
            ForeColor = Color.FromArgb(155, 162, 172),
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            Dock = DockStyle.Bottom,
            Height = 20
        };
        footer.Controls.Add(footerText);

        var menuPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 12, 12, 0)
        };
        sidebar.Controls.Add(menuPanel);
        menuPanel.BringToFront();

        // =========================================================
        // ROLE-BASED NAVIGATION  (grouped into modules)
        // =========================================================
        var navItems = new (string Name, string Icon, string Group)[]
        {
            ("Overview",   "home",      "MAIN"),

            ("Customers",  "contact",   "SALES & CRM"),
            ("Leads",      "funnel",    "SALES & CRM"),
            ("Quotations", "document",  "SALES & CRM"),

            ("Projects",   "briefcase", "OPERATIONS"),
            ("Activities", "clock",     "OPERATIONS"),
            ("Issues",     "alert",     "OPERATIONS"),
            ("Feedback",   "star",      "OPERATIONS"),

            ("Designers",  "pencil",    "TEAM"),
            ("Users",      "people",    "TEAM"),

            ("Analytics",  "chart",     "INSIGHTS"),
            ("Retention",  "retention", "INSIGHTS"),
            ("Promotions", "gift",      "INSIGHTS"),
            ("Reports",    "report",    "INSIGHTS")
        };

        int y = 4;
        string? currentGroup = null;

        foreach (var item in navItems)
        {
            if (!CanSee(item.Name)) continue;

            if (item.Group != currentGroup)
            {
                y += AddGroupHeader(menuPanel, item.Group, y, currentGroup == null);
                currentGroup = item.Group;
            }

            AddNavigationButton(menuPanel, item.Name, item.Icon, y);
            y += 46;
        }
    }

    // =========================================================
    // GROUP HEADER (module label)
    // =========================================================
    private int AddGroupHeader(Panel parent, string title, int top, bool isFirst)
    {
        int topSpacing = isFirst ? 4 : 18;

        var header = new Label
        {
            Text = title,
            Left = 10,
            Top = top + topSpacing,
            Width = 200,
            Height = 18,
            ForeColor = Color.FromArgb(160, 166, 176),
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        };
        parent.Controls.Add(header);

        return topSpacing + header.Height + 4;
    }

    private void AddNavigationButton(Panel parent, string text, string iconKey, int top)
    {
        var button = new Button
        {
            Text = text,
            Left = 4,
            Top = top,
            Width = 216,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorWhite,
            ForeColor = Color.FromArgb(70, 78, 92),
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(44, 0, 0, 0),
            Cursor = Cursors.Hand,
            Tag = text
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ColorSidebarHover;

        button.Paint += (s, e) =>
        {
            var btn = (Button)s;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (btn.BackColor == ColorAccentSoft)
            {
                using var brush = new SolidBrush(ColorAccent);
                e.Graphics.FillRectangle(brush, 0, 6, 4, btn.Height - 12);
            }

            var iconRect = new Rectangle(14, (btn.Height - 18) / 2, 18, 18);
            DrawNavIcon(e.Graphics, iconKey, iconRect, btn.ForeColor);
        };

        button.Click += (_, _) => SelectNavigation(text);
        parent.Controls.Add(button);
        navigationButtons[text] = button;
    }

    // =========================================================
    // PROFESSIONAL LINE ICONS (hand-drawn vector, no emoji/fonts)
    // =========================================================
    private static void DrawNavIcon(Graphics g, string key, Rectangle r, Color color)
    {
        using var pen = new Pen(color, 1.6f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(color);
        int x = r.X, y = r.Y, w = r.Width, h = r.Height;

        switch (key)
        {
            case "home":
                g.DrawLines(pen, new[]
                {
                    new Point(x, y + h / 2), new Point(x + w / 2, y + 1),
                    new Point(x + w, y + h / 2)
                });
                g.DrawRectangle(pen, x + 3, y + h / 2 - 1, w - 6, h / 2 - 1);
                g.DrawLine(pen, x + w / 2, y + h - 2, x + w / 2, y + h / 2 + 3);
                break;

            case "contact":
                g.DrawEllipse(pen, x + w / 2 - 4, y + 1, 8, 8);
                g.DrawArc(pen, x + 1, y + h - 10, w - 2, 11, 180, 180);
                break;

            case "people":
                g.DrawEllipse(pen, x + 1, y + 2, 7, 7);
                g.DrawArc(pen, x - 1, y + h - 9, 9, 10, 180, 180);
                g.DrawEllipse(pen, x + w - 8, y + 2, 7, 7);
                g.DrawArc(pen, x + w - 10, y + h - 9, 9, 10, 180, 180);
                break;

            case "funnel":
                g.DrawLines(pen, new[]
                {
                    new Point(x, y + 1), new Point(x + w, y + 1),
                    new Point(x + w / 2 + 3, y + h / 2 + 1),
                    new Point(x + w / 2 + 3, y + h - 2),
                    new Point(x + w / 2 - 3, y + h - 5),
                    new Point(x + w / 2 - 3, y + h / 2 + 1),
                    new Point(x, y + 1)
                });
                break;

            case "document":
                g.DrawRectangle(pen, x + 2, y, w - 6, h);
                g.DrawLine(pen, x + 5, y + 5, x + w - 7, y + 5);
                g.DrawLine(pen, x + 5, y + 9, x + w - 7, y + 9);
                g.DrawLine(pen, x + 5, y + 13, x + w - 10, y + 13);
                break;

            case "briefcase":
                g.DrawRectangle(pen, x, y + 5, w, h - 7);
                g.DrawArc(pen, x + w / 2 - 4, y, 8, 8, 180, 180);
                g.DrawLine(pen, x, y + 9, x + w, y + 9);
                break;

            case "clock":
                g.DrawEllipse(pen, x, y, w - 1, h - 1);
                g.DrawLine(pen, x + w / 2, y + h / 2, x + w / 2, y + 4);
                g.DrawLine(pen, x + w / 2, y + h / 2, x + w - 5, y + h / 2 + 2);
                break;

            case "pencil":
                g.DrawLine(pen, x + 2, y + h - 2, x + w - 3, y + 1);
                g.DrawLine(pen, x + 2, y + h - 2, x + 5, y + h - 5);
                g.DrawLine(pen, x + 5, y + h - 5, x + w - 3, y + 1);
                g.FillPolygon(brush, new[]
                {
                    new Point(x + 1, y + h - 1), new Point(x + 4, y + h - 4), new Point(x + 2, y + h - 2)
                });
                break;

            case "alert":
                g.DrawPolygon(pen, new[]
                {
                    new Point(x + w / 2, y), new Point(x + w, y + h - 2), new Point(x, y + h - 2)
                });
                g.DrawLine(pen, x + w / 2, y + 5, x + w / 2, y + h - 7);
                g.FillEllipse(brush, x + w / 2 - 1, y + h - 5, 2, 2);
                break;

            case "star":
                DrawStar(g, pen, x + w / 2, y + h / 2, w / 2 - 1);
                break;

            case "chart":
                g.DrawLine(pen, x, y + h - 1, x + w, y + h - 1);
                g.DrawLine(pen, x + 2, y + h - 2, x + 2, y + h - 7);
                g.DrawLine(pen, x + w / 2 - 1, y + h - 2, x + w / 2 - 1, y + 4);
                g.DrawLine(pen, x + w - 3, y + h - 2, x + w - 3, y + h - 12);
                break;

            case "retention":
                g.DrawArc(pen, x + 1, y + 1, w - 2, h - 2, -40, 260);
                g.DrawLines(pen, new[]
                {
                    new Point(x + w - 1, y + 1), new Point(x + w - 1, y + 6), new Point(x + w - 6, y + 4)
                });
                break;

            case "gift":
                g.DrawRectangle(pen, x, y + 6, w, h - 6);
                g.DrawLine(pen, x, y + 10, x + w, y + 10);
                g.DrawLine(pen, x + w / 2, y + 6, x + w / 2, y + h);
                g.DrawArc(pen, x + 1, y, w / 2 - 1, 8, 180, 180);
                g.DrawArc(pen, x + w / 2, y, w / 2 - 1, 8, 180, 180);
                break;

            case "report":
                g.DrawRectangle(pen, x + 2, y, w - 6, h);
                g.DrawLine(pen, x + 5, y + 5, x + w - 7, y + 5);
                g.DrawLine(pen, x + 5, y + 9, x + w - 7, y + 9);
                g.DrawLines(pen, new[] { new Point(x + 5, y + 13), new Point(x + 7, y + 15), new Point(x + w - 7, y + 11) });
                break;

            default:
                g.DrawEllipse(pen, x, y, w - 1, h - 1);
                break;
        }
    }

    private static void DrawStar(Graphics g, Pen pen, int cx, int cy, int radius)
    {
        var pts = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            double angle = Math.PI / 2 * 3 + i * Math.PI / 5;
            double rad = (i % 2 == 0) ? radius : radius * 0.42;
            pts[i] = new PointF((float)(cx + rad * Math.Cos(angle)), (float)(cy + rad * Math.Sin(angle)));
        }
        g.DrawPolygon(pen, pts);
    }

    // =========================================================
    // MAIN AREA
    // =========================================================
    private void BuildMainArea()
    {
        mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = ColorBackground };
        Controls.Add(mainPanel);
        mainPanel.BringToFront();

        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = ColorWhite
        };
        mainPanel.Controls.Add(topBar);

        topGoldBar = new Panel { Dock = DockStyle.Bottom, Height = 4, BackColor = ColorAccent };
        topBar.Controls.Add(topGoldBar);

        lblPageTitle = new Label
        {
            Text = "Dashboard",
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(36, 12)
        };
        topBar.Controls.Add(lblPageTitle);

        lblPageSubtitle = new Label
        {
            Text = "Overview of your company operations",
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Location = new Point(38, 44)
        };
        topBar.Controls.Add(lblPageSubtitle);

        var profilePanel = new Panel { Dock = DockStyle.Right, Width = 280 };
        topBar.Controls.Add(profilePanel);

        var avatar = new Label
        {
            Text = GetInitials(Session.Email ?? "FA"),
            Width = 42,
            Height = 42,
            Location = new Point(0, 16),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 95, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };
        avatar.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(ColorAccentSoft);
            e.Graphics.FillEllipse(brush, 0, 0, 41, 41);
            TextRenderer.DrawText(e.Graphics, avatar.Text, avatar.Font,
                new Rectangle(0, 0, 42, 42), avatar.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        profilePanel.Controls.Add(avatar);

        lblUserName = new Label
        {
            Text = Session.Email ?? "Fuerto Admin",
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Width = 210,
            Height = 20,
            Location = new Point(54, 16)
        };
        profilePanel.Controls.Add(lblUserName);

        lblUserRole = new Label
        {
            Text = GetUserRole(),
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 8.5f),
            Width = 210,
            Height = 20,
            Location = new Point(54, 36)
        };
        profilePanel.Controls.Add(lblUserRole);

        contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(32, 24, 32, 32),
            BackColor = ColorBackground
        };
        mainPanel.Controls.Add(contentPanel);
        contentPanel.BringToFront();
    }

    private string GetUserRole()
    {
        if (Session.Roles == null || Session.Roles.Count == 0) return "CRM User";
        return string.Join("  ·  ", Session.Roles);
    }

    // =========================================================
    // NAVIGATION
    // =========================================================
    public void SelectNavigation(string page)
    {
        if (!CanSee(page))
        {
            MessageBox.Show(
                $"You don't have permission to view \"{page}\".",
                "Access Denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        foreach (var item in navigationButtons)
        {
            bool selected = item.Key.Equals(page, StringComparison.OrdinalIgnoreCase);

            item.Value.BackColor = selected ? ColorAccentSoft : ColorWhite;
            item.Value.ForeColor = selected ? Color.FromArgb(140, 85, 0) : Color.FromArgb(70, 78, 92);
            item.Value.Font = new Font("Segoe UI", 9.5f, selected ? FontStyle.Bold : FontStyle.Regular);
            item.Value.Invalidate();
        }

        lblPageTitle.Text = page;
        lblPageSubtitle.Text = page switch
        {
            "Overview" => "Live snapshot of your company records",
            "Customers" => "Manage and view your client relationships",
            "Leads" => "Track potential customers and opportunities",
            "Projects" => "Monitor your interior design projects",
            "Quotations" => "Manage proposals and quotation records",
            "Activities" => "Track client interactions and follow-ups",
            "Designers" => "Manage staff members and view their ratings",
            "Users" => "Manage managers, staff members",
            "Issues" => "Complaints, adjustments, and rework requests",
            "Feedback" => "Customer feedback and ratings",
            "Analytics" => "KPIs, trends, and retention intelligence",
            "Retention" => "Customer segments & recommended actions",
            "Promotions" => "Create and manage promotional offers",
            "Reports" => "Review company performance and records",
            _ => "Fuerto Interior Design Services CRM"
        };

        if (page == "Overview")
            _ = LoadDashboardAsync();
        else if (page == "Customers")
            _ = LoadEntityPageAsync("Customers", "customers",
                new[] { "CustomerId", "FirstName", "LastName", "CustomerType", "Email", "Phone", "IsActive" });
        else if (page == "Leads")
            _ = LoadEntityPageAsync("Leads", "leads",
                new[] { "LeadId", "FirstName", "LastName", "Status", "LeadSource", "Email" });
        else if (page == "Projects")
            _ = LoadEntityPageAsync("Projects", "projects",
                new[] { "ProjectId", "ProjectCode", "ProjectName", "DesignStage", "Status", "ProgressPercentage" });
        else if (page == "Quotations")
            _ = LoadEntityPageAsync("Quotations", "quotations",
                new[] { "QuotationId", "QuotationNumber", "Status", "TotalAmount", "PaymentStatus" });
        else if (page == "Activities")
            _ = LoadEntityPageAsync("Activities", "activities",
                new[] { "ActivityId", "ActivityType", "Subject", "Status", "ActivityDate" });
        else if (page == "Designers")
            BuildDesignersPage();
        else if (page == "Users")
            BuildUsersPage();
        else if (page == "Issues")
            _ = LoadEntityPageAsync("Issues", "issues",
                new[] { "ProjectIssueId", "Title", "IssueType", "Severity", "Status", "ReportedAt" });
        else if (page == "Feedback")
            _ = LoadEntityPageAsync("Feedback", "feedback",
                new[] { "ProjectFeedbackId", "ProjectId", "OverallRating", "TimelinessRating", "CommunicationRating", "ValueRating", "SubmittedAt" });
        else if (page == "Analytics")
            BuildBiDashboard();
        else if (page == "Retention")
            BuildRetentionPage();
        else if (page == "Promotions")
            BuildPromotionsPage();
        else if (page == "Reports")
            BuildReportsPage();
    }

    // =========================================================
    // DASHBOARD
    // =========================================================
    private async Task LoadDashboardAsync()
    {
        ShowLoading();

        try
        {
            if (string.IsNullOrWhiteSpace(Session.Token))
                throw new InvalidOperationException(
                    "No authentication token is available. Please log in again.");

            if (!Session.CompanyId.HasValue)
                throw new InvalidOperationException(
                    "No company is assigned to the current login session.");

            var customersTask = GetArrayAsync("customers");
            var leadsTask = GetArrayAsync("leads");
            var projectsTask = GetArrayAsync("projects");
            var quotationsTask = GetArrayAsync("quotations");
            var activitiesTask = GetArrayAsync("activities");

            await Task.WhenAll(
                customersTask, leadsTask, projectsTask, quotationsTask, activitiesTask);

            SetApiConnected();

            BuildDashboard(
                (await customersTask).Count,
                (await leadsTask).Count,
                (await projectsTask).Count,
                (await quotationsTask).Count,
                await activitiesTask);
        }
        catch (Exception ex)
        {
            SetApiDisconnected();
            BuildErrorPage("Unable to load dashboard data.", ex.Message);
        }
    }

    // =========================================================
    // DASHBOARD UI
    // =========================================================
    private void BuildDashboard(int customerCount, int leadCount, int projectCount, int quotationCount, List<JsonElement> activities)
    {
        contentPanel.Controls.Clear();
        contentPanel.AutoScroll = true;
        contentPanel.Padding = new Padding(32, 20, 32, 32);
        contentPanel.BackColor = Color.FromArgb(245, 247, 250);

        int availableWidth = Math.Max(1200, contentPanel.ClientSize.Width - 64);

        // ---- Greeting ----
        contentPanel.Controls.Add(new Label
        {
            Text = "Good day, Admin 👋",
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 0)
        });

        contentPanel.Controls.Add(new Label
        {
            Text = "Here's your business at a glance.",
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Location = new Point(2, 38)
        });

        // ---- KPI cards ----
        int y = 82;
        int cardW = (availableWidth - 60) / 4;
        int cardH = 130;

        var kpiCards = new (string Label, string Value, string Delta, bool Positive, string Icon, string TargetPage, Color Accent)[]
        {
            ("CUSTOMERS",  customerCount.ToString("N0"),  "+12.4%", true, "♙", "Customers",  Color.FromArgb(255, 168, 0)),
            ("LEADS",      leadCount.ToString("N0"),      "+8.2%",  true, "◆", "Leads",      Color.FromArgb(80, 140, 200)),
            ("PROJECTS",   projectCount.ToString("N0"),   "+15.1%", true, "▣", "Projects",   Color.FromArgb(34, 140, 78)),
            ("QUOTATIONS", quotationCount.ToString("N0"), "+3.5%",  true, "▤", "Quotations", Color.FromArgb(140, 80, 190)),
        };

        var trendTemplates = new[]
        {
            new double[] { 180, 195, 205, 210, 225, 240, 250 },
            new double[] { 280, 300, 310, 320, 340, 345, 350 },
            new double[] { 250, 280, 300, 320, 340, 355, 366 },
            new double[] { 300, 310, 320, 340, 350, 360, 366 },
        };

        for (int i = 0; i < kpiCards.Length; i++)
        {
            var kpi = kpiCards[i];
            var card = new CrmKpiCard
            {
                Label = kpi.Label,
                Value = kpi.Value,
                DeltaText = kpi.Delta,
                DeltaPositive = kpi.Positive,
                Icon = kpi.Icon,
                AccentColor = kpi.Accent,
                SubLabel = "Last 30 days",
                Location = new Point(i * (cardW + 15), y),
                Size = new Size(cardW, cardH),
                TrendValues = trendTemplates[i].ToList(),
                TargetPage = kpi.TargetPage
            };

            card.NavigateRequested += (_, targetPage) => SelectNavigation(targetPage);

            contentPanel.Controls.Add(card);
        }
        y += cardH + 20;

        // ---- Top Customers + Project Stats ----
        int rowH = 320;
        int leftW = (int)(availableWidth * 0.35);
        int rightW = availableWidth - leftW - 15;

        var topCustCard = new CrmCard
        {
            Title = "💎  Top Customers",
            Subtitle = "By lifetime revenue",
            Location = new Point(0, y),
            Size = new Size(leftW, rowH),
            ShowTopAccent = true
        };
        contentPanel.Controls.Add(topCustCard);

        var allCustomerRows = new[]
        {
            ("Juan Dela Cruz", "VIP", "₱619,310"),
            ("Roberto Santos", "Regular", "₱383,832"),
            ("Diego Villanueva", "Regular", "₱310,496"),
            ("Miguel Salazar", "VIP", "₱863,762"),
            ("Rosa Bautista", "Regular", "₱1,030,585"),
        };

        int custY = 12;
        foreach (var (name, type, revenue) in allCustomerRows)
        {
            var rowPanel = new Panel
            {
                Left = 12,
                Top = custY,
                Width = leftW - 24,
                Height = 52,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            topCustCard.ContentArea.Controls.Add(rowPanel);

            var avatar = new CrmAvatar { Location = new Point(0, 6), Size = new Size(40, 40) };
            avatar.SetFromName(name);
            rowPanel.Controls.Add(avatar);

            rowPanel.Controls.Add(new Label
            {
                Text = name,
                Left = 52,
                Top = 6,
                Width = rowPanel.Width - 130,
                Height = 20,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorText,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            });

            rowPanel.Controls.Add(new Label
            {
                Text = type,
                Left = 52,
                Top = 26,
                Width = 120,
                Height = 18,
                Font = new Font("Segoe UI", 8f),
                ForeColor = ColorMuted,
                BackColor = Color.Transparent
            });

            rowPanel.Controls.Add(new Label
            {
                Text = revenue,
                Left = rowPanel.Width - 110,
                Top = 14,
                Width = 100,
                Height = 22,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 95, 0),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            });

            custY += 56;
        }

        var projStatsCard = new CrmCard
        {
            Title = "📊  Project Statistics",
            Subtitle = "Active · In Progress · Completed",
            Location = new Point(leftW + 15, y),
            Size = new Size(rightW, rowH),
            ShowTopAccent = true
        };
        contentPanel.Controls.Add(projStatsCard);

        var barChart = new CrmBarChart { Dock = DockStyle.Fill };
        projStatsCard.ContentArea.Controls.Add(barChart);

        var categories = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul" };
        var s1 = new CrmBarChart.Series { Label = "Active", Color = Color.FromArgb(80, 140, 200), Values = new List<double> { 45, 52, 48, 60, 55, 62, 58 } };
        var s2 = new CrmBarChart.Series { Label = "In Progress", Color = Color.FromArgb(255, 168, 0), Values = new List<double> { 30, 38, 42, 45, 50, 48, 55 } };
        var s3 = new CrmBarChart.Series { Label = "Completed", Color = Color.FromArgb(34, 140, 78), Values = new List<double> { 25, 32, 28, 40, 45, 52, 60 } };
        barChart.SetData(categories, new List<CrmBarChart.Series> { s1, s2, s3 });

        y += rowH + 20;

        // ---- Top Designers + Activity Mix ----
        int row2H = 300;
        int leftW2 = (int)(availableWidth * 0.55);
        int rightW2 = availableWidth - leftW2 - 15;

        var designerCard = new CrmCard
        {
            Title = "🏆  Top Designers",
            Subtitle = "By overall rating & volume",
            Location = new Point(0, y),
            Size = new Size(leftW2, row2H),
            ShowTopAccent = true
        };
        contentPanel.Controls.Add(designerCard);

        var designerData = new[]
        {
            ("Marco Reyes", "Senior Designer", 4.51, 94, Color.FromArgb(34, 140, 78)),
            ("Sofia Lim", "Senior Designer", 4.25, 95, Color.FromArgb(80, 140, 200)),
            ("Rafael Tan", "Designer", 3.81, 47, Color.FromArgb(255, 168, 0)),
            ("Elena Cruz", "Designer", 3.85, 41, Color.FromArgb(140, 80, 190)),
            ("Diego Santos", "Designer", 4.03, 36, Color.FromArgb(220, 120, 30)),
        };

        int desY = 14;
        foreach (var (name, role, rating, projects, color) in designerData)
        {
            var rowPanel = new Panel
            {
                Left = 16,
                Top = desY,
                Width = leftW2 - 32,
                Height = 48,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            designerCard.ContentArea.Controls.Add(rowPanel);

            var avatar = new CrmAvatar { Location = new Point(0, 4), Size = new Size(38, 38) };
            avatar.SetFromName(name);
            rowPanel.Controls.Add(avatar);

            rowPanel.Controls.Add(new Label
            {
                Text = name,
                Left = 50,
                Top = 4,
                Width = 200,
                Height = 18,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorText,
                BackColor = Color.Transparent
            });

            rowPanel.Controls.Add(new Label
            {
                Text = $"{role}  ·  {projects} projects",
                Left = 50,
                Top = 24,
                Width = 220,
                Height = 16,
                Font = new Font("Segoe UI", 8f),
                ForeColor = ColorMuted,
                BackColor = Color.Transparent
            });

            rowPanel.Controls.Add(new Label
            {
                Text = $"{rating:F2}★",
                Left = rowPanel.Width - 70,
                Top = 12,
                Width = 60,
                Height = 22,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = color,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            });

            desY += 52;
        }

        var mixCard = new CrmCard
        {
            Title = "◐  Activity Mix",
            Subtitle = "By status",
            Location = new Point(leftW2 + 15, y),
            Size = new Size(rightW2, row2H),
            ShowTopAccent = true
        };
        contentPanel.Controls.Add(mixCard);

        var donut = new CrmDonutChart { Dock = DockStyle.Fill, DonutThickness = 32 };
        mixCard.ContentArea.Controls.Add(donut);

        var completed = activities.Count(a => GetString(a, "Status", "status") == "Completed");
        var resolved = activities.Count(a => GetString(a, "Status", "status") == "Resolved");
        var closed = activities.Count(a => GetString(a, "Status", "status") == "Closed");
        var other = activities.Count - completed - resolved - closed;

        donut.SetData(new[]
        {
            new CrmDonutChart.Slice { Label = "Completed", Value = completed, Color = Color.FromArgb(34, 140, 78) },
            new CrmDonutChart.Slice { Label = "Resolved",  Value = resolved,  Color = Color.FromArgb(80, 140, 200) },
            new CrmDonutChart.Slice { Label = "Closed",    Value = closed,    Color = Color.FromArgb(255, 168, 0) },
            new CrmDonutChart.Slice { Label = "Other",     Value = Math.Max(0, other), Color = Color.FromArgb(180, 186, 196) }
        }, activities.Count.ToString("N0"), "Activities");

        y += row2H + 20;

        // ---- Recent Activities ----
        int activityCardH = 380;
        var activityCard = new CrmCard
        {
            Title = "◷  Recent Activities",
            Subtitle = "Latest client interactions",
            Location = new Point(0, y),
            Size = new Size(availableWidth, activityCardH),
            ShowTopAccent = true
        };
        contentPanel.Controls.Add(activityCard);

        var activityGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            Margin = new Padding(12)
        };
        activityGrid.Columns.Add("type", "TYPE");
        activityGrid.Columns.Add("subject", "SUBJECT");
        activityGrid.Columns.Add("date", "DATE");
        activityGrid.Columns.Add("status", "STATUS");
        activityGrid.Columns["type"].FillWeight = 60;
        activityGrid.Columns["subject"].FillWeight = 180;
        activityGrid.Columns["date"].FillWeight = 60;
        activityGrid.Columns["status"].FillWeight = 60;

        CrmTableStyler.Apply(activityGrid, "status", "type");

        foreach (var activity in activities.Take(15))
        {
            activityGrid.Rows.Add(
                GetString(activity, "ActivityType", "Type") ?? "—",
                GetString(activity, "Subject", "Description") ?? "—",
                GetDate(activity, "ActivityDate", "CreatedAt"),
                GetString(activity, "Status") ?? "—");
        }
        activityCard.ContentArea.Controls.Add(activityGrid);
    }

    // =========================================================
    // ENTITY PAGES
    // =========================================================
    private async Task LoadEntityPageAsync(string title, string endpoint, string[] preferredColumns)
    {
        ShowLoading();
        try
        {
            var records = await GetArrayAsync(endpoint);
            SetApiConnected();
            BuildEntityPage(title, endpoint, records, preferredColumns);
        }
        catch (Exception ex)
        {
            SetApiDisconnected();
            BuildErrorPage($"Unable to load {title.ToLowerInvariant()}.", ex.Message);
        }
    }

    private void BuildEntityPage(string title, string endpoint, List<JsonElement> records, string[] preferredColumns)
    {
        contentPanel.Controls.Clear();
        contentPanel.Padding = new Padding(32, 20, 32, 32);

        // ---- Header toolbar ----
        var header = new Panel { Dock = DockStyle.Top, Height = 56 };
        contentPanel.Controls.Add(header);

        var lblRecordCount = new Label
        {
            Text = $"{records.Count:N0} record{(records.Count == 1 ? "" : "s")}",
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            Location = new Point(0, 16)
        };
        header.Controls.Add(lblRecordCount);

        var refreshButton = CreateSecondaryButton("↻  Refresh", 110, 36);
        refreshButton.Location = new Point(140, 8);
        refreshButton.Click += (_, _) => _ = LoadEntityPageAsync(title, endpoint, preferredColumns);
        header.Controls.Add(refreshButton);

        var newButton = CreateSecondaryButton($"＋  New {title.TrimEnd('s')}", 160, 36);
        newButton.Location = new Point(8, 8);
        header.Controls.Add(newButton);

        var editButton = CreateSecondaryButton("✎  Edit", 90, 36);
        editButton.Location = new Point(272, 8);
        header.Controls.Add(editButton);

        var deleteButton = CreateSecondaryButton("🗑️  Delete", 110, 36);
        deleteButton.Location = new Point(370, 8);
        header.Controls.Add(deleteButton);

        var searchBox = new TextBox
        {
            PlaceholderText = $"Search {title.ToLowerInvariant()}...",
            Width = 280,
            Height = 34,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        searchBox.Left = Math.Max(900, contentPanel.ClientSize.Width - 340);
        searchBox.Top = 10;
        header.Controls.Add(searchBox);

        contentPanel.Resize += (_, _) =>
        {
            if (!searchBox.IsDisposed)
                searchBox.Left = Math.Max(900, contentPanel.ClientSize.Width - 340);
        };

        var card = CreateCard("", "");
        card.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(card);
        card.BringToFront();

        var currentRecords = records.ToList();

        if (records.Count == 0)
        {
            card.Controls.Add(new Label
            {
                Text = $"No {title.ToLowerInvariant()} records found.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ColorMuted,
                Font = new Font("Segoe UI", 11f)
            });

            AddWorkflowButtons(header, endpoint, null!, () => currentRecords);
            return;
        }

        // ---- Grid ----
        var grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        var columns = DetermineColumns(records, preferredColumns);

        foreach (var col in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = col,
                HeaderText = FriendlyHeader(col),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 110
            });
        }

        CrmTableStyler.Apply(grid,
            "Status", "ApprovalStatus", "PaymentStatus", "Severity", "Priority",
            "Segment", "Role", "IssueType", "ActivityType", "DesignStage",
            "LeadSource", "CustomerType", "IsActive");

        // ---- Pagination state ----
        const int PageSize = 17;
        int currentPage = 1;
        List<JsonElement> _renderRecords = new();
        List<JsonElement> _filteredRecords = currentRecords.ToList();

        // ---- Pager ----
        var pager = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
        card.Controls.Add(pager);

        pager.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 235, 240), 1);
            e.Graphics.DrawLine(pen, 0, 0, pager.Width, 0);
        };

        var btnPrev = new Button
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
        btnPrev.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        pager.Controls.Add(btnPrev);

        var lblPageInfo = new Label
        {
            Left = 118,
            Top = 20,
            Width = 420,
            Height = 24,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ColorText
        };
        pager.Controls.Add(lblPageInfo);

        var btnNext = new Button
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
        btnNext.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        pager.Controls.Add(btnNext);

        void RenderPage()
        {
            grid.Rows.Clear();

            int totalPages = _filteredRecords.Count == 0
                ? 1
                : (int)Math.Ceiling(_filteredRecords.Count / (double)PageSize);

            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            int start = (currentPage - 1) * PageSize;
            int end = Math.Min(start + PageSize, _filteredRecords.Count);

            _renderRecords = _filteredRecords.Skip(start).Take(PageSize).ToList();

            foreach (var record in _renderRecords)
            {
                var values = new object[columns.Count];
                for (int i = 0; i < columns.Count; i++)
                    values[i] = GetValueFromJson(record, columns[i]) ?? "";
                grid.Rows.Add(values);
            }

            lblPageInfo.Text = $"Page {currentPage} of {totalPages}   ·   " +
                              $"Showing {start + 1}–{end} of {_filteredRecords.Count}";

            btnPrev.Enabled = currentPage > 1;
            btnNext.Enabled = currentPage < totalPages;
            btnPrev.ForeColor = btnPrev.Enabled ? ColorText : Color.FromArgb(180, 186, 196);
            btnNext.ForeColor = btnNext.Enabled ? ColorText : Color.FromArgb(180, 186, 196);

            lblRecordCount.Text = $"{_filteredRecords.Count:N0} record{(_filteredRecords.Count == 1 ? "" : "s")}";
        }

        btnPrev.Click += (_, _) => { if (currentPage > 1) { currentPage--; RenderPage(); } };
        btnNext.Click += (_, _) =>
        {
            int totalPages = _filteredRecords.Count == 0
                ? 1
                : (int)Math.Ceiling(_filteredRecords.Count / (double)PageSize);
            if (currentPage < totalPages) { currentPage++; RenderPage(); }
        };

        searchBox.TextChanged += (_, _) =>
        {
            var search = searchBox.Text.Trim();
            _filteredRecords = string.IsNullOrWhiteSpace(search)
                ? currentRecords.ToList()
                : currentRecords.Where(r =>
                {
                    foreach (var col in columns)
                    {
                        var val = GetValueFromJson(r, col)?.ToString();
                        if (val != null && val.Contains(search, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    return false;
                }).ToList();

            currentPage = 1;
            RenderPage();
        };

        // ---- New / Edit / Delete ----
        newButton.Click += async (_, _) =>
        {
            if (string.Equals(endpoint, "projects", StringComparison.OrdinalIgnoreCase))
            {
                using var dlg = new Form { Text = "New Project", Size = new Size(520, 420), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
                var txtCode = new TextBox { Left = 20, Top = 24, Width = 460, PlaceholderText = "Project code" };
                var txtName = new TextBox { Left = 20, Top = 64, Width = 460, PlaceholderText = "Project name" };
                var cmbCustomer = new ComboBox { Left = 20, Top = 104, Width = 460, DropDownStyle = ComboBoxStyle.DropDownList };
                var txtType = new TextBox { Left = 20, Top = 144, Width = 460, PlaceholderText = "Project type" };
                var txtLocation = new TextBox { Left = 20, Top = 184, Width = 460, PlaceholderText = "Location" };
                var dtStart = new DateTimePicker { Left = 20, Top = 224, Width = 220, Format = DateTimePickerFormat.Short };
                var dtEnd = new DateTimePicker { Left = 260, Top = 224, Width = 220, Format = DateTimePickerFormat.Short };
                var txtNotes = new TextBox { Left = 20, Top = 264, Width = 460, Height = 60, Multiline = true, PlaceholderText = "Notes" };
                var btnOk = new Button { Text = "Create", Left = 300, Width = 80, Top = 336, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Left = 392, Width = 80, Top = 336, DialogResult = DialogResult.Cancel };

                dlg.Controls.AddRange(new Control[]
                {
                    new Label { Text = "Code", Left = 20, Top = 6 },
                    new Label { Text = "Name", Left = 20, Top = 46 },
                    new Label { Text = "Customer", Left = 20, Top = 86 },
                    new Label { Text = "Type", Left = 20, Top = 126 },
                    new Label { Text = "Location", Left = 260, Top = 126 },
                    new Label { Text = "Start", Left = 20, Top = 206 },
                    new Label { Text = "End", Left = 260, Top = 206 },
                    txtCode, txtName, cmbCustomer, txtType, txtLocation,
                    dtStart, dtEnd, txtNotes, btnOk, btnCancel
                });

                try
                {
                    var customers = await GetArrayAsync("customers");
                    cmbCustomer.Items.AddRange(customers.Select(c => new
                    {
                        Element = c,
                        Text = (GetString(c, "FirstName") ?? "Unnamed") + " " + (GetString(c, "LastName") ?? "") + " (ID:" + (GetValueFromJson(c, "CustomerId") ?? GetValueFromJson(c, "Id")) + ")"
                    }).Cast<object>().ToArray());
                    cmbCustomer.DisplayMember = "Text";
                }
                catch { }

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    object? customerIdValue = null;
                    if (cmbCustomer.SelectedItem != null)
                    {
                        var elemProp = cmbCustomer.SelectedItem.GetType().GetProperty("Element");
                        if (elemProp != null)
                        {
                            var elem = (JsonElement)elemProp.GetValue(cmbCustomer.SelectedItem)!;
                            customerIdValue = GetValueFromJson(elem, "CustomerId") ?? GetValueFromJson(elem, "Id");
                        }
                    }

                    var body = new Dictionary<string, object?>
                    {
                        ["projectCode"] = txtCode.Text.Trim(),
                        ["projectName"] = txtName.Text.Trim(),
                        ["customerId"] = customerIdValue,
                        ["projectType"] = txtType.Text.Trim(),
                        ["location"] = txtLocation.Text.Trim(),
                        ["startDate"] = dtStart.Value.ToString("o"),
                        ["targetEndDate"] = dtEnd.Value.ToString("o"),
                        ["notes"] = txtNotes.Text.Trim()
                    };

                    await PostObjectAsync(endpoint, body);
                    await LoadEntityPageAsync(title, endpoint, preferredColumns);
                }
                return;
            }

            if (string.Equals(endpoint, "quotations", StringComparison.OrdinalIgnoreCase))
            {
                using var dlg = new Form { Text = "New Quotation", Size = new Size(520, 360), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
                var cmbProject = new ComboBox { Left = 20, Top = 24, Width = 460, DropDownStyle = ComboBoxStyle.DropDownList };
                var lblNumberInfo = new Label
                {
                    Left = 20,
                    Top = 64,
                    Width = 460,
                    Height = 22,
                    Text = "Quotation # will be auto-generated (e.g. QT-202609-0001)",
                    ForeColor = Color.FromArgb(110, 118, 132),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
                };
                var txtSubtotal = new TextBox { Left = 20, Top = 104, Width = 220, PlaceholderText = "Subtotal" };
                var txtDiscount = new TextBox { Left = 260, Top = 104, Width = 220, PlaceholderText = "Discount" };
                var txtNotes = new TextBox { Left = 20, Top = 144, Width = 460, Height = 80, Multiline = true, PlaceholderText = "Notes" };
                var btnOk = new Button { Text = "Create", Left = 300, Width = 80, Top = 240, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Left = 392, Width = 80, Top = 240, DialogResult = DialogResult.Cancel };

                dlg.Controls.AddRange(new Control[]
                {
                    new Label { Text = "Project", Left = 20, Top = 6 },
                    new Label { Text = "Quotation #", Left = 20, Top = 46 },
                    new Label { Text = "Subtotal", Left = 20, Top = 86 },
                    new Label { Text = "Discount", Left = 260, Top = 86 },
                    cmbProject, lblNumberInfo, txtSubtotal, txtDiscount, txtNotes, btnOk, btnCancel
                });

                try
                {
                    var projects = await GetArrayAsync("projects");
                    cmbProject.Items.AddRange(projects.Select(p => new
                    {
                        Element = p,
                        Text = (GetString(p, "ProjectName") ?? "Unnamed") + " (ID:" + (GetValueFromJson(p, "ProjectId") ?? GetValueFromJson(p, "Id")) + ")"
                    }).Cast<object>().ToArray());
                    cmbProject.DisplayMember = "Text";
                }
                catch { }

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    object? projectIdValue = null;
                    if (cmbProject.SelectedItem != null)
                    {
                        var elemProp = cmbProject.SelectedItem.GetType().GetProperty("Element");
                        if (elemProp != null)
                        {
                            var elem = (JsonElement)elemProp.GetValue(cmbProject.SelectedItem)!;
                            projectIdValue = GetValueFromJson(elem, "ProjectId") ?? GetValueFromJson(elem, "Id");
                        }
                    }

                    var subtotal = 0m; decimal.TryParse(txtSubtotal.Text, out subtotal);
                    var discount = 0m; decimal.TryParse(txtDiscount.Text, out discount);

                    var body = new Dictionary<string, object?>
                    {
                        ["projectId"] = projectIdValue,
                        ["subtotal"] = subtotal,
                        ["discount"] = discount,
                        ["notes"] = txtNotes.Text.Trim()
                    };

                    await PostObjectAsync(endpoint, body);
                    await LoadEntityPageAsync(title, endpoint, preferredColumns);
                }
                return;
            }

            // Fallback create (Customers, Leads, etc.)
            using var dlgGen = new Form { Text = $"New {title.TrimEnd('s')}", Size = new Size(420, 300), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
            var txtFirst = new TextBox { Left = 20, Top = 24, Width = 360, PlaceholderText = "First name" };
            var txtLast = new TextBox { Left = 20, Top = 64, Width = 360, PlaceholderText = "Last name" };
            var txtEmail = new TextBox { Left = 20, Top = 104, Width = 360, PlaceholderText = "Email (optional)" };
            var txtPhone = new TextBox { Left = 20, Top = 144, Width = 360, PlaceholderText = "Phone (optional)" };
            var btnOkF = new Button { Text = "Create", Left = 200, Width = 80, Top = 200, DialogResult = DialogResult.OK };
            var btnCancelF = new Button { Text = "Cancel", Left = 292, Width = 80, Top = 200, DialogResult = DialogResult.Cancel };

            dlgGen.Controls.AddRange(new Control[]
            {
                new Label { Text = "First name", Left = 20, Top = 6 },
                new Label { Text = "Last name", Left = 20, Top = 46 },
                new Label { Text = "Email", Left = 20, Top = 86 },
                new Label { Text = "Phone", Left = 20, Top = 126 },
                txtFirst, txtLast, txtEmail, txtPhone, btnOkF, btnCancelF
            });

            if (dlgGen.ShowDialog(this) == DialogResult.OK)
            {
                var body = new Dictionary<string, object?>
                {
                    ["firstName"] = txtFirst.Text.Trim(),
                    ["lastName"] = txtLast.Text.Trim(),
                    ["email"] = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                    ["phone"] = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim()
                };

                await PostObjectAsync(endpoint, body);
                await LoadEntityPageAsync(title, endpoint, preferredColumns);
            }
        };

        editButton.Click += async (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) { MessageBox.Show("Select a record to edit.", "Edit"); return; }
            var visibleIdx = grid.SelectedRows[0].Index;
            if (visibleIdx < 0 || visibleIdx >= _renderRecords.Count) return;

            var record = _renderRecords[visibleIdx];
            var idObj = endpoint.ToLower() switch
            {
                "customers" => GetValueFromJson(record, "CustomerId"),
                "leads" => GetValueFromJson(record, "LeadId"),
                "projects" => GetValueFromJson(record, "ProjectId"),
                "quotations" => GetValueFromJson(record, "QuotationId"),
                "activities" => GetValueFromJson(record, "ActivityId"),
                "suppliers" => GetValueFromJson(record, "SupplierId"),
                _ => GetValueFromJson(record, "Id")
            };
            if (idObj == null) { MessageBox.Show("Cannot determine record id.", "Edit"); return; }
            var id = idObj.ToString();

            JsonElement elem = await GetObjectAsync($"{endpoint}/{id}");
            if (elem.ValueKind == JsonValueKind.Undefined)
            {
                MessageBox.Show("Unable to fetch record details.", "Edit");
                return;
            }

            // Generic edit (works for most entities)
            using var dlg = new Form { Text = "Edit", Size = new Size(420, 360), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
            var txtFirst = new TextBox { Left = 20, Top = 24, Width = 360, Text = GetString(elem, "FirstName", "firstName") ?? "" };
            var txtLast = new TextBox { Left = 20, Top = 64, Width = 360, Text = GetString(elem, "LastName", "lastName") ?? "" };
            var txtEmail = new TextBox { Left = 20, Top = 104, Width = 360, Text = GetString(elem, "Email", "email") ?? "" };
            var txtPhone = new TextBox { Left = 20, Top = 144, Width = 360, Text = GetString(elem, "Phone", "phone") ?? "" };
            var chkActive = new CheckBox { Left = 20, Top = 184, Width = 200, Checked = (GetValueFromJson(elem, "IsActive") as bool?) ?? true, Text = "Active" };
            var btnOk = new Button { Text = "Save", Left = 200, Width = 80, Top = 240, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Left = 292, Width = 80, Top = 240, DialogResult = DialogResult.Cancel };

            dlg.Controls.AddRange(new Control[]
            {
                new Label { Text = "First name", Left = 20, Top = 6 },
                new Label { Text = "Last name", Left = 20, Top = 46 },
                new Label { Text = "Email", Left = 20, Top = 86 },
                new Label { Text = "Phone", Left = 20, Top = 126 },
                txtFirst, txtLast, txtEmail, txtPhone, chkActive, btnOk, btnCancel
            });

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var body = new Dictionary<string, object?>
                {
                    ["firstName"] = txtFirst.Text.Trim(),
                    ["lastName"] = txtLast.Text.Trim(),
                    ["email"] = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                    ["phone"] = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim(),
                    ["isActive"] = chkActive.Checked
                };

                await PutObjectAsync($"{endpoint}/{id}", body);
                await LoadEntityPageAsync(title, endpoint, preferredColumns);
            }
        };

        deleteButton.Click += async (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) { MessageBox.Show("Select a record to delete.", "Delete"); return; }
            var visibleIdx = grid.SelectedRows[0].Index;
            if (visibleIdx < 0 || visibleIdx >= _renderRecords.Count) return;

            var record = _renderRecords[visibleIdx];
            var idObj = endpoint.ToLower() switch
            {
                "customers" => GetValueFromJson(record, "CustomerId"),
                "leads" => GetValueFromJson(record, "LeadId"),
                "projects" => GetValueFromJson(record, "ProjectId"),
                "quotations" => GetValueFromJson(record, "QuotationId"),
                "activities" => GetValueFromJson(record, "ActivityId"),
                "suppliers" => GetValueFromJson(record, "SupplierId"),
                _ => GetValueFromJson(record, "Id")
            };
            if (idObj == null) { MessageBox.Show("Cannot determine record id.", "Delete"); return; }
            var id = idObj.ToString();

            if (MessageBox.Show("Are you sure you want to delete this record?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            await DeleteAsync($"{endpoint}/{id}");
            await LoadEntityPageAsync(title, endpoint, preferredColumns);
        };

        card.Controls.Add(grid);
        grid.BringToFront();

        List<JsonElement> GetRecords() => currentRecords;
        AddWorkflowButtons(header, endpoint, grid, GetRecords);

        RenderPage();
    }

    // =========================================================
    // WORKFLOW BUTTONS
    // =========================================================
    private void AddWorkflowButtons(Panel header, string endpoint, DataGridView grid, Func<List<JsonElement>> getCurrentRecords)
    {
        int x = 500;

        if (endpoint.Equals("leads", StringComparison.OrdinalIgnoreCase))
        {
            var btnConvert = CreateSecondaryButton("⇄  Convert to Customer", 200, 36);
            btnConvert.Location = new Point(x, 8);
            btnConvert.BackColor = ColorAccentSoft;
            btnConvert.ForeColor = Color.FromArgb(160, 95, 0);
            header.Controls.Add(btnConvert);

            btnConvert.Click += async (_, _) =>
            {
                if (grid.SelectedRows.Count == 0) { MessageBox.Show("Select a lead to convert.", "Convert"); return; }
                var records = getCurrentRecords();
                var idx = grid.SelectedRows[0].Index;
                if (idx < 0 || idx >= records.Count) return;

                var lead = records[idx];
                var leadId = GetValueFromJson(lead, "LeadId")?.ToString();
                if (leadId == null) return;

                var name = $"{GetString(lead, "FirstName")} {GetString(lead, "LastName")}".Trim();

                using var dlg = new ConvertLeadDialog(name);
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

                try
                {
                    await PostWorkflowAsync($"leads/{leadId}/convert", dlg.ToPayload());
                    MessageBox.Show("Lead converted successfully.", "Success");
                    await LoadEntityPageAsync("Leads", "leads",
                        new[] { "LeadId", "FirstName", "LastName", "Status", "LeadSource", "CreatedAt" });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Convert failed:\n\n" + ex.Message, "Error");
                }
            };
        }

        if (endpoint.Equals("quotations", StringComparison.OrdinalIgnoreCase))
        {
            var btnIssue = CreateSecondaryButton("✓  Issue", 90, 36);
            btnIssue.Location = new Point(x, 8);
            header.Controls.Add(btnIssue);
            btnIssue.Click += async (_, _) => await RunQuotationAction(grid, getCurrentRecords, "Issue", "issue");

            var btnAccept = CreateSecondaryButton("☑  Accept", 100, 36);
            btnAccept.Location = new Point(x + 100, 8);
            header.Controls.Add(btnAccept);
            btnAccept.Click += async (_, _) => await RunQuotationAction(grid, getCurrentRecords, "Accept", "accept");

            var btnPay = CreateSecondaryButton("₱  Payment", 120, 36);
            btnPay.Location = new Point(x + 210, 8);
            btnPay.BackColor = ColorAccentSoft;
            btnPay.ForeColor = Color.FromArgb(160, 95, 0);
            header.Controls.Add(btnPay);
            btnPay.Click += async (_, _) => await ShowPaymentDialog(grid, getCurrentRecords);
        }

        if (endpoint.Equals("projects", StringComparison.OrdinalIgnoreCase))
        {
            var btnAssign = CreateSecondaryButton("👤  Assign Designer", 170, 36);
            btnAssign.Location = new Point(x, 8);
            btnAssign.BackColor = ColorAccentSoft;
            btnAssign.ForeColor = Color.FromArgb(160, 95, 0);
            header.Controls.Add(btnAssign);
            btnAssign.Click += async (_, _) => await ShowAssignDesignerDialog(grid, getCurrentRecords);

            var btnProgress = CreateSecondaryButton("📈  Update Progress", 170, 36);
            btnProgress.Location = new Point(x + 180, 8);
            header.Controls.Add(btnProgress);
            btnProgress.Click += async (_, _) => await ShowUpdateProgressDialog(grid, getCurrentRecords);

            var btnFeedback = CreateSecondaryButton("⭐  Leave Feedback", 160, 36);
            btnFeedback.Location = new Point(x + 360, 8);
            btnFeedback.BackColor = ColorAccentSoft;
            btnFeedback.ForeColor = Color.FromArgb(160, 95, 0);
            header.Controls.Add(btnFeedback);
            btnFeedback.Click += async (_, _) => await ShowFeedbackDialog(grid, getCurrentRecords);

            var btnIssue = CreateSecondaryButton("⚠  Report Issue", 140, 36);
            btnIssue.Location = new Point(x + 530, 8);
            header.Controls.Add(btnIssue);
            btnIssue.Click += async (_, _) => await ShowNewIssueDialog(grid, getCurrentRecords);
        }

        if (endpoint.Equals("issues", StringComparison.OrdinalIgnoreCase))
        {
            var btnResolve = CreateSecondaryButton("✓  Resolve", 120, 36);
            btnResolve.Location = new Point(x, 8);
            btnResolve.BackColor = ColorAccentSoft;
            btnResolve.ForeColor = Color.FromArgb(160, 95, 0);
            header.Controls.Add(btnResolve);
            btnResolve.Click += async (_, _) =>
            {
                if (grid.SelectedRows.Count == 0) { MessageBox.Show("Select an issue.", "Resolve"); return; }
                var records = getCurrentRecords();
                var idx = grid.SelectedRows[0].Index;
                if (idx < 0 || idx >= records.Count) return;

                var issue = records[idx];
                var issueId = GetValueFromJson(issue, "ProjectIssueId")?.ToString();
                var title = GetString(issue, "Title") ?? "Issue";
                if (issueId == null) return;

                using var dlg = new ResolveIssueDialog(title);
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

                try
                {
                    var url = $"{ApiUrl}/tenant/{Session.CompanyId}/issues/{issueId}/resolve";
                    using var req = new HttpRequestMessage(HttpMethod.Post, url);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
                    req.Content = new StringContent(JsonSerializer.Serialize(dlg.ToPayload()), System.Text.Encoding.UTF8, "application/json");

                    using var res = await _httpClient.SendAsync(req);
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

                    MessageBox.Show("Issue resolved.", "Success");
                    await LoadEntityPageAsync("Issues", "issues",
                        new[] { "ProjectIssueId", "Title", "IssueType", "Severity", "Status", "ReportedAt" });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Resolve failed:\n\n" + ex.Message, "Error");
                }
            };
        }
    }

    // =========================================================
    // WORKFLOW HELPERS
    // =========================================================
    private async Task PostWorkflowAsync(string workflowPath, object? payload)
    {
        if (Session.CompanyId == null)
            throw new InvalidOperationException("No company in session.");

        var url = $"{ApiUrl}/tenant/{Session.CompanyId.Value}/workflow/{workflowPath}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

        if (payload != null)
        {
            req.Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        }

        using var res = await _httpClient.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            string message = json;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var m))
                    message = m.GetString() ?? json;
            }
            catch { }
            throw new HttpRequestException(message);
        }
    }

    private async Task RunQuotationAction(DataGridView grid, Func<List<JsonElement>> getCurrentRecords, string actionLabel, string endpointAction)
    {
        if (grid.SelectedRows.Count == 0) { MessageBox.Show($"Select a quotation to {actionLabel.ToLower()}.", actionLabel); return; }
        var records = getCurrentRecords();
        var idx = grid.SelectedRows[0].Index;
        if (idx < 0 || idx >= records.Count) return;

        var quot = records[idx];
        var quotId = GetValueFromJson(quot, "QuotationId")?.ToString();
        if (quotId == null) return;

        try
        {
            await PostWorkflowAsync($"quotations/{quotId}/{endpointAction}", null);
            MessageBox.Show($"Quotation {actionLabel.ToLower()}ed successfully.", "Success");
            await LoadEntityPageAsync("Quotations", "quotations",
                new[] { "QuotationNumber", "Status", "TotalAmount", "PaymentStatus", "CreatedAt" });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{actionLabel} failed:\n\n" + ex.Message, "Error");
        }
    }

    private async Task ShowPaymentDialog(DataGridView grid, Func<List<JsonElement>> getCurrentRecords)
    {
        if (grid.SelectedRows.Count == 0) return;
        var records = getCurrentRecords();
        var idx = grid.SelectedRows[0].Index;
        if (idx < 0 || idx >= records.Count) return;

        var quot = records[idx];
        var quotId = GetValueFromJson(quot, "QuotationId")?.ToString();
        if (quotId == null) return;

        var amountDue = GetValueFromJson(quot, "DepositRequired")?.ToString() ?? "0";
        var total = GetValueFromJson(quot, "TotalAmount")?.ToString() ?? "0";

        using var dlg = new Form
        {
            Text = "Record Payment",
            Size = new Size(460, 320),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White
        };

        dlg.Controls.Add(new Label
        {
            Text = $"Total: ₱{total}    Deposit Required: ₱{amountDue}",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(20, 16)
        });

        dlg.Controls.Add(new Label { Text = "Amount", Font = new Font("Segoe UI", 8.75f, FontStyle.Bold), AutoSize = true, Location = new Point(20, 50) });
        var txtAmount = new TextBox { Left = 20, Top = 72, Width = 400, Text = amountDue, Font = new Font("Segoe UI", 10f) };
        dlg.Controls.Add(txtAmount);

        dlg.Controls.Add(new Label { Text = "Payment Method", Font = new Font("Segoe UI", 8.75f, FontStyle.Bold), AutoSize = true, Location = new Point(20, 106) });
        var cmbMethod = new ComboBox { Left = 20, Top = 128, Width = 400, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
        cmbMethod.Items.AddRange(new[] { "Bank Transfer", "Cash", "Check", "GCash", "Credit Card" });
        cmbMethod.SelectedIndex = 0;
        dlg.Controls.Add(cmbMethod);

        dlg.Controls.Add(new Label { Text = "Reference / OR #", Font = new Font("Segoe UI", 8.75f, FontStyle.Bold), AutoSize = true, Location = new Point(20, 162) });
        var txtRef = new TextBox { Left = 20, Top = 184, Width = 400, Font = new Font("Segoe UI", 9.5f) };
        dlg.Controls.Add(txtRef);

        var btnOk = new Button { Text = "Record", Left = 240, Top = 232, Width = 90, Height = 34, BackColor = Color.FromArgb(255, 168, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), DialogResult = DialogResult.OK };
        btnOk.FlatAppearance.BorderSize = 0;
        dlg.Controls.Add(btnOk);

        var btnCancel = new Button { Text = "Cancel", Left = 338, Top = 232, Width = 82, Height = 34, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f), DialogResult = DialogResult.Cancel };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        dlg.Controls.Add(btnCancel);

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        if (!decimal.TryParse(txtAmount.Text, out var amount) || amount <= 0)
        {
            MessageBox.Show("Invalid amount.", "Payment");
            return;
        }

        try
        {
            await PostWorkflowAsync($"quotations/{quotId}/payments", new
            {
                amount = amount,
                paymentMethod = cmbMethod.SelectedItem?.ToString() ?? "Cash",
                reference = txtRef.Text.Trim(),
                notes = ""
            });

            MessageBox.Show("Payment recorded successfully.", "Success");
            await LoadEntityPageAsync("Quotations", "quotations",
                new[] { "QuotationNumber", "Status", "TotalAmount", "PaymentStatus", "CreatedAt" });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Payment failed:\n\n" + ex.Message, "Error");
        }
    }

    private async Task ShowAssignDesignerDialog(DataGridView grid, Func<List<JsonElement>> getCurrentRecords)
    {
        if (grid.SelectedRows.Count == 0) return;
        var records = getCurrentRecords();
        var idx = grid.SelectedRows[0].Index;
        if (idx < 0 || idx >= records.Count) return;

        var proj = records[idx];
        var projId = GetValueFromJson(proj, "ProjectId")?.ToString();
        if (projId == null) return;

        using var dlg = new AssignDesignerDialog(ApiUrl, _httpClient);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await PostWorkflowAsync($"projects/{projId}/assign-designer", dlg.ToPayload());
            MessageBox.Show("Designer assigned successfully.", "Success");
            await LoadEntityPageAsync("Projects", "projects",
                new[] { "ProjectCode", "ProjectName", "DesignStage", "Status", "ProgressPercentage" });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Assign failed:\n\n" + ex.Message, "Error");
        }
    }

    private async Task ShowUpdateProgressDialog(DataGridView grid, Func<List<JsonElement>> getCurrentRecords)
    {
        if (grid.SelectedRows.Count == 0) return;
        var records = getCurrentRecords();
        var idx = grid.SelectedRows[0].Index;
        if (idx < 0 || idx >= records.Count) return;

        var proj = records[idx];
        var projId = GetValueFromJson(proj, "ProjectId")?.ToString();
        if (projId == null) return;

        var currentProgress = 0;
        if (int.TryParse(GetValueFromJson(proj, "ProgressPercentage")?.ToString(), out var p))
            currentProgress = p;
        var currentNotes = GetString(proj, "DesignNotes") ?? "";

        using var dlg = new UpdateProgressDialog(currentProgress, currentNotes);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await PostWorkflowAsync($"projects/{projId}/progress", new
            {
                progressPercentage = dlg.ProgressPercentage,
                designNotes = dlg.DesignNotes
            });

            MessageBox.Show("Progress updated successfully.", "Success");
            await LoadEntityPageAsync("Projects", "projects",
                new[] { "ProjectCode", "ProjectName", "DesignStage", "Status", "ProgressPercentage" });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Update failed:\n\n" + ex.Message, "Error");
        }
    }

    // =========================================================
    // FEEDBACK + ISSUES
    // =========================================================
    private async Task ShowFeedbackDialog(DataGridView grid, Func<List<JsonElement>> getCurrentRecords)
    {
        if (grid.SelectedRows.Count == 0) { MessageBox.Show("Select a project first.", "Feedback"); return; }
        var records = getCurrentRecords();
        var idx = grid.SelectedRows[0].Index;
        if (idx < 0 || idx >= records.Count) return;

        var proj = records[idx];
        var projId = GetValueFromJson(proj, "ProjectId")?.ToString();
        var projName = GetString(proj, "ProjectName") ?? "Project";
        var stage = GetString(proj, "DesignStage") ?? "";
        if (projId == null) return;

        if (!string.Equals(stage, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Feedback can only be submitted for Completed projects.\n\n" + $"Current stage: {stage}", "Feedback");
            return;
        }

        using var dlg = new FeedbackDialog(projName);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var url = $"{ApiUrl}/tenant/{Session.CompanyId}/projects/{projId}/feedback";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            req.Content = new StringContent(JsonSerializer.Serialize(dlg.ToPayload(int.Parse(projId))), System.Text.Encoding.UTF8, "application/json");

            using var res = await _httpClient.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                string msg = json;
                try { using var doc = JsonDocument.Parse(json); if (doc.RootElement.TryGetProperty("message", out var m)) msg = m.GetString() ?? json; }
                catch { }
                throw new HttpRequestException(msg);
            }

            MessageBox.Show("Thank you for your feedback!", "Success");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Feedback failed:\n\n" + ex.Message, "Error");
        }
    }

    private async Task ShowNewIssueDialog(DataGridView grid, Func<List<JsonElement>> getCurrentRecords)
    {
        if (grid.SelectedRows.Count == 0) { MessageBox.Show("Select a project first.", "Report Issue"); return; }
        var records = getCurrentRecords();
        var idx = grid.SelectedRows[0].Index;
        if (idx < 0 || idx >= records.Count) return;

        var proj = records[idx];
        var projId = GetValueFromJson(proj, "ProjectId")?.ToString();
        var projName = GetString(proj, "ProjectName") ?? "Project";
        if (projId == null) return;

        using var dlg = new NewIssueDialog(projName);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var url = $"{ApiUrl}/tenant/{Session.CompanyId}/projects/{projId}/issues";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            req.Content = new StringContent(JsonSerializer.Serialize(dlg.ToPayload(int.Parse(projId))), System.Text.Encoding.UTF8, "application/json");

            using var res = await _httpClient.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                string msg = json;
                try { using var doc = JsonDocument.Parse(json); if (doc.RootElement.TryGetProperty("message", out var m)) msg = m.GetString() ?? json; }
                catch { }
                throw new HttpRequestException(msg);
            }

            MessageBox.Show("Issue reported successfully.", "Success");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Report failed:\n\n" + ex.Message, "Error");
        }
    }

    // =========================================================
    // PAGE BUILDERS
    // =========================================================
    public void NavigateTo(string page) => SelectNavigation(page);

    private void BuildDesignersPage()
    {
        contentPanel.Controls.Clear();
        contentPanel.Padding = new Padding(32, 20, 32, 32);

        var page = new DesignersPage(ApiUrl, _httpClient);
        contentPanel.Controls.Add(page);
        _ = page.LoadAsync();
    }

    private void BuildUsersPage()
    {
        contentPanel.Controls.Clear();
        contentPanel.Padding = new Padding(32, 20, 32, 32);

        var page = new UsersPage(ApiUrl, _httpClient);
        contentPanel.Controls.Add(page);
        _ = page.LoadAsync();
    }

    private void BuildBiDashboard()
    {
        contentPanel.Controls.Clear();
        contentPanel.Padding = new Padding(32, 20, 32, 32);

        var page = new BiDashboardPage(ApiUrl, _httpClient);
        contentPanel.Controls.Add(page);
        _ = page.LoadAsync();
    }

    private void BuildRetentionPage()
    {
        contentPanel.Controls.Clear();
        contentPanel.Padding = new Padding(32, 20, 32, 32);

        var page = new RetentionPage(ApiUrl, _httpClient);
        contentPanel.Controls.Add(page);
        _ = page.LoadAsync();
    }

    private void BuildPromotionsPage()
    {
        contentPanel.Controls.Clear();
        contentPanel.Padding = new Padding(32, 20, 32, 32);

        var page = new PromotionsPage(ApiUrl, _httpClient);
        contentPanel.Controls.Add(page);
        _ = page.LoadAsync();
    }

    private void BuildReportsPage()
    {
        contentPanel.Controls.Clear();
        contentPanel.Padding = new Padding(32, 20, 32, 32);

        var page = new ReportsPage(ApiUrl, _httpClient);
        contentPanel.Controls.Add(page);
        _ = page.LoadAllAsync();
    }

    // =========================================================
    // ROLE-BASED VISIBILITY
    // =========================================================
    private bool CanSee(string pageName)
    {
        var roles = Session.Roles ?? new List<string>();

        bool isSuperAdmin = roles.Contains("Super Admin");
        bool isAdmin = roles.Contains("Admin");
        bool isManager = roles.Contains("Manager");
        bool isStaff = roles.Contains("Staff");

        if (isSuperAdmin || isAdmin) return true;

        if (isManager)
        {
            return pageName switch
            {
                "Overview" => true,
                "Customers" => true,
                "Leads" => true,
                "Projects" => true,
                "Quotations" => true,
                "Activities" => true,
                "Designers" => true,
                "Users" => true,
                "Issues" => true,
                "Feedback" => true,
                "Analytics" => true,
                "Retention" => true,
                "Promotions" => true,
                "Reports" => true,
                _ => false
            };
        }

        // Staff
        return pageName switch
        {
            "Overview" => true,
            "Projects" => true,
            "Activities" => true,
            "Issues" => true,
            "Feedback" => true,

            "Customers" => isStaff,
            "Leads" => isStaff,
            "Quotations" => isStaff,
            "Designers" => isStaff,
            "Promotions" => isStaff,
            "Reports" => isStaff,

            "Analytics" => false,
            "Retention" => false,
            "Users" => false,

            _ => false
        };
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private Button CreateSecondaryButton(string text, int width, int height)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = height,
            BackColor = ColorWhite,
            ForeColor = ColorText,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = ColorBorder;
        btn.FlatAppearance.MouseOverBackColor = ColorAccentSoft;
        return btn;
    }

    private void TryLoadLogo(PictureBox pictureBox)
    {
        try
        {
            string[] paths =
            {
                Path.Combine(AppContext.BaseDirectory, "Images", "fuerto-logo.png"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Images", "fuerto-logo.png"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Images", "fuerto-logo.png")
            };

            string? logoPath = paths.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
            if (logoPath != null)
            {
                using var stream = new FileStream(logoPath, FileMode.Open, FileAccess.Read);
                using var image = Image.FromStream(stream);
                pictureBox.Image = new Bitmap(image);
            }
        }
        catch { }
    }

    private static string GetInitials(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "FA";
        var name = email.Split('@')[0].Replace('.', ' ').Replace('_', ' ');
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
    }

    private static object? GetValueFromJson(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;

        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return prop.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.TryGetDecimal(out var d) ? d : prop.Value.ToString(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.ToString()
                };
            }
        }
        return null;
    }

    private Panel CreateCard(string title, string subtitle)
    {
        var card = new Panel { BackColor = ColorWhite, Padding = new Padding(22) };

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorCardBorder, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        if (!string.IsNullOrWhiteSpace(title))
        {
            card.Controls.Add(new Label
            {
                Text = title,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(22, 18)
            });

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                card.Controls.Add(new Label
                {
                    Text = subtitle,
                    ForeColor = ColorMuted,
                    Font = new Font("Segoe UI", 8.5f),
                    AutoSize = true,
                    Location = new Point(22, 44)
                });
            }
        }
        return card;
    }

    private void ShowLoading()
    {
        contentPanel.Controls.Clear();
        contentPanel.Controls.Add(new Label
        {
            Text = "Loading live CRM data…",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 11.5f)
        });
    }

    private void BuildErrorPage(string title, string detail)
    {
        contentPanel.Controls.Clear();
        var card = CreateCard("Connection / Data Error", "");
        card.Dock = DockStyle.Top;
        card.Height = 240;
        contentPanel.Controls.Add(card);

        card.Controls.Add(new Label
        {
            Text = title,
            ForeColor = ColorDanger,
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(28, 40)
        });

        card.Controls.Add(new Label
        {
            Text = detail,
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 9.5f),
            MaximumSize = new Size(860, 0),
            AutoSize = true,
            Location = new Point(28, 82)
        });

        card.Controls.Add(new Label
        {
            Text = "Make sure CRM.api is running on http://localhost:5068 and that you are logged in.",
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(28, 150)
        });
    }

    private void SetApiConnected()
    {
        if (lblApiStatus.InvokeRequired) { lblApiStatus.Invoke(SetApiConnected); return; }
        lblApiStatus.Text = "●  API Connected";
        lblApiStatus.ForeColor = ColorSuccess;
    }

    private void SetApiDisconnected()
    {
        if (lblApiStatus.InvokeRequired) { lblApiStatus.Invoke(SetApiDisconnected); return; }
        lblApiStatus.Text = "●  API Unavailable";
        lblApiStatus.ForeColor = ColorDanger;
    }

    // =========================================================
    // API HELPERS
    // =========================================================
    private async Task<List<JsonElement>> GetArrayAsync(string endpoint)
    {
        if (Session.CompanyId == null)
            throw new InvalidOperationException("No company is assigned to the current login session.");

        if (string.IsNullOrWhiteSpace(Session.Token))
            throw new InvalidOperationException("No authentication token is available.");

        var url = $"{ApiUrl}/tenant/{Session.CompanyId.Value}/{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

        using var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Endpoint: {url}\n\nStatus: {(int)response.StatusCode} {response.ReasonPhrase}\n\nResponse:\n{json}");

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind == JsonValueKind.Array)
            return document.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();

        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "data", "items", "results", endpoint })
            {
                if (document.RootElement.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Array)
                    return prop.EnumerateArray().Select(e => e.Clone()).ToList();
            }
        }

        return new List<JsonElement>();
    }

    private async Task<JsonElement> GetObjectAsync(string endpoint)
    {
        if (Session.CompanyId == null)
            throw new InvalidOperationException("No company is assigned to the current login session.");

        if (string.IsNullOrWhiteSpace(Session.Token))
            throw new InvalidOperationException("No authentication token is available.");

        var url = $"{ApiUrl}/tenant/{Session.CompanyId.Value}/{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

        using var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Endpoint: {url}\n\nStatus: {(int)response.StatusCode} {response.ReasonPhrase}\n\nResponse:\n{json}");

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind == JsonValueKind.Object)
            return document.RootElement.Clone();

        foreach (var name in new[] { "data", "item", "result" })
        {
            if (document.RootElement.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Object)
                return prop.Clone();
        }

        return new JsonElement();
    }

    private async Task PostObjectAsync(string endpoint, object body)
    {
        if (Session.CompanyId == null)
            throw new InvalidOperationException("No company is assigned to the current login session.");

        if (string.IsNullOrWhiteSpace(Session.Token))
            throw new InvalidOperationException("No authentication token is available.");

        var url = $"{ApiUrl}/tenant/{Session.CompanyId.Value}/{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

        using var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"POST {url} failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{json}");
    }

    private async Task PutObjectAsync(string endpoint, object body)
    {
        if (Session.CompanyId == null)
            throw new InvalidOperationException("No company is assigned to the current login session.");

        if (string.IsNullOrWhiteSpace(Session.Token))
            throw new InvalidOperationException("No authentication token is available.");

        var url = $"{ApiUrl}/tenant/{Session.CompanyId.Value}/{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

        using var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"PUT {url} failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{json}");
    }

    private async Task DeleteAsync(string endpoint)
    {
        if (Session.CompanyId == null)
            throw new InvalidOperationException("No company is assigned to the current login session.");

        if (string.IsNullOrWhiteSpace(Session.Token))
            throw new InvalidOperationException("No authentication token is available.");

        var url = $"{ApiUrl}/tenant/{Session.CompanyId.Value}/{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

        using var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"DELETE {url} failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{json}");
    }

    private static List<string> DetermineColumns(List<JsonElement> records, string[] preferred)
    {
        var available = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            if (record.ValueKind != JsonValueKind.Object) continue;
            foreach (var p in record.EnumerateObject())
            {
                if (!available.ContainsKey(p.Name))
                    available[p.Name] = p.Name;
            }
        }

        var result = new List<string>();
        foreach (var pref in preferred)
        {
            if (available.TryGetValue(pref, out var realName) && !result.Contains(realName))
                result.Add(realName);
        }

        if (result.Count < 3)
        {
            foreach (var kv in available)
            {
                if (result.Count >= 8) break;
                if (kv.Key.Equals("CompanyId", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Equals("companyId", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!result.Contains(kv.Value))
                    result.Add(kv.Value);
            }
        }

        return result;
    }

    private static string FriendlyHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var chars = new List<char> { char.ToUpper(value[0]) };
        for (int i = 1; i < value.Length; i++)
        {
            if (char.IsUpper(value[i]) && !char.IsUpper(value[i - 1]))
                chars.Add(' ');
            chars.Add(value[i]);
        }
        return new string(chars.ToArray()).ToUpperInvariant();
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in element.EnumerateObject())
        {
            foreach (var name in names)
            {
                if (!p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
                if (p.Value.ValueKind == JsonValueKind.String) return p.Value.GetString();
                if (p.Value.ValueKind != JsonValueKind.Null) return p.Value.ToString();
            }
        }
        return null;
    }

    private static string GetDate(JsonElement element, params string[] names)
    {
        var value = GetString(element, names);
        if (string.IsNullOrWhiteSpace(value)) return "—";
        if (DateTime.TryParse(value, out var date))
            return date.ToString("MMM dd, yyyy");
        return value.Length > 16 ? value[..16] : value;
    }
}