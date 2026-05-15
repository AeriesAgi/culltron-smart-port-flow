using Microsoft.AspNetCore.Mvc;

namespace SmartPort.Web.Controllers;

/// <summary>
/// Public-facing website controller — landing page, product overview, contact.
/// Does NOT require authentication.
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger) => _logger = logger;

    // GET /
    public IActionResult Index() => View();

    // GET /product or /platform
    [Route("/platform")]
    [Route("/product")]
    public IActionResult Product() => View();

    // GET /features
    public IActionResult Features() => View();

    // GET /about
    public IActionResult About() => View();

    // GET /pricing
    public IActionResult Pricing() => View();

    // GET /contact
    public IActionResult Contact() => View();

    // POST /contact
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Contact(ContactFormModel model)
    {
        if (!ModelState.IsValid) return View(model);
        // TODO: send email via IEmailSender
        TempData["Success"] = "Thank you — the Smart Port team will review your enquiry for demo or pilot follow-up.";
        return RedirectToAction(nameof(Contact));
    }

    // GET /demo
    public IActionResult Demo() => View();

    // GET /error
    [Route("/error")]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View();
    }

    // GET /error/404
    [Route("/error/{statusCode}")]
    public IActionResult ErrorCode(int statusCode)
    {
        Response.StatusCode = statusCode;
        return View("Error", statusCode);
    }

    // AMD page route
    // GET /about-amd
    [Route("about-amd")]
    public IActionResult AboutAmd() => View("~/Views/Home/AboutAmd.cshtml");
}

public class ContactFormModel
{
    [System.ComponentModel.DataAnnotations.Required]
    public string FullName { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string? Organisation { get; set; }
    public string? Phone { get; set; }
    public string? Enquiry { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public string Message { get; set; } = string.Empty;
}
