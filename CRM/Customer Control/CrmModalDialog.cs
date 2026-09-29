using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Modern SaaS Modal Dialog system providing pixel-perfect dialogs across all modules:
/// - Prominent header banner with category icon badge, bold title, and descriptive subtitle
/// - Generous, non-overlapping field spacing with clean labels and modern rounded input cards
/// - Single-column and high-density two-column layout support
/// - Polished action footer with Cancel (Esc) and Primary Submit (Enter) buttons
/// - Responsive auto-sizing ensuring no clipping or overlapping occurs on any display/DPI
/// </summary>
public class CrmModalDialog : Form
{
    private readonly Panel _headerPanel;
    private readonly Panel _bodyPanel;
    private readonly Panel _footerPanel;
    private readonly Button _btnCancel;
    private readonly Button _btnSubmit;
    private readonly Label _lblTitle;
    private readonly Label _lblSubtitle;
    private readonly Panel _pnlIcon;

    private int _currentY = 16;
    private readonly int _formContentWidth;

    public static readonly Color ColorPrimary     = Color.FromArgb(2, 132, 199);   // Electric Blue #0284C7
    public static readonly Color ColorPrimaryHover= Color.FromArgb(3, 105, 161);   // #0369A1
    public static readonly Color ColorHeaderBg    = Color.White;
    public static readonly Color ColorBodyBg      = Color.White;
    public static readonly Color ColorFooterBg    = Color.FromArgb(248, 250, 252); // #F8FAFC
    public static readonly Color ColorBorder      = Color.FromArgb(226, 232, 240); // #E2E8F0
    public static readonly Color ColorTextDark    = Color.FromArgb(15, 23, 42);    // #0F172A
    public static readonly Color ColorTextSec     = Color.FromArgb(71, 85, 105);   // #475569
    public static readonly Color ColorTextMuted   = Color.FromArgb(148, 163, 184); // #94A3B8

    public Button SubmitButton => _btnSubmit;
    public Button CancelBtn => _btnCancel;

    public CrmModalDialog(
        string title,
        string subtitle,
        string actionText = "Save",
        string iconSymbol = "✦",
        int dialogWidth = 560,
        Color? accentColor = null)
    {
        var accent = accentColor ?? ColorPrimary;

        Text            = title;
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        BackColor       = ColorBodyBg;
        Width           = dialogWidth;

        _formContentWidth = dialogWidth - 64; // 32px padding on each side

        // =========================================================
        // 1. HEADER BANNER
        // =========================================================
        _headerPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 76,
            BackColor = ColorHeaderBg,
            Padding   = new Padding(28, 16, 28, 14)
        };
        Controls.Add(_headerPanel);

        _headerPanel.Paint += (_, pe) =>
        {
            using var divPen = new Pen(ColorBorder, 1);
            pe.Graphics.DrawLine(divPen, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
        };

        // Circular Icon Badge
        _pnlIcon = new Panel
        {
            Left      = 28,
            Top       = 18,
            Width     = 40,
            Height    = 40,
            BackColor = Color.FromArgb(240, 249, 255)
        };
        _pnlIcon.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var bg = new SolidBrush(Color.FromArgb(240, 249, 255));
            pe.Graphics.FillEllipse(bg, 0, 0, _pnlIcon.Width - 1, _pnlIcon.Height - 1);
            using var border = new Pen(Color.FromArgb(186, 230, 253), 1);
            pe.Graphics.DrawEllipse(border, 0, 0, _pnlIcon.Width - 1, _pnlIcon.Height - 1);

            using var font = new Font("Segoe UI", 12f, FontStyle.Bold);
            using var brush = new SolidBrush(accent);
            var sz = pe.Graphics.MeasureString(iconSymbol, font);
            pe.Graphics.DrawString(iconSymbol, font, brush,
                (_pnlIcon.Width - sz.Width) / 2f,
                (_pnlIcon.Height - sz.Height) / 2f);
        };
        _headerPanel.Controls.Add(_pnlIcon);

        _lblTitle = new Label
        {
            Text      = title,
            Font      = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = ColorTextDark,
            AutoSize  = true,
            Location  = new Point(78, 16)
        };
        _headerPanel.Controls.Add(_lblTitle);

        _lblSubtitle = new Label
        {
            Text      = subtitle,
            Font      = new Font("Segoe UI", 9f),
            ForeColor = ColorTextMuted,
            AutoSize  = false,
            Width     = dialogWidth - 110,
            Height    = 20,
            Location  = new Point(80, 42)
        };
        _headerPanel.Controls.Add(_lblSubtitle);

        // =========================================================
        // 2. FOOTER ACTION BAR
        // =========================================================
        _footerPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 66,
            BackColor = ColorFooterBg,
            Padding   = new Padding(28, 14, 28, 14)
        };
        Controls.Add(_footerPanel);

        _footerPanel.Paint += (_, pe) =>
        {
            using var divPen = new Pen(ColorBorder, 1);
            pe.Graphics.DrawLine(divPen, 0, 0, _footerPanel.Width, 0);
        };

        // Submit Button
        _btnSubmit = new Button
        {
            Text      = actionText,
            Height    = 38,
            AutoSize  = true,
            MinimumSize = new Size(130, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = accent,
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            DialogResult = DialogResult.OK,
            Anchor    = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnSubmit.FlatAppearance.BorderSize = 0;
        _btnSubmit.FlatAppearance.MouseOverBackColor = ColorPrimaryHover;
        _btnSubmit.Paint += (s, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(0, 0, _btnSubmit.Width - 1, _btnSubmit.Height - 1), 6);
            _btnSubmit.Region = new Region(path);
        };
        _footerPanel.Controls.Add(_btnSubmit);

        // Cancel Button
        _btnCancel = new Button
        {
            Text      = "Cancel",
            Width     = 95,
            Height    = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = ColorTextSec,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
            Anchor    = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnCancel.FlatAppearance.BorderSize = 1;
        _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        _btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnCancel.Paint += (s, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(0, 0, _btnCancel.Width - 1, _btnCancel.Height - 1), 6);
            _btnCancel.Region = new Region(path);
        };
        _footerPanel.Controls.Add(_btnCancel);

        _footerPanel.Resize += (_, _) =>
        {
            _btnSubmit.Left = _footerPanel.Width - 28 - _btnSubmit.Width;
            _btnSubmit.Top  = (_footerPanel.Height - _btnSubmit.Height) / 2;

            _btnCancel.Left = _btnSubmit.Left - 10 - _btnCancel.Width;
            _btnCancel.Top  = (_footerPanel.Height - _btnCancel.Height) / 2;
        };

        AcceptButton = _btnSubmit;
        CancelButton = _btnCancel;

        // =========================================================
        // 3. BODY PANEL (Houses Controls)
        // =========================================================
        _bodyPanel = new Panel
        {
            Dock       = DockStyle.Fill,
            BackColor  = ColorBodyBg,
            AutoScroll = true,
            Padding    = new Padding(32, 16, 32, 16)
        };
        Controls.Add(_bodyPanel);
        _bodyPanel.BringToFront();
    }

    // =========================================================================
    // BUILDER METHODS FOR FORM FIELDS
    // =========================================================================

    /// <summary>
    /// Add a single full-width text input field.
    /// </summary>
    public TextBox AddTextField(string label, string placeholder, string initialValue = "", bool required = false)
    {
        AddLabel(label, required, 32, _currentY, _formContentWidth);
        _currentY += 22;

        var box = CreateInputCard(_formContentWidth, 38, placeholder, false, out var tb);
        box.Location = new Point(32, _currentY);
        tb.Text = initialValue;
        _bodyPanel.Controls.Add(box);

        _currentY += 54;
        UpdateDialogHeight();
        return tb;
    }

    /// <summary>
    /// Add two side-by-side text input fields (50% / 50% split).
    /// </summary>
    public void AddTwoTextFields(
        string label1, string placeholder1, out TextBox txt1,
        string label2, string placeholder2, out TextBox txt2,
        bool req1 = false, bool req2 = false)
    {
        int colW = (_formContentWidth - 14) / 2;
        int col2X = 32 + colW + 14;

        AddLabel(label1, req1, 32, _currentY, colW);
        AddLabel(label2, req2, col2X, _currentY, colW);
        _currentY += 22;

        var box1 = CreateInputCard(colW, 38, placeholder1, false, out txt1);
        box1.Location = new Point(32, _currentY);
        _bodyPanel.Controls.Add(box1);

        var box2 = CreateInputCard(colW, 38, placeholder2, false, out txt2);
        box2.Location = new Point(col2X, _currentY);
        _bodyPanel.Controls.Add(box2);

        _currentY += 54;
        UpdateDialogHeight();
    }

    /// <summary>
    /// Add a dropdown combobox field.
    /// </summary>
    public ComboBox AddDropdownField(string label, object[] items, object? selectedItem = null, bool required = false)
    {
        AddLabel(label, required, 32, _currentY, _formContentWidth);
        _currentY += 22;

        var cmb = new ComboBox
        {
            Left          = 32,
            Top           = _currentY,
            Width         = _formContentWidth,
            Height        = 36,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Segoe UI", 9.75f),
            FlatStyle     = FlatStyle.System
        };
        cmb.Items.AddRange(items);
        if (selectedItem != null && cmb.Items.Contains(selectedItem))
            cmb.SelectedItem = selectedItem;
        else if (cmb.Items.Count > 0)
            cmb.SelectedIndex = 0;

        _bodyPanel.Controls.Add(cmb);

        _currentY += 54;
        UpdateDialogHeight();
        return cmb;
    }

    /// <summary>
    /// Add two side-by-side date pickers.
    /// </summary>
    public void AddDatePickerField(string label1, out DateTimePicker dt1, string label2, out DateTimePicker dt2)
    {
        int colW = (_formContentWidth - 14) / 2;
        int col2X = 32 + colW + 14;

        AddLabel(label1, false, 32, _currentY, colW);
        AddLabel(label2, false, col2X, _currentY, colW);
        _currentY += 22;

        dt1 = new DateTimePicker
        {
            Left   = 32,
            Top    = _currentY,
            Width  = colW,
            Height = 36,
            Format = DateTimePickerFormat.Short,
            Font   = new Font("Segoe UI", 9.5f)
        };
        _bodyPanel.Controls.Add(dt1);

        dt2 = new DateTimePicker
        {
            Left   = col2X,
            Top    = _currentY,
            Width  = colW,
            Height = 36,
            Format = DateTimePickerFormat.Short,
            Font   = new Font("Segoe UI", 9.5f),
            Value  = DateTime.Today.AddMonths(1)
        };
        _bodyPanel.Controls.Add(dt2);

        _currentY += 54;
        UpdateDialogHeight();
    }

    /// <summary>
    /// Add a multiline text area field.
    /// </summary>
    public TextBox AddTextAreaField(string label, string placeholder, int height = 70, string initialValue = "")
    {
        AddLabel(label, false, 32, _currentY, _formContentWidth);
        _currentY += 22;

        var panel = new Panel
        {
            Left      = 32,
            Top       = _currentY,
            Width     = _formContentWidth,
            Height    = height,
            BackColor = Color.White
        };

        bool isFocused = false;
        var tb = new TextBox
        {
            BorderStyle     = BorderStyle.None,
            Multiline       = true,
            ScrollBars      = ScrollBars.Vertical,
            Font            = new Font("Segoe UI", 9.5f),
            ForeColor       = ColorTextDark,
            PlaceholderText = placeholder,
            Text            = initialValue,
            Left            = 12,
            Top             = 8,
            Width           = _formContentWidth - 24,
            Height          = height - 16
        };

        tb.GotFocus  += (_, _) => { isFocused = true; panel.Invalidate(); };
        tb.LostFocus += (_, _) => { isFocused = false; panel.Invalidate(); };
        panel.Click  += (_, _) => tb.Focus();

        panel.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            using var path = RoundedRect(rect, 6);
            using var bg = new SolidBrush(Color.White);
            pe.Graphics.FillPath(bg, path);

            var borderColor = isFocused ? ColorPrimary : ColorBorder;
            using var pen = new Pen(borderColor, isFocused ? 1.5f : 1f);
            pe.Graphics.DrawPath(pen, path);
        };

        panel.Controls.Add(tb);
        _bodyPanel.Controls.Add(panel);

        _currentY += height + 16;
        UpdateDialogHeight();
        return tb;
    }

    /// <summary>
    /// Add a modern toggle/checkbox field.
    /// </summary>
    public CheckBox AddCheckboxField(string text, bool initialChecked = true)
    {
        var chk = new CheckBox
        {
            Text      = text,
            Checked   = initialChecked,
            Left      = 34,
            Top       = _currentY,
            Width     = _formContentWidth,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ColorTextDark,
            AutoSize  = true,
            Cursor    = Cursors.Hand
        };
        _bodyPanel.Controls.Add(chk);

        _currentY += 34;
        UpdateDialogHeight();
        return chk;
    }

    // =========================================================================
    // INTERNAL HELPERS
    // =========================================================================

    private void AddLabel(string text, bool required, int x, int y, int width)
    {
        var lbl = new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ColorTextSec,
            Left      = x,
            Top       = y,
            Width     = width,
            Height    = 18,
            AutoSize  = false
        };
        _bodyPanel.Controls.Add(lbl);
    }

    private static Panel CreateInputCard(int width, int height, string placeholder, bool isPassword, out TextBox tb)
    {
        var panel = new Panel
        {
            Width     = width,
            Height    = height,
            BackColor = Color.White,
            Cursor    = Cursors.IBeam
        };

        bool isFocused = false;

        var txt = new TextBox
        {
            BorderStyle           = BorderStyle.None,
            Font                  = new Font("Segoe UI", 9.75f),
            ForeColor             = ColorTextDark,
            PlaceholderText       = placeholder,
            UseSystemPasswordChar = isPassword,
            Left                  = 12,
            Top                   = (height - 20) / 2,
            Width                 = width - 24
        };
        tb = txt;

        txt.GotFocus  += (_, _) => { isFocused = true; panel.Invalidate(); };
        txt.LostFocus += (_, _) => { isFocused = false; panel.Invalidate(); };
        panel.Click   += (_, _) => txt.Focus();

        panel.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            using var path = RoundedRect(rect, 6);
            using var bg = new SolidBrush(Color.White);
            pe.Graphics.FillPath(bg, path);

            var borderColor = isFocused ? ColorPrimary : ColorBorder;
            using var pen = new Pen(borderColor, isFocused ? 1.5f : 1f);
            pe.Graphics.DrawPath(pen, path);
        };

        panel.Controls.Add(txt);
        return panel;
    }

    private void UpdateDialogHeight()
    {
        int desiredHeight = _headerPanel.Height + _currentY + _footerPanel.Height + 50;
        int screenH = Screen.FromControl(this).WorkingArea.Height;
        int finalH = Math.Min(desiredHeight, screenH - 80);
        Height = Math.Max(380, finalH);
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
