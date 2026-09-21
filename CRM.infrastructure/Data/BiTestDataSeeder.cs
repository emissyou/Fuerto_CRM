using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Data;

public static class BiTestDataSeeder
{
    private static readonly Random Rng = new(42);

    // The 5 new designers (seniors first)
    private static readonly string[] SeniorEmails =
    {
        "marco.reyes@fuerto.local",
        "sofia.lim@fuerto.local"
    };

    private static readonly string[] JuniorEmails =
    {
        "diego.santos@fuerto.local",
        "elena.cruz@fuerto.local",
        "rafael.tan@fuerto.local"
    };

    private static readonly string[] FirstNames =
    {
        "Maria", "Juan", "Pedro", "Ana", "Jose", "Rosa", "Carlos", "Lucia",
        "Miguel", "Sofia", "Rafael", "Elena", "Antonio", "Isabel", "Diego",
        "Carmen", "Manuel", "Teresa", "Ricardo", "Beatriz", "Fernando",
        "Gloria", "Alberto", "Pilar", "Eduardo", "Marta", "Sergio", "Julia",
        "Andres", "Monica", "Javier", "Sandra", "Roberto", "Patricia",
        "Alfonso", "Veronica", "Emilio", "Natalia", "Arturo", "Daniela"
    };

    private static readonly string[] LastNames =
    {
        "Santos", "Reyes", "Cruz", "Bautista", "Ocampo", "Garcia", "Mendoza",
        "Torres", "Ramos", "Gonzales", "Aquino", "Villanueva", "Castillo",
        "Flores", "Rivera", "Domingo", "Navarro", "Salazar", "Pascual",
        "Fernandez", "Lopez", "Perez", "Roman", "Aguilar", "Fernando",
        "Roxas", "Lim", "Tan", "Chua", "Sy"
    };

    private static readonly string[] Locations =
    {
        "Makati", "BGC", "Ortigas", "Quezon City", "Alabang",
        "Pasig", "Mandaluyong", "San Juan", "Taguig", "Paranaque"
    };

    private static readonly string[] ProjectTypes =
    {
        "Interior Design", "Renovation", "Office Setup", "Condo Fit-out",
        "Kitchen Remodel", "Bedroom Makeover", "Living Room Design",
        "Commercial Space", "Cafe Design", "Retail Store"
    };

    private static readonly string[] LeadSources =
    {
        "Referral", "Website", "Instagram", "Facebook", "Walk-in", "Google Ads"
    };

    private static readonly string[] Comments =
    {
        "Excellent work! The design exceeded our expectations.",
        "Very professional team. Highly recommended.",
        "Good quality but took a bit longer than expected.",
        "The designers really understood our vision.",
        "Beautiful result. We love our new space.",
        "Great communication throughout the project.",
        "Good value for the price.",
        "The team was responsive and creative.",
        "Slight delay but worth the wait.",
        "Will definitely hire again for our next project."
    };

    private static readonly string[] ImprovementComments =
    {
        "Could have finished earlier.",
        "Some minor paint touch-ups needed.",
        "Communication about timelines could improve.",
        "Pricing could be more transparent.",
        "A few fixtures arrived late.",
        "Nothing major — very satisfied overall."
    };

    private static readonly string[] IssueTitles =
    {
        "Paint color slightly off",
        "Cabinet handle misaligned",
        "Outlet not working",
        "Tile grout needed rework",
        "Lighting fixture delay",
        "Door trim needs adjustment",
        "Wallpaper seam visible",
        "Cabinet color adjustment",
        "Flooring scuff marks"
    };

    // ⚠️ DEV ONLY — takes an explicit map of designer email → (UserId, FullName)
    public static async Task<(
        int customers, int projects, int quotations, int feedbacks, int issues)>
        SeedAsync(
            TenantErpDbContext db,
            int companyId,
            Dictionary<string, (string UserId, string FullName)> designers,
            int targetTransactions = 250)
    {
        // ---- Wipe existing data ----
        await db.ProjectIssues.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.ProjectFeedbacks.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.Activities.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.QuotationItems.Where(x => db.Quotations.Any(q => q.QuotationId == x.QuotationId && q.CompanyId == companyId)).ExecuteDeleteAsync();
        await db.Quotations.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.Projects.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.Customers.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.Leads.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();

        int customerCount = 0, projectCount = 0, quotationCount = 0,
            feedbackCount = 0, issueCount = 0;

        int customerTarget = targetTransactions;
        int leadTarget = (int)(customerTarget * 1.4);

        // ---- Leads ----
        var leads = new List<Lead>();
        for (int i = 0; i < leadTarget; i++)
        {
            var fn = Pick(FirstNames);
            var ln = Pick(LastNames);
            var createdAt = RandomDateWithin(720);

            leads.Add(new Lead
            {
                CompanyId = companyId,
                FirstName = fn,
                LastName = ln,
                Email = $"{fn.ToLower()}.{ln.ToLower()}{i}@test.local",
                Phone = $"0917{Rng.Next(1000000, 9999999)}",
                LeadSource = Pick(LeadSources),
                ServiceInterest = Pick(ProjectTypes),
                Status = i < customerTarget
                    ? "Converted"
                    : Pick(new[] { "New", "Contacted", "Qualified", "Lost" }),
                Notes = "BI test data",
                IsActive = true,
                CreatedAt = createdAt
            });
        }
        db.Leads.AddRange(leads);
        await db.SaveChangesAsync();

        var convertedLeads = leads.Take(customerTarget).ToList();

        foreach (var lead in convertedLeads)
        {
            var customer = new Customer
            {
                CompanyId = companyId,
                FirstName = lead.FirstName,
                LastName = lead.LastName,
                Email = lead.Email,
                Phone = lead.Phone,
                Address = $"{Rng.Next(1, 999)} {Pick(Locations)} St.",
                CustomerType = Pick(new[] { "Regular", "VIP", "Regular", "Regular", "Corporate" }),
                Notes = "BI test data",
                IsActive = true,
                CreatedAt = lead.CreatedAt.AddDays(Rng.Next(1, 5)),
                ConvertedFromLeadId = lead.LeadId
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            customerCount++;

            lead.Status = "Converted";
            lead.ConvertedToCustomerId = customer.CustomerId;
            lead.ConvertedAt = customer.CreatedAt;
            lead.ConvertedByUserId = "seeder";

            int projectCountForCustomer = 1;
            if (Rng.NextDouble() < 0.30) projectCountForCustomer = 2;
            if (Rng.NextDouble() < 0.10) projectCountForCustomer = 3;

            DateTime projectBaseDate = customer.CreatedAt.AddDays(Rng.Next(3, 14));

            for (int p = 0; p < projectCountForCustomer; p++)
            {
                var projectDate = projectBaseDate.AddDays(Rng.Next(180, 400) * p);
                if (projectDate > DateTime.UtcNow.AddDays(-15))
                    projectDate = DateTime.UtcNow.AddDays(-15);

                // ---- Pick a designer ----
                var designerEmail = PickDesignerEmail();
                if (!designers.TryGetValue(designerEmail, out var designerInfo))
                {
                    // Fallback: pick first available designer
                    var fallback = designers.FirstOrDefault();
                    designerEmail = fallback.Key;
                    designerInfo = fallback.Value;
                }

                var isSenior = SeniorEmails.Contains(designerEmail);

                var project = new Project
                {
                    CompanyId = companyId,
                    ProjectCode = $"PRJ-{projectDate:yyyyMM}-{projectCount + 1:D4}",
                    ProjectName = $"{customer.FirstName} {customer.LastName} - {Pick(ProjectTypes)}",
                    CustomerId = customer.CustomerId,
                    ProjectType = Pick(ProjectTypes),
                    Location = Pick(Locations),
                    Description = "BI test data",
                    Status = "Completed",
                    DesignStage = ProjectDesignStage.Completed,
                    ProgressPercentage = 100,
                    IsActive = true,
                    CreatedAt = projectDate,
                    DesignStartDate = projectDate.AddDays(Rng.Next(1, 7)),
                    DesignCompletionDate = projectDate.AddDays(Rng.Next(30, 90)),
                    DesignerId = designerInfo.UserId,
                    DesignerName = designerInfo.FullName,
                    DesignerAssignedAt = projectDate.AddDays(Rng.Next(1, 7)),
                    DesignerAssignedBy = "seeder"
                };
                db.Projects.Add(project);
                await db.SaveChangesAsync();
                projectCount++;

                var subtotal = RandomAmount(20000, 500000);
                var discount = Rng.NextDouble() < 0.3 ? Math.Round(subtotal * 0.05m, 2) : 0m;

                var quote = new Quotation
                {
                    CompanyId = companyId,
                    QuotationNumber = $"QT-{projectDate:yyyyMM}-{quotationCount + 1:D4}",
                    ProjectId = project.ProjectId,
                    CustomerId = customer.CustomerId,
                    QuotationDate = projectDate.AddDays(Rng.Next(1, 10)),
                    Subtotal = subtotal,
                    Discount = discount,
                    TotalAmount = subtotal - discount,
                    Status = QuotationStatus.Accepted,
                    AmountPaid = subtotal - discount,
                    DepositRequired = Math.Round((subtotal - discount) * 0.5m, 2),
                    PaymentStatus = PaymentStatus.FullyPaid,
                    PaymentMethod = Pick(new[] { "Bank Transfer", "Cash", "GCash", "Check" }),
                    PaymentReference = $"REF-{Rng.Next(100000, 999999)}",
                    IssuedAt = projectDate.AddDays(Rng.Next(1, 5)),
                    IssuedByUserId = "seeder",
                    AcceptedAt = projectDate.AddDays(Rng.Next(5, 12)),
                    DepositPaidDate = projectDate.AddDays(Rng.Next(12, 20)),
                    FullyPaidDate = projectDate.AddDays(Rng.Next(30, 90)),
                    CreatedAt = projectDate
                };
                db.Quotations.Add(quote);
                await db.SaveChangesAsync();
                quotationCount++;

                project.AcceptedQuotationId = quote.QuotationId;

                db.Activities.Add(new Activity
                {
                    CompanyId = companyId,
                    ProjectId = project.ProjectId,
                    CustomerId = customer.CustomerId,
                    ActivityType = "LeadConverted",
                    Subject = "Lead converted to Customer & Project",
                    Description = "BI seed",
                    ActivityDate = projectDate,
                    Status = "Completed"
                });

                // ---- Feedback (seniors get better ratings) ----
                if (project.DesignStage == ProjectDesignStage.Completed && Rng.NextDouble() < 0.80)
                {
                    int WeightedRating()
                    {
                        var roll = Rng.NextDouble();
                        if (isSenior)
                        {
                            return roll switch
                            {
                                < 0.02 => 1,
                                < 0.05 => 2,
                                < 0.10 => 3,
                                < 0.45 => 4,
                                _ => 5
                            };
                        }
                        return roll switch
                        {
                            < 0.05 => 1,
                            < 0.12 => 2,
                            < 0.25 => 3,
                            < 0.60 => 4,
                            _ => 5
                        };
                    }

                    int overall = WeightedRating();
                    int time = WeightedRating();
                    int comm = WeightedRating();
                    int value = WeightedRating();

                    var feedback = new ProjectFeedback
                    {
                        CompanyId = companyId,
                        ProjectId = project.ProjectId,
                        CustomerId = customer.CustomerId,
                        OverallRating = overall,
                        TimelinessRating = time,
                        CommunicationRating = comm,
                        ValueRating = value,
                        Comments = Pick(Comments),
                        DesignLikes = "Loved the layout and finish",
                        DesignImprovements = Pick(ImprovementComments),
                        WouldRecommend = overall >= 4,
                        SubmittedAt = project.DesignCompletionDate?.AddDays(Rng.Next(1, 14)) ?? DateTime.UtcNow,
                        SubmittedByUserId = "seeder"
                    };
                    db.ProjectFeedbacks.Add(feedback);
                    feedbackCount++;

                    db.Activities.Add(new Activity
                    {
                        CompanyId = companyId,
                        ProjectId = project.ProjectId,
                        CustomerId = customer.CustomerId,
                        ActivityType = "FeedbackSubmitted",
                        Subject = $"Customer rated project {overall}/5",
                        Description = feedback.Comments,
                        ActivityDate = feedback.SubmittedAt,
                        Status = "Completed"
                    });
                }

                // ---- Issues (seniors: 10%, juniors: 20%) ----
                var issueChance = isSenior ? 0.10 : 0.20;
                if (Rng.NextDouble() < issueChance)
                {
                    var issue = new ProjectIssue
                    {
                        CompanyId = companyId,
                        ProjectId = project.ProjectId,
                        CustomerId = customer.CustomerId,
                        IssueType = Pick(new[] { "Adjustment", "Complaint", "Rework" }),
                        Severity = Pick(new[] { "Low", "Medium", "Medium", "High" }),
                        Status = Pick(new[] { "Resolved", "Resolved", "Closed", "Open" }),
                        Title = Pick(IssueTitles),
                        Description = "BI test data",
                        RequestedAction = "Please fix",
                        ReportedByUserId = "seeder",
                        ReportedAt = project.DesignCompletionDate?.AddDays(Rng.Next(1, 20)) ?? DateTime.UtcNow,
                        IsActive = true
                    };

                    if (issue.Status == "Resolved" || issue.Status == "Closed")
                    {
                        issue.ResolvedAt = issue.ReportedAt.AddDays(Rng.Next(1, 15));
                        issue.ResolvedByUserId = "seeder";
                        issue.ResolutionNotes = "Fixed and verified";
                    }

                    db.ProjectIssues.Add(issue);
                    issueCount++;

                    db.Activities.Add(new Activity
                    {
                        CompanyId = companyId,
                        ProjectId = project.ProjectId,
                        CustomerId = customer.CustomerId,
                        ActivityType = "IssueReported",
                        Subject = $"{issue.IssueType}: {issue.Title}",
                        Description = issue.Description,
                        ActivityDate = issue.ReportedAt,
                        Status = issue.Status
                    });
                }

                await db.SaveChangesAsync();
            }
        }

        return (customerCount, projectCount, quotationCount, feedbackCount, issueCount);
    }

    // Designers to seed (email + full name) — used by Program.cs to look them up
    public static IEnumerable<(string Email, string FullName)> GetDesignerList()
    {
        yield return ("marco.reyes@fuerto.local", "Marco Reyes");
        yield return ("sofia.lim@fuerto.local", "Sofia Lim");
        yield return ("diego.santos@fuerto.local", "Diego Santos");
        yield return ("elena.cruz@fuerto.local", "Elena Cruz");
        yield return ("rafael.tan@fuerto.local", "Rafael Tan");
    }

    private static string Pick(string[] arr) => arr[Rng.Next(arr.Length)];

    // 65% chance senior (Marco or Sofia), 35% junior (Diego/Elena/Rafael)
    private static string PickDesignerEmail()
    {
        if (Rng.NextDouble() < 0.65)
            return Pick(SeniorEmails);
        return Pick(JuniorEmails);
    }

    private static decimal RandomAmount(int min, int max) =>
        Math.Round((decimal)(Rng.NextDouble() * (max - min) + min), 2);

    private static DateTime RandomDateWithin(int daysAgo)
    {
        var offset = Rng.Next(0, daysAgo);
        return DateTime.UtcNow.AddDays(-offset);
    }
}