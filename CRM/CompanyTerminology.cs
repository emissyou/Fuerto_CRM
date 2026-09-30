namespace CRM_DesignServices.winforms;

/// <summary>
/// Provides company-specific terminology so that each tenant sees
/// language that matches their industry:
/// - FUERTO: Interior Design Services & Architecture
/// - GILBB: GLI Bahay Builds (Construction Company)
/// - CCDAVAO: Custom Crafters Davao (Renovation & Remodeling Company)
/// </summary>
public static class CompanyTerminology
{
    // ── Company codes ───────────────────────────────────────────────────────
    public const string Fuerto = "FUERTO";
    public const string Gilbb = "GILBB";
    public const string Ccdavao = "CCDAVAO";

    // Backwards-compatibility aliases
    public const string Salon = "GILBB";
    public const string Donut = "CCDAVAO";

    public static bool IsFuerto  => Code.Equals(Fuerto, StringComparison.OrdinalIgnoreCase);
    public static bool IsGilbb   => Code.Equals(Gilbb, StringComparison.OrdinalIgnoreCase) || Code.Equals("GLIBB", StringComparison.OrdinalIgnoreCase) || Code.Equals("LRSALON", StringComparison.OrdinalIgnoreCase);
    public static bool IsCcdavao => Code.Equals(Ccdavao, StringComparison.OrdinalIgnoreCase) || Code.Equals("MRDONUT", StringComparison.OrdinalIgnoreCase);

    // ── Sidebar nav labels ─────────────────────────────────────────────────
    public static string Customers  => "Clients";
    public static string Leads      => IsGilbb ? "Project Inquiries" : IsCcdavao ? "Renovation Leads" : "Leads";
    public static string Quotations => IsGilbb ? "Building Estimates" : IsCcdavao ? "Renovation Quotes" : "Quotations";
    public static string Projects   => IsGilbb ? "Construction Projects" : IsCcdavao ? "Renovation Projects" : "Projects";
    public static string Activities => IsGilbb ? "Site Visits & Inspections" : IsCcdavao ? "Site Consultations" : "Activities";
    public static string Issues     => IsGilbb ? "Site Issues & Defects" : IsCcdavao ? "Rework & Punch List" : "Issues";
    public static string Feedback   => "Client Reviews";
    public static string Designers  => IsGilbb ? "Site Engineers & Architects" : IsCcdavao ? "Craftsmen & Designers" : "Designers";
    public static string Users      => "Team Accounts";
    public static string Retention  => "Retention & Accounts";
    public static string Promotions => "Packages & Promotions";
    public static string Overview   => "Overview";
    public static string Analytics  => "Analytics";
    public static string Reports    => "Reports";

    // ── Group labels in the sidebar ────────────────────────────────────────
    public static string GroupSalesCrm   => IsGilbb ? "CONSTRUCTION SALES" : IsCcdavao ? "RENOVATION CLIENTS" : "SALES & CRM";
    public static string GroupOperations => IsGilbb ? "CONSTRUCTION OPS"   : IsCcdavao ? "RENOVATION OPS"     : "OPERATIONS";
    public static string GroupInsights   => "INSIGHTS";

    // ── Page subtitles ─────────────────────────────────────────────────────
    public static string SubtitleOverview => IsGilbb
        ? "Live snapshot of construction milestones, bids, and active job sites"
        : IsCcdavao
            ? "Real-time renovation progress, remodeling estimates, and client contracts"
            : "Live snapshot of your company records";

    public static string SubtitleCustomers => IsGilbb
        ? "Manage property owners, general contractors, and corporate client accounts"
        : IsCcdavao
            ? "Manage homeowner and commercial remodeling client profiles"
            : "Manage and view your client relationships";

    public static string SubtitleLeads => IsGilbb
        ? "Track prospective build projects, architectural bids, and plot inquiries"
        : IsCcdavao
            ? "Track residential and commercial renovation project inquiries"
            : "Track potential customers and opportunities";

    public static string SubtitleQuotations => IsGilbb
        ? "Prepare, send, and track construction estimates and bills of quantities"
        : IsCcdavao
            ? "Manage remodeling bids, material schedules, and renovation proposals"
            : "Manage proposals and quotation records";

    public static string SubtitleProjects => IsGilbb
        ? "Monitor structural phases, site milestones, and turnkey construction progress"
        : IsCcdavao
            ? "Track remodeling phases, craftsmanship milestones, and completion dates"
            : "Monitor your client projects and execution progress";

    public static string SubtitleActivities => IsGilbb
        ? "Site visits, engineering inspections, concrete pouring logs, and client check-ins"
        : IsCcdavao
            ? "On-site measurements, design consultations, and client follow-ups"
            : "Track client interactions and follow-ups";

    public static string SubtitleIssues => IsGilbb
        ? "Site punch list, engineering defect notices, weather delays, and structural rework"
        : IsCcdavao
            ? "Carpentry adjustments, material snags, and renovation punch list items"
            : "Complaints, adjustments, and rework requests";

    public static string SubtitleFeedback => IsGilbb
        ? "Homeowner and developer satisfaction ratings and construction reviews"
        : IsCcdavao
            ? "Client feedback on renovation quality, finish craftsmanship, and cleanliness"
            : "Customer feedback and ratings";

    public static string SubtitleDesigners => IsGilbb
        ? "Manage licensed civil engineers, project architects, and site foremen"
        : IsCcdavao
            ? "Manage master carpenters, interior remodelers, and finish craftsmen"
            : "Manage team members and view performance ratings";

    public static string SubtitleRetention  => "Customer segments & account retention actions";
    public static string SubtitlePromotions => "Seasonal packages, referral programs, and project bundles";
    public static string SubtitleAnalytics  => "Revenue trends, milestone completion rates, and profit margin analysis";
    public static string SubtitleReports    => "Comprehensive operational, financial, and construction reports";

    // ── Dashboard KPI card labels ──────────────────────────────────────────
    public static string KpiCustomersLabel => "TOTAL CLIENTS";
    public static string KpiLeadsLabel     => IsGilbb ? "BUILD INQUIRIES" : IsCcdavao ? "RENOVATION LEADS" : "OPEN LEADS";
    public static string KpiProjectsLabel  => IsGilbb ? "ACTIVE BUILDS" : IsCcdavao ? "ACTIVE RENOVATIONS" : "ACTIVE PROJECTS";
    public static string KpiRevenueLabel   => IsGilbb ? "CONSTRUCTION VALUE" : IsCcdavao ? "RENOVATION VALUE" : "QUOTATION VALUE";

    // ── Action button labels ───────────────────────────────────────────────
    public static string BtnNewCustomer   => "＋  New Client";
    public static string BtnNewLead       => IsGilbb ? "＋  New Build Inquiry" : IsCcdavao ? "＋  New Renovation Lead" : "＋  New Lead";
    public static string BtnNewQuotation  => IsGilbb ? "＋  New Estimate" : IsCcdavao ? "＋  New Renovation Quote" : "＋  New Quotation";
    public static string BtnNewProject    => IsGilbb ? "＋  New Construction" : IsCcdavao ? "＋  New Renovation" : "＋  New Project";
    public static string BtnNewRetention  => "＋  Retain Any Client";
    public static string BtnNewPromotion  => "＋  New Promotion";

    // ── Retention page labels ──────────────────────────────────────────────
    public static string RetentionPageTitle    => "Client Retention & Accounts";
    public static string RetentionCardLabel    => "RETAINED CLIENTS";
    public static string RetentionRiskLabel    => "AT-RISK CLIENTS";
    public static string RetentionWinbackLabel => "WIN-BACK TARGETS";
    public static string RetentionSegmentCol   => "SEGMENT";

    // ── Promotions page labels ─────────────────────────────────────────────
    public static string PromotionsPageTitle   => "Packages & Seasonal Offers";
    public static string PromotionTypeLabel    => "OFFER TYPE";
    public static string PromotionNewBtn       => "＋  New Offer";

    // ── Helper ─────────────────────────────────────────────────────────────
    public static string Code => Session.CompanyCode ?? string.Empty;
}
