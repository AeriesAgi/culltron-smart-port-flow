using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Infrastructure.Persistence;
using SmartPort.Shared.Constants;

namespace SmartPort.Web.Controllers;

public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    // GET /auth/login
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(await ResolveCurrentUserLandingAsync());
        ViewData["ReturnUrl"] = NormalizeLocalReturnUrl(returnUrl);
        return View();
    }

    // POST /auth/login
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        var safeReturnUrl = NormalizeLocalReturnUrl(returnUrl);
        ViewData["ReturnUrl"] = safeReturnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
            _logger.LogInformation("User {Email} logged in.", model.Email);
            return Redirect(!string.IsNullOrWhiteSpace(safeReturnUrl) ? safeReturnUrl : user == null ? "/demo-tour" : await ResolveLandingForUserAsync(user));
        }
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out: {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Account is locked out after multiple failed attempts. Try again in 15 minutes.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    // POST /auth/logout
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        Response.Cookies.Delete(DemoAccessController.CookieName);
        Response.Cookies.Delete(DemoAccessController.RoleCookieName);
        Response.Cookies.Delete("SmartPort.DriverAppAccess");
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    // GET /auth/access-denied
    [HttpGet]
    public async Task<IActionResult> AccessDenied()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = user == null ? Array.Empty<string>() : (await _userManager.GetRolesAsync(user)).ToArray();
        var vm = new AccessDeniedViewModel
        {
            DisplayName = user?.FullName ?? User.Identity?.Name ?? "Demo visitor",
            Roles = roles.ToList(),
            DashboardPath = ResolveLandingFromRoles(roles)
        };
        return View(vm);
    }

    // GET /auth/profile
    [HttpGet, Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));
        var roles = await _userManager.GetRolesAsync(user);
        var vm = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            JobTitle = user.JobTitle,
            Organisation = user.Organisation,
            Terminal = user.Terminal,
            ContactNumber = user.ContactNumber,
            Roles = roles.ToList(),
            LastLoginAt = user.LastLoginAt
        };
        return View(vm);
    }
    private string? NormalizeLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return null;
        if (!Url.IsLocalUrl(returnUrl)) return null;
        if (returnUrl.StartsWith("/auth", StringComparison.OrdinalIgnoreCase)) return null;
        return returnUrl;
    }

    private async Task<string> ResolveCurrentUserLandingAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user == null ? "/demo-access" : await ResolveLandingForUserAsync(user);
    }

    private async Task<string> ResolveLandingForUserAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return ResolveLandingFromRoles(roles);
    }

    private static string ResolveLandingFromRoles(IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleSet.Contains(Roles.Driver)) return "/driver-app";
        if (roleSet.Contains(Roles.FleetOwner) || roleSet.Contains(Roles.LogisticsPartner)) return "/fleet";
        if (roleSet.Contains(Roles.JudgeDemo)) return "/demo-tour";
        if (roleSet.Contains(Roles.Admin) || roleSet.Contains(Roles.PortOperationsManager) || roleSet.Contains(Roles.TerminalStaff)) return "/dashboard";
        return "/demo-tour";
    }

}

// ─── ViewModels ───────────────────────────────────────────────────────────────

public class LoginViewModel
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class AccessDeniedViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string DashboardPath { get; set; } = "/demo-tour";
}

public class ProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Organisation { get; set; }
    public string? Terminal { get; set; }
    public string? ContactNumber { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime? LastLoginAt { get; set; }
}
