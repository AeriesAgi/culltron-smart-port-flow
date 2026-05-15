using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPort.Application.Interfaces;
using SmartPort.Infrastructure.Persistence;

namespace SmartPort.Web.Controllers;

public class HealthController : Controller
{
    private readonly SmartPortDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IFleetDriverQueueService _queue;

    public HealthController(SmartPortDbContext db, IConfiguration configuration, IWebHostEnvironment environment, IFleetDriverQueueService queue)
    {
        _db = db;
        _configuration = configuration;
        _environment = environment;
        _queue = queue;
    }

    [HttpGet("/health")]
    public IActionResult Index() => Ok(new { status = "running", app = "Culltron Smart Port Flow", environment = _environment.EnvironmentName, requestId = HttpContext.TraceIdentifier });

    [HttpGet("/health/readiness")]
    public async Task<IActionResult> Readiness()
    {
        var databaseReachable = await _db.Database.CanConnectAsync();
        var whatsapp = _queue.GetWhatsAppConnectorStatus();
        return Ok(new
        {
            appRunning = true,
            databaseReachable,
            geminiConfigured = !string.IsNullOrWhiteSpace(_configuration["GEMINI_API_KEY"] ?? _configuration["Gemini:ApiKey"]),
            geminiEnabled = bool.TryParse(_configuration["Gemini:Enabled"], out var enabled) && enabled,
            whatsappMode = whatsapp.Mode.ToString(),
            mobileApiEnabled = true,
            dataConnectorMode = _configuration["SmartPortIntegration:Mode"] ?? "SyntheticDemoData",
            environment = _environment.EnvironmentName,
            requestId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("/health/integrations")]
    public IActionResult Integrations()
    {
        var whatsapp = _queue.GetWhatsAppConnectorStatus();
        return Ok(new
        {
            gemini = new { configured = !string.IsNullOrWhiteSpace(_configuration["GEMINI_API_KEY"] ?? _configuration["Gemini:ApiKey"]), enabled = bool.TryParse(_configuration["Gemini:Enabled"], out var enabled) && enabled, model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash" },
            whatsApp = new { mode = whatsapp.Mode.ToString(), whatsapp.Enabled, whatsapp.CredentialsConfigured, whatsapp.VerifyTokenConfigured, whatsapp.LiveSendingAllowed, whatsapp.SafetyMessage },
            mobileApi = new { enabled = true, tokenHeader = "X-SmartPort-Mobile-Token", storesSecrets = false },
            dataConnectors = new { mode = _configuration["SmartPortIntegration:Mode"] ?? "SyntheticDemoData", claim = "connector-ready; pilot credentials required for live integration" },
            environment = _environment.EnvironmentName,
            requestId = HttpContext.TraceIdentifier
        });
    }
}
