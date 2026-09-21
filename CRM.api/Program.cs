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

builder.Services.AddDbContext<MasterErpDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MasterErp")));


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
    var roleManager =
        scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

    // Create roles if they do not exist.
    await IdentitySeeder.SeedRolesAsync(roleManager);

    // Create the first local Super Admin if needed.
    await IdentitySeeder.SeedSuperAdminAsync(
        userManager,
        roleManager);

    // 👇 ADD THIS LINE
    // Seed 5 designers for company #1
    await IdentitySeeder.SeedDesignersAsync(userManager, companyId: 1);
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

    if (request.Role != "Super Admin" && !request.CompanyId.HasValue)
    {
        return Results.BadRequest(new
        {
            message = "CompanyId is required for Admin and Staff."
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
    IConfiguration configuration) =>
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

    if (user.CompanyId.HasValue)
    {
        claims.Add(new System.Security.Claims.Claim(
            "CompanyId",
            user.CompanyId.Value.ToString()));
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

    return Results.Ok(new
    {
        message = "Login successful.",
        userId = user.Id,
        email = user.Email,
        companyId = user.CompanyId,
        roles = roles,
        token = tokenString
    });
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


// ============================================================
// DESIGNERS LIST (with stats)
// GET /tenant/{companyId}/designers
// ============================================================
app.MapGet("/tenant/{companyId:int}/designers", async (
    int companyId,
    HttpContext http,
    UserManager<ApplicationUser> userManager,
    ITenantDbContextFactory tenantFactory) =>
{
    if (!CRM.api.Security.TenantAuthorization.IsAuthorized(http, companyId))
        return Results.Forbid();

    // ---- Get all users in this company ----
    var users = userManager.Users
        .Where(u => u.CompanyId == companyId)
        .ToList();

    // ---- Filter to Designers only ----
    var designers = new List<ApplicationUser>();
    foreach (var u in users)
    {
        if (await userManager.IsInRoleAsync(u, "Designer"))
            designers.Add(u);
    }

    // ---- Get tenant DB to compute stats ----
    await using var db = await tenantFactory.CreateAsync(companyId);

    var designerIds = designers.Select(d => d.Id).ToList();

    var projectsByDesigner = await db.Projects
        .Where(p => p.CompanyId == companyId
                 && p.DesignerId != null
                 && designerIds.Contains(p.DesignerId))
        .Select(p => new
        {
            p.ProjectId,
            p.DesignerId,
            p.DesignStage,
            p.ProgressPercentage
        })
        .ToListAsync();

    var feedbackRows = await db.ProjectFeedbacks
        .Where(f => f.CompanyId == companyId)
        .Join(
            db.Projects.Where(p => p.DesignerId != null
                                && designerIds.Contains(p.DesignerId)),
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
            db.Projects.Where(p => p.DesignerId != null
                                && designerIds.Contains(p.DesignerId)),
            i => i.ProjectId,
            p => p.ProjectId,
            (i, p) => new
            {
                DesignerId = p.DesignerId!,
                i.Status
            })
        .ToListAsync();

    var result = new List<object>();

    foreach (var d in designers)
    {
        var myProjects = projectsByDesigner
            .Where(p => p.DesignerId == d.Id)
            .ToList();

        var myFeedback = feedbackRows
            .Where(f => f.DesignerId == d.Id)
            .ToList();

        var myIssues = issuesByDesigner
            .Where(i => i.DesignerId == d.Id)
            .ToList();

        var totalProjects = myProjects.Count;
        var activeProjects = myProjects.Count(p =>
            p.DesignStage != "Completed" && p.DesignStage != "Cancelled");
        var completedProjects = myProjects.Count(p => p.DesignStage == "Completed");
        var openIssues = myIssues.Count(i =>
            i.Status == "Open" || i.Status == "InProgress");

        double? avgOverall = myFeedback.Count > 0
            ? Math.Round(myFeedback.Average(f => f.OverallRating), 2) : null;
        double? avgTimeliness = myFeedback.Count > 0
            ? Math.Round(myFeedback.Average(f => f.TimelinessRating), 2) : null;
        double? avgCommunication = myFeedback.Count > 0
            ? Math.Round(myFeedback.Average(f => f.CommunicationRating), 2) : null;
        double? avgValue = myFeedback.Count > 0
            ? Math.Round(myFeedback.Average(f => f.ValueRating), 2) : null;

        double? overallScore = (avgOverall.HasValue && avgTimeliness.HasValue
                             && avgCommunication.HasValue && avgValue.HasValue)
            ? Math.Round((avgOverall.Value + avgTimeliness.Value
                        + avgCommunication.Value + avgValue.Value) / 4.0, 2)
            : (double?)null;

        result.Add(new
        {
            userId = d.Id,
            fullName = string.IsNullOrWhiteSpace(d.FullName)
                ? (d.Email ?? d.UserName ?? "Designer")
                : d.FullName,
            email = d.Email,
            totalProjects,
            activeProjects,
            completedProjects,
            openIssues,
            feedbackCount = myFeedback.Count,
            avgOverallRating = avgOverall,
            avgTimelinessRating = avgTimeliness,
            avgCommunicationRating = avgCommunication,
            avgValueRating = avgValue,
            overallScore
        });
    }

    return Results.Ok(result);
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
        return Results.NotFound(new { message = "Designer not found." });

    var isDesigner = await userManager.IsInRoleAsync(designer, "Designer");
    if (!isDesigner)
        return Results.NotFound(new { message = "User is not a Designer." });

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
            ? (designer.Email ?? designer.UserName ?? "Designer")
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
// ============================================================
// BI SEED (DEV ONLY)
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
        // ---- Resolve the 5 designers from the master DB ----
        var designers = new Dictionary<string, (string UserId, string FullName)>();

        foreach (var (email, fullName) in CRM.infrastructure.Data.BiTestDataSeeder.GetDesignerList())
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return Results.BadRequest(new
                {
                    message = $"Designer '{email}' not found. Restart the API first so it seeds designers."
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

app.Run();