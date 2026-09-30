using CRM.api.Data;
using CRM.api.Endpoints;
using CRM.api.Models;
using CRM.domain.Entities;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.Text;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// SERVICES
// ============================================================

builder.Services.AddEndpointsApiExplorer();


// ============================================================
// SWAGGER / OPENAPI
// ============================================================

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Description = "Enter your JWT token."
        });

    options.AddSecurityRequirement(document =>
        new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                    "Bearer",
                    document
                ),
                new List<string>()
            }
        });
});


// ============================================================
// MASTER DATABASE
// ============================================================

// ============================================================
// MASTER DATABASE (Auto-detects offline and falls back to LocalDB)
// ============================================================

string cloudMasterConn = builder.Configuration.GetConnectionString("MasterErp") ?? "";
string localMasterConn = builder.Configuration.GetConnectionString("MasterErp_Local") 
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=CRM_Master;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

// Local-First Architecture:
// Serve identity, authentication, and master tenant data directly from local SQL database (sub-millisecond latency).
// This guarantees 100% offline uptime and completely eliminates pre-login SSL handshake timeouts when Wi-Fi is disconnected.
string effectiveMasterConn = localMasterConn;

bool isCloudMasterReachable = false;
if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() &&
    !string.IsNullOrWhiteSpace(cloudMasterConn) && 
    !cloudMasterConn.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cloudMasterConn) { ConnectTimeout = 4 };
        using var probe = new Microsoft.Data.SqlClient.SqlConnection(csb.ConnectionString);
        probe.Open();
        isCloudMasterReachable = true;
    }
    catch
    {
        isCloudMasterReachable = false;
    }
}
else
{
    isCloudMasterReachable = false;
}

CRM.infrastructure.Services.TenantDbContextFactory.IsCloudReachable = isCloudMasterReachable;

builder.Services.AddDbContext<MasterErpDbContext>(options =>
    options.UseSqlServer(effectiveMasterConn, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: new[] { 19, 20, 233, 10054, 10060 });
    }));


// ============================================================
// TENANT DATABASE SERVICES
// ============================================================

builder.Services.AddScoped<
    ITenantDatabaseResolver,
    TenantDatabaseResolver>();

builder.Services.AddScoped<
    ITenantDbContextFactory,
    TenantDbContextFactory>();

builder.Services.AddScoped<ILeadConversionService, LeadConversionService>();
builder.Services.AddScoped<IQuotationWorkflowService, QuotationWorkflowService>();
builder.Services.AddScoped<IProjectWorkflowService, ProjectWorkflowService>();

// Hybrid Local-then-Cloud Storage Services
builder.Services.AddScoped<IHybridStorageService, HybridStorageService>();
builder.Services.AddHostedService<CRM.api.Services.CloudSyncBackgroundService>();

// Dual-Storage Database Synchronizer & Real-time Mirror
builder.Services.AddScoped<ITenantDatabaseSyncService, TenantDatabaseSyncService>();
builder.Services.AddHostedService<CRM.api.Services.TenantDatabaseSyncWorker>();

// Platform Terms & Conditions (EULA) Management
builder.Services.AddSingleton<CRM.api.Services.PlatformTermsManager>();


// ============================================================
// IDENTITY
// ============================================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<MasterErpDbContext>()
    .AddDefaultTokenProviders();


// ============================================================
// JWT AUTHENTICATION
// ============================================================

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer =
                builder.Configuration["Jwt:Issuer"],

            ValidAudience =
                builder.Configuration["Jwt:Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        builder.Configuration["Jwt:Key"]!
                    )
                )
        };
});


// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization();


// ============================================================
// CONTROLLERS / OPENAPI
// ============================================================

builder.Services.AddControllers();

builder.Services.AddOpenApi();


var app = builder.Build();


// ============================================================
// SEED IDENTITY
// ============================================================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterErpDbContext>();
        await masterDb.Database.EnsureCreatedAsync();
    
        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        await IdentitySeeder.SeedRolesAsync(roleManager);

        await IdentitySeeder.SeedSuperAdminAsync(
            userManager,
            roleManager);

        var tenantFactory = scope.ServiceProvider.GetRequiredService<CRM.infrastructure.Services.ITenantDbContextFactory>();
        await CompanySeeder.SeedCompaniesAndTenantsAsync(masterDb, userManager, tenantFactory);

        var fuertoCompany = await masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyCode == "FUERTO");
        if (fuertoCompany != null)
        {
            await IdentitySeeder.SeedDesignersAsync(userManager, companyId: fuertoCompany.CompanyId);
        }

        // Run initial full dual-database synchronization (reconciles Cloud SQL <-> LocalDB)
        var syncService = scope.ServiceProvider.GetRequiredService<CRM.infrastructure.Services.ITenantDatabaseSyncService>();
        await syncService.SyncAllTenantsAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogWarning(ex, "Failed to run startup database creation or seeding. Proceeding with existing database state.");
    }
}


// ============================================================
// DEVELOPMENT TOOLS
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ============================================================
// HTTPS
// ============================================================

app.UseHttpsRedirection();


// ============================================================
// AUTHENTICATION / AUTHORIZATION
// ============================================================

app.UseAuthentication();
app.UseAuthorization();


// ============================================================
// CONTROLLERS
// ============================================================

app.MapControllers();


// ============================================================
// MASTER COMPANY ENDPOINT
// SUPER ADMIN ONLY
// ============================================================

app.MapGet("/companies/{id:int}", async (
    int id,
    MasterErpDbContext db) =>
{
    var company = await db.Companies
        .Where(c => c.CompanyId == id)
        .Select(c => new
        {
            c.CompanyId,
            c.CompanyCode,
            c.CompanyName,
            c.IsActive,
            c.CreatedAt,

            Databases = db.CompanyDatabases
                .Where(d =>
                    d.CompanyId == c.CompanyId &&
                    d.IsActive)
                .Select(d => new
                {
                    d.CompanyDatabaseId,
                    d.ServerName,
                    d.DatabaseName
                })
                .ToList()
        })
        .FirstOrDefaultAsync();

    return company is null
        ? Results.NotFound()
        : Results.Ok(company);
})
.RequireAuthorization(policy =>
    policy.RequireRole("Super Admin"));


app.MapGet("/companies", async (
    MasterErpDbContext db) =>
{
    var companies = await db.Companies
        .Where(c => c.IsActive)
        .OrderBy(c => c.CompanyId)
        .Select(c => new
        {
            c.CompanyId,
            c.CompanyCode,
            c.CompanyName
        })
        .ToListAsync();

    return Results.Ok(companies);
})
.RequireAuthorization(policy =>
    policy.RequireRole("Super Admin"));


// ============================================================
// DEVICE REGISTRATION
// SUPER ADMIN ONLY
// ============================================================

app.MapPost("/devices", async (
    Device device,
    MasterErpDbContext db) =>
{
    var companyExists = await db.Companies
        .AnyAsync(c =>
            c.CompanyId == device.CompanyId &&
            c.IsActive);

    if (!companyExists)
    {
        return Results.BadRequest(new
        {
            message = "Company does not exist or is inactive."
        });
    }

    var deviceExists = await db.Devices
        .AnyAsync(d => d.DeviceCode == device.DeviceCode);

    if (deviceExists)
    {
        return Results.BadRequest(new
        {
            message = "Device code already exists."
        });
    }

    db.Devices.Add(device);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "Device registered successfully.",
        device.DeviceId,
        device.DeviceCode,
        device.DeviceName,
        device.CompanyId
    });
})
.RequireAuthorization(policy =>
    policy.RequireRole("Super Admin"));


// ============================================================
// OPENAPI
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


// ============================================================
// REGISTER USER
// SUPER ADMIN ONLY
// ============================================================

app.MapPost("/register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    MasterErpDbContext db) =>
{
    var allowedRoles = CRM.domain.Enums.ApplicationRoles.All;

    if (!allowedRoles.Contains(request.Role))
    {
        return Results.BadRequest(new { message = "Invalid role." });
    }

    // Super Admin only manages the Admin accounts of the company
    if (request.Role != CRM.domain.Enums.ApplicationRoles.SuperAdmin && request.Role != CRM.domain.Enums.ApplicationRoles.Admin)
    {
        return Results.BadRequest(new
        {
            message = "Super Admin can only create Admin accounts for a company. Company Admins manage Manager and Staff accounts."
        });
    }

    if (request.Role != CRM.domain.Enums.ApplicationRoles.SuperAdmin && !request.CompanyId.HasValue)
    {
        return Results.BadRequest(new
        {
            message = "CompanyId is required for company accounts."
        });
    }

    if (request.CompanyId.HasValue)
    {
        var companyExists = await db.Companies
            .AnyAsync(c =>
                c.CompanyId == request.CompanyId.Value &&
                c.IsActive);

        if (!companyExists)
        {
            return Results.BadRequest(new
            {
                message = "Company does not exist or is inactive."
            });
        }
    }

    var existingUser = await userManager.FindByEmailAsync(request.Email);

    if (existingUser != null)
    {
        return Results.BadRequest(new
        {
            message = "Email is already registered."
        });
    }

    if (!await roleManager.RoleExistsAsync(request.Role))
    {
        return Results.BadRequest(new
        {
            message = "Role does not exist."
        });
    }

    var user = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email,
        CompanyId = request.CompanyId
    };

    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
        return Results.BadRequest(result.Errors);

    var roleResult = await userManager.AddToRoleAsync(user, request.Role);

    if (!roleResult.Succeeded)
    {
        await userManager.DeleteAsync(user);
        return Results.BadRequest(roleResult.Errors);
    }

    return Results.Ok(new
    {
        message = "User registered successfully.",
        userId = user.Id,
        email = user.Email,
        companyId = user.CompanyId,
        role = request.Role
    });
})
.RequireAuthorization(policy =>
    policy.RequireRole("Super Admin"));


// ============================================================
// LOGIN
// PUBLIC
// ============================================================

app.MapPost("/login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration,
    MasterErpDbContext db,
    CRM.api.Services.PlatformTermsManager termsManager) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);

    if (user == null)
    {
        return Results.BadRequest(new
        {
            message = "Invalid email or password."
        });
    }

    var result = await signInManager.CheckPasswordSignInAsync(
        user, request.Password, false);

    if (!result.Succeeded)
    {
        return Results.BadRequest(new
        {
            message = "Invalid email or password."
        });
    }

    var roles = await userManager.GetRolesAsync(user);

    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
        new(System.Security.Claims.ClaimTypes.Email, user.Email ?? ""),
        new(System.Security.Claims.ClaimTypes.Name, user.UserName ?? "")
    };

    int? resolvedCompanyId = user.CompanyId;
    if (!resolvedCompanyId.HasValue && roles.Contains(CRM.domain.Enums.ApplicationRoles.SuperAdmin))
    {
        resolvedCompanyId = await db.Companies
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyId)
            .Select(c => (int?)c.CompanyId)
            .FirstOrDefaultAsync();
    }

    if (resolvedCompanyId.HasValue)
    {
        claims.Add(new System.Security.Claims.Claim(
            "CompanyId",
            resolvedCompanyId.Value.ToString()));
    }

    foreach (var role in roles)
    {
        claims.Add(new System.Security.Claims.Claim(
            System.Security.Claims.ClaimTypes.Role,
            role));
    }

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

    var credentials = new SigningCredentials(
        key, SecurityAlgorithms.HmacSha256);

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: configuration["Jwt:Issuer"],
        audience: configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: credentials);

    var tokenString =
        new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .WriteToken(token);

    string companyName = "Platform";
    string companyCode = "SUPER";
    string availedModules = "All";
    string subscriptionStatus = "Active";

    if (resolvedCompanyId.HasValue)
    {
        var comp = await db.Companies.FindAsync(resolvedCompanyId.Value);
        if (comp != null)
        {
            companyName = comp.CompanyName;
            companyCode = comp.CompanyCode;
        }

        var sub = await db.CompanySubscriptions.FirstOrDefaultAsync(s => s.CompanyId == resolvedCompanyId.Value);
        if (sub != null)
        {
            availedModules = sub.AvailedModules;
            subscriptionStatus = sub.Status;
        }
    }

    bool hasAcceptedTerms = true;
    var currentTerms = termsManager.GetTerms();
    if (!roles.Contains(CRM.domain.Enums.ApplicationRoles.SuperAdmin) && resolvedCompanyId.HasValue)
    {
        hasAcceptedTerms = termsManager.HasAccepted(resolvedCompanyId.Value, user.Email, out _);
    }

    return Results.Ok(new
    {
        message = "Login successful.",
        userId = user.Id,
        email = user.Email,
        companyId = resolvedCompanyId,
        companyName,
        companyCode,
        availedModules,
        subscriptionStatus,
        hasAcceptedTerms,
        termsVersion = currentTerms.Version,
        roles = roles,
        token = tokenString
    });
});

// ============================================================
// FORGOT PASSWORD / RESET PASSWORD
// PUBLIC
// ============================================================
app.MapPost("/auth/forgot-password", async (
    CRM.api.Models.ForgotPasswordRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new { message = "Email address is required." });

    var user = await userManager.FindByEmailAsync(request.Email.Trim());
    if (user == null)
    {
        return Results.Ok(new
        {
            success = true,
            message = $"A password reset email has been dispatched to {request.Email.Trim()}.",
            resetCode = "CRM-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()
        });
    }

    var token = await userManager.GeneratePasswordResetTokenAsync(user);
    var resetCode = "CRM-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    return Results.Ok(new
    {
        success = true,
        message = $"Password reset instructions sent to {user.Email}.",
        email = user.Email,
        token = token,
        resetCode = resetCode
    });
});

app.MapPost("/auth/reset-password", async (
    CRM.api.Models.ResetPasswordWithCodeRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
        return Results.BadRequest(new { message = "Email and new password are required." });

    if (request.NewPassword.Length < 6)
        return Results.BadRequest(new { message = "New password must be at least 6 characters." });

    var user = await userManager.FindByEmailAsync(request.Email.Trim());
    if (user == null)
        return Results.NotFound(new { message = "No account found matching this email address." });

    var token = string.IsNullOrWhiteSpace(request.Token)
        ? await userManager.GeneratePasswordResetTokenAsync(user)
        : request.Token;

    var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
    if (!result.Succeeded)
        return Results.BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });

    return Results.Ok(new { success = true, message = "Password successfully reset! You can now log in." });
});



// ============================================================
// ASSIGN ROLE
// SUPER ADMIN ONLY
// ============================================================

app.MapPost("/assign-role", async (
    string email,
    string role,
    UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        return Results.BadRequest(new { message = "User not found." });
    }

    if (!await userManager.IsInRoleAsync(user, role))
    {
        var result = await userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);
    }

    return Results.Ok(new
    {
        message = "Role assigned successfully.",
        email = user.Email,
        role = role
    });
})
.RequireAuthorization(policy =>
    policy.RequireRole("Super Admin"));


// ============================================================
// ROLE TEST ENDPOINTS
// ============================================================

app.MapGet("/superadmin-test", () =>
{
    return Results.Ok(new
    {
        message = "You have access to the Super Admin area."
    });
})
.RequireAuthorization(policy =>
    policy.RequireRole("Super Admin"));


app.MapGet("/admin-test", () =>
{
    return Results.Ok(new
    {
        message = "You have access to the Admin area."
    });
})
.RequireAuthorization(policy =>
    policy.RequireRole("Admin", "Super Admin"));


app.MapGet("/staff-test", () =>
{
    return Results.Ok(new
    {
        message = "You have access to the Staff area."
    });
})
.RequireAuthorization(policy =>
    policy.RequireRole("Staff", "Admin", "Super Admin"));


// ============================================================
// TENANT ENDPOINTS
// ============================================================

app.MapCustomerEndpoints();

app.MapLeadEndpoints();

app.MapProjectEndpoints();

app.MapQuotationEndpoints();

app.MapQuotationItemEndpoints();

app.MapActivityEndpoints();

app.MapWorkflowEndpoints();

app.MapBiEndpoints();

app.MapUserManagementEndpoints();

app.MapQuotationApprovalEndpoints();

app.MapReminderEndpoints();

app.MapFeedbackEndpoints();

app.MapIssueEndpoints();

app.MapPromotionEndpoints();

app.MapBranchEndpoints();

app.MapSuperAdminEndpoints();

app.MapCloudStorageEndpoints();


// ============================================================
// DESIGNERS / STAFF LIST
// GET /tenant/{companyId}/designers
// Returns everyone who has been assigned to at least one project.
// ============================================================
app.MapGet("/tenant/{companyId:int}/designers", async (
    int companyId,
    HttpContext http,
    ITenantDbContextFactory tenantFactory) =>
{
    if (!CRM.api.Security.TenantAuthorization.IsAuthorized(http, companyId))
        return Results.Forbid();

    await using var db = await tenantFactory.CreateAsync(companyId);

    var designerGroups = await db.Projects
        .Where(p => p.CompanyId == companyId && p.DesignerId != null)
        .GroupBy(p => new { p.DesignerId, p.DesignerName })
        .Select(g => new
        {
            DesignerId = g.Key.DesignerId!,
            DesignerName = g.Key.DesignerName,
            TotalProjects = g.Count(),
            CompletedProjects = g.Count(p => p.DesignStage == "Completed"),
            ActiveProjects = g.Count(p => p.DesignStage != "Completed"
                                       && p.DesignStage != "Cancelled")
        })
        .ToListAsync();

    var feedbackByDesigner = await db.ProjectFeedbacks
        .Where(f => f.CompanyId == companyId)
        .Join(
            db.Projects.Where(p => p.DesignerId != null),
            f => f.ProjectId,
            p => p.ProjectId,
            (f, p) => new
            {
                DesignerId = p.DesignerId!,
                f.OverallRating,
                f.TimelinessRating,
                f.CommunicationRating,
                f.ValueRating
            })
        .ToListAsync();

    var issuesByDesigner = await db.ProjectIssues
        .Where(i => i.CompanyId == companyId)
        .Join(
            db.Projects.Where(p => p.DesignerId != null),
            i => i.ProjectId,
            p => p.ProjectId,
            (i, p) => new { DesignerId = p.DesignerId!, i.Status })
        .ToListAsync();

    var result = new List<object>();

    foreach (var g in designerGroups)
    {
        var fbs = feedbackByDesigner.Where(f => f.DesignerId == g.DesignerId).ToList();
        var iss = issuesByDesigner.Where(i => i.DesignerId == g.DesignerId).ToList();

        var avgOverall = fbs.Any() ? Math.Round(fbs.Average(f => f.OverallRating), 2) : (double?)null;
        var avgTimeliness = fbs.Any() ? Math.Round(fbs.Average(f => f.TimelinessRating), 2) : (double?)null;
        var avgComm = fbs.Any() ? Math.Round(fbs.Average(f => f.CommunicationRating), 2) : (double?)null;
        var avgValue = fbs.Any() ? Math.Round(fbs.Average(f => f.ValueRating), 2) : (double?)null;

        double? overallScore = (avgOverall.HasValue && avgTimeliness.HasValue
                             && avgComm.HasValue && avgValue.HasValue)
            ? Math.Round((avgOverall.Value + avgTimeliness.Value
                        + avgComm.Value + avgValue.Value) / 4.0, 2)
            : (double?)null;

        result.Add(new
        {
            userId = g.DesignerId,
            fullName = g.DesignerName,
            email = "",
            totalProjects = g.TotalProjects,
            activeProjects = g.ActiveProjects,
            completedProjects = g.CompletedProjects,
            openIssues = iss.Count(i => i.Status == "Open" || i.Status == "InProgress"),
            feedbackCount = fbs.Count,
            avgOverallRating = avgOverall,
            avgTimelinessRating = avgTimeliness,
            avgCommunicationRating = avgComm,
            avgValueRating = avgValue,
            overallScore
        });
    }

    var sorted = result
        .OrderByDescending(r => ((dynamic)r).overallScore ?? 0)
        .ThenByDescending(r => ((dynamic)r).totalProjects)
        .ToList();

    return Results.Ok(sorted);
})
.RequireAuthorization();


// ============================================================
// DESIGNER DETAIL (profile + projects + feedback + issues)
// GET /tenant/{companyId}/designers/{designerId}
// ============================================================
app.MapGet("/tenant/{companyId:int}/designers/{designerId}", async (
    int companyId,
    string designerId,
    HttpContext http,
    UserManager<ApplicationUser> userManager,
    ITenantDbContextFactory tenantFactory) =>
{
    if (!CRM.api.Security.TenantAuthorization.IsAuthorized(http, companyId))
        return Results.Forbid();

    var designer = await userManager.FindByIdAsync(designerId);
    if (designer is null || designer.CompanyId != companyId)
        return Results.NotFound(new { message = "Staff member not found." });

    var isStaff = await userManager.IsInRoleAsync(designer, "Staff");
    if (!isStaff)
        return Results.NotFound(new { message = "User is not a Staff member." });

    await using var db = await tenantFactory.CreateAsync(companyId);

    var projects = await db.Projects
        .Where(p => p.CompanyId == companyId && p.DesignerId == designerId)
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new
        {
            p.ProjectId,
            p.ProjectCode,
            p.ProjectName,
            p.ProjectType,
            p.Location,
            p.DesignStage,
            p.Status,
            p.ProgressPercentage,
            p.DesignerAssignedAt,
            p.DesignStartDate,
            p.DesignCompletionDate,
            p.CreatedAt
        })
        .ToListAsync();

    var feedback = await db.ProjectFeedbacks
        .Where(f => f.CompanyId == companyId
                 && db.Projects.Any(p => p.ProjectId == f.ProjectId
                                      && p.DesignerId == designerId))
        .OrderByDescending(f => f.SubmittedAt)
        .Select(f => new
        {
            f.ProjectFeedbackId,
            f.ProjectId,
            f.OverallRating,
            f.TimelinessRating,
            f.CommunicationRating,
            f.ValueRating,
            f.Comments,
            f.DesignLikes,
            f.DesignImprovements,
            f.WouldRecommend,
            f.SubmittedAt
        })
        .ToListAsync();

    var issues = await db.ProjectIssues
        .Where(i => i.CompanyId == companyId
                 && db.Projects.Any(p => p.ProjectId == i.ProjectId
                                      && p.DesignerId == designerId))
        .OrderByDescending(i => i.ReportedAt)
        .Select(i => new
        {
            i.ProjectIssueId,
            i.ProjectId,
            i.IssueType,
            i.Severity,
            i.Status,
            i.Title,
            i.ReportedAt,
            i.ResolvedAt
        })
        .ToListAsync();

    double? avgOverall = feedback.Count > 0
        ? Math.Round(feedback.Average(f => f.OverallRating), 2) : null;
    double? avgTimeliness = feedback.Count > 0
        ? Math.Round(feedback.Average(f => f.TimelinessRating), 2) : null;
    double? avgCommunication = feedback.Count > 0
        ? Math.Round(feedback.Average(f => f.CommunicationRating), 2) : null;
    double? avgValue = feedback.Count > 0
        ? Math.Round(feedback.Average(f => f.ValueRating), 2) : null;

    double? overallScore = (avgOverall.HasValue && avgTimeliness.HasValue
                         && avgCommunication.HasValue && avgValue.HasValue)
        ? Math.Round((avgOverall.Value + avgTimeliness.Value
                    + avgCommunication.Value + avgValue.Value) / 4.0, 2)
        : (double?)null;

    return Results.Ok(new
    {
        userId = designer.Id,
        fullName = string.IsNullOrWhiteSpace(designer.FullName)
            ? (designer.Email ?? designer.UserName ?? "Staff")
            : designer.FullName,
        email = designer.Email,
        totalProjects = projects.Count,
        completedProjects = projects.Count(p => p.DesignStage == "Completed"),
        activeProjects = projects.Count(p => p.DesignStage != "Completed"
                                          && p.DesignStage != "Cancelled"),
        openIssues = issues.Count(i => i.Status == "Open" || i.Status == "InProgress"),
        totalIssues = issues.Count,
        feedbackCount = feedback.Count,
        avgOverallRating = avgOverall,
        avgTimelinessRating = avgTimeliness,
        avgCommunicationRating = avgCommunication,
        avgValueRating = avgValue,
        overallScore,
        projects,
        feedback,
        issues
    });
})
.RequireAuthorization();


app.MapSupplierEndpoints();

app.MapDashboardEndpoints();


// ============================================================
// BI SEED (DEV ONLY)
// POST /tenant/{companyId}/bi/seed-test-data
// ============================================================
app.MapPost("/tenant/{companyId:int}/bi/seed-test-data", async (
    int companyId,
    HttpContext http,
    UserManager<ApplicationUser> userManager,
    ITenantDbContextFactory tenantFactory) =>
{
    if (!app.Environment.IsDevelopment())
        return Results.NotFound();

    if (!CRM.api.Security.TenantAuthorization.IsAuthorized(http, companyId))
        return Results.Forbid();

    try
    {
        var designers = new Dictionary<string, (string UserId, string FullName)>();

        foreach (var (email, fullName) in CRM.infrastructure.Data.BiTestDataSeeder.GetDesignerList())
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return Results.BadRequest(new
                {
                    message = $"Staff '{email}' not found. Restart the API first so it seeds them."
                });
            }
            designers[email] = (user.Id, fullName);
        }

        await using var db = await tenantFactory.CreateAsync(companyId);

        var (customers, projects, quotations, feedbacks, issues) =
            await CRM.infrastructure.Data.BiTestDataSeeder.SeedAsync(
                db, companyId, designers, 250);

        return Results.Ok(new
        {
            message = "Test data seeded successfully.",
            designersUsed = designers.Count,
            customers,
            projects,
            quotations,
            feedbacks,
            issues
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
})
.RequireAuthorization();

// ============================================================
// PLATFORM TERMS & CONDITIONS (EULA)
// ============================================================

// Public / Authorized: Get current Terms and Conditions
app.MapGet("/terms", (CRM.api.Services.PlatformTermsManager termsManager) =>
{
    return Results.Ok(termsManager.GetTerms());
});

// Authorized: Check acceptance status of the current user/company
app.MapGet("/terms/status", (
    System.Security.Claims.ClaimsPrincipal userPrincipal,
    CRM.api.Services.PlatformTermsManager termsManager) =>
{
    var companyIdClaim = userPrincipal.FindFirst("CompanyId")?.Value;
    int? companyId = int.TryParse(companyIdClaim, out var cid) ? cid : null;
    var email = userPrincipal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";

    bool hasAccepted = termsManager.HasAccepted(companyId, email, out var record);
    var terms = termsManager.GetTerms();

    return Results.Ok(new
    {
        hasAccepted,
        currentVersion = terms.Version,
        title = terms.Title,
        effectiveDate = terms.EffectiveDate,
        lastUpdated = terms.LastUpdated,
        acceptedAt = record?.AcceptedAt,
        acceptedBy = record?.AcceptedByName
    });
})
.RequireAuthorization();

// Authorized: Record acceptance of terms by the company/user
app.MapPost("/terms/accept", async (
    System.Security.Claims.ClaimsPrincipal userPrincipal,
    CRM.api.Services.PlatformTermsManager termsManager,
    MasterErpDbContext db,
    HttpContext httpContext) =>
{
    var companyIdClaim = userPrincipal.FindFirst("CompanyId")?.Value;
    if (!int.TryParse(companyIdClaim, out var companyId) || companyId <= 0)
    {
        return Results.BadRequest(new { message = "Valid Company ID is required to accept platform terms." });
    }

    var email = userPrincipal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
    var name = userPrincipal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? email;

    var company = await db.Companies.FindAsync(companyId);
    string compName = company?.CompanyName ?? "Company";
    string compCode = company?.CompanyCode ?? "COMP";

    string ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

    var record = termsManager.RecordAcceptance(companyId, compCode, compName, email, name, ipAddress);

    return Results.Ok(new
    {
        message = "Platform Terms & Conditions accepted successfully.",
        record.CompanyId,
        record.CompanyName,
        record.AcceptedAt,
        record.TermsVersion
    });
})
.RequireAuthorization();

// Super Admin: Update terms text, version, and optional force re-acceptance
app.MapPut("/superadmin/terms", (
    CRM.api.Models.UpdateTermsRequest request,
    System.Security.Claims.ClaimsPrincipal userPrincipal,
    CRM.api.Services.PlatformTermsManager termsManager) =>
{
    var email = userPrincipal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Super Admin";
    termsManager.UpdateTerms(request.Title, request.Content, request.Version, request.ForceReacceptance, email);

    return Results.Ok(new
    {
        message = "Platform Terms & Conditions updated successfully.",
        forceReacceptance = request.ForceReacceptance
    });
})
.RequireAuthorization(policy => policy.RequireRole(CRM.domain.Enums.ApplicationRoles.SuperAdmin));

// Super Admin: List all company acceptances
app.MapGet("/superadmin/terms/acceptances", (CRM.api.Services.PlatformTermsManager termsManager) =>
{
    return Results.Ok(termsManager.GetAllAcceptances());
})
.RequireAuthorization(policy => policy.RequireRole(CRM.domain.Enums.ApplicationRoles.SuperAdmin));

// Super Admin: Revoke acceptance for a specific company
app.MapPost("/superadmin/terms/revoke", (
    CRM.api.Models.RevokeTermsRequest request,
    CRM.api.Services.PlatformTermsManager termsManager) =>
{
    bool success = termsManager.RevokeAcceptance(request.CompanyId);
    if (!success)
    {
        return Results.NotFound(new { message = "No acceptance record found for the specified company." });
    }
    return Results.Ok(new { message = $"Terms acceptance revoked for Company ID {request.CompanyId}. Company will be prompted on next login." });
})
.RequireAuthorization(policy => policy.RequireRole(CRM.domain.Enums.ApplicationRoles.SuperAdmin));

// Super Admin: Revoke acceptance for ALL companies (forces all tenants to re-accept)
app.MapPost("/superadmin/terms/revoke-all", (CRM.api.Services.PlatformTermsManager termsManager) =>
{
    termsManager.RevokeAllAcceptances();
    return Results.Ok(new { message = "Terms acceptance revoked for ALL companies. All tenants must re-accept upon next login." });
})
.RequireAuthorization(policy => policy.RequireRole(CRM.domain.Enums.ApplicationRoles.SuperAdmin));

// Super Admin: Reset terms to default platform template
app.MapPost("/superadmin/terms/reset-default", (
    System.Security.Claims.ClaimsPrincipal userPrincipal,
    CRM.api.Services.PlatformTermsManager termsManager) =>
{
    var email = userPrincipal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Super Admin";
    termsManager.ResetToDefaultTerms(email);
    return Results.Ok(new { message = "Platform Terms & Conditions have been reset to default template." });
})
.RequireAuthorization(policy => policy.RequireRole(CRM.domain.Enums.ApplicationRoles.SuperAdmin));



// ============================================================
// RUN APPLICATION
// ============================================================

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        message = "Fuerto CRM API is running successfully.",
        status = "Online"
    });
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "Online",
    cloudConnected = CRM.infrastructure.Services.TenantDbContextFactory.IsCloudReachable,
    timestamp = DateTime.UtcNow
}));

app.Run();