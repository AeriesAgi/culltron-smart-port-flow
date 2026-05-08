using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartPort.Application.Interfaces;
using SmartPort.Infrastructure.Persistence;
using SmartPort.Infrastructure.Services;
using SmartPort.Shared.Constants;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<SmartPortDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Identity ─────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<SmartPortDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/auth/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Name = "SmartPort.Session";
});

// ─── Authorization Policies ───────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanManageUsers,       p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.CanManageVessels,     p => p.RequireRole(Roles.Admin, Roles.PortOperationsManager));
    options.AddPolicy(Policies.CanAcknowledgeAlerts, p => p.RequireRole(Roles.Admin, Roles.PortOperationsManager, Roles.TerminalStaff));
    options.AddPolicy(Policies.CanManageIncidents,   p => p.RequireRole(Roles.Admin, Roles.PortOperationsManager, Roles.TerminalStaff));
    options.AddPolicy(Policies.CanApproveDocuments,  p => p.RequireRole(Roles.Admin, Roles.PortOperationsManager));
    options.AddPolicy(Policies.CanViewAnalytics,     p => p.RequireRole(Roles.Admin, Roles.PortOperationsManager, Roles.Viewer));
});

// ─── Configuration bindings ───────────────────────────────────────────────────
builder.Services.Configure<FlowIntelligenceSettings>(
    builder.Configuration.GetSection("FlowIntelligence"));
builder.Services.Configure<AiAgentSettings>(
    builder.Configuration.GetSection("AiAgent"));

// ─── Existing port services ───────────────────────────────────────────────────
builder.Services.AddScoped<IDashboardService,      DashboardService>();
builder.Services.AddScoped<IVesselService,         VesselService>();
builder.Services.AddScoped<IBerthService,          BerthService>();
builder.Services.AddScoped<IContainerService,      ContainerService>();
builder.Services.AddScoped<IYardService,           YardService>();
builder.Services.AddScoped<IGateService,           GateService>();
builder.Services.AddScoped<IIncidentService,       IncidentService>();
builder.Services.AddScoped<IAlertService,          AlertService>();
builder.Services.AddScoped<IDocumentService,       DocumentService>();
builder.Services.AddScoped<IAnalyticsService,      AnalyticsService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// ─── Culltron Flow Intelligence services ─────────────────────────────────────
builder.Services.AddScoped<ICongestionRiskService,         CongestionRiskService>();
builder.Services.AddScoped<IDispatchRecommendationService, DispatchRecommendationService>();
builder.Services.AddScoped<IIdlingEmissionService,         IdlingEmissionService>();
builder.Services.AddScoped<IPilotMetricsService,           PilotMetricsService>();
builder.Services.AddScoped<IOrganisationService,           OrganisationService>();
builder.Services.AddScoped<IFleetVehicleService,           FleetVehicleService>();
builder.Services.AddScoped<IDispatchTripService,           DispatchTripService>();
builder.Services.AddScoped<IDisruptionService,             DisruptionService>();
builder.Services.AddScoped<IFlowIntelligenceService,       FlowIntelligenceService>();
builder.Services.AddScoped<IEmissionsSummaryService,       EmissionsSummaryService>();

// ─── Hackathon features ───────────────────────────────────────────────────────
builder.Services.AddScoped<IAiAgentService,         AiAgentService>();
builder.Services.AddScoped<ISmartPortCopilotChatService, SmartPortCopilotChatService>();
builder.Services.AddScoped<IScenarioSimulatorService, ScenarioSimulatorService>();

// ─── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// ─── Database Initialisation with retry loop ─────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db          = scope.ServiceProvider.GetRequiredService<SmartPortDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var logger      = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries   = 10;
    const int retryDelayMs = 3000;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("DB initialisation attempt {Attempt}/{Max}…", attempt, maxRetries);
            await db.Database.EnsureCreatedAsync();
            await SeedData.SeedAllAsync(db, userManager, roleManager);
            await CulltronSeedData.SeedAsync(db);
            logger.LogInformation("Database initialised and seeded successfully.");
            break;
        }
        catch (Npgsql.NpgsqlException ex) when (attempt < maxRetries)
        {
            logger.LogWarning("PostgreSQL not ready ({Attempt}/{Max}): {Msg}. Retrying in {Delay}s…",
                attempt, maxRetries, ex.Message, retryDelayMs / 1000);
            await Task.Delay(retryDelayMs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DB initialisation failed on attempt {Attempt}. Aborting.", attempt);
            throw;
        }
    }
}

app.Run();
