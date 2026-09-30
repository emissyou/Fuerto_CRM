-- =========================================================================
-- Seed rich data for Leo Revita Salon (CompanyId = 2)
-- =========================================================================

-- 1. LEADS (20 Salon Leads)
IF (SELECT COUNT(*) FROM Leads WHERE CompanyId = 2) = 0
BEGIN
    INSERT INTO Leads (CompanyId, FirstName, LastName, Email, Phone, Status, LeadSource, ServiceInterest, Notes, IsActive, CreatedAt, ConvertedByUserId)
    VALUES
    (2, 'Patricia', 'Mercado', 'patricia.mercado@gmail.com', '09175551001', 'New', 'Instagram', 'Balayage & Toner', 'Inquiring about full ash blonde balayage and toner.', 1, DATEADD(day, -2, GETUTCDATE()), 'None'),
    (2, 'Stephanie', 'Soriano', 'stephanie.soriano@yahoo.com', '09175551002', 'Contacted', 'TikTok', 'Bridal Entourage', 'Wants bridal hair and makeup package for 8 bridesmaids.', 1, DATEADD(day, -4, GETUTCDATE()), 'None'),
    (2, 'Giselle', 'Lim', 'giselle.lim@outlook.com', '09175551003', 'Qualified', 'Facebook Ads', 'Keratin Therapy', 'Interested in Keratin Brazilian blowout treatment.', 1, DATEADD(day, -6, GETUTCDATE()), 'None'),
    (2, 'Katrina', 'Villanueva', 'kat.villanueva@gmail.com', '09175551004', 'Proposal', 'Referral', 'Corporate Gala Styling', 'Corporate hair styling package for annual gala.', 1, DATEADD(day, -8, GETUTCDATE()), 'None'),
    (2, 'Christine', 'Tan', 'christine.tan@gmail.com', '09175551005', 'Won', 'Walk-in', 'Japanese Rebonding', 'Booked Japanese silk rebonding and moisture lock.', 1, DATEADD(day, -10, GETUTCDATE()), 'None'),
    (2, 'Angelica', 'Roxas', 'angelica.roxas@phmail.com', '09175551006', 'New', 'Instagram', 'Fashion Hair Color', 'Interested in pastel pink fashion color transformation.', 1, DATEADD(day, -12, GETUTCDATE()), 'None'),
    (2, 'Monique', 'Castillo', 'monique.c@gmail.com', '09175551007', 'Contacted', 'Google Search', 'Scalp Detox Therapy', 'Looking for scalp detox and anti-hairfall therapy.', 1, DATEADD(day, -14, GETUTCDATE()), 'None'),
    (2, 'Denise', 'Aquino', 'denise.aquino@yahoo.com', '09175551008', 'Qualified', 'Instagram', 'Digital Wave Perm', 'Wants Korean C-Curl digital wave perm consultation.', 1, DATEADD(day, -16, GETUTCDATE()), 'None'),
    (2, 'Bernadette', 'Navarro', 'b.navarro@gmail.com', '09175551009', 'Proposal', 'TikTok', 'Olaplex Hair Repair', 'Inquiring on Olaplex complete bond builder service.', 1, DATEADD(day, -18, GETUTCDATE()), 'None'),
    (2, 'Roxanne', 'Domingo', 'roxanne.domingo@gmail.com', '09175551010', 'Won', 'Referral', 'VIP Membership', 'Signed up for salon VIP membership card.', 1, DATEADD(day, -20, GETUTCDATE()), 'None'),
    (2, 'Charmaine', 'Pascual', 'charmaine.p@phmail.com', '09175551011', 'New', 'Facebook Ads', 'Root Retouch', 'Balayage retouch and shadow root inquiry.', 1, DATEADD(day, -22, GETUTCDATE()), 'None'),
    (2, 'Eileen', 'Flores', 'eileen.flores@outlook.com', '09175551012', 'Contacted', 'Walk-in', 'Nail Spa & Gel Polish', 'Express gel spa manicure and pedicure booking.', 1, DATEADD(day, -24, GETUTCDATE()), 'None'),
    (2, 'Hazel', 'Salazar', 'hazel.salazar@gmail.com', '09175551013', 'Qualified', 'Google Search', 'Botanical Hair Spa', 'Deep conditioning botanical hair treatment package.', 1, DATEADD(day, -26, GETUTCDATE()), 'None'),
    (2, 'Joanna', 'Perez', 'joanna.perez@yahoo.com', '09175551014', 'Proposal', 'Instagram', 'Complete Makeover', 'Hair makeover before international travel.', 1, DATEADD(day, -28, GETUTCDATE()), 'None'),
    (2, 'Alyssa', 'Torres', 'alyssa.torres@gmail.com', '09175551015', 'Won', 'TikTok', 'Graduation Makeup', 'Airbrush makeup for graduation photoshoot.', 1, DATEADD(day, -30, GETUTCDATE()), 'None'),
    (2, 'Bianca', 'Mendoza', 'bianca.mendoza@phmail.com', '09175551016', 'New', 'Referral', 'Hair Botox Treatment', 'Hair botox smoothing inquiry for damaged bleached hair.', 1, DATEADD(day, -32, GETUTCDATE()), 'None'),
    (2, 'Clara', 'Garcia', 'clara.garcia@gmail.com', '09175551017', 'Contacted', 'Facebook Ads', 'Men Executive Grooming', 'Men executive fade haircut and scalp massage inquiry.', 1, DATEADD(day, -34, GETUTCDATE()), 'None'),
    (2, 'Daphne', 'Ocampo', 'daphne.ocampo@outlook.com', '09175551018', 'Qualified', 'Instagram', 'Ombre Highlights', 'Ombre caramel highlights consultation request.', 1, DATEADD(day, -36, GETUTCDATE()), 'None'),
    (2, 'Fiona', 'Bautista', 'fiona.bautista@gmail.com', '09175551019', 'Proposal', 'TikTok', 'Bridal Entourage Styling', 'Bridal entourage booking for December wedding.', 1, DATEADD(day, -38, GETUTCDATE()), 'None'),
    (2, 'Gemma', 'Cruz', 'gemma.cruz@yahoo.com', '09175551020', 'Won', 'Google Search', 'Platinum Blonde Bleach', 'Booked full platinum blonde bleach & toning.', 1, DATEADD(day, -40, GETUTCDATE()), 'None');
END;

-- 2. PROMOTIONS (8 Salon Promotions)
IF (SELECT COUNT(*) FROM Promotions WHERE CompanyId = 2) = 0
BEGIN
    INSERT INTO Promotions (CompanyId, Name, Code, Description, OfferType, OfferValue, TargetSegment, ValidFrom, ValidUntil, IsActive, MaxUses, UsedCount, Notes, CreatedByUserId, CreatedByName, CreatedAt)
    VALUES
    (2, 'Balayage & Tone 20% Off', 'BALAYAGE20', 'Get 20% off on all Balayage and Toner packages.', 'Percentage', 20.00, 'Any', DATEADD(day, -30, GETUTCDATE()), DATEADD(day, 60, GETUTCDATE()), 1, 100, 24, 'All branches', 'Admin', 'Salon Admin', GETUTCDATE()),
    (2, 'Bridal Entourage Package ₱2,000 Off', 'BRIDALGLOW', '₱2,000 discount on bridal entourage hair & makeup for 5+ pax.', 'FixedAmount', 2000.00, 'VIP', DATEADD(day, -15, GETUTCDATE()), DATEADD(day, 90, GETUTCDATE()), 1, 50, 11, 'Requires advance deposit', 'Admin', 'Salon Admin', GETUTCDATE()),
    (2, 'Keratin Brazilian Therapy 15% Off', 'KERATIN15', '15% discount on Brazilian Blowout and Keratin moisture therapy.', 'Percentage', 15.00, 'Any', DATEADD(day, -20, GETUTCDATE()), DATEADD(day, 45, GETUTCDATE()), 1, 200, 58, 'Valid weekdays only', 'Admin', 'Salon Admin', GETUTCDATE()),
    (2, 'Weekday Hair Spa Treat', 'WEEKDAYSPA', 'Complimentary botanical hair spa with any hair color service.', 'FreeService', 850.00, 'Loyal', DATEADD(day, -10, GETUTCDATE()), DATEADD(day, 30, GETUTCDATE()), 1, 80, 32, 'Mon-Thu bookings', 'Admin', 'Salon Admin', GETUTCDATE()),
    (2, 'Japanese Rebonding & Moisture ₱1,000 Off', 'REBOND1000', '₱1,000 off on Japanese Silk Rebonding with free argan mask.', 'FixedAmount', 1000.00, 'Any', DATEADD(day, -25, GETUTCDATE()), DATEADD(day, 60, GETUTCDATE()), 1, 120, 44, 'Senior stylist included', 'Admin', 'Salon Admin', GETUTCDATE()),
    (2, 'VIP Birthday Glam Voucher 25% Off', 'BDAYGLAM25', 'Birthday celebration voucher: 25% off on total service bill.', 'Percentage', 25.00, 'VIP', DATEADD(day, -60, GETUTCDATE()), DATEADD(day, 120, GETUTCDATE()), 1, 150, 67, 'Valid birthday month', 'Admin', 'Salon Admin', GETUTCDATE()),
    (2, 'Hair Botox Anti-Frizz 15% Off', 'BOTOX15', '15% discount on deep restorative Hair Botox treatments.', 'Percentage', 15.00, 'AtRisk', DATEADD(day, -10, GETUTCDATE()), DATEADD(day, 45, GETUTCDATE()), 1, 75, 19, 'Re-engagement offer', 'Admin', 'Salon Admin', GETUTCDATE()),
    (2, 'Olaplex Hair Repair Promo', 'OLAPLEX500', '₱500 off complete Olaplex bond reconstruction system.', 'FixedAmount', 500.00, 'Champion', DATEADD(day, -5, GETUTCDATE()), DATEADD(day, 40, GETUTCDATE()), 1, 50, 15, 'Champion VIP clients', 'Admin', 'Salon Admin', GETUTCDATE());
END;

-- 3. PROJECT ISSUES / COMPLAINTS (30 Salon Issues)
IF (SELECT COUNT(*) FROM ProjectIssues WHERE CompanyId = 2) = 0
BEGIN
    INSERT INTO ProjectIssues (CompanyId, ProjectId, CustomerId, IssueType, Severity, Status, Title, Description, ResolutionNotes, ResolvedByUserId, ResolvedAt, RequestedAction, ReportedByUserId, ReportedAt, TargetResolutionDate, IsActive)
    SELECT TOP 30
        2,
        p.ProjectId,
        p.CustomerId,
        CASE (p.ProjectId % 4)
            WHEN 0 THEN 'Complaint'
            WHEN 1 THEN 'Adjustment'
            WHEN 2 THEN 'Rework'
            ELSE 'Other'
        END,
        CASE (p.ProjectId % 3)
            WHEN 0 THEN 'Low'
            WHEN 1 THEN 'Medium'
            ELSE 'High'
        END,
        CASE (p.ProjectId % 3)
            WHEN 0 THEN 'Closed'
            WHEN 1 THEN 'Resolved'
            ELSE 'Open'
        END,
        CASE (p.ProjectId % 5)
            WHEN 0 THEN 'Toner brassiness adjustment requested'
            WHEN 1 THEN 'Bangs length slight trimming needed'
            WHEN 2 THEN 'Scalp sensitivity during bleach application'
            WHEN 3 THEN 'Appointment rescheduling request'
            ELSE 'Curl definition maintenance advice requested'
        END,
        'Client requested follow-up adjustment after treatment session.',
        'Provided complimentary gloss treatment and aftercare conditioning mask.',
        'Senior Stylist',
        DATEADD(day, 2, p.CreatedAt),
        'Follow-up gloss & tone service',
        'Front Desk',
        p.CreatedAt,
        DATEADD(day, 5, p.CreatedAt),
        1
    FROM Projects p
    WHERE p.CompanyId = 2;
END;

-- 4. PROJECT FEEDBACKS (50 Salon Reviews)
IF (SELECT COUNT(*) FROM ProjectFeedbacks WHERE CompanyId = 2) = 0
BEGIN
    INSERT INTO ProjectFeedbacks (CompanyId, ProjectId, CustomerId, OverallRating, TimelinessRating, CommunicationRating, ValueRating, Comments, DesignLikes, DesignImprovements, WouldRecommend, SubmittedAt, SubmittedByUserId)
    SELECT TOP 50
        2,
        p.ProjectId,
        p.CustomerId,
        4 + (p.ProjectId % 2),
        4 + ((p.ProjectId + 1) % 2),
        5,
        4 + (p.ProjectId % 2),
        CASE (p.ProjectId % 6)
            WHEN 0 THEN 'Loved the balayage outcome! Matched my inspo photo perfectly and my hair feels so soft.'
            WHEN 1 THEN 'Super polite senior stylist and the head massage during shampoo was incredible!'
            WHEN 2 THEN 'The Brazilian keratin made my stubborn curls manageable in under two hours. 10/10.'
            WHEN 3 THEN 'Very clean tools and elegant salon vibe at SM North branch. Highly recommended!'
            WHEN 4 THEN 'Excellent service! My bridal makeup and hair stayed fresh through the entire wedding.'
            ELSE 'Great customer service and transparent pricing. Will definitely be a regular here.'
        END,
        'Stylist consulted thoroughly on face shape and undertone before starting.',
        'Would love if tea/coffee was served during long bleaching sessions.',
        1,
        DATEADD(day, 1, p.CreatedAt),
        'Customer Portal'
    FROM Projects p
    WHERE p.CompanyId = 2;
END;

-- 5. ACTIVITIES (80 Salon Activities)
IF (SELECT COUNT(*) FROM Activities WHERE CompanyId = 2) = 0
BEGIN
    INSERT INTO Activities (CompanyId, CustomerId, ProjectId, ActivityType, Subject, Description, ActivityDate, FollowUpDate, Status, Notes)
    SELECT TOP 80
        2,
        p.CustomerId,
        p.ProjectId,
        CASE (p.ProjectId % 4)
            WHEN 0 THEN 'Phone Call'
            WHEN 1 THEN 'Email'
            WHEN 2 THEN 'Meeting'
            ELSE 'Follow-up'
        END,
        CASE (p.ProjectId % 5)
            WHEN 0 THEN 'Appointment Confirmation & Reminder'
            WHEN 1 THEN 'Post-Treatment Haircare Check-in'
            WHEN 2 THEN 'Bridal Styling Consultation Call'
            WHEN 3 THEN 'Color Retouch Recommendation SMS'
            ELSE 'VIP Birthday Offer Notification'
        END,
        'Automated & staff follow-up for client appointment and service care.',
        p.CreatedAt,
        DATEADD(day, 14, p.CreatedAt),
        'Completed',
        'Client was very responsive and appreciated the prompt reminder.'
    FROM Projects p
    WHERE p.CompanyId = 2;
END;

-- 6. RETENTION ACTIONS (40 Salon Retention Actions)
IF (SELECT COUNT(*) FROM RetentionActions WHERE CompanyId = 2) = 0
BEGIN
    INSERT INTO RetentionActions (CompanyId, CustomerId, ProjectId, OfferType, OfferValue, OfferDescription, Segment, Basis, Notes, ScriptUsed, Source, ActionTaken, Status, FollowUpDate, CreatedByUserId, CreatedByName, CreatedAt)
    SELECT TOP 40
        2,
        c.CustomerId,
        NULL,
        CASE (c.CustomerId % 2) WHEN 0 THEN 'Percentage' ELSE 'FixedAmount' END,
        CASE (c.CustomerId % 2) WHEN 0 THEN 20.00 ELSE 500.00 END,
        CASE (c.CustomerId % 2) WHEN 0 THEN '20% discount on next Hair Spa or Treatment' ELSE '₱500 voucher on signature color services' END,
        c.CustomerType,
        'RFM Retention campaign for active salon clients.',
        'Sent via automated SMS and Email notification.',
        'Hi [Client], we miss you at Leo Revita Salon! Enjoy this exclusive gift on your next visit.',
        'Automated',
        'Dispatched Email & SMS voucher',
        'Logged',
        DATEADD(day, 30, GETUTCDATE()),
        'System',
        'Retention Engine',
        DATEADD(day, -c.CustomerId % 60, GETUTCDATE())
    FROM Customers c
    WHERE c.CompanyId = 2;
END;
