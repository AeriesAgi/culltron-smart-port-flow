using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPort.Application.Interfaces;
using SmartPort.Infrastructure.Persistence;
using SmartPort.Infrastructure.Services;

namespace SmartPort.Web.Controllers;

public class HealthController : Controller
{
    private readonly SmartPortDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IFleetDriverQueueService _queue;
    private readonly IAgentNarrativeService _narrative;

    public HealthController(SmartPortDbContext db, IConfiguration configuration, IWebHostEnvironment environment, IFleetDriverQueueService queue, IAgentNarrativeService narrative)
    {
        _db = db;
        _configuration = configuration;
        _environment = environment;
        _queue = queue;
        _narrative = narrative;
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

    private static string ResolveGeminiFallbackReason(AgentModeStatus status)
    {
        if (!status.GeminiConfigured) return "key missing; local deterministic fallback active";
        if (!status.GeminiEnabled) return "disabled; local deterministic fallback active";
        if (status.QuotaLimited) return "quota-limited; local deterministic fallback active";
        if (!string.IsNullOrWhiteSpace(status.LastResult) && !status.LastResult.Contains("Gemini available", StringComparison.OrdinalIgnoreCase)) return $"{status.LastResult}; fallback active";
        return status.FallbackActive ? "fallback active" : "none recorded";
    }

    private object BuildGeminiStatus()
    {
        var status = _narrative.GetStatus();
        return new
        {
            apiKeyPresent = status.GeminiConfigured,
            enabled = status.GeminiEnabled,
            primaryModel = status.PrimaryModel,
            premiumModel = status.PremiumModel,
            routineModel = status.RoutineModel,
            fallbackModels = status.FallbackModels,
            mode = status.CurrentModeLabel,
            modelAttempted = string.IsNullOrWhiteSpace(status.LastModelAttempted) ? "No call attempted since startup" : status.LastModelAttempted,
            modelUsed = string.IsNullOrWhiteSpace(status.LastModelUsed) ? "No call used since startup" : status.LastModelUsed,
            status = status.GeminiStatus,
            fallbackReason = ResolveGeminiFallbackReason(status),
            quotaOrModelError = status.QuotaLimited ? "quota-limited" : status.LastResult.Contains("unsupported", StringComparison.OrdinalIgnoreCase) || status.LastResult.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ? status.LastResult : "none recorded",
            lastCallUtc = status.LastCallUtc,
            lastActionType = string.IsNullOrWhiteSpace(status.LastActionType) ? "No explicit Gemini action since startup" : status.LastActionType,
            lastRouteSource = status.LastRouteSource,
            lastResult = string.IsNullOrWhiteSpace(status.LastResult) ? "No result since startup" : status.LastResult,
            lastLatencyMs = status.LastLatencyMs,
            callsSinceStart = status.CallsSinceStart,
            callsByActionType = status.CallsByActionType,
            localFallbackActive = status.FallbackActive || !status.GeminiEnabled || !status.GeminiConfigured,
            fallbackActive = status.FallbackActive,
            quotaLimited = status.QuotaLimited,
            outputSource = status.FallbackActive ? "Local deterministic fallback or Gemini fallback" : "Gemini on-demand / no health-call usage"
        };
    }
}
