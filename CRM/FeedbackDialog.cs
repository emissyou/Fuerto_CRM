namespace CRM_DesignServices.winforms;

public class FeedbackDialog : Form
{
    // Result payload
    public int OverallRating { get; private set; } = 5;
    public int TimelinessRating { get; private set; } = 5;
    public int CommunicationRating { get; private set; } = 5;
    public int ValueRating { get; private set; } = 5;
    public string Comments { get; private set; } = "";
    public string DesignLikes { get; private set; } = "";
    public string DesignImprovements { get; private set; } = "";
    public bool WouldRecommend { get; private set; } = true;

    private int _overall = 5, _time = 5, _comm = 5, _value = 5;

    private TextBox _txtComments = null!;
    private TextBox _txtLikes = null!;
    private TextBox _txtImprovements = null!;
    private CheckBox _chkRecommend = null!;

    public FeedbackDialog(string projectName)
    {
        Text = "Customer Feedback";
        Size = new Size(620, 640);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        var y = 20;

        Controls.Add(new Label
        {
            Text = $"Feedback for: {projectName}",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 32, 40),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 34;

        Controls.Add(new Label
        {
            Text = "Customer rating (1 = poor, 5 = excellent)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(110, 118, 132),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 30;

        AddRatingRow("Overall Design", y, v => _overall = v); y += 48;
        AddRatingRow("Timeliness", y, v => _time = v); y += 48;
        AddRatingRow("Communication", y, v => _comm = v); y += 48;
        AddRatingRow("Value for Money", y, v => _value = v); y += 60;

        Controls.Add(new Label
        {
            Text = "What did you like about the design?",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 22;
        _txtLikes = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 556,
            Height = 50,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "e.g. The minimalist layout, color palette..."
        };
        Controls.Add(_txtLikes);
        y += 62;

        Controls.Add(new Label
        {
            Text = "What could be improved?",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 22;
        _txtImprovements = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 556,
            Height = 50,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "Optional"
        };
        Controls.Add(_txtImprovements);
        y += 62;

        Controls.Add(new Label
        {
            Text = "Additional comments",
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, y)
        });
        y += 22;
        _txtComments = new TextBox
        {
            Left = 24,
            Top = y,
            Width = 556,
            Height = 50,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "Optional"
        };
        Controls.Add(_txtComments);
        y += 62;

        _chkRecommend = new CheckBox
        {
            Left = 24,
            Top = y,
            Width = 400,
            Text = "I would recommend Fuerto to others",
            Checked = true,
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.Add(_chkRecommend);
        y += 44;

        var btnOk = new Button
        {
            Text = "Submit Feedback",
            Left = 400,
            Top = y,
            Width = 130,
            Height = 36,
            BackColor = Color.FromArgb(255, 168, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "✕",
            Left = 538,
            Top = y,
            Width = 42,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            DialogResult = DialogResult.Cancel,
            TextAlign = ContentAlignment.MiddleCenter
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        Controls.Add(btnCancel);

        ClientSize = new Size(600, y + 60);

        btnOk.Click += (_, _) =>
        {
            OverallRating = _overall;
            TimelinessRating = _time;
            CommunicationRating = _comm;
            ValueRating = _value;
            DesignLikes = _txtLikes.Text.Trim();
            DesignImprovements = _txtImprovements.Text.Trim();
            Comments = _txtComments.Text.Trim();
            WouldRecommend = _chkRecommend.Checked;
        };
    }

    // Renders a label + 5 clickable stars
    private void AddRatingRow(string label, int top, Action<int> onChange)
    {
        Controls.Add(new Label
        {
            Text = label,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 78, 92),
            AutoSize = true,
            Location = new Point(24, top + 6)
        });

        var starsPanel = new Panel
        {
            Left = 220,
            Top = top,
            Width = 260,
            Height = 34,
            BackColor = Color.White
        };
        Controls.Add(starsPanel);

        var valueLabel = new Label
        {
            Left = 500,
            Top = top + 6,
            Width = 80,
            Height = 24,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 95, 0),
            Text = "5 / 5"
        };
        Controls.Add(valueLabel);

        var starLabels = new List<Label>();
        int current = 5;

        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            var star = new Label
            {
                Text = "★",
                Left = i * 42,
                Top = 0,
                Width = 40,
                Height = 34,
                Font = new Font("Segoe UI", 22f),
                ForeColor = Color.FromArgb(255, 168, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Tag = idx + 1
            };

            star.Click += (_, _) =>
            {
                current = (int)star.Tag!;
                foreach (var s in starLabels)
                {
                    int n = (int)s.Tag!;
                    s.ForeColor = n <= current
                        ? Color.FromArgb(255, 168, 0)
                        : Color.FromArgb(210, 214, 220);
                }
                valueLabel.Text = $"{current} / 5";
                onChange(current);
            };

            star.MouseEnter += (_, _) => star.ForeColor = Color.FromArgb(255, 200, 60);
            star.MouseLeave += (_, _) =>
            {
                int n = (int)star.Tag!;
                star.ForeColor = n <= current
                    ? Color.FromArgb(255, 168, 0)
                    : Color.FromArgb(210, 214, 220);
            };

            starsPanel.Controls.Add(star);
            starLabels.Add(star);
        }

        onChange(5);
    }

    public object ToPayload(int projectId) => new
    {
        projectId,
        overallRating = OverallRating,
        timelinessRating = TimelinessRating,
        communicationRating = CommunicationRating,
        valueRating = ValueRating,
        comments = Comments,
        designLikes = DesignLikes,
        designImprovements = DesignImprovements,
        wouldRecommend = WouldRecommend
    };
}