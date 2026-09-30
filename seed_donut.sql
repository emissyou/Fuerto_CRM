-- =========================================================================
-- Seed rich data for Mister Donut (CompanyId = 3)
-- =========================================================================

-- 1. LEADS (15 Commercial Leads)
IF (SELECT COUNT(*) FROM Leads WHERE CompanyId = 3) = 0
BEGIN
    INSERT INTO Leads (CompanyId, FirstName, LastName, Email, Phone, Status, LeadSource, ServiceInterest, Notes, IsActive, CreatedAt, ConvertedByUserId)
    VALUES
    (3, 'Ferdinand', 'Marcos', 'f.marcos.franchise@gmail.com', '09176662001', 'New', 'Website', 'Mall Kiosk Franchise', 'Inquiring about mall kiosk franchise in Laguna.', 1, DATEADD(day, -3, GETUTCDATE()), 'None'),
    (3, 'Corazon', 'Aquino', 'c.aquino.catering@gmail.com', '09176662002', 'Contacted', 'Referral', 'Corporate Holiday Catering', 'Corporate holiday donut boxes order for 500 staff.', 1, DATEADD(day, -5, GETUTCDATE()), 'None'),
    (3, 'Ramon', 'Ang', 'r.ang.wholesale@smfoods.ph', '09176662003', 'Qualified', 'Direct Inquiry', 'Convenience Store Consignment', 'Convenience store consignment proposal across 40 branches.', 1, DATEADD(day, -7, GETUTCDATE()), 'None'),
    (3, 'Teresita', 'Sy-Coson', 't.sycoson@smretail.com', '09176662004', 'Proposal', 'Trade Expo', 'Food Hall Counter', 'Food hall permanent counter expansion inquiry.', 1, DATEADD(day, -9, GETUTCDATE()), 'None'),
    (3, 'Lance', 'Gokongwei', 'lance.g@robinsons.com.ph', '09176662005', 'Won', 'Website', 'Office Pantry Supply', 'Signed weekly supply contract for corporate pantry.', 1, DATEADD(day, -11, GETUTCDATE()), 'None'),
    (3, 'Jaime', 'Zobel', 'jaime.z@ayalamalls.com', '09176662006', 'New', 'Referral', 'Mall Event Catering', 'Ayala Malls event catering package request.', 1, DATEADD(day, -13, GETUTCDATE()), 'None'),
    (3, 'Enrique', 'Razon', 'e.razon@ictsi.ph', '09176662007', 'Contacted', 'Website', 'Port Coffee & Donut Cart', 'Port operations night-shift donut & coffee cart contract.', 1, DATEADD(day, -15, GETUTCDATE()), 'None'),
    (3, 'Andrew', 'Tan', 'a.tan@megaworld.com.ph', '09176662008', 'Qualified', 'Trade Expo', 'Office Tower Delivery', 'BGC office tower tenant bulk delivery partnership.', 1, DATEADD(day, -17, GETUTCDATE()), 'None'),
    (3, 'Lucio', 'Tan', 'lucio.t@pnb.com.ph', '09176662009', 'Proposal', 'Referral', 'Anniversary Celebration Snacks', 'PNB nationwide branch anniversary celebration snacks.', 1, DATEADD(day, -19, GETUTCDATE()), 'None'),
    (3, 'Manuel', 'Pangilinan', 'mvp@pldt.com.ph', '09176662010', 'Won', 'Direct Inquiry', 'Sportsfest Event Catering', 'Smart-PLDT sportsfest catering contract for 2,000 pax.', 1, DATEADD(day, -21, GETUTCDATE()), 'None'),
    (3, 'Edgar', 'Sia', 'edgar.sia@manginasal.ph', '09176662011', 'New', 'Website', 'Highway Rest-Stop Concession', 'Highway rest-stop kiosk concession discussion.', 1, DATEADD(day, -23, GETUTCDATE()), 'None'),
    (3, 'Dennis', 'Uy', 'dennis.uy@phoenix.ph', '09176662012', 'Contacted', 'Trade Expo', 'Gas Station Display Stand', 'Gas station quick-bite donut display stand partnership.', 1, DATEADD(day, -25, GETUTCDATE()), 'None'),
    (3, 'Betty', 'Ang', 'b.ang@monde.com.ph', '09176662013', 'Qualified', 'Referral', 'Factory Employee Treats', 'Factory employee monthly appreciation treats supply.', 1, DATEADD(day, -27, GETUTCDATE()), 'None'),
    (3, 'Mercedes', 'Gotianun', 'm.gotianun@filinvest.com', '09176662014', 'Proposal', 'Website', 'Residential Pop-up Kiosk', 'Filinvest City residential community weekend pop-up.', 1, DATEADD(day, -29, GETUTCDATE()), 'None'),
    (3, 'Isidro', 'Consunji', 'i.consunji@dmcinet.com', '09176662015', 'Won', 'Direct Inquiry', 'Site Daily Coffee & Treats', 'Construction project site morning coffee and donut service.', 1, DATEADD(day, -31, GETUTCDATE()), 'None');
END;

-- 2. PROMOTIONS (6 Mister Donut Promotions)
IF (SELECT COUNT(*) FROM Promotions WHERE CompanyId = 3) = 0
BEGIN
    INSERT INTO Promotions (CompanyId, Name, Code, Description, OfferType, OfferValue, TargetSegment, ValidFrom, ValidUntil, IsActive, MaxUses, UsedCount, Notes, CreatedByUserId, CreatedByName, CreatedAt)
    VALUES
    (3, 'Bavarian Dozen Buy 1 Take 1', 'MD-BAV2026', 'Buy 1 dozen Bavarian, get 1 dozen free.', 'Percentage', 50.00, 'Any', DATEADD(day, -30, GETUTCDATE()), DATEADD(day, 60, GETUTCDATE()), 1, 200, 48, 'All stores', 'Admin', 'Donut Admin', GETUTCDATE()),
    (3, 'Corporate Catering 15% Rebate', 'MD-CORP15', '15% rebate on all bulk corporate catering orders exceeding ₱20,000.', 'Percentage', 15.00, 'Corporate', DATEADD(day, -20, GETUTCDATE()), DATEADD(day, 90, GETUTCDATE()), 1, 100, 22, 'B2B Accounts', 'Admin', 'Donut Admin', GETUTCDATE()),
    (3, 'Choco Butternut Holiday Box ₱200 Off', 'MD-CHOCO200', '₱200 discount on premium Choco Butternut tins.', 'FixedAmount', 200.00, 'Loyal', DATEADD(day, -15, GETUTCDATE()), DATEADD(day, 45, GETUTCDATE()), 1, 150, 61, 'Holiday special', 'Admin', 'Donut Admin', GETUTCDATE()),
    (3, 'Smidgets Bite-Sized Party 25% Off', 'MD-SMIDGETS25', '25% discount on 100-pc Smidgets party buckets.', 'Percentage', 25.00, 'Any', DATEADD(day, -10, GETUTCDATE()), DATEADD(day, 30, GETUTCDATE()), 1, 300, 89, 'Party orders', 'Admin', 'Donut Admin', GETUTCDATE()),
    (3, 'Campus Canteen Subsidy ₱300 Off', 'MD-CAMPUS300', '₱300 subsidy on campus canteen wholesale consignments.', 'FixedAmount', 300.00, 'Wholesale', DATEADD(day, -25, GETUTCDATE()), DATEADD(day, 60, GETUTCDATE()), 1, 80, 31, 'Schools & Universities', 'Admin', 'Donut Admin', GETUTCDATE()),
    (3, 'Franchise Partner Launch Perk', 'MD-FRAN2026', 'Free marketing collateral package for new franchise stores.', 'FreeService', 5000.00, 'Franchisee', DATEADD(day, -45, GETUTCDATE()), DATEADD(day, 180, GETUTCDATE()), 1, 20, 7, 'Franchisees only', 'Admin', 'Donut Admin', GETUTCDATE());
END;

-- 3. PROJECT ISSUES / COMPLAINTS (30 Mister Donut Issues)
IF (SELECT COUNT(*) FROM ProjectIssues WHERE CompanyId = 3) = 0
BEGIN
    INSERT INTO ProjectIssues (CompanyId, ProjectId, CustomerId, IssueType, Severity, Status, Title, Description, ResolutionNotes, ResolvedByUserId, ResolvedAt, RequestedAction, ReportedByUserId, ReportedAt, TargetResolutionDate, IsActive)
    SELECT TOP 30
        3,
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
            WHEN 0 THEN 'Morning bulk delivery delayed by 30 mins'
            WHEN 1 THEN 'Donut box packaging crushed during transit'
            WHEN 2 THEN 'Choco Butternut flavor shortage in delivered batch'
            WHEN 3 THEN 'Billing invoice address adjustment needed'
            ELSE 'Kiosk display warmer heating temperature check'
        END,
        'Wholesale partner reported consignment delivery issue during morning store handover.',
        'Dispatched replacement dozen boxes and issued delivery logistics credit memo.',
        'Logistics Supervisor',
        DATEADD(day, 1, p.CreatedAt),
        'Priority replacement delivery',
        'Commissary Dispatch',
        p.CreatedAt,
        DATEADD(day, 3, p.CreatedAt),
        1
    FROM Projects p
    WHERE p.CompanyId = 3;
END;

-- 4. PROJECT FEEDBACKS (50 Mister Donut Reviews)
IF (SELECT COUNT(*) FROM ProjectFeedbacks WHERE CompanyId = 3) = 0
BEGIN
    INSERT INTO ProjectFeedbacks (CompanyId, ProjectId, CustomerId, OverallRating, TimelinessRating, CommunicationRating, ValueRating, Comments, DesignLikes, DesignImprovements, WouldRecommend, SubmittedAt, SubmittedByUserId)
    SELECT TOP 50
        3,
        p.ProjectId,
        p.CustomerId,
        4 + (p.ProjectId % 2),
        4 + ((p.ProjectId + 1) % 2),
        5,
        4 + (p.ProjectId % 2),
        CASE (p.ProjectId % 6)
            WHEN 0 THEN 'Donuts were extremely fresh and still warm when delivered to our office tower. Employees were thrilled!'
            WHEN 1 THEN 'Bavarian filling was rich and generous. Best catering snack choice for our monthly meeting.'
            WHEN 2 THEN 'Consignment delivery was on point right before morning peak hours at our supermarket.'
            WHEN 3 THEN 'Professional delivery riders and clean sanitized food-grade delivery boxes.'
            WHEN 4 THEN 'Choco Butternut donuts are iconic. Excellent value and fast order dispatch.'
            ELSE 'Smooth transaction with Mister Donut B2B team. Highly recommended for company events.'
        END,
        'Flavors clearly separated and boxed with branded parchment lining.',
        'Include more wet wipes or coffee stirrer packets for party orders.',
        1,
        DATEADD(day, 1, p.CreatedAt),
        'B2B Portal'
    FROM Projects p
    WHERE p.CompanyId = 3;
END;

-- 5. ACTIVITIES (80 Mister Donut Commercial Activities)
IF (SELECT COUNT(*) FROM Activities WHERE CompanyId = 3) = 0
BEGIN
    INSERT INTO Activities (CompanyId, CustomerId, ProjectId, ActivityType, Subject, Description, ActivityDate, FollowUpDate, Status, Notes)
    SELECT TOP 80
        3,
        p.CustomerId,
        p.ProjectId,
        CASE (p.ProjectId % 4)
            WHEN 0 THEN 'Phone Call'
            WHEN 1 THEN 'Email'
            WHEN 2 THEN 'Meeting'
            ELSE 'Follow-up'
        END,
        CASE (p.ProjectId % 5)
            WHEN 0 THEN 'Weekly Bulk Standing Order Confirmation'
            WHEN 1 THEN 'Delivery Route & Schedule Coordination'
            WHEN 2 THEN 'Corporate Billing Invoice Follow-up'
            WHEN 3 THEN 'Holiday Donut Pre-Order Consultation'
            ELSE 'Store Kiosk Inventory Restocking Call'
        END,
        'Regular commercial account management and logistics synchronization.',
        p.CreatedAt,
        DATEADD(day, 7, p.CreatedAt),
        'Completed',
        'Account manager confirmed weekly delivery window and item breakdown.'
    FROM Projects p
    WHERE p.CompanyId = 3;
END;

-- 6. RETENTION ACTIONS (40 Mister Donut Retention Actions)
IF (SELECT COUNT(*) FROM RetentionActions WHERE CompanyId = 3) = 0
BEGIN
    INSERT INTO RetentionActions (CompanyId, CustomerId, ProjectId, OfferType, OfferValue, OfferDescription, Segment, Basis, Notes, ScriptUsed, Source, ActionTaken, Status, FollowUpDate, CreatedByUserId, CreatedByName, CreatedAt)
    SELECT TOP 40
        3,
        c.CustomerId,
        NULL,
        CASE (c.CustomerId % 2) WHEN 0 THEN 'Percentage' ELSE 'FixedAmount' END,
        CASE (c.CustomerId % 2) WHEN 0 THEN 15.00 ELSE 1000.00 END,
        CASE (c.CustomerId % 2) WHEN 0 THEN '15% rebate on next corporate bulk order' ELSE '₱1,000 commissary voucher for recurring franchise partners' END,
        c.CustomerType,
        'B2B Wholesale retention program for corporate and wholesale partners.',
        'Sent via automated partner dispatch.',
        'Mister Donut values your partnership. Claim your seasonal volume incentive on your next replenishment order.',
        'Automated',
        'Dispatched corporate partner voucher',
        'Logged',
        DATEADD(day, 30, GETUTCDATE()),
        'System',
        'Wholesale Retention Engine',
        DATEADD(day, -c.CustomerId % 60, GETUTCDATE())
    FROM Customers c
    WHERE c.CompanyId = 3;
END;
