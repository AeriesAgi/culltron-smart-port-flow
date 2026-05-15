using Microsoft.AspNetCore.Mvc;

namespace SmartPort.Web.Controllers;

public class DemoAccessController : Controller
{
    private const string CookieName = "SmartPort.DemoAccess";
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
        ViewBag.Next = safeNext;
        ViewBag.DevDefaultAvailable = _environment.IsDevelopment() && string.IsNullOrWhiteSpace(ConfiguredCode);
        return View("~/Views/DemoAccess/Index.cshtml");
    }

    [HttpPost("/demo-access")]
    [HttpPost("/signin")]
    [IgnoreAntiforgeryToken]
    public IActionResult Submit(string accessCode, string role = "Port Admin Demo", string? next = null)
    {
        var safeNext = NormalizeNext(next);
        if (!IsCodeValid(accessCode))
        {
            ViewBag.Next = safeNext;
            ViewBag.SelectedRole = role;
            ViewBag.Warning = "The demo access code was not accepted. Please use the code shared by the Smart Port team.";
            ViewBag.DevDefaultAvailable = _environment.IsDevelopment() && string.IsNullOrWhiteSpace(ConfiguredCode);
            return View("~/Views/DemoAccess/Index.cshtml");
        }

        var normalizedRole = role switch
        {
            "Fleet Owner Demo" => "Fleet Owner Demo",
            "Driver Demo" => "Driver Demo",
            _ => "Port Admin Demo"
        };

        Response.Cookies.Append(CookieName, normalizedRole, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });

        return Redirect(safeNext);
    }

    [HttpPost("/demo-access/signout")]
    [IgnoreAntiforgeryToken]
    public IActionResult SignOutDemo()
    {
        Response.Cookies.Delete(CookieName);
        return Redirect("/");
    }

    private string? ConfiguredCode => _configuration["SMARTPORT_DEMO_ACCESS_CODE"];

    private bool IsCodeValid(string? accessCode)
    {
        if (string.IsNullOrWhiteSpace(accessCode)) return false;
        var configured = ConfiguredCode;
        if (!string.IsNullOrWhiteSpace(configured)) return SlowEquals(accessCode.Trim(), configured.Trim());
        return _environment.IsDevelopment() && SlowEquals(accessCode.Trim(), "SMARTPORT-DEMO");
    }

    private bool HasAccess() => Request.Cookies.ContainsKey(CookieName);

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
}
