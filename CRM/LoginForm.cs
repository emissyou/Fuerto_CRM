using System.Drawing.Drawing2D;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CRM_DesignServices.winforms;

public partial class LoginForm : Form
{
    private TextBox txtEmail = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!;
    private Label lblStatus = null!;
    private CheckBox chkRemember = null!;
    private Panel pnlSignIn = null!;

    private const string ApiUrl = "http://localhost:5068";
    private readonly HttpClient _http = new();

    // ---- Yellow & Black Luxury Theme Colors (Swapped: Dark Form / Light Hero) ----
    private static readonly Color CPrimaryYellow      = Color.FromArgb(255, 184, 0);   // #FFB800 (Warm golden brand yellow)
    private static readonly Color CPrimaryYellowHover = Color.FromArgb(235, 166, 0);   // #EBA600
    private static readonly Color CPrimaryYellowDark  = Color.FromArgb(204, 147, 0);   // #CC9300
    private static readonly Color CBlack              = Color.FromArgb(17, 24, 39);    // #111827 (Rich onyx black)
    private static readonly Color CDarkFormBg         = Color.FromArgb(15, 20, 32);    // #0F1420 (Deep luxury dark obsidian)
    private static readonly Color CDarkInputBg        = Color.FromArgb(24, 32, 47);    // #18202F (Dark input surface)
    private static readonly Color CDarkInputBorder     = Color.FromArgb(51, 65, 85);    // #334155 (Subtle dark border)
    private static readonly Color CHeroBg             = Color.FromArgb(248, 250, 252); // #F8FAFC (Clean light hero showcase)
    private static readonly Color CTextDark           = Color.FromArgb(17, 24, 39);    // #111827
    private static readonly Color CTextMuted          = Color.FromArgb(100, 116, 139); // #64748B
    private static readonly Color CInputFocus         = Color.FromArgb(255, 184, 0);   // Golden focus outline
    private static readonly Color CGreen              = Color.FromArgb(22, 163, 74);
    private static readonly Color CBlue               = Color.FromArgb(37, 99, 235);

    private static Image? _cachedLogo;

    public LoginForm()
    {
        BuildModernLoginInterface();
    }

    private void BuildModernLoginInterface()
    {
        Text            = "Fuerto CRM — Welcome Back";
        StartPosition   = FormStartPosition.CenterScreen;
        ClientSize      = new Size(1040, 640);
        MinimumSize     = new Size(1040, 640);
        BackColor       = CDarkFormBg;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;

        Controls.Clear();

        // =========================================================
        // MAIN TWO-COLUMN CONTAINER (Guarantees ZERO Overlap)
        // =========================================================
        var mainLayout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 1,
            ColumnCount = 2,
            BackColor   = CDarkFormBg,
            Margin      = new Padding(0),
            Padding     = new Padding(0)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 510f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(mainLayout);

        // =========================================================
        // 1. LEFT FORM PANEL (Now Deep Luxury Onyx Black)
        // =========================================================
        var leftPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = CDarkFormBg,
            Margin    = new Padding(0)
        };
        mainLayout.Controls.Add(leftPanel, 0, 0);

        // 4px Golden Yellow Accent Rail on very left edge
        leftPanel.Paint += (_, pe) =>
        {
            using var yellowRail = new SolidBrush(CPrimaryYellow);
            pe.Graphics.FillRectangle(yellowRail, 0, 0, 4, leftPanel.Height);
        };

        pnlSignIn = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = CDarkFormBg,
            Padding   = new Padding(52, 40, 52, 30)
        };
        leftPanel.Controls.Add(pnlSignIn);

        int sy = 40;
        const int FieldWidth = 404;

        // Top Brand Header with Logo inside a crisp badge
        var logoImg = GetFuertoLogo();
        if (logoImg != null)
        {
            var pnlLogoBadge = new Panel
            {
                Location  = new Point(52, sy),
                Size      = new Size(116, 48),
                BackColor = Color.White
            };
            pnlLogoBadge.Paint += (_, pe) =>
            {
                using var path = RoundedRect(new Rectangle(0, 0, pnlLogoBadge.Width - 1, pnlLogoBadge.Height - 1), 8);
                using var pen  = new Pen(CPrimaryYellow, 1.5f);
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pe.Graphics.DrawPath(pen, path);
                pnlLogoBadge.Region = new Region(path);
            };

            var picLogo = new PictureBox
            {
                Image     = logoImg,
                SizeMode  = PictureBoxSizeMode.Zoom,
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(6)
            };
            pnlLogoBadge.Controls.Add(picLogo);
            pnlSignIn.Controls.Add(pnlLogoBadge);
            sy += 64;
        }
        else
        {
            var lblLogoText = new Label
            {
                Text      = "⚡ FUERTO CRM",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = CPrimaryYellow,
                AutoSize  = true,
                Location  = new Point(52, sy)
            };
            pnlSignIn.Controls.Add(lblLogoText);
            sy += 44;
        }

        // Title: WELCOME BACK (Pure Crisp White)
        var lblTitle = new Label
        {
            Text      = "WELCOME BACK",
            Font      = new Font("Segoe UI", 24f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize  = true,
            Location  = new Point(52, sy)
        };
        pnlSignIn.Controls.Add(lblTitle);
        sy += 46;

        // Subtitle: Welcome back! Please enter your details.
        var lblSub = new Label
        {
            Text      = "Welcome back! Please enter your details.",
            Font      = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize  = true,
            Location  = new Point(52, sy)
        };
        pnlSignIn.Controls.Add(lblSub);
        sy += 42;

        // Email Label
        pnlSignIn.Controls.Add(new Label
        {
            Text      = "Email",
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location  = new Point(52, sy),
            AutoSize  = true
        });
        sy += 25;

        // Email Input Field (Rounded Dark Box with Yellow Focus Ring)
        var pnlEmailInput = CreateRoundedInputBox(FieldWidth, 46, "Enter your email", false, out txtEmail);
        pnlEmailInput.Location = new Point(52, sy);
        txtEmail.Text = "admin@fuerto.local";
        pnlSignIn.Controls.Add(pnlEmailInput);
        sy += 62;

        // Password Label
        pnlSignIn.Controls.Add(new Label
        {
            Text      = "Password",
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location  = new Point(52, sy),
            AutoSize  = true
        });
        sy += 25;

        // Password Input Field (Rounded Dark Box + Eye Toggle + Yellow Focus Ring)
        var pnlPwdInput = CreateRoundedInputBox(FieldWidth, 46, "••••••••", true, out txtPassword);
        pnlPwdInput.Location = new Point(52, sy);
        txtPassword.Text = "Admin@12345";
        pnlSignIn.Controls.Add(pnlPwdInput);
        sy += 60;

        // Remember Me Row
        var optRow = new Panel
        {
            Location  = new Point(52, sy),
            Width     = FieldWidth,
            Height    = 24,
            BackColor = Color.Transparent
        };
        pnlSignIn.Controls.Add(optRow);

        chkRemember = new CheckBox
        {
            Text      = "Remember me",
            Font      = new Font("Segoe UI", 9.25f),
            ForeColor = Color.FromArgb(203, 213, 225),
            Checked   = true,
            AutoSize  = true,
            Location  = new Point(0, 1),
            Cursor    = Cursors.Hand
        };
        optRow.Controls.Add(chkRemember);
        sy += 36;

        // Primary "Sign in" Button (High-Contrast Golden Yellow on Black)
        btnLogin = CreateRoundedButton("Sign in", FieldWidth, 48, CPrimaryYellow, CPrimaryYellowHover, CBlack);
        btnLogin.Location = new Point(52, sy);
        btnLogin.Click += BtnLogin_Click;
        pnlSignIn.Controls.Add(btnLogin);
        AcceptButton = btnLogin;
        sy += 64;

        // Security / Admin Note
        var lblSecurityNote = new Label
        {
            Text      = "🔒 Enterprise Managed Access · Contact Admin for Password Updates",
            Font      = new Font("Segoe UI", 8.25f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location  = new Point(52, sy),
            Width     = FieldWidth,
            Height    = 20,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlSignIn.Controls.Add(lblSecurityNote);
        sy += 26;

        // Status Banner
        lblStatus = new Label
        {
            Location  = new Point(52, sy),
            Width     = FieldWidth,
            Height    = 26,
            ForeColor = Color.FromArgb(248, 113, 113),
            Font      = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlSignIn.Controls.Add(lblStatus);

        // =========================================================
        // 2. RIGHT HERO SHOWCASE PANEL (Now Crisp Light SaaS Canvas)
        // =========================================================
        var heroPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = CHeroBg,
            Margin    = new Padding(0)
        };
        mainLayout.Controls.Add(heroPanel, 1, 0);

        heroPanel.Paint += (_, pe) =>
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int hw = heroPanel.Width;
            int hh = heroPanel.Height;

            // 1. Subtle radial golden ambient glow in upper-center
            int glowSize = 380;
            int glowX = (hw - glowSize) / 2;
            int glowY = 30;
            using (var glowPath = new GraphicsPath())
            {
                glowPath.AddEllipse(glowX, glowY, glowSize, glowSize);
                using var pgb = new PathGradientBrush(glowPath)
                {
                    CenterColor = Color.FromArgb(35, 255, 184, 0),
                    SurroundColors = new[] { Color.FromArgb(0, 248, 250, 252) }
                };
                g.FillEllipse(pgb, glowX, glowY, glowSize, glowSize);
            }

            // Divider on the left edge of hero panel
            using (var divPen = new Pen(Color.FromArgb(226, 232, 240), 1))
            {
                g.DrawLine(divPen, 0, 0, 0, hh);
            }

            // 2. Top Badge: "⭐ Enterprise Intelligence Platform"
            string topBadge = "⭐  ENTERPRISE CRM & INTELLIGENCE SUITE";
            using (var badgeFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
            {
                var sz = g.MeasureString(topBadge, badgeFont);
                int bw = (int)sz.Width + 24;
                int bh = 28;
                int bx = (hw - bw) / 2;
                int by = 34;
                using var bPath = RoundedRect(new Rectangle(bx, by, bw, bh), 14);
                using var bFill = new SolidBrush(Color.White);
                using var bPen  = new Pen(CPrimaryYellow, 1.25f);
                using var bTxt  = new SolidBrush(CBlack);
                g.FillPath(bFill, bPath);
                g.DrawPath(bPen, bPath);
                g.DrawString(topBadge, badgeFont, bTxt, bx + 12, by + 6);
            }

            // 3. Central Logo Card Pedestal (Crisp White Card with Gold Glow)
            int cardW = 340;
            int cardH = 180;
            int cardX = (hw - cardW) / 2;
            int cardY = 80;

            using (var cardPath = RoundedRect(new Rectangle(cardX, cardY, cardW, cardH), 16))
            {
                // Soft golden drop glow
                using (var glowPen = new Pen(Color.FromArgb(50, 255, 184, 0), 4f))
                {
                    g.DrawPath(glowPen, cardPath);
                }

                using (var cardFill = new SolidBrush(Color.White))
                {
                    g.FillPath(cardFill, cardPath);
                }

                using (var cardPen = new Pen(Color.FromArgb(226, 232, 240), 1.25f))
                {
                    g.DrawPath(cardPen, cardPath);
                }
            }

            // Draw Logo inside the pedestal
            var logo = GetFuertoLogo();
            if (logo != null)
            {
                float maxLogoW = 160;
                float maxLogoH = 80;
                float aspect = (float)logo.Width / logo.Height;
                float lw = maxLogoW;
                float lh = lw / aspect;
                if (lh > maxLogoH)
                {
                    lh = maxLogoH;
                    lw = lh * aspect;
                }
                float lx = cardX + (cardW - lw) / 2f;
                float ly = cardY + 16;
                g.DrawImage(logo, lx, ly, lw, lh);
            }

            // Typography under logo inside pedestal
            using (var fontBrand = new Font("Segoe UI", 13.5f, FontStyle.Bold))
            using (var fontTag = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (var brushBrand = new SolidBrush(CBlack))
            using (var brushTag = new SolidBrush(CTextMuted))
            {
                string bName = "FUERTO CRM";
                var szB = g.MeasureString(bName, fontBrand);
                g.DrawString(bName, fontBrand, brushBrand, cardX + (cardW - szB.Width) / 2f, cardY + 104);

                string bSub = "Intelligent Multi-Tenant Platform";
                var szS = g.MeasureString(bSub, fontTag);
                g.DrawString(bSub, fontTag, brushTag, cardX + (cardW - szS.Width) / 2f, cardY + 130);

                // Small golden indicator line
                using var goldLine = new Pen(CPrimaryYellow, 2.5f);
                int lineW = 36;
                g.DrawLine(goldLine, cardX + (cardW - lineW) / 2f, cardY + 154, cardX + (cardW + lineW) / 2f, cardY + 154);
            }

            // 4. Feature Cards Below Logo (Clean Elevated White SaaS Cards)
            int featY = 284;
            int featW = 390;
            int featH = 70;
            int featX = (hw - featW) / 2;

            DrawFeatureCard(g, featX, featY, featW, featH, "📊", "Real-Time KPI & Retention Analytics", "AI-driven RFM scoring, sales health & retention campaigns");
            featY += 80;
            DrawFeatureCard(g, featX, featY, featW, featH, "🏢", "Multi-Branch & Tenant Operations", "Isolated schemas for Salon, Retail, Donut & Custom businesses");
            featY += 80;
            DrawFeatureCard(g, featX, featY, featW, featH, "🛡️", "Enterprise Security & Role Governance", "Granular RBAC, Super Admin controls & automated audit trail");

            // 5. Hero Footer Note
            using (var footFont = new Font("Segoe UI", 8f))
            using (var footBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                string footText = "© 2026 Fuerto Systems Inc. · Secured by Enterprise Cloud";
                var szF = g.MeasureString(footText, footFont);
                g.DrawString(footText, footFont, footBrush, (hw - szF.Width) / 2f, hh - 32);
            }
        };
    }

    private static void DrawFeatureCard(Graphics g, int x, int y, int w, int h, string icon, string title, string desc)
    {
        using var cardPath = RoundedRect(new Rectangle(x, y, w, h), 10);
        using var bgBrush  = new SolidBrush(Color.White);
        using var pen      = new Pen(Color.FromArgb(226, 232, 240), 1f);

        g.FillPath(bgBrush, cardPath);
        g.DrawPath(pen, cardPath);

        // Left yellow accent rail
        using var yellowBar = new SolidBrush(CPrimaryYellow);
        g.FillRectangle(yellowBar, x, y + 8, 3, h - 16);

        // Icon
        using var iconFont = new Font("Segoe UI Emoji", 13f);
        using var iconBrush = new SolidBrush(CBlack);
        g.DrawString(icon, iconFont, iconBrush, x + 14, y + 12);

        // Title
        using var titleFont = new Font("Segoe UI", 9.25f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
        g.DrawString(title, titleFont, titleBrush, x + 44, y + 12);

        // Description
        using var descFont = new Font("Segoe UI", 8f);
        using var descBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
        g.DrawString(desc, descFont, descBrush, x + 44, y + 36);
    }

    // =========================================================================
    // UI HELPERS (Inputs, Buttons, Graphics)
    // =========================================================================
    private static Panel CreateRoundedInputBox(int width, int height, string placeholder, bool isPassword, out TextBox textBox)
    {
        var panel = new Panel
        {
            Width     = width,
            Height    = height,
            BackColor = CDarkInputBg,
            Cursor    = Cursors.IBeam
        };

        bool isFocused = false;

        var tb = new TextBox
        {
            BorderStyle           = BorderStyle.None,
            Font                  = new Font("Segoe UI", 10.25f),
            ForeColor             = Color.White,
            BackColor             = CDarkInputBg,
            PlaceholderText       = placeholder,
            UseSystemPasswordChar = isPassword,
            Left                  = 14,
            Top                   = (height - 20) / 2,
            Width                 = isPassword ? width - 54 : width - 28
        };
        textBox = tb;

        tb.GotFocus  += (_, _) => { isFocused = true; panel.Invalidate(); };
        tb.LostFocus += (_, _) => { isFocused = false; panel.Invalidate(); };
        panel.Click  += (_, _) => tb.Focus();

        panel.Paint += (_, pe) =>
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            using var path = RoundedRect(rect, 8);
            using var bgBrush = new SolidBrush(CDarkInputBg);
            g.FillPath(bgBrush, path);

            var borderColor = isFocused ? CInputFocus : CDarkInputBorder;
            using var pen = new Pen(borderColor, isFocused ? 1.75f : 1f);
            g.DrawPath(pen, path);
        };

        panel.Controls.Add(tb);

        if (isPassword)
        {
            var btnEye = new Label
            {
                Text      = "👁",
                Font      = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Width     = 30,
                Height    = 24,
                Left      = width - 38,
                Top       = (height - 24) / 2,
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnEye.Click += (_, _) =>
            {
                tb.UseSystemPasswordChar = !tb.UseSystemPasswordChar;
                btnEye.ForeColor = tb.UseSystemPasswordChar ? Color.FromArgb(148, 163, 184) : CPrimaryYellow;
            };
            panel.Controls.Add(btnEye);
        }

        return panel;
    }

    private static Button CreateRoundedButton(string text, int width, int height, Color bg, Color hoverBg, Color fg)
    {
        var btn = new Button
        {
            Text      = text,
            Width     = width,
            Height    = height,
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = fg,
            Font      = new Font("Segoe UI", 10.75f, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = hoverBg;

        btn.Paint += (s, pe) =>
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
            using var path = RoundedRect(rect, 8);
            btn.Region = new Region(path);
        };

        return btn;
    }

    private static Image? GetFuertoLogo()
    {
        if (_cachedLogo != null) return _cachedLogo;
        try
        {
            string[] possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "fuerto-logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fuerto-logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Images", "fuerto-logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "fuerto-logo.png"),
                @"c:\Users\john mark bolanon\source\repos\CRM\CRM\Images\fuerto-logo.png"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return _cachedLogo = Image.FromFile(path);
                }
            }
        }
        catch { }
        return null;
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

    // =========================================================
    // LOGIN CLICK
    // =========================================================
    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblStatus.Text = "Authenticating credentials...";
        lblStatus.ForeColor = CBlue;
        btnLogin.Enabled = false;

        try
        {
            var payload = new
            {
                email = txtEmail.Text.Trim(),
                password = txtPassword.Text
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{ApiUrl}/login", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string msg = "Invalid email or password.";
                try
                {
                    using var errDoc = JsonDocument.Parse(result);
                    if (errDoc.RootElement.TryGetProperty("message", out var m))
                        msg = m.GetString() ?? msg;
                }
                catch { }

                lblStatus.Text = msg;
                lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                btnLogin.Enabled = true;
                return;
            }

            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            Session.Token = root.GetProperty("token").GetString();
            Session.Email = root.GetProperty("email").GetString();

            if (root.TryGetProperty("companyId", out var cid) && cid.ValueKind == JsonValueKind.Number)
                Session.CompanyId = cid.GetInt32();
            else
                Session.CompanyId = null;

            if (root.TryGetProperty("companyName", out var cn) && cn.ValueKind == JsonValueKind.String)
                Session.CompanyName = cn.GetString();
            else
                Session.CompanyName = "Platform Operations";

            if (root.TryGetProperty("companyCode", out var cc) && cc.ValueKind == JsonValueKind.String)
                Session.CompanyCode = cc.GetString();
            else
                Session.CompanyCode = "SUPER";

            if (root.TryGetProperty("availedModules", out var am) && am.ValueKind == JsonValueKind.String)
                Session.AvailedModules = am.GetString();
            else
                Session.AvailedModules = "All";

            if (root.TryGetProperty("subscriptionStatus", out var ss) && ss.ValueKind == JsonValueKind.String)
                Session.SubscriptionStatus = ss.GetString();
            else
                Session.SubscriptionStatus = "Active";

            Session.Roles = new List<string>();
            if (root.TryGetProperty("roles", out var rolesProp) && rolesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in rolesProp.EnumerateArray())
                {
                    var s = r.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        Session.Roles.Add(s);
                }
            }

            if (root.TryGetProperty("branchId", out var bIdProp) && bIdProp.ValueKind == JsonValueKind.Number)
            {
                Session.CurrentBranchId = bIdProp.GetInt32();
            }
            else
            {
                Session.CurrentBranchId = null;
            }

            if (root.TryGetProperty("branchName", out var bNameProp) && bNameProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(bNameProp.GetString()))
            {
                Session.CurrentBranchName = bNameProp.GetString();
            }
            else
            {
                Session.CurrentBranchName = Session.CurrentBranchId.HasValue ? $"Branch #{Session.CurrentBranchId.Value}" : "All Branches";
            }
            Session.IsOffline = false;

            bool hasAccepted = true;
            if (root.TryGetProperty("hasAcceptedTerms", out var hat))
            {
                hasAccepted = hat.GetBoolean();
            }
            Session.HasAcceptedTerms = hasAccepted;

            // If tenant company has not accepted terms yet, display the Desktop Installer EULA Dialog!
            if (!Session.IsSuperAdmin && !hasAccepted)
            {
                lblStatus.Text = "Please review and accept the Platform Terms & Conditions...";
                lblStatus.ForeColor = Color.FromArgb(234, 179, 8);

                using var termsDlg = new TermsAndConditionsDialog(
                    ApiUrl,
                    Session.Token,
                    Session.CompanyId,
                    Session.CompanyName,
                    Session.CompanyCode,
                    Session.Email);

                var dlgResult = termsDlg.ShowDialog(this);
                if (dlgResult != DialogResult.OK)
                {
                    // User rejected terms: clear session, deny access, return to login screen
                    Session.Token = null;
                    Session.CompanyId = null;
                    Session.Roles.Clear();
                    Session.HasAcceptedTerms = false;

                    lblStatus.Text = "Access Denied: You must accept the Terms & Conditions to enter Fuerto CRM.";
                    lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    btnLogin.Enabled = true;
                    return;
                }

                Session.HasAcceptedTerms = true;
            }

            // Cache credentials for seamless offline login
            OfflineSyncManager.CacheAuth(
                txtEmail.Text.Trim(),
                txtPassword.Text,
                Session.Token ?? "",
                Session.CompanyId,
                Session.CompanyName,
                Session.CompanyCode,
                Session.AvailedModules,
                Session.SubscriptionStatus,
                Session.Roles,
                Session.HasAcceptedTerms);

            lblStatus.Text = "Authentication successful! Launching workspace...";
            lblStatus.ForeColor = CGreen;

            Hide();
            var mainForm = new Form1();
            mainForm.FormClosed += (_, _) => Close();
            mainForm.Show();
        }
        catch (Exception ex)
        {
            // If offline, attempt offline cached authentication
            if (OfflineSyncManager.TryOfflineLogin(
                txtEmail.Text.Trim(),
                txtPassword.Text,
                out var token,
                out var companyId,
                out var companyName,
                out var companyCode,
                out var availedModules,
                out var subscriptionStatus,
                out var roles,
                out var offlineAccepted))
            {
                Session.Token = token;
                Session.Email = txtEmail.Text.Trim();
                Session.CompanyId = companyId;
                Session.CompanyName = companyName ?? "Offline Workspace";
                Session.CompanyCode = companyCode ?? "LOCAL";
                Session.AvailedModules = availedModules ?? "All";
                Session.SubscriptionStatus = subscriptionStatus ?? "Active";
                Session.Roles = roles ?? new List<string> { "Admin" };
                Session.CurrentBranchId = null;
                Session.CurrentBranchName = "Local Offline Store";
                Session.IsOffline = true;
                Session.HasAcceptedTerms = offlineAccepted;

                if (!Session.IsSuperAdmin && !offlineAccepted)
                {
                    using var termsDlg = new TermsAndConditionsDialog(
                        ApiUrl,
                        Session.Token,
                        Session.CompanyId,
                        Session.CompanyName,
                        Session.CompanyCode,
                        Session.Email);

                    if (termsDlg.ShowDialog(this) != DialogResult.OK)
                    {
                        Session.Token = null;
                        Session.CompanyId = null;
                        Session.Roles.Clear();
                        lblStatus.Text = "Access Denied: You must accept the Terms & Conditions to enter Fuerto CRM.";
                        lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                        btnLogin.Enabled = true;
                        return;
                    }
                    Session.HasAcceptedTerms = true;
                }

                lblStatus.Text = "Offline mode active · Logged in with cached credentials...";
                lblStatus.ForeColor = Color.FromArgb(234, 179, 8); // Yellow

                Hide();
                var mainForm = new Form1();
                mainForm.FormClosed += (_, _) => Close();
                mainForm.Show();
                return;
            }

            lblStatus.Text = "Server offline: " + ex.Message;
            lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
            btnLogin.Enabled = true;
        }
    }
}