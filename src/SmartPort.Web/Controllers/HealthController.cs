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
            geminiEnabled = bool.TryParse(_configuration["Gemini:Enabled"] ?? _configuration["GEMINI_ENABLED"], out var enabled) && enabled,
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
            gemini = BuildGeminiStatus(),
            whatsApp = new
            {
                mode = whatsapp.Mode.ToString(),
                readiness = whatsapp.LiveSendingAllowed ? "Send Ready" : whatsapp.VerifyTokenConfigured && whatsapp.PublicBaseUrlConfigured ? "Webhook Ready" : whatsapp.CredentialsConfigured ? "Configured" : "Sandbox",
                whatsapp.Enabled,
                accessTokenPresent = whatsapp.AccessTokenConfigured,
                phoneNumberIdPresent = whatsapp.PhoneNumberIdConfigured,
                businessAccountIdPresent = whatsapp.BusinessAccountIdConfigured,
                verifyTokenPresent = whatsapp.VerifyTokenConfigured,
                publicBaseUrlPresent = whatsapp.PublicBaseUrlConfigured,
                callbackUrl = whatsapp.WebhookCallbackUrl,
                whatsapp.CredentialsConfigured,
                whatsapp.LiveSendingAllowed,
                lastWebhookStatus = "Available via /webhooks/whatsapp",
                lastSendStatus = "See /fleet/notifications history",
                whatsapp.SafetyMessage
            },
            mobileApi = new { enabled = true, tokenHeader = "X-SmartPort-Mobile-Token", storesSecrets = false },
            dataConnectors = new { mode = _configuration["SmartPortIntegration:Mode"] ?? "SyntheticDemoData", claim = "connector-ready; pilot credentials required for live integration" },
            environment = _environment.EnvironmentName,
            requestId = HttpContext.TraceIdentifier
        });
    }

    private object BuildGeminiStatus()
    {
        var configured = !string.IsNullOrWhiteSpace(_configuration["GEMINI_API_KEY"] ?? _configuration["Gemini:ApiKey"]);
        var enabled = bool.TryParse(_configuration["Gemini:Enabled"] ?? _configuration["GEMINI_ENABLED"], out var parsed) && parsed;
        var model = _configuration["Gemini:Model"] ?? _configuration["GEMINI_MODEL"] ?? "gemini-2.5-flash";
        var mode = _configuration["Gemini:Mode"] ?? _configuration["GEMINI_MODE"] ?? "Hybrid";
        var status = enabled && configured ? "Live Ready" : configured ? "Configured" : enabled ? "Needs API Key" : "Fallback Active";
        return new { apiKeyPresent = configured, enabled, model, mode, status, lastTestStatus = "See /gemini-agent audit history", lastLatencyMs = (int?)null, outputSource = enabled && configured ? "Gemini / Hybrid" : "Local Fallback" };
    }
}
