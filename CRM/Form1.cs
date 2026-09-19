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
        SelectNavigation("Dashboard");
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

        AddNavigationButton(menuPanel, "Dashboard", "⌂", 0);
        AddNavigationButton(menuPanel, "Customers", "♙", 52);
        AddNavigationButton(menuPanel, "Leads", "◆", 104);
        AddNavigationButton(menuPanel, "Projects", "▣", 156);
        AddNavigationButton(menuPanel, "Quotations", "▤", 208);
        AddNavigationButton(menuPanel, "Activities", "◷", 260);
        AddNavigationButton(menuPanel, "Designers", "✎", 312);
        AddNavigationButton(menuPanel, "Issues", "⚠", 364);
        AddNavigationButton(menuPanel, "Feedback", "★", 416);
        AddNavigationButton(menuPanel, "Reports", "▥", 468);
    }

    private void AddNavigationButton(Panel parent, string text, string icon, int top)
    {
        var button = new Button
        {
            Text = $"  {icon}     {text}",
            Left = 4,
            Top = top,
            Width = 216,
            Height = 44,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorWhite,
            ForeColor = Color.FromArgb(70, 78, 92),
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Cursor = Cursors.Hand,
            Tag = text
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ColorSidebarHover;

        button.Paint += (s, e) =>
        {
            var btn = (Button)s;
            if (btn.BackColor == ColorAccentSoft)
            {
                using var brush = new SolidBrush(ColorAccent);
                e.Graphics.FillRectangle(brush, 0, 6, 4, btn.Height - 12);
            }
        };

        button.Click += (_, _) => SelectNavigation(text);
        parent.Controls.Add(button);
        navigationButtons[text] = button;
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
    private void SelectNavigation(string page)
    {
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
            "Dashboard" => "Overview of your company operations",
            "Customers" => "Manage and view your client relationships",
            "Leads" => "Track potential customers and opportunities",
            "Projects" => "Monitor your interior design projects",
            "Quotations" => "Manage proposals and quotation records",
            "Activities" => "Track client interactions and follow-ups",
            "Reports" => "Review company performance and records",
            _ => "Fuerto Interior Design Services CRM"
        };

        if (page == "Dashboard")
            _ = LoadDashboardAsync();
        else if (page == "Customers")
            _ = LoadEntityPageAsync("Customers", "customers",
                new[] { "CustomerCode", "CustomerName", "ContactNumber", "EmailAddress", "Address", "IsActive", "Phone", "Email", "Name", "Code" });
        else if (page == "Leads")
            _ = LoadEntityPageAsync("Leads", "leads",
                new[] { "LeadId", "FirstName", "LastName", "Status", "LeadSource", "CreatedAt" });
        else if (page == "Projects")
            _ = LoadEntityPageAsync("Projects", "projects",
                new[] { "ProjectCode", "ProjectName", "DesignStage", "Status", "ProgressPercentage" });
        else if (page == "Quotations")
            _ = LoadEntityPageAsync("Quotations", "quotations",
                new[] { "QuotationNumber", "Status", "TotalAmount", "PaymentStatus", "CreatedAt" });
        else if (page == "Activities")
            _ = LoadEntityPageAsync("Activities", "activities",
                new[] { "ActivityType", "Subject", "Description", "CustomerName", "LeadName", "ProjectName", "ActivityDate", "Status", "Type", "Date" });
        else if (page == "Designers")
            BuildDesignersPage();
        else if (page == "Issues")
            _ = LoadEntityPageAsync("Issues", "issues",
                new[] { "ProjectIssueId", "Title", "IssueType", "Severity", "Status", "ReportedAt" });
        else if (page == "Feedback")
            _ = LoadEntityPageAsync("Feedback", "feedback",
                new[] { "ProjectFeedbackId", "ProjectId", "OverallRating", "TimelinessRating", "CommunicationRating", "ValueRating", "SubmittedAt" });
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

        var greeting = new Label
        {
            Text = "Good day, Admin 👋",
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 0)
        };
        contentPanel.Controls.Add(greeting);

        var greetingSub = new Label
        {
            Text = "Here is a quick overview of your interior design business.",
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Location = new Point(2, 36)
        };
        contentPanel.Controls.Add(greetingSub);

        var cards = new TableLayoutPanel
        {
            Location = new Point(0, 78),
            Size = new Size(Math.Max(900, contentPanel.ClientSize.Width - 20), 128),
            ColumnCount = 4,
            RowCount = 1,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        for (int i = 0; i < 4; i++)
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        contentPanel.Controls.Add(cards);

        AddMetricCard(cards, 0, "CUSTOMERS", customerCount, "Client records", "●");
        AddMetricCard(cards, 1, "LEADS", leadCount, "Potential opportunities", "◆");
        AddMetricCard(cards, 2, "PROJECTS", projectCount, "Design projects", "▣");
        AddMetricCard(cards, 3, "QUOTATIONS", quotationCount, "Quotation records", "▤");

        var analytics = new TableLayoutPanel
        {
            Location = new Point(0, 222),
            Size = new Size(Math.Max(900, contentPanel.ClientSize.Width - 20), 300),
            ColumnCount = 2,
            RowCount = 1,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
        analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
        contentPanel.Controls.Add(analytics);

        var recordChart = CreateRecordChartCard(customerCount, leadCount, projectCount, quotationCount);
        recordChart.Dock = DockStyle.Fill;
        recordChart.Margin = new Padding(0, 0, 12, 0);
        analytics.Controls.Add(recordChart, 0, 0);

        var statusChart = CreateActivityStatusCard(activities);
        statusChart.Dock = DockStyle.Fill;
        statusChart.Margin = new Padding(12, 0, 0, 0);
        analytics.Controls.Add(statusChart, 1, 0);

        var lower = new TableLayoutPanel
        {
            Location = new Point(0, 538),
            Size = new Size(Math.Max(900, contentPanel.ClientSize.Width - 20), 310),
            ColumnCount = 2,
            RowCount = 1,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
        contentPanel.Controls.Add(lower);

        var activityCard = CreateCard("Recent Activities", "Latest client interactions and follow-ups");
        activityCard.Dock = DockStyle.Fill;
        activityCard.Margin = new Padding(0, 0, 12, 0);
        lower.Controls.Add(activityCard, 0, 0);

        var activityList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BorderStyle = BorderStyle.None,
            BackColor = ColorWhite,
            Font = new Font("Segoe UI", 9.25f),
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            OwnerDraw = false
        };

        activityList.Columns.Add("ACTIVITY", 180);
        activityList.Columns.Add("DETAIL", 340);
        activityList.Columns.Add("DATE", 120);
        activityList.Columns.Add("STATUS", 100);

        foreach (var activity in activities.Take(8))
        {
            string subject = GetString(activity, "Subject", "ActivityType", "subject", "activityType") ?? "Activity";
            string detail = GetString(activity, "Description", "CustomerName", "LeadName", "ProjectName", "description") ?? "—";
            string date = GetDate(activity, "ActivityDate", "CreatedAt", "activityDate", "createdAt");
            string status = GetString(activity, "Status", "status") ?? "Recorded";

            var item = new ListViewItem(subject);
            item.SubItems.Add(detail);
            item.SubItems.Add(date);
            item.SubItems.Add(status);
            activityList.Items.Add(item);
        }

        if (activities.Count == 0)
        {
            var item = new ListViewItem("No activity records");
            item.SubItems.Add("No activities have been recorded yet.");
            item.SubItems.Add("—");
            item.SubItems.Add("—");
            activityList.Items.Add(item);
        }

        activityCard.Controls.Add(activityList);
        activityList.BringToFront();

        var overviewCard = CreateCard("CRM Overview", "Current database records");
        overviewCard.Dock = DockStyle.Fill;
        overviewCard.Margin = new Padding(12, 0, 0, 0);
        lower.Controls.Add(overviewCard, 1, 0);

        var overviewPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 14, 18, 10),
            BackColor = ColorWhite
        };
        overviewCard.Controls.Add(overviewPanel);

        AddOverviewRow(overviewPanel, "Customers", customerCount, 0);
        AddOverviewRow(overviewPanel, "Leads", leadCount, 52);
        AddOverviewRow(overviewPanel, "Projects", projectCount, 104);
        AddOverviewRow(overviewPanel, "Quotations", quotationCount, 156);
        AddOverviewRow(overviewPanel, "Activities", activities.Count, 208);

        void OnResize(object? s, EventArgs e)
        {
            if (cards.IsDisposed) return;
            int w = Math.Max(900, contentPanel.ClientSize.Width - 20);
            cards.Width = w;
            analytics.Width = w;
            lower.Width = w;
        }
        contentPanel.Resize += OnResize;
    }

    private void AddMetricCard(TableLayoutPanel table, int column, string title, int value, string subtitle, string icon)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorWhite,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(20)
        };

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorCardBorder, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);

            using var accent = new SolidBrush(ColorAccent);
            e.Graphics.FillRectangle(accent, 0, 0, card.Width, 3);
        };

        table.Controls.Add(card, column, 0);

        card.Controls.Add(new Label
        {
            Text = icon,
            ForeColor = ColorAccent,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 16)
        });

        card.Controls.Add(new Label
        {
            Text = title,
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(46, 20)
        });

        card.Controls.Add(new Label
        {
            Text = value.ToString("N0"),
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 48)
        });

        card.Controls.Add(new Label
        {
            Text = subtitle,
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 8.25f),
            AutoSize = true,
            Location = new Point(22, 90)
        });
    }

    private void AddOverviewRow(Panel parent, string name, int value, int top)
    {
        var row = new Panel
        {
            Left = 0,
            Top = top,
            Width = Math.Max(180, parent.ClientSize.Width - 4),
            Height = 44,
            BackColor = Color.FromArgb(248, 249, 252),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        parent.Controls.Add(row);

        row.Controls.Add(new Label
        {
            Text = name,
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 9.25f),
            AutoSize = true,
            Location = new Point(14, 12)
        });

        var valueLabel = new Label
        {
            Text = value.ToString("N0"),
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        row.Controls.Add(valueLabel);
        valueLabel.Left = row.Width - valueLabel.Width - 16;
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

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56
        };
        contentPanel.Controls.Add(header);

        header.Controls.Add(new Label
        {
            Text = $"{records.Count:N0} record{(records.Count == 1 ? "" : "s")}",
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            Location = new Point(0, 16)
        });

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

            // Still add workflow buttons even if empty
            AddWorkflowButtons(header, endpoint, null!, () => currentRecords);
            return;
        }

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ColorWhite,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 42,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };
        grid.RowTemplate.Height = 46;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorHeaderBg,
            ForeColor = Color.FromArgb(85, 93, 106),
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
            Padding = new Padding(12, 0, 12, 0),
            SelectionBackColor = ColorHeaderBg,
            SelectionForeColor = Color.FromArgb(85, 93, 106)
        };

        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorWhite,
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(12, 0, 12, 0),
            SelectionBackColor = ColorAccentSoft,
            SelectionForeColor = ColorText
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 251, 253)
        };

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

        foreach (var record in currentRecords)
        {
            var values = new object[columns.Count];
            for (int i = 0; i < columns.Count; i++)
                values[i] = GetValueFromJson(record, columns[i]) ?? "";
            grid.Rows.Add(values);
        }

        searchBox.TextChanged += (_, _) =>
        {
            var search = searchBox.Text.Trim();
            grid.Rows.Clear();

            var filtered = string.IsNullOrWhiteSpace(search)
                ? records
                : records.Where(r =>
                {
                    foreach (var col in columns)
                    {
                        var val = GetValueFromJson(r, col)?.ToString();
                        if (val != null && val.Contains(search, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    return false;
                }).ToList();

            currentRecords = filtered.ToList();

            foreach (var record in filtered)
            {
                var values = new object[columns.Count];
                for (int i = 0; i < columns.Count; i++)
                    values[i] = GetValueFromJson(record, columns[i]) ?? "";
                grid.Rows.Add(values);
            }
        };

        // NEW button
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

                dlg.Controls.Add(new Label { Text = "Code", Left = 20, Top = 6 });
                dlg.Controls.Add(new Label { Text = "Name", Left = 20, Top = 46 });
                dlg.Controls.Add(new Label { Text = "Customer", Left = 20, Top = 86 });
                dlg.Controls.Add(new Label { Text = "Type", Left = 20, Top = 126 });
                dlg.Controls.Add(new Label { Text = "Location", Left = 260, Top = 126 });
                dlg.Controls.Add(new Label { Text = "Start", Left = 20, Top = 206 });
                dlg.Controls.Add(new Label { Text = "End", Left = 260, Top = 206 });

                dlg.Controls.Add(txtCode);
                dlg.Controls.Add(txtName);
                dlg.Controls.Add(cmbCustomer);
                dlg.Controls.Add(txtType);
                dlg.Controls.Add(txtLocation);
                dlg.Controls.Add(dtStart);
                dlg.Controls.Add(dtEnd);
                dlg.Controls.Add(txtNotes);
                dlg.Controls.Add(btnOk);
                dlg.Controls.Add(btnCancel);

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

                // Show a note instead of an editable textbox
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

                dlg.Controls.Add(new Label { Text = "Project", Left = 20, Top = 6 });
                dlg.Controls.Add(new Label { Text = "Quotation #", Left = 20, Top = 46 });
                dlg.Controls.Add(new Label { Text = "Subtotal", Left = 20, Top = 86 });
                dlg.Controls.Add(new Label { Text = "Discount", Left = 260, Top = 86 });

                dlg.Controls.Add(cmbProject);
                dlg.Controls.Add(lblNumberInfo);
                dlg.Controls.Add(txtSubtotal);
                dlg.Controls.Add(txtDiscount);
                dlg.Controls.Add(txtNotes);
                dlg.Controls.Add(btnOk);
                dlg.Controls.Add(btnCancel);

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
                        // quotationNumber removed — API generates it automatically
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

            // Fallback create
            using var dlgFallback = new Form { Text = $"New {title.TrimEnd('s')}", Size = new Size(420, 300), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
            var txtFirst = new TextBox { Left = 20, Top = 24, Width = 360, PlaceholderText = "First name" };
            var txtLast = new TextBox { Left = 20, Top = 64, Width = 360, PlaceholderText = "Last name" };
            var txtEmail = new TextBox { Left = 20, Top = 104, Width = 360, PlaceholderText = "Email (optional)" };
            var txtPhone = new TextBox { Left = 20, Top = 144, Width = 360, PlaceholderText = "Phone (optional)" };
            var btnOkF = new Button { Text = "Create", Left = 200, Width = 80, Top = 200, DialogResult = DialogResult.OK };
            var btnCancelF = new Button { Text = "Cancel", Left = 292, Width = 80, Top = 200, DialogResult = DialogResult.Cancel };

            dlgFallback.Controls.Add(new Label { Text = "First name", Left = 20, Top = 6 });
            dlgFallback.Controls.Add(new Label { Text = "Last name", Left = 20, Top = 46 });
            dlgFallback.Controls.Add(new Label { Text = "Email", Left = 20, Top = 86 });
            dlgFallback.Controls.Add(new Label { Text = "Phone", Left = 20, Top = 126 });

            dlgFallback.Controls.Add(txtFirst);
            dlgFallback.Controls.Add(txtLast);
            dlgFallback.Controls.Add(txtEmail);
            dlgFallback.Controls.Add(txtPhone);
            dlgFallback.Controls.Add(btnOkF);
            dlgFallback.Controls.Add(btnCancelF);

            if (dlgFallback.ShowDialog(this) == DialogResult.OK)
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

        // EDIT button
        editButton.Click += async (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) { MessageBox.Show("Select a record to edit.", "Edit"); return; }
            var idx = grid.SelectedRows[0].Index;
            if (idx < 0 || idx >= currentRecords.Count) return;

            var record = currentRecords[idx];
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

            // Simple generic edit (email/phone/name)
            using var dlg = new Form { Text = $"Edit", Size = new Size(420, 360), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
            var txtFirst = new TextBox { Left = 20, Top = 24, Width = 360, Text = GetString(elem, "FirstName", "firstName") ?? "" };
            var txtLast = new TextBox { Left = 20, Top = 64, Width = 360, Text = GetString(elem, "LastName", "lastName") ?? "" };
            var txtEmail = new TextBox { Left = 20, Top = 104, Width = 360, Text = GetString(elem, "Email", "email") ?? "" };
            var txtPhone = new TextBox { Left = 20, Top = 144, Width = 360, Text = GetString(elem, "Phone", "phone") ?? "" };
            var chkActive = new CheckBox { Left = 20, Top = 184, Width = 200, Checked = (GetValueFromJson(elem, "IsActive") as bool?) ?? true, Text = "Active" };
            var btnOk = new Button { Text = "Save", Left = 200, Width = 80, Top = 240, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Left = 292, Width = 80, Top = 240, DialogResult = DialogResult.Cancel };

            dlg.Controls.Add(new Label { Text = "First name", Left = 20, Top = 6 });
            dlg.Controls.Add(new Label { Text = "Last name", Left = 20, Top = 46 });
            dlg.Controls.Add(new Label { Text = "Email", Left = 20, Top = 86 });
            dlg.Controls.Add(new Label { Text = "Phone", Left = 20, Top = 126 });

            dlg.Controls.Add(txtFirst);
            dlg.Controls.Add(txtLast);
            dlg.Controls.Add(txtEmail);
            dlg.Controls.Add(txtPhone);
            dlg.Controls.Add(chkActive);
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancel);

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

        // DELETE button
        deleteButton.Click += async (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) { MessageBox.Show("Select a record to delete.", "Delete"); return; }
            var idx = grid.SelectedRows[0].Index;
            if (idx < 0 || idx >= currentRecords.Count) return;

            var record = currentRecords[idx];
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

        // Attach workflow buttons for this page
        List<JsonElement> GetRecords() => currentRecords;
        AddWorkflowButtons(header, endpoint, grid, GetRecords);
    }

    // =========================================================
    // WORKFLOW BUTTONS — added per page
    // =========================================================
    private void AddWorkflowButtons(
        Panel header,
        string endpoint,
        DataGridView grid,
        Func<List<JsonElement>> getCurrentRecords)
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
                if (grid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Select a lead to convert.", "Convert");
                    return;
                }

                var records = getCurrentRecords();
                var idx = grid.SelectedRows[0].Index;
                if (idx < 0 || idx >= records.Count) return;

                var lead = records[idx];
                var leadId = GetValueFromJson(lead, "LeadId")?.ToString();
                if (leadId == null) return;

                var name = $"{GetString(lead, "FirstName")} {GetString(lead, "LastName")}".Trim();

                using var dlg = new ConvertLeadDialog(name);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

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
                if (grid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Select an issue.", "Resolve");
                    return;
                }

                var records = getCurrentRecords();
                var idx = grid.SelectedRows[0].Index;
                if (idx < 0 || idx >= records.Count) return;

                var issue = records[idx];
                var issueId = GetValueFromJson(issue, "ProjectIssueId")?.ToString();
                var title = GetString(issue, "Title") ?? "Issue";
                if (issueId == null) return;

                using var dlg = new ResolveIssueDialog(title);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    var url = $"{ApiUrl}/tenant/{Session.CompanyId}/issues/{issueId}/resolve";
                    using var req = new HttpRequestMessage(HttpMethod.Post, url);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
                    req.Content = new StringContent(
                        JsonSerializer.Serialize(dlg.ToPayload()),
                        System.Text.Encoding.UTF8,
                        "application/json");

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
    // WORKFLOW — HTTP + Handlers
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
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");
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

    private async Task RunQuotationAction(
        DataGridView grid,
        Func<List<JsonElement>> getCurrentRecords,
        string actionLabel,
        string endpointAction)
    {
        if (grid.SelectedRows.Count == 0)
        {
            MessageBox.Show($"Select a quotation to {actionLabel.ToLower()}.", actionLabel);
            return;
        }

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

        var btnOk = new Button
        {
            Text = "Record",
            Left = 240,
            Top = 232,
            Width = 90,
            Height = 34,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;
        dlg.Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "Cancel",
            Left = 338,
            Top = 232,
            Width = 82,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            DialogResult = DialogResult.Cancel
        };
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
    // REPORTS
    // =========================================================
    private void BuildReportsPage()
    {
        contentPanel.Controls.Clear();
        contentPanel.Padding = new Padding(32, 20, 32, 32);

        contentPanel.Controls.Add(new Label
        {
            Text = "CRM Reports",
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 0)
        });

        contentPanel.Controls.Add(new Label
        {
            Text = "Review company records and operational information.",
            ForeColor = ColorMuted,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Location = new Point(2, 38)
        });

        var reportCard = CreateCard("Available CRM Data", "Live information is retrieved from your authenticated tenant database.");
        reportCard.Location = new Point(0, 90);
        reportCard.Size = new Size(720, 280);
        contentPanel.Controls.Add(reportCard);

        reportCard.Controls.Add(new Label
        {
            Text =
                "The CRM currently provides live data for:\n\n" +
                "•  Customer records\n" +
                "•  Lead pipeline\n" +
                "•  Interior design projects\n" +
                "•  Quotation records\n" +
                "•  Client activities and follow-ups\n\n" +
                "Detailed printable reports can be added after the core modules are completed.",
            ForeColor = ColorText,
            Font = new Font("Segoe UI", 10.25f),
            AutoSize = true,
            Location = new Point(28, 28)
        });
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

    private Panel CreateRecordChartCard(int customers, int leads, int projects, int quotations)
    {
        var card = CreateCard("Records Overview", "Current records by CRM module");
        var chart = new Panel { Dock = DockStyle.Fill, BackColor = ColorWhite, Padding = new Padding(18, 72, 18, 20) };

        int[] values = { customers, leads, projects, quotations };
        string[] labels = { "Customers", "Leads", "Projects", "Quotations" };

        chart.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int left = 40, bottom = chart.Height - 44;
            int chartHeight = Math.Max(80, chart.Height - 110);
            int chartWidth = Math.Max(300, chart.Width - 70);
            int max = Math.Max(1, values.Max());
            int slot = Math.Max(60, chartWidth / values.Length);
            int barWidth = Math.Min(46, slot - 22);

            using var axisPen = new Pen(Color.FromArgb(228, 231, 236), 1);
            g.DrawLine(axisPen, left, bottom, left + chartWidth, bottom);

            for (int i = 0; i < values.Length; i++)
            {
                int x = left + i * slot + Math.Max(0, (slot - barWidth) / 2);
                int h = values[i] == 0 ? 4 : (int)(values[i] / (double)max * (chartHeight - 18));
                int y = bottom - h;

                using var brush = new SolidBrush(ColorAccent);
                g.FillRectangle(brush, x, y, barWidth, h);

                TextRenderer.DrawText(g, values[i].ToString(), new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    new Rectangle(x - 10, y - 24, barWidth + 20, 20), ColorText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(g, labels[i], new Font("Segoe UI", 7.75f),
                    new Rectangle(x - 28, bottom + 8, barWidth + 56, 22), ColorMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        };

        card.Controls.Add(chart);
        chart.BringToFront();
        return card;
    }

    private Panel CreateActivityStatusCard(List<JsonElement> activities)
    {
        var card = CreateCard("Activity Status", "Distribution of recorded activities");
        var chart = new Panel { Dock = DockStyle.Fill, BackColor = ColorWhite, Padding = new Padding(12, 68, 12, 12) };

        var groups = activities
            .Select(a => GetString(a, "Status", "status") ?? "Recorded")
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(4)
            .ToList();

        chart.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int total = Math.Max(1, activities.Count);
            int cx = Math.Max(75, chart.Width / 2);
            int cy = 118;
            int diameter = Math.Min(130, Math.Max(95, chart.Width - 60));
            var rect = new Rectangle(cx - diameter / 2, cy - diameter / 2, diameter, diameter);

            Color[] colors = { ColorAccent, Color.FromArgb(105, 120, 235), Color.FromArgb(65, 170, 155), Color.FromArgb(235, 115, 85) };

            if (activities.Count == 0)
            {
                using var pen = new Pen(Color.FromArgb(230, 233, 238), 20);
                g.DrawEllipse(pen, rect);
                TextRenderer.DrawText(g, "0", new Font("Segoe UI", 16, FontStyle.Bold),
                    new Rectangle(cx - 30, cy - 22, 60, 44), ColorText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else
            {
                float start = -90;
                for (int i = 0; i < groups.Count; i++)
                {
                    float sweep = groups[i].Count() / (float)total * 360f;
                    using var pen = new Pen(colors[i % colors.Length], 20);
                    g.DrawArc(pen, rect, start, sweep);
                    start += sweep;
                }

                TextRenderer.DrawText(g, activities.Count.ToString("N0"), new Font("Segoe UI", 15, FontStyle.Bold),
                    new Rectangle(cx - 35, cy - 24, 70, 28), ColorText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(g, "Activities", new Font("Segoe UI", 7.5f),
                    new Rectangle(cx - 45, cy + 6, 90, 18), ColorMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            int legendY = Math.Min(chart.Height - 28, 190);
            using var legendFont = new Font("Segoe UI", 7.75f);

            for (int i = 0; i < groups.Count; i++)
            {
                int y = legendY + i * 22;
                using var brush = new SolidBrush(colors[i % 4]);
                g.FillEllipse(brush, 14, y + 5, 8, 8);
                TextRenderer.DrawText(g, $"{groups[i].Key}  {groups[i].Count()}", legendFont,
                    new Rectangle(28, y, chart.Width - 40, 18), ColorMuted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        };

        card.Controls.Add(chart);
        chart.BringToFront();
        return card;
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
            throw new HttpRequestException(
                $"Endpoint: {url}\n\nStatus: {(int)response.StatusCode} {response.ReasonPhrase}\n\nResponse:\n{json}");

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind == JsonValueKind.Array)
            return document.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();

        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "data", "items", "results", endpoint })
            {
                if (document.RootElement.TryGetProperty(name, out var prop) &&
                    prop.ValueKind == JsonValueKind.Array)
                {
                    return prop.EnumerateArray().Select(e => e.Clone()).ToList();
                }
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
            throw new HttpRequestException(
                $"Endpoint: {url}\n\nStatus: {(int)response.StatusCode} {response.ReasonPhrase}\n\nResponse:\n{json}");

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

    // =========================================================
    // FEEDBACK + ISSUES — UI Actions
    // =========================================================
    private async Task ShowFeedbackDialog(
        DataGridView grid, Func<List<JsonElement>> getCurrentRecords)
    {
        if (grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Select a project first.", "Feedback");
            return;
        }

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
            MessageBox.Show(
                "Feedback can only be submitted for Completed projects.\n\n" +
                $"Current stage: {stage}",
                "Feedback");
            return;
        }

        using var dlg = new FeedbackDialog(projName);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var url = $"{ApiUrl}/tenant/{Session.CompanyId}/projects/{projId}/feedback";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
            req.Content = new StringContent(
                JsonSerializer.Serialize(dlg.ToPayload(int.Parse(projId))),
                System.Text.Encoding.UTF8,
                "application/json");

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

            MessageBox.Show("Thank you for your feedback!", "Success");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Feedback failed:\n\n" + ex.Message, "Error");
        }
    }

    private async Task ShowNewIssueDialog(
        DataGridView grid, Func<List<JsonElement>> getCurrentRecords)
    {
        if (grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Select a project first.", "Report Issue");
            return;
        }

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
            req.Content = new StringContent(
                JsonSerializer.Serialize(dlg.ToPayload(int.Parse(projId))),
                System.Text.Encoding.UTF8,
                "application/json");

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

            MessageBox.Show("Issue reported successfully.", "Success");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Report failed:\n\n" + ex.Message, "Error");
        }
    }

    // =========================================================
    // DESIGNERS PAGE
    // =========================================================
    private void BuildDesignersPage()
    {
        contentPanel.Controls.Clear();
        contentPanel.Padding = new Padding(32, 20, 32, 32);

        var page = new DesignersPage(ApiUrl, _httpClient);
        contentPanel.Controls.Add(page);

        // Trigger the initial load after the panel is attached
        _ = page.LoadAsync();
    }
}