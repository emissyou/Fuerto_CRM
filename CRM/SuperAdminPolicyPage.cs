using System.Drawing.Drawing2D;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Super Admin – Platform Policy & Compliance page.
/// Completely modernized with robust responsive layout, no text truncation, and clean pastel section headers.
/// </summary>
public class SuperAdminPolicyPage : Panel
{
    private static readonly Color CBg        = Color.FromArgb(245, 247, 250);
    private static readonly Color CCard      = Color.White;
    private static readonly Color CBorder    = Color.FromArgb(226, 230, 236);
    private static readonly Color CText      = Color.FromArgb(15, 23, 42);
    private static readonly Color CMuted     = Color.FromArgb(100, 116, 139);
    private static readonly Color CAccent    = Color.FromArgb(255, 168, 0);
    private static readonly Color CGreen     = Color.FromArgb(22, 163, 74);
    private static readonly Color CGreenBg   = Color.FromArgb(240, 253, 244);
    private static readonly Color CBlue      = Color.FromArgb(37, 99, 235);
    private static readonly Color CBlueBg    = Color.FromArgb(239, 246, 255);
    private static readonly Color CRed       = Color.FromArgb(220, 38, 38);
    private static readonly Color CRedBg     = Color.FromArgb(254, 242, 242);
    private static readonly Color CPurple    = Color.FromArgb(139, 92, 246);
    private static readonly Color CPurpleBg  = Color.FromArgb(250, 245, 255);
    private static readonly Color CAmberBg   = Color.FromArgb(254, 252, 232);

    private readonly Panel _container;

    public SuperAdminPolicyPage()
    {
        Dock       = DockStyle.Fill;
        BackColor  = CBg;
        AutoScroll = true;
        Padding    = new Padding(28, 20, 28, 40);

        _container = new Panel
        {
            Location  = new Point(28, 20),
            Width     = Math.Max(760, Width - 56),
            BackColor = Color.Transparent,
            AutoSize  = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        Controls.Add(_container);

        Resize += (_, _) =>
        {
            int targetW = Math.Max(760, ClientSize.Width - 56);
            if (_container.Width != targetW)
            {
                _container.Width = targetW;
                foreach (Control c in _container.Controls)
                {
                    c.Width = targetW;
                }
            }
        };

        BuildUI();
    }

    private void BuildUI()
    {
        _container.SuspendLayout();
        _container.Controls.Clear();

        int targetW = Math.Max(760, ClientSize.Width - 56);
        int y = 0;

        // 1. Header Banner
        var banner = BuildBanner(targetW);
        banner.Location = new Point(0, y);
        _container.Controls.Add(banner);
        y += banner.Height + 16;

        // 2. Section 1 – Role Definition
        var sec1 = BuildPolicy(targetW, "👤  1. Super Administrator Role Definition", CBlue, CBlueBg,
            "The Super Administrator (Super Admin) is the highest-privilege account in the Fuerto CRM Platform. " +
            "This role is assigned strictly to platform owners and designated IT security executives.\n\n" +
            "The Super Admin oversees all tenant organizations, manages subscriptions, configures platform-wide security policies, " +
            "and maintains high system availability.\n\n" +
            "Super Admin accounts are completely separated from Company Admin and Tenant accounts. They possess zero operational affiliation " +
            "with regular tenant day-to-day business operations.");
        sec1.Location = new Point(0, y);
        _container.Controls.Add(sec1);
        y += sec1.Height + 16;

        // 3. Section 2 – Authorized Modules
        var sec2 = BuildModules(targetW, "🧩  2. Authorized Super Admin Modules");
        sec2.Location = new Point(0, y);
        _container.Controls.Add(sec2);
        y += sec2.Height + 16;

        // 4. Section 3 – What the Super Admin CAN Do
        var sec3 = BuildList(targetW, "✅  3. What the Super Admin CAN Do", CGreen, CGreenBg, "✓", new[]
        {
            "Create, edit, and deactivate tenant company accounts across the platform",
            "Manage subscription tiers, billing fees, and SaaS feature module entitlements",
            "Supervise all system users, promote or demote company admins, and reset passwords",
            "Access Platform Business Intelligence dashboards, cross-tenant KPIs, and revenue metrics",
            "Inspect full system Audit Logs (tamper-proof, read-only security trail)",
            "Configure platform-wide settings: SMTP mail service, session timeouts, and maintenance mode",
            "Create and manage branches, locations, and stores for any registered tenant company",
            "Trigger manual database backups and initiate disaster recovery procedures",
            "Enforce Multi-Factor Authentication (MFA) requirements across tenant organizations",
            "Engage system-wide Maintenance Mode to safeguard data during platform updates"
        });
        sec3.Location = new Point(0, y);
        _container.Controls.Add(sec3);
        y += sec3.Height + 16;

        // 5. Section 4 – What the Super Admin CANNOT Do
        var sec4 = BuildList(targetW, "🚫  4. What the Super Admin CANNOT Do", CRed, CRedBg, "✕", new[]
        {
            "Access, read, or modify confidential tenant business data (clients, projects, quotes, invoices) — tenant data is strictly private",
            "Create financial transactions, quotations, appointments, or operational records inside tenant databases",
            "Impersonate a tenant company employee or executive without formal written authorization and audit tracking",
            "Delete any tenant company without an mandatory 30-day deactivation cooling period",
            "Bypass platform audit logging — all Super Admin sessions and modifications are recorded irrevocably",
            "Share Super Admin master credentials with external contractors or unauthorized personnel",
            "Disable, delete, or alter any historical entries within the platform audit log",
            "Grant Super Admin master privileges to regular company employee accounts"
        });
        sec4.Location = new Point(0, y);
        _container.Controls.Add(sec4);
        y += sec4.Height + 16;

        // 6. Section 5 – Data Privacy & Tenant Isolation
        var sec5 = BuildPolicy(targetW, "🔒  5. Data Privacy & Multi-Tenant Isolation", CPurple, CPurpleBg,
            "Tenant data is strictly separated at the database and schema level. The Super Admin has administrative " +
            "access to the Master Database (tenant directory, subscription billing, and user authentication tables) but does NOT " +
            "have direct read access to tenant business databases.\n\n" +
            "Any access to tenant data for technical diagnostics or customer support must be formally requested, " +
            "approved by the tenant administrator, and permanently logged in the audit trail. Data may never be exported or shared externally.");
        sec5.Location = new Point(0, y);
        _container.Controls.Add(sec5);
        y += sec5.Height + 16;

        // 7. Section 6 – Accountability & Compliance
        var sec6 = BuildPolicy(targetW, "📝  6. Accountability & Compliance Standards", CAccent, CAmberBg,
            "All Super Admin actions are automatically recorded with UTC timestamps, originating IP addresses, user identity, and exact payload modifications. " +
            "Audit records are immutable and retained for a minimum of 365 days.\n\n" +
            "Super Admin credentials must be rotated every 90 days. Master passwords must exceed 12 characters and include uppercase, lowercase, numerical, and symbolic components.\n\n" +
            "Violations of this compliance policy result in immediate account termination and legal action under Republic Act 10173 (Data Privacy Act of 2012).");
        sec6.Location = new Point(0, y);
        _container.Controls.Add(sec6);
        y += sec6.Height + 16;

        // 8. Section 7 – Emergency Protocols
        var sec7 = BuildPolicy(targetW, "🚨  7. Emergency & Security Incident Protocols", CRed, CRedBg,
            "In the event of a detected security breach or anomalous access pattern:\n\n" +
            "  Step 1  ▸  Immediately engage Maintenance Mode from System Settings to freeze all external sessions.\n" +
            "  Step 2  ▸  Filter the Audit Log to identify the compromised account ID, IP origin, and affected entities.\n" +
            "  Step 3  ▸  Instantly terminate active tokens and deactivate the compromised user accounts.\n" +
            "  Step 4  ▸  Initiate an isolated snapshot backup of the current database state.\n" +
            "  Step 5  ▸  Notify designated company administrators of affected tenants within 24 hours.\n" +
            "  Step 6  ▸  Document forensic logs and file a formal security disclosure report.\n\n" +
            "Security Operations Center:  security@fuerto.local   ·   24/7 Response Hotline: +63 2 8888-FUERTO");
        sec7.Location = new Point(0, y);
        _container.Controls.Add(sec7);
        y += sec7.Height + 16;

        // 9. Acknowledgement Badge Card
        var ack = BuildAckCard(targetW);
        ack.Location = new Point(0, y);
        _container.Controls.Add(ack);
        y += ack.Height + 24;

        _container.ResumeLayout(true);
        AutoScrollPosition = new Point(0, 0);
    }

    // ── 1. Top Banner ────────────────────────────────────────────────────────
    private Panel BuildBanner(int width)
    {
        var pnl = new Panel
        {
            Width     = width,
            Height    = 76,
            BackColor = CCard
        };
        pnl.Paint += (_, pe) =>
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var grad = new LinearGradientBrush(new Rectangle(0, 0, pnl.Width, 5), CRed, CAccent, LinearGradientMode.Horizontal);
            g.FillRectangle(grad, 0, 0, pnl.Width, 5);
            g.DrawRectangle(new Pen(CBorder, 1), 0, 0, pnl.Width - 1, pnl.Height - 1);
        };

        pnl.Controls.Add(new Label
        {
            Text      = "📜  Super Administrator Governance Policy  ·  Version 2.0  ·  Effective: " + DateTime.Today.ToString("MMMM d, yyyy"),
            Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 30, 0),
            AutoSize  = true,
            Location  = new Point(20, 14)
        });

        pnl.Controls.Add(new Label
        {
            Text      = "This charter defines the operational bounds, access constraints, and statutory compliance mandates for all Super Administrator accounts.",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = CMuted,
            AutoSize  = true,
            Location  = new Point(20, 42)
        });

        return pnl;
    }

    // ── 2. Standard Policy Card ──────────────────────────────────────────────
    private Panel BuildPolicy(int width, string title, Color accent, Color headerBg, string body)
    {
        var card = new Panel
        {
            Width     = width,
            BackColor = CCard
        };

        // Header Strip
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 42,
            BackColor = headerBg
        };
        card.Controls.Add(header);

        header.Controls.Add(new Label
        {
            Text      = title,
            Font      = new Font("Segoe UI", 10.25f, FontStyle.Bold),
            ForeColor = accent,
            AutoSize  = true,
            Location  = new Point(16, 10)
        });

        // Body Content
        var lblBody = new Label
        {
            Text      = body,
            Font      = new Font("Segoe UI", 9.25f),
            ForeColor = Color.FromArgb(51, 65, 85),
            AutoSize  = true,
            MaximumSize = new Size(width - 48, 0),
            Location  = new Point(24, 54),
            Padding   = new Padding(0, 0, 0, 16)
        };
        card.Controls.Add(lblBody);

        card.Height = lblBody.Bottom + 16;

        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
            using var b = new SolidBrush(accent);
            pe.Graphics.FillRectangle(b, 0, 0, 5, card.Height);
        };

        return card;
    }

    // ── 3. Bullet List Card (Full Width, Zero Truncation) ───────────────────
    private Panel BuildList(int width, string title, Color accent, Color headerBg, string icon, string[] items)
    {
        var card = new Panel
        {
            Width     = width,
            BackColor = CCard
        };

        // Header Strip
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 42,
            BackColor = headerBg
        };
        card.Controls.Add(header);

        header.Controls.Add(new Label
        {
            Text      = title,
            Font      = new Font("Segoe UI", 10.25f, FontStyle.Bold),
            ForeColor = accent,
            AutoSize  = true,
            Location  = new Point(16, 10)
        });

        int y = 52;
        foreach (var item in items)
        {
            var row = new Panel
            {
                Location  = new Point(16, y),
                Width     = width - 32,
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            // Badge Icon (✓ or ✕)
            var badge = new Label
            {
                Text      = icon,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = accent,
                Width     = 24,
                Height    = 24,
                Location  = new Point(4, 2),
                TextAlign = ContentAlignment.MiddleCenter
            };
            row.Controls.Add(badge);

            // Full text label with word wrapping and NO ellipsis
            var lblText = new Label
            {
                Text        = item,
                Font        = new Font("Segoe UI", 9.25f),
                ForeColor   = Color.FromArgb(30, 41, 59),
                AutoSize    = true,
                MaximumSize = new Size(width - 70, 0),
                Location    = new Point(32, 4)
            };
            row.Controls.Add(lblText);

            row.Height = Math.Max(26, lblText.Height + 8);
            card.Controls.Add(row);
            y += row.Height + 4;
        }

        card.Height = y + 12;

        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
            using var b = new SolidBrush(accent);
            pe.Graphics.FillRectangle(b, 0, 0, 5, card.Height);
        };

        return card;
    }

    // ── 4. Authorized Modules Card ───────────────────────────────────────────
    private Panel BuildModules(int width, string title)
    {
        var card = new Panel
        {
            Width     = width,
            BackColor = CCard
        };

        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 42,
            BackColor = CBlueBg
        };
        card.Controls.Add(header);

        header.Controls.Add(new Label
        {
            Text      = title,
            Font      = new Font("Segoe UI", 10.25f, FontStyle.Bold),
            ForeColor = CBlue,
            AutoSize  = true,
            Location  = new Point(16, 10)
        });

        var modules = new[]
        {
            ("📊  Subscriptions & Billing",  CBlue),
            ("🏢  Company Accounts",          CAccent),
            ("👥  System Users",              CPurple),
            ("📋  Audit Logs",               CGreen),
            ("⚙️  System Settings",          Color.FromArgb(75, 85, 99)),
            ("📈  Platform BI & KPIs",        Color.FromArgb(236, 72, 153)),
            ("🗂  Tenant Admin Panel",        Color.FromArgb(14, 116, 144)),
            ("📜  Super Admin Policy",       CRed),
        };

        var flow = new FlowLayoutPanel
        {
            Location      = new Point(16, 52),
            Width         = width - 32,
            AutoSize      = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = true,
            BackColor     = Color.Transparent,
            Anchor        = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        card.Controls.Add(flow);

        foreach (var (name, color) in modules)
        {
            var c = color;
            var tile = new Panel
            {
                Width     = 225,
                Height    = 42,
                BackColor = Color.White,
                Margin    = new Padding(0, 4, 12, 8)
            };
            tile.Paint += (_, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pe.Graphics.DrawRectangle(new Pen(c, 1.25f), 1, 1, tile.Width - 2, tile.Height - 2);
                using var b = new SolidBrush(c);
                pe.Graphics.FillRectangle(b, 0, 0, 5, tile.Height);
                using var bg = new SolidBrush(Color.FromArgb(14, c.R, c.G, c.B));
                pe.Graphics.FillRectangle(bg, 5, 0, tile.Width - 5, tile.Height);
            };
            tile.Controls.Add(new Label
            {
                Text      = name,
                Font      = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                ForeColor = c,
                AutoSize  = false,
                Width     = 210,
                Height    = 42,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(12, 0, 0, 0)
            });
            flow.Controls.Add(tile);
        }

        card.Height = flow.Bottom + 16;

        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(CBorder, 1), 0, 0, card.Width - 1, card.Height - 1);
            using var b = new SolidBrush(CBlue);
            pe.Graphics.FillRectangle(b, 0, 0, 5, card.Height);
        };

        return card;
    }

    // ── 5. Acknowledgement Card ──────────────────────────────────────────────
    private Panel BuildAckCard(int width)
    {
        var card = new Panel
        {
            Width     = width,
            Height    = 96,
            BackColor = CGreenBg
        };
        card.Paint += (_, pe) =>
        {
            pe.Graphics.DrawRectangle(new Pen(Color.FromArgb(134, 239, 172), 1), 0, 0, card.Width - 1, card.Height - 1);
            using var b = new SolidBrush(CGreen);
            pe.Graphics.FillRectangle(b, 0, 0, 5, card.Height);
        };

        card.Controls.Add(new Label
        {
            Text      = "✅  Super Administrator Policy Acknowledged",
            Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = CGreen,
            AutoSize  = true,
            Location  = new Point(20, 14)
        });

        card.Controls.Add(new Label
        {
            Text      = "I have read, understood, and agreed to adhere strictly to the Super Administrator Governance & Security Policy.",
            Font      = new Font("Segoe UI", 9.25f),
            ForeColor = Color.FromArgb(21, 128, 61),
            AutoSize  = true,
            Location  = new Point(20, 40)
        });

        card.Controls.Add(new Label
        {
            Text      = $"Acknowledged by: {Session.Email ?? "admin@fuerto.local"}   ·   System Verified: {DateTime.Now:MMMM d, yyyy  hh:mm tt}",
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(21, 128, 61),
            AutoSize  = true,
            Location  = new Point(20, 64)
        });

        return card;
    }
}
