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

    private TextBox _searchBox = null!;
    private Button _btnClear = null!;
    private readonly Dictionary<string, ComboBox> _dropdowns = new();
    private readonly List<FilterOption> _options = new();

    public string SearchText => _searchBox.Text.Trim();

    public CrmFilterBar(string placeholder = "Search...")
    {
        Dock = DockStyle.Top;
        Height = 60;
        BackColor = Color.Transparent;
        Padding = new Padding(0, 4, 0, 4);

        // Search box
        _searchBox = new TextBox
        {
            Left = 0,
            Top = 12,
            Width = 280,
            Height = 34,
            PlaceholderText = placeholder,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle
        };
        _searchBox.TextChanged += (_, _) => FiltersChanged?.Invoke(this, EventArgs.Empty);
        Controls.Add(_searchBox);
    }

    public void AddFilter(string key, string label, params string[] values)
    {
        _options.Add(new FilterOption { Key = key, Label = label, Values = values });
        RebuildFilters();
    }

    private void RebuildFilters()
    {
        // Remove existing dropdowns (keep search + clear)
        var toRemove = Controls.OfType<ComboBox>().Cast<Control>().ToList();
        foreach (var c in toRemove) Controls.Remove(c);
        if (_btnClear != null) Controls.Remove(_btnClear);

        int x = 300;

        _dropdowns.Clear();

        foreach (var opt in _options)
        {
            var cmb = new ComboBox
            {
                Left = x,
                Top = 12,
                Width = 180,
                Height = 34,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Tag = opt.Key
            };

            cmb.Items.Add($"All {opt.Label}s");
            cmb.Items.AddRange(opt.Values);
            cmb.SelectedIndex = 0;
            cmb.SelectedIndexChanged += (_, _) => FiltersChanged?.Invoke(this, EventArgs.Empty);

            _dropdowns[opt.Key] = cmb;
            Controls.Add(cmb);

            x += 190;
        }

        _btnClear = new Button
        {
            Text = "✕  Clear",
            Left = x,
            Top = 12,
            Width = 90,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnClear.FlatAppearance.BorderColor = Color.FromArgb(226, 230, 236);
        _btnClear.Click += (_, _) =>
        {
            _searchBox.Text = "";
            foreach (var cmb in _dropdowns.Values)
                cmb.SelectedIndex = 0;
            FiltersChanged?.Invoke(this, EventArgs.Empty);
        };
        Controls.Add(_btnClear);
    }

    public string? GetFilterValue(string key)
    {
        if (!_dropdowns.TryGetValue(key, out var cmb)) return null;
        if (cmb.SelectedIndex <= 0) return null;
        return cmb.SelectedItem?.ToString();
    }
}