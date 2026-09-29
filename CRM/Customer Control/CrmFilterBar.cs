using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

public class CrmFilterBar : Panel
{
    public class FilterOption
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string[] Values { get; set; } = Array.Empty<string>();
    }

    public event EventHandler? FiltersChanged;
    public event EventHandler? ActionsClicked;
    public event EventHandler<string>? ViewModeChanged;

    private readonly Panel _searchContainer;
    private readonly TextBox _searchBox;
    private readonly FlowLayoutPanel _filterFlow;
    private readonly Label _lblCount;
    private readonly Button _btnClear;
    private readonly Panel _rightActionsPanel;
    private readonly Button _btnActions;
    private readonly Label _lblNotification;
    private readonly Button _btnListView;
    private readonly Button _btnGridView;
    private readonly Button _btnChartView;
    private readonly Dictionary<string, ComboBox> _dropdowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FilterOption> _options = new();

    public string CurrentViewMode { get; private set; } = "List";

    public string SearchText => _searchBox.Text.Trim();

    public IReadOnlyList<string> FilterKeys => _options.Select(o => o.Key).ToList();

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(_searchBox.Text) ||
        _dropdowns.Values.Any(c => c.SelectedIndex > 0);

    public CrmFilterBar(string placeholder = "Search...")
    {
        Dock = DockStyle.Top;
        Height = 52;
        BackColor = Color.Transparent;
        Padding = new Padding(0, 6, 0, 8);

        // 1. Search Box container with modern border
        _searchContainer = new Panel
        {
            Width = 240,
            Height = 36,
            Location = new Point(0, 8),
            BackColor = Color.White
        };
        _searchContainer.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(0, 0, _searchContainer.Width - 1, _searchContainer.Height - 1), 6);
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1.25f);
            g.DrawPath(pen, path);
        };

        var lblSearchIcon = new Label
        {
            Text = "🔍",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = false,
            Size = new Size(26, 26),
            Location = new Point(6, 6),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _searchContainer.Controls.Add(lblSearchIcon);

        _searchBox = new TextBox
        {
            PlaceholderText = placeholder,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.None,
            Location = new Point(34, 8),
            Width = 196,
            BackColor = Color.White
        };
        _searchBox.TextChanged += (_, _) =>
        {
            UpdateClearButtonState();
            FiltersChanged?.Invoke(this, EventArgs.Empty);
        };
        _searchContainer.Controls.Add(_searchBox);
        Controls.Add(_searchContainer);

        // 2. Right Actions Panel matching reference mockup:
        // [ ☰ List ] [ ⊞ Grid ] [ 📊 Chart ]   ✉(2)   [ Actions ⌵ ]
        _rightActionsPanel = new Panel
        {
            Height = 38,
            Width = 330,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.Transparent
        };
        Controls.Add(_rightActionsPanel);

        // View mode tabs
        _btnListView = MakeTabButton("☰", 0, true);
        _btnListView.Click += (_, _) => SetViewMode("List");
        _rightActionsPanel.Controls.Add(_btnListView);

        _btnGridView = MakeTabButton("⊞", 38, false);
        _btnGridView.Click += (_, _) => SetViewMode("Grid");
        _rightActionsPanel.Controls.Add(_btnGridView);

        _btnChartView = MakeTabButton("📊", 76, false);
        _btnChartView.Click += (_, _) => SetViewMode("Chart");
        _rightActionsPanel.Controls.Add(_btnChartView);

        // Notification Mail badge
        _lblNotification = new Label
        {
            Text = "✉ 2",
            AutoSize = false,
            Width = 44,
            Height = 32,
            Left = 120,
            Top = 3,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(244, 63, 94),
            BackColor = Color.FromArgb(255, 241, 242),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        _lblNotification.Paint += (_, pe) =>
        {
            using var p = new Pen(Color.FromArgb(254, 202, 202), 1f);
            pe.Graphics.DrawRectangle(p, 0, 0, _lblNotification.Width - 1, _lblNotification.Height - 1);
        };
        _rightActionsPanel.Controls.Add(_lblNotification);

        // Solid Blue Actions Button
        _btnActions = new Button
        {
            Text = "Actions ⌵",
            Width = 98,
            Height = 34,
            Left = 172,
            Top = 2,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(2, 132, 199),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnActions.FlatAppearance.BorderSize = 0;
        _btnActions.Click += (s, e) => ActionsClicked?.Invoke(s, e);
        _rightActionsPanel.Controls.Add(_btnActions);

        // Record Count Badge
        _lblCount = new Label
        {
            AutoSize = false,
            Width = 140,
            Height = 34,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(Math.Max(300, ClientSize.Width - 480), 9)
        };
        Controls.Add(_lblCount);

        // Clear button
        _btnClear = new Button
        {
            Text = "✕ Clear",
            Width = 76,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Visible = false
        };
        _btnClear.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnClear.Click += (_, _) => ResetFilters();

        // Filter dropdowns flow
        _filterFlow = new FlowLayoutPanel
        {
            Location = new Point(248, 6),
            Height = 40,
            AutoSize = false,
            WrapContents = false,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(_filterFlow);

        RepositionControls();
        Resize += (_, _) => RepositionControls();
    }

    private void RepositionControls()
    {
        if (IsDisposed) return;
        _rightActionsPanel.Left = Math.Max(200, ClientSize.Width - _rightActionsPanel.Width);
        _lblCount.Left = Math.Max(250, _rightActionsPanel.Left - _lblCount.Width - 10);
        _filterFlow.Width = Math.Max(100, _lblCount.Left - _filterFlow.Left - 8);
    }

    private Button MakeTabButton(string icon, int left, bool active)
    {
        var btn = new Button
        {
            Text = icon,
            Width = 34,
            Height = 34,
            Left = left,
            Top = 2,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = active ? Color.FromArgb(2, 132, 199) : Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Paint += (_, pe) =>
        {
            if (btn.Tag as string == "active" || (btn.Tag == null && active))
            {
                using var pen = new Pen(Color.FromArgb(2, 132, 199), 2.5f);
                pe.Graphics.DrawLine(pen, 4, btn.Height - 2, btn.Width - 4, btn.Height - 2);
            }
        };
        btn.Tag = active ? "active" : "inactive";
        return btn;
    }

    private void SetViewMode(string mode)
    {
        CurrentViewMode = mode;
        _btnListView.Tag = (mode == "List") ? "active" : "inactive";
        _btnGridView.Tag = (mode == "Grid") ? "active" : "inactive";
        _btnChartView.Tag = (mode == "Chart") ? "active" : "inactive";

        _btnListView.ForeColor = (mode == "List") ? Color.FromArgb(2, 132, 199) : Color.FromArgb(148, 163, 184);
        _btnGridView.ForeColor = (mode == "Grid") ? Color.FromArgb(2, 132, 199) : Color.FromArgb(148, 163, 184);
        _btnChartView.ForeColor = (mode == "Chart") ? Color.FromArgb(2, 132, 199) : Color.FromArgb(148, 163, 184);

        _btnListView.Invalidate();
        _btnGridView.Invalidate();
        _btnChartView.Invalidate();

        ViewModeChanged?.Invoke(this, mode);
    }

    public void AddFilter(string key, string label, params string[] values)
    {
        AddFilter(key, label, (IEnumerable<string?>)values);
    }

    public void AddFilter(string key, string label, IEnumerable<string?> values)
    {
        var distinctVals = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v)
            .ToArray();

        if (distinctVals.Length == 0) return;

        _options.Add(new FilterOption { Key = key, Label = label, Values = distinctVals });
        RebuildFilters();
    }

    private void RebuildFilters()
    {
        _filterFlow.SuspendLayout();
        _filterFlow.Controls.Clear();
        _dropdowns.Clear();

        foreach (var opt in _options)
        {
            var pnl = new Panel
            {
                Height = 36,
                Width = 165,
                Margin = new Padding(0, 1, 8, 0),
                BackColor = Color.White
            };

            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 6);
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1.25f);
                g.DrawPath(pen, path);
            };

            var cmb = new ComboBox
            {
                Location = new Point(6, 6),
                Width = 153,
                Height = 24,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                Tag = opt.Key,
                BackColor = Color.White
            };

            cmb.Items.Add($"All {Pluralize(opt.Label)}");
            foreach (var val in opt.Values)
                cmb.Items.Add(val);

            cmb.SelectedIndex = 0;
            cmb.SelectedIndexChanged += (_, _) =>
            {
                UpdateClearButtonState();
                FiltersChanged?.Invoke(this, EventArgs.Empty);
            };

            pnl.Controls.Add(cmb);
            _dropdowns[opt.Key] = cmb;
            _filterFlow.Controls.Add(pnl);
        }

        _filterFlow.Controls.Add(_btnClear);
        _filterFlow.ResumeLayout(true);
        UpdateClearButtonState();
    }

    public string? GetFilterValue(string key)
    {
        if (!_dropdowns.TryGetValue(key, out var cmb)) return null;
        if (cmb.SelectedIndex <= 0) return null;
        return cmb.SelectedItem?.ToString();
    }

    public void ResetFilters()
    {
        _searchBox.Text = "";
        foreach (var cmb in _dropdowns.Values)
            cmb.SelectedIndex = 0;

        UpdateClearButtonState();
        FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateClearButtonState()
    {
        bool active = HasActiveFilters;
        _btnClear.Visible = active;
        if (active)
        {
            _btnClear.ForeColor = Color.FromArgb(239, 68, 68);
            _btnClear.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            _btnClear.BackColor = Color.FromArgb(255, 241, 242);
        }
    }

    public void SetRecordCount(int filtered, int total)
    {
        if (filtered == total)
        {
            _lblCount.Text = $"{total:N0} record{(total == 1 ? "" : "s")}";
            _lblCount.ForeColor = Color.FromArgb(100, 116, 139);
        }
        else
        {
            _lblCount.Text = $"Filtered: {filtered:N0} of {total:N0}";
            _lblCount.ForeColor = Color.FromArgb(2, 132, 199);
        }
    }

    private static string Pluralize(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return label;
        if (label.EndsWith("y", StringComparison.OrdinalIgnoreCase) &&
            !label.EndsWith("ay", StringComparison.OrdinalIgnoreCase) &&
            !label.EndsWith("ey", StringComparison.OrdinalIgnoreCase) &&
            !label.EndsWith("oy", StringComparison.OrdinalIgnoreCase))
        {
            return label[..^1] + "ies";
        }
        if (label.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            label.EndsWith("sh", StringComparison.OrdinalIgnoreCase) ||
            label.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            label.EndsWith("x", StringComparison.OrdinalIgnoreCase))
        {
            return label + "es";
        }
        return label + "s";
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        if (d > bounds.Width) d = bounds.Width;
        if (d > bounds.Height) d = bounds.Height;
        if (d <= 0) { path.AddRectangle(bounds); return path; }

        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}