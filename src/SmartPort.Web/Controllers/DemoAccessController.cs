using Microsoft.AspNetCore.Mvc;

namespace SmartPort.Web.Controllers;

public class DemoAccessController : Controller
{
    private const string CookieName = "SmartPort.DemoAccess";
    private const string RoleCookieName = "SmartPort.DemoRole";
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public DemoAccessController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("/demo-access")]
    [HttpGet("/signin")]
    public IActionResult Index(string? next = null)
    {
        var safeNext = NormalizeNext(next);
        if (HasAccess()) return Redirect(safeNext);
        PopulateView(safeNext, "Port Admin Demo");
        return View("~/Views/DemoAccess/Index.cshtml");
    }

    [HttpPost("/demo-access")]
    [HttpPost("/signin")]
    [IgnoreAntiforgeryToken]
    public IActionResult Submit(string accessCode, string role = "Port Admin Demo", string? next = null)
    {
        var safeNext = NormalizeNext(next);
        var normalizedRole = NormalizeRole(role);
        if (!IsCodeValid(accessCode, normalizedRole, out var matchedRole))
        {
            PopulateView(safeNext, normalizedRole);
            ViewBag.Warning = "The demo access code was not accepted. Choose the role you want to preview and use the code shared by the Smart Port team.";
            return View("~/Views/DemoAccess/Index.cshtml");
        }

        var grantedRole = matchedRole ?? normalizedRole;
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        };

        Response.Cookies.Append(CookieName, "granted", options);
        Response.Cookies.Append(RoleCookieName, grantedRole, options);

        return Redirect(safeNext);
    }

    [HttpPost("/demo-access/signout")]
    [IgnoreAntiforgeryToken]
    public IActionResult SignOutDemo()
    {
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
        ViewBag.DevDefaultAvailable = _environment.IsDevelopment() && credentials.Any(c => c.IsDevelopmentDefault);
    }

    private IReadOnlyList<DemoRoleCredential> BuildCredentials()
    {
        return new[]
        {
            BuildCredential("Port Admin Demo", "SMARTPORT_ADMIN_DEMO_CODE", "culltron-admin-2026", "/dashboard", "Control room, settings, integrations and reports."),
            BuildCredential("Fleet Owner Demo", "SMARTPORT_FLEET_DEMO_CODE", "culltron-fleet-2026", "/fleet", "Fleet queue, trucks, notifications and execution plans."),
            BuildCredential("Driver Demo", "SMARTPORT_DRIVER_DEMO_CODE", "culltron-driver-2026", "/driver", "Mobile-first driver queue status and confirmations."),
            BuildCredential("General Demo", "SMARTPORT_DEMO_ACCESS_CODE", "culltron-demo-2026", "/dashboard", "General access for the full protected demo.")
        };
    }

    private DemoRoleCredential BuildCredential(string role, string envName, string developmentDefault, string landingPath, string description)
    {
        var configured = _configuration[envName];
        var usingDefault = string.IsNullOrWhiteSpace(configured) && _environment.IsDevelopment();
        return new DemoRoleCredential(role, envName, usingDefault ? developmentDefault : configured ?? string.Empty, landingPath, description, usingDefault);
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
        var credentials = BuildCredentials();

        foreach (var credential in credentials.Where(c => !string.IsNullOrWhiteSpace(c.Code)))
        {
            if (!SlowEquals(candidate, credential.Code.Trim())) continue;
            matchedRole = credential.Role == "General Demo" ? selectedRole : credential.Role;
            return true;
        }

        return false;
    }

    private bool HasAccess() => Request.Cookies.ContainsKey(CookieName);

    private static string NormalizeRole(string? role) => role switch
    {
        "Fleet Owner Demo" => "Fleet Owner Demo",
        "Driver Demo" => "Driver Demo",
        _ => "Port Admin Demo"
    };

    private static string NormalizeNext(string? next)
    {
        if (string.IsNullOrWhiteSpace(next)) return "/dashboard";
        if (!next.StartsWith('/')) return "/dashboard";
        if (next.StartsWith("//", StringComparison.Ordinal)) return "/dashboard";
        if (next.StartsWith("/demo-access", StringComparison.OrdinalIgnoreCase) || next.StartsWith("/signin", StringComparison.OrdinalIgnoreCase)) return "/dashboard";
        return next;
    }

    private static bool SlowEquals(string a, string b)
    {
        var diff = a.Length ^ b.Length;
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }

    public sealed record DemoRoleCredential(string Role, string EnvName, string Code, string LandingPath, string Description, bool IsDevelopmentDefault);
}
