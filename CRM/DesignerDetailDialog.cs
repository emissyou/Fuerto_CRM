using System.Net.Http.Headers;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public class DesignerDetailDialog : Form
{
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private readonly string _designerId;

    private Label _lblName = null!;
    private Label _lblEmail = null!;
    private Label _lblStats = null!;
    private Label _lblStars = null!;
    private TabControl _tabs = null!;
    private DataGridView _gridProjects = null!;
    private DataGridView _gridFeedback = null!;
    private DataGridView _gridIssues = null!;
    private Label _lblStatus = null!;

    public DesignerDetailDialog(string apiUrl, HttpClient http, string designerId)
    {
        _apiUrl = apiUrl;
        _http = http;
        _designerId = designerId;

        Text = "Designer Profile";
        Size = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        MinimumSize = new Size(800, 600);

        // ---- Header panel ----
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 130,
            BackColor = Color.FromArgb(252, 250, 245),
            Padding = new Padding(24, 16, 24, 16)
        };
        Controls.Add(header);

        _lblName = new Label
        {
            Text = "Designer",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, 20)
        };
        header.Controls.Add(_lblName);

        _lblEmail = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(26, 56)
        };
        header.Controls.Add(_lblEmail);

        _lblStars = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 18f),
            ForeColor = Color.FromArgb(255, 168, 0),
            AutoSize = true,
            Location = new Point(24, 82)
        };
        header.Controls.Add(_lblStars);

        _lblStats = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(220, 86)
        };
        header.Controls.Add(_lblStats);

        // ---- Status label (loading / error) ----
        _lblStatus = new Label
        {
            Text = "Loading...",
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(110, 118, 132),
            Font = new Font("Segoe UI", 9f, FontStyle.Italic)
        };
        Controls.Add(_lblStatus);

        // ---- Tabs ----
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_tabs);

        // Tab: Projects
        var tabProjects = new TabPage("Projects") { BackColor = Color.White };
        _tabs.TabPages.Add(tabProjects);
        _gridProjects = CreateGrid();
        _gridProjects.Columns.Add("projectCode", "CODE");
        _gridProjects.Columns.Add("projectName", "PROJECT");
        _gridProjects.Columns.Add("projectType", "TYPE");
        _gridProjects.Columns.Add("designStage", "STAGE");
        _gridProjects.Columns.Add("progress", "PROGRESS %");
        _gridProjects.Columns.Add("assignedAt", "ASSIGNED");
        _gridProjects.Columns.Add("completedAt", "COMPLETED");
        tabProjects.Controls.Add(_gridProjects);

        // Tab: Feedback
        var tabFeedback = new TabPage("Feedback") { BackColor = Color.White };
        _tabs.TabPages.Add(tabFeedback);
        _gridFeedback = CreateGrid();
        _gridFeedback.Columns.Add("submittedAt", "DATE");
        _gridFeedback.Columns.Add("overall", "OVERALL");
        _gridFeedback.Columns.Add("time", "TIMELINESS");
        _gridFeedback.Columns.Add("comm", "COMM");
        _gridFeedback.Columns.Add("value", "VALUE");
        _gridFeedback.Columns.Add("recommend", "RECOMMEND");
        _gridFeedback.Columns.Add("comments", "COMMENTS");
        tabFeedback.Controls.Add(_gridFeedback);

        // Tab: Issues
        var tabIssues = new TabPage("Issues") { BackColor = Color.White };
        _tabs.TabPages.Add(tabIssues);
        _gridIssues = CreateGrid();
        _gridIssues.Columns.Add("reportedAt", "REPORTED");
        _gridIssues.Columns.Add("type", "TYPE");
        _gridIssues.Columns.Add("severity", "SEVERITY");
        _gridIssues.Columns.Add("status", "STATUS");
        _gridIssues.Columns.Add("title", "TITLE");
        _gridIssues.Columns.Add("resolvedAt", "RESOLVED");
        tabIssues.Controls.Add(_gridIssues);

        // ---- Close button at bottom ----
        var btnClose = new Button
        {
            Text = "Close",
            Dock = DockStyle.Bottom,
            Height = 40,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);

        Load += async (_, _) => await LoadDetailAsync();
    }

    private DataGridView CreateGrid()
    {
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
            ColumnHeadersHeight = 40
        };
        grid.RowTemplate.Height = 42;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(249, 250, 252),
            ForeColor = Color.FromArgb(85, 93, 106),
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0),
            SelectionBackColor = Color.FromArgb(249, 250, 252),
            SelectionForeColor = Color.FromArgb(85, 93, 106)
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
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 251, 253)
        };
        return grid;
    }

    private async Task LoadDetailAsync()
    {
        try
        {
            var url = $"{_apiUrl}/tenant/{Session.CompanyId}/designers/{_designerId}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            using var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _lblStatus.Text = $"Failed to load: {(int)res.StatusCode} {res.ReasonPhrase}";
                _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
                return;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _lblName.Text = root.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "Designer" : "Designer";
            _lblEmail.Text = root.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";

            int feedbackCount = root.TryGetProperty("feedbackCount", out var fc) ? fc.GetInt32() : 0;
            double? overallScore = root.TryGetProperty("overallScore", out var os) && os.ValueKind != JsonValueKind.Null
                ? os.GetDouble() : null;

            if (overallScore.HasValue && feedbackCount > 0)
            {
                int full = (int)Math.Round(overallScore.Value);
                _lblStars.Text = new string('★', full) + new string('☆', 5 - full);
                _lblStats.Text = $"Overall: {overallScore.Value:F2} / 5  ({feedbackCount} rating{(feedbackCount == 1 ? "" : "s")})";
            }
            else
            {
                _lblStars.Text = "☆☆☆☆☆";
                _lblStats.Text = "No ratings yet";
            }

            // ---- Projects ----
            _gridProjects.Rows.Clear();
            if (root.TryGetProperty("projects", out var projs) && projs.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in projs.EnumerateArray())
                {
                    _gridProjects.Rows.Add(
                        GetStr(p, "projectCode"),
                        GetStr(p, "projectName"),
                        GetStr(p, "projectType"),
                        GetStr(p, "designStage"),
                        GetStr(p, "progressPercentage"),
                        GetDateStr(p, "designerAssignedAt"),
                        GetDateStr(p, "designCompletionDate"));
                }
            }

            // ---- Feedback ----
            _gridFeedback.Rows.Clear();
            if (root.TryGetProperty("feedback", out var fbs) && fbs.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in fbs.EnumerateArray())
                {
                    _gridFeedback.Rows.Add(
                        GetDateStr(f, "submittedAt"),
                        Stars(GetInt(f, "overallRating")) + " " + GetInt(f, "overallRating"),
                        Stars(GetInt(f, "timelinessRating")) + " " + GetInt(f, "timelinessRating"),
                        Stars(GetInt(f, "communicationRating")) + " " + GetInt(f, "communicationRating"),
                        Stars(GetInt(f, "valueRating")) + " " + GetInt(f, "valueRating"),
                        GetBool(f, "wouldRecommend") ? "✓ Yes" : "✗ No",
                        GetStr(f, "comments"));
                }
            }

            // ---- Issues ----
            _gridIssues.Rows.Clear();
            if (root.TryGetProperty("issues", out var iss) && iss.ValueKind == JsonValueKind.Array)
            {
                foreach (var i in iss.EnumerateArray())
                {
                    _gridIssues.Rows.Add(
                        GetDateStr(i, "reportedAt"),
                        GetStr(i, "issueType"),
                        GetStr(i, "severity"),
                        GetStr(i, "status"),
                        GetStr(i, "title"),
                        GetDateStr(i, "resolvedAt"));
                }
            }

            _lblStatus.Text = $"Projects: {_gridProjects.Rows.Count}   Feedback: {_gridFeedback.Rows.Count}   Issues: {_gridIssues.Rows.Count}";
            _lblStatus.ForeColor = Color.FromArgb(110, 118, 132);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error: " + ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(200, 55, 55);
        }
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

    private static bool GetBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return false;
        return p.ValueKind == JsonValueKind.True;
    }

    private static string GetDateStr(JsonElement el, string name)
    {
        var s = GetStr(el, name);
        if (string.IsNullOrWhiteSpace(s)) return "—";
        if (DateTime.TryParse(s, out var d)) return d.ToString("MMM dd, yyyy");
        return s;
    }

    private static string Stars(int n)
    {
        if (n < 0) n = 0;
        if (n > 5) n = 5;
        return new string('★', n) + new string('☆', 5 - n);
    }
}