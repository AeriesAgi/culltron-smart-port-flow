using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Infrastructure.Persistence;

namespace SmartPort.Web.Controllers;

public class DemoAccessController : Controller
{
    public const string CookieName = "SmartPort.DemoAccess";
    public const string RoleCookieName = "SmartPort.DemoRole";
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DemoAccessController(IConfiguration configuration, IWebHostEnvironment environment, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _environment = environment;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("/demo-access")]
    [HttpGet("/signin")]
    public IActionResult Index(string? next = null)
    {
        var safeNext = NormalizeNext(next);
        if (User.Identity?.IsAuthenticated == true && Request.Cookies.ContainsKey(CookieName)) return Redirect(safeNext);
        PopulateView(safeNext, "Judge Demo");
        return View("~/Views/DemoAccess/Index.cshtml");
    }

    [HttpPost("/demo-access")]
    [HttpPost("/signin")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Submit(string accessCode, string role = "Judge Demo", string? next = null)
    {
        var normalizedRole = NormalizeRole(role);
        if (!IsCodeValid(accessCode, normalizedRole, out var matchedRole))
        {
            PopulateView(NormalizeNext(next), normalizedRole);
            ViewBag.Warning = "The demo access code was not accepted. Choose the role you want to preview and use the code shared by the Smart Port team.";
            return View("~/Views/DemoAccess/Index.cshtml");
        }

        var grantedRole = matchedRole ?? normalizedRole;
        var credential = BuildCredentials().First(c => c.Role == grantedRole || (c.Role == "General Demo" && grantedRole == "Judge Demo"));
        var user = await _userManager.FindByEmailAsync(credential.IdentityEmail);
        if (user == null)
        {
            PopulateView(NormalizeNext(next), normalizedRole);
            ViewBag.Warning = "Seeded demo user is not available yet. Restart the app or run database seed, then retry.";
            return View("~/Views/DemoAccess/Index.cshtml");
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        var options = new CookieOptions { HttpOnly = true, Secure = !_environment.IsDevelopment(), SameSite = SameSiteMode.Lax, IsEssential = true, Expires = DateTimeOffset.UtcNow.AddHours(8) };
        Response.Cookies.Append(CookieName, "granted", options);
        Response.Cookies.Append(RoleCookieName, grantedRole, options);
        return Redirect(ResolveLanding(grantedRole, next));
    }

    [HttpPost("/demo-access/signout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SignOutDemo()
    {
        await _signInManager.SignOutAsync();
        Response.Cookies.Delete(CookieName);
        Response.Cookies.Delete(RoleCookieName);
        return Redirect("/");
    }

    private void PopulateView(string safeNext, string selectedRole)
    {
        var credentials = BuildCredentials();
        var showCredentials = ShouldShowCredentials(credentials);
        ViewBag.Next = safeNext;
        ViewBag.SelectedRole = selectedRole;
        ViewBag.Roles = credentials;
        ViewBag.ShowDemoCredentials = showCredentials;
        ViewBag.DemoCredentialMessage = showCredentials
            ? "Use a role card and quick-fill button for a guided judging path. Public product pages remain separate from operational demo screens."
            : "Demo access code provided by the Smart Port team.";
    }

    private IReadOnlyList<DemoRoleCredential> BuildCredentials() => new[]
    {
        BuildCredential("Port Admin Demo", "SMARTPORT_ADMIN_DEMO_CODE", "culltron-admin-2026", "/dashboard", "Control room, Gemini agent, integrations and reports.", "admin@smartport.culltron.app"),
        BuildCredential("Fleet Owner Demo", "SMARTPORT_FLEET_DEMO_CODE", "culltron-fleet-2026", "/fleet", "Fleet queue, trucks, notifications and execution plans.", "fleet.owner@smartport.culltron.app"),
        BuildCredential("Driver Demo", "SMARTPORT_DRIVER_DEMO_CODE", "culltron-driver-2026", "/driver/demo", "Mobile-first driver queue status and confirmations.", "driver@smartport.culltron.app"),
        BuildCredential("Judge Demo", "SMARTPORT_JUDGE_DEMO_CODE", "culltron-judge-2026", "/demo-tour", "Guided end-to-end hackathon judging tour.", "judge@smartport.culltron.app"),
        BuildCredential("General Demo", "SMARTPORT_DEMO_ACCESS_CODE", "culltron-demo-2026", "/demo-tour", "General access for the full protected demo.", "judge@smartport.culltron.app")
    };

    private DemoRoleCredential BuildCredential(string role, string envName, string developmentDefault, string landingPath, string description, string identityEmail)
    {
        var configured = _configuration[envName];
        var usingDefault = string.IsNullOrWhiteSpace(configured) && _environment.IsDevelopment();
        return new DemoRoleCredential(role, envName, usingDefault ? developmentDefault : configured ?? string.Empty, landingPath, description, usingDefault, identityEmail);
    }

    private bool ShouldShowCredentials(IReadOnlyList<DemoRoleCredential> credentials)
    {
        if (bool.TryParse(_configuration["SMARTPORT_SHOW_DEMO_CREDENTIALS"], out var explicitShow)) return explicitShow;
        return _environment.IsDevelopment() && credentials.Any(c => c.IsDevelopmentDefault);
    }

    private bool IsCodeValid(string? accessCode, string selectedRole, out string? matchedRole)
    {
        matchedRole = null;
        if (string.IsNullOrWhiteSpace(accessCode)) return false;
        var candidate = accessCode.Trim();
        foreach (var credential in BuildCredentials().Where(c => !string.IsNullOrWhiteSpace(c.Code)))
        {
            if (!SlowEquals(candidate, credential.Code.Trim())) continue;
            matchedRole = credential.Role == "General Demo" ? selectedRole : credential.Role;
            return true;
        }
        return false;
    }

    private string ResolveLanding(string role, string? next)
    {
        if (!string.IsNullOrWhiteSpace(next) && next.StartsWith('/') && !next.StartsWith("//") && !next.StartsWith("/auth", StringComparison.OrdinalIgnoreCase)) return NormalizeNext(next);
        return BuildCredentials().FirstOrDefault(c => c.Role == role)?.LandingPath ?? "/demo-tour";
    }

    private static string NormalizeRole(string? role) => role switch
    {
        "Port Admin Demo" => "Port Admin Demo",
        "Fleet Owner Demo" => "Fleet Owner Demo",
        "Driver Demo" => "Driver Demo",
        "Judge Demo" => "Judge Demo",
        _ => "Judge Demo"
    };

    private static string NormalizeNext(string? next)
    {
        if (string.IsNullOrWhiteSpace(next)) return "/demo-tour";
        if (!next.StartsWith('/')) return "/demo-tour";
        if (next.StartsWith("//", StringComparison.Ordinal)) return "/demo-tour";
        if (next.StartsWith("/demo-access", StringComparison.OrdinalIgnoreCase) || next.StartsWith("/signin", StringComparison.OrdinalIgnoreCase)) return "/demo-tour";
        return next;
    }

    private static bool SlowEquals(string a, string b)
    {
        var diff = a.Length ^ b.Length;
        for (var i = 0; i < a.Length && i < b.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }

    public sealed record DemoRoleCredential(string Role, string EnvName, string Code, string LandingPath, string Description, bool IsDevelopmentDefault, string IdentityEmail);
}
