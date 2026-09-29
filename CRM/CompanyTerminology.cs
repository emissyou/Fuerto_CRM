namespace CRM_DesignServices.winforms;

/// <summary>
/// Provides company-specific terminology so that each tenant sees
/// language that matches their industry (salon, food-retail, etc.).
/// </summary>
public static class CompanyTerminology
{
    // ── Company codes ───────────────────────────────────────────────────────
    public const string Salon  = "LRSALON";
    public const string Donut  = "MRDONUT";
    public static bool IsSalon => Code == Salon;
    public static bool IsDonut => Code == Donut;

    // ── Sidebar nav labels ─────────────────────────────────────────────────
    public static string Customers  => Code switch { Salon => "Clients",          _ => "Customers"  };
    public static string Leads      => Code switch { Salon => "Leads",            _ => "Leads"      };
    public static string Quotations => Code switch { Salon => "Appointments",     _ => "Quotations" };
    public static string Projects   => Code switch { Salon => "Salon Services",   _ => "Projects"   };
    public static string Activities => Code switch { Salon => "Follow-ups",       _ => "Activities" };
    public static string Issues     => Code switch { Salon => "Service Complaints", _ => "Issues"   };
    public static string Feedback   => Code switch { Salon => "Client Reviews",   _ => "Feedback"   };
    public static string Designers  => Code switch { Salon => "Stylists",         _ => "Designers"  };
    public static string Users      => "Team Accounts";
    public static string Retention  => Code switch { Donut => "Loyalty Programs", _ => "Retention"  };
    public static string Promotions => Code switch { Donut => "Product Deals",    _ => "Promotions" };
    public static string Overview   => Code switch { Donut => "Store Performance",_ => "Overview"   };
    public static string Analytics  => Code switch { Donut => "Sales Analytics",  _ => "Analytics"  };
    public static string Reports    => Code switch { Donut => "Sales Reports",     _ => "Reports"    };

    // ── Group labels in the sidebar ────────────────────────────────────────
    public static string GroupSalesCrm   => Code switch { Salon => "CLIENTS & BOOKINGS", Donut => "CUSTOMER HUB", _ => "SALES & CRM" };
    public static string GroupOperations => Code switch { Salon => "SALON OPERATIONS",   Donut => "STORE OPS",   _ => "OPERATIONS" };
    public static string GroupInsights   => Code switch { Donut => "PERFORMANCE",        _ => "INSIGHTS"         };

    // ── Page subtitles ─────────────────────────────────────────────────────
    public static string SubtitleOverview   => Code switch
    {
        Donut => "Real-time store performance, daily sales, and foot traffic",
        Salon => "Live snapshot of today's appointments and client activity",
        _     => "Live snapshot of your company records"
    };
    public static string SubtitleCustomers  => Code switch
    {
        Salon => "Manage client profiles, preferences, and appointment history",
        _     => "Manage and view your client relationships"
    };
    public static string SubtitleLeads      => Code switch
    {
        Salon => "Track prospective clients and walk-in inquiries",
        _     => "Track potential customers and opportunities"
    };
    public static string SubtitleQuotations => Code switch
    {
        Salon => "Manage appointment bookings and service reservations",
        _     => "Manage proposals and quotation records"
    };
    public static string SubtitleProjects   => Code switch
    {
        Salon => "Track ongoing styling sessions and service delivery",
        _     => "Monitor your client projects and execution progress"
    };
    public static string SubtitleActivities => Code switch
    {
        Salon => "Client follow-ups, reminders, and post-service check-ins",
        _     => "Track client interactions and follow-ups"
    };
    public static string SubtitleIssues     => Code switch
    {
        Salon => "Service complaints, rework requests, and resolution tracking",
        _     => "Complaints, adjustments, and rework requests"
    };
    public static string SubtitleFeedback   => Code switch
    {
        Salon => "Client reviews, ratings, and stylist performance feedback",
        _     => "Customer feedback and ratings"
    };
    public static string SubtitleDesigners  => Code switch
    {
        Salon => "Manage stylists, schedule availability, and performance",
        _     => "Manage team members and view performance ratings"
    };
    public static string SubtitleRetention  => Code switch
    {
        Donut => "Loyalty points, reward tiers, and member engagement programs",
        _     => "Customer segments & recommended actions"
    };
    public static string SubtitlePromotions => Code switch
    {
        Donut => "Combo offers, product deals, and limited-time discounts",
        _     => "Create and manage promotional offers"
    };
    public static string SubtitleAnalytics  => Code switch
    {
        Donut => "Daily sales trends, best sellers, and revenue breakdown",
        _     => "KPIs, trends, and retention intelligence"
    };
    public static string SubtitleReports    => Code switch
    {
        Donut => "Sales reports, product performance, and franchise summaries",
        _     => "Review company performance and records"
    };

    // ── Dashboard KPI card labels ──────────────────────────────────────────
    public static string KpiCustomersLabel => Code switch
    {
        Salon => "TOTAL CLIENTS",
        _     => "TOTAL CUSTOMERS"
    };
    public static string KpiLeadsLabel     => Code switch
    {
        Salon => "OPEN INQUIRIES",
        _     => "OPEN LEADS"
    };
    public static string KpiProjectsLabel  => Code switch
    {
        Salon => "ACTIVE SERVICES",
        _     => "ACTIVE PROJECTS"
    };
    public static string KpiRevenueLabel   => Code switch
    {
        Donut => "TODAY'S SALES",
        Salon => "MONTH REVENUE",
        _     => "QUOTATION VALUE"
    };

    // ── Action button labels ───────────────────────────────────────────────
    public static string BtnNewCustomer   => Code switch { Salon => "＋  New Client",     _ => "＋  New Customer"  };
    public static string BtnNewLead       => Code switch { Salon => "＋  New Inquiry",    _ => "＋  New Lead"      };
    public static string BtnNewQuotation  => Code switch { Salon => "＋  Book Appointment", _ => "＋  New Quotation" };
    public static string BtnNewProject    => Code switch { Salon => "＋  New Service",    _ => "＋  New Project"   };
    public static string BtnNewRetention  => Code switch { Donut => "＋  Add Member",     _ => "＋  Retain Any Customer" };
    public static string BtnNewPromotion  => Code switch { Donut => "＋  New Deal",       _ => "＋  New Promotion" };

    // ── Retention page labels ──────────────────────────────────────────────
    public static string RetentionPageTitle    => Code switch { Donut => "Customer Loyalty Programs", _ => "Retention" };
    public static string RetentionCardLabel    => Code switch { Donut => "LOYALTY MEMBERS",           _ => "RETAINED CUSTOMERS" };
    public static string RetentionRiskLabel    => Code switch { Donut => "INACTIVE MEMBERS",          _ => "AT-RISK CUSTOMERS" };
    public static string RetentionWinbackLabel => Code switch { Donut => "WIN-BACK TARGETS",          _ => "WIN-BACK TARGETS" };
    public static string RetentionSegmentCol   => Code switch { Donut => "LOYALTY TIER",              _ => "SEGMENT" };

    // ── Promotions page labels ─────────────────────────────────────────────
    public static string PromotionsPageTitle   => Code switch { Donut => "Product Deals & Offers",   _ => "Promotions" };
    public static string PromotionTypeLabel    => Code switch { Donut => "DEAL TYPE",                 _ => "TYPE" };
    public static string PromotionNewBtn       => Code switch { Donut => "＋  New Deal",              _ => "＋  New Promotion" };

    // ── Helper ─────────────────────────────────────────────────────────────
    public static string Code => Session.CompanyCode ?? string.Empty;
}
