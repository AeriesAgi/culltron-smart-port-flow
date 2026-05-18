using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.DTOs;
using SmartPort.Application.Interfaces;
using SmartPort.Infrastructure.Services;
using SmartPort.Shared.Constants;

namespace SmartPort.Web.Controllers;

[Authorize(Policy = Policies.CanAccessDriver)]
public class DriverController : Controller
{
    private readonly IFleetDriverQueueService _queue;
    private readonly INotificationService _notifications;
    private readonly IDriverStatusCommandService _commands;
    private readonly ISmartPortCopilotChatService _copilot;
    public DriverController(IFleetDriverQueueService queue, INotificationService notifications, IDriverStatusCommandService commands, ISmartPortCopilotChatService copilot) { _queue = queue; _notifications = notifications; _commands = commands; _copilot = copilot; }

    [HttpGet("/driver")]
    public async Task<IActionResult> Index()
    {
        ViewBag.DemoReferences = await _queue.GetDemoReferencesAsync();
        return View();
    }

    [HttpGet("/driver/demo")]
    public Task<IActionResult> Demo()
    {
        return Task.FromResult<IActionResult>(Redirect("/driver-app"));
    }

    [HttpGet("/driver/status/{reference}")]
    public async Task<IActionResult> Status(string reference)
    {
        if (User.IsInRole(Roles.Driver) || string.Equals(Request.Cookies[DemoAccessController.RoleCookieName], "Driver Demo", StringComparison.OrdinalIgnoreCase))
            return Redirect($"/driver-app/{reference}");
        var truck = await _queue.GetTruckAsync(reference);
        if (truck == null) return NotFound();
        truck.NotificationHistory = (await _notifications.GetHistoryAsync(reference)).ToList();
        return View(truck);
    }

    [HttpPost("/driver/action")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DriverAction(string reference, string commandText)
    {
        var result = await _commands.HandleCommandAsync(new DriverCommandRequestDto { Reference = reference, CommandText = commandText, Actor = "Driver web portal", Source = DataProvenanceType.WebDriverCompanion });
        TempData[result.Success ? "Success" : "Warning"] = result.ReplyMessage;
        return Redirect($"/driver/status/{reference}");
    }

    [HttpPost("/driver/copilot")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DriverCopilot(string reference, string question)
    {
        var response = await _copilot.GenerateResponseAsync($"Driver asks about Smart Port queue job {reference}: {question}. Keep answer scoped to status, ETA, gate, staging and instruction.");
        TempData["CopilotAnswer"] = response.ShortAnswer;
        return Redirect($"/driver/status/{reference}");
    }

    [HttpPost("/driver/location-checkin")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> LocationCheckIn(string reference, string? locationLabel, decimal? latitude, decimal? longitude, decimal? accuracy)
    {
        var label = string.IsNullOrWhiteSpace(locationLabel) ? "Manual/demo web companion check-in" : locationLabel;
        var truck = await _queue.RecordLocationCheckInAsync(reference, latitude, longitude, label, DataProvenanceType.WebDriverCompanion, "Web Driver Companion");
        TempData[truck == null ? "Warning" : "Success"] = truck == null ? "Truck/reference not found." : $"Check-in shared from Web Driver Companion: {truck.LastKnownLocationLabel}.";
        return Redirect($"/driver/status/{reference}");
    }

    [HttpPost("/driver/acknowledge")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Acknowledge(string reference, DriverAcknowledgement acknowledgement)
    {
        await _queue.AcknowledgeAsync(reference, acknowledgement);
        return Redirect($"/driver/status/{reference}");
    }
}

[Authorize(Policy = Policies.CanAccessDriver)]
public class TruckController : Controller
{
    private readonly IFleetDriverQueueService _queue;
    private readonly INotificationService _notifications;
    public TruckController(IFleetDriverQueueService queue, INotificationService notifications) { _queue = queue; _notifications = notifications; }

    [HttpGet("/truck/check")]
    public IActionResult Check() => View("~/Views/Driver/Index.cshtml");

    [HttpPost("/truck/check")]
    [IgnoreAntiforgeryToken]
    public IActionResult CheckPost(string reference) => Redirect($"/truck/status/{reference}");

    [HttpGet("/truck/status/{reference}")]
    public async Task<IActionResult> Status(string reference)
    {
        var truck = await _queue.GetTruckAsync(reference);
        if (truck == null) return NotFound();
        truck.NotificationHistory = (await _notifications.GetHistoryAsync(reference)).ToList();
        return View("~/Views/Driver/Status.cshtml", truck);
    }
}

[ApiController]
[IgnoreAntiforgeryToken]
public class MobileApiController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> DemoTokens = new(StringComparer.Ordinal);
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IFleetDriverQueueService _queue;
    private readonly INotificationService _notifications;
    private readonly IMobileDeviceRegistrationService _devices;
    private readonly IDriverStatusCommandService _commands;
    private readonly ISmartPortCopilotChatService _copilot;
    private readonly IExecutionPlanService _plans;
    public MobileApiController(IConfiguration configuration, IWebHostEnvironment environment, IFleetDriverQueueService queue, INotificationService notifications, IMobileDeviceRegistrationService devices, IDriverStatusCommandService commands, ISmartPortCopilotChatService copilot, IExecutionPlanService plans)
    { _configuration = configuration; _environment = environment; _queue = queue; _notifications = notifications; _devices = devices; _commands = commands; _copilot = copilot; _plans = plans; }

    [HttpPost("/api/mobile/auth/demo-login")]
    public async Task<IActionResult> DemoLogin([FromBody] MobileDemoLoginRequest request)
    {
        if (!IsMobileDemoCodeValid(request.AccessCode, request.Role)) return Unauthorized(new { message = "Demo access required" });
        var expires = DateTimeOffset.UtcNow.AddHours(8);
        var token = $"spm_{Guid.NewGuid():N}";
        DemoTokens[token] = expires;
        var reference = (await _queue.GetDemoReferencesAsync()).FirstOrDefault() ?? "SPQ-2026-0042";
        return Ok(new { token, expiresAt = expires, role = "Driver", demoReference = reference });
    }

    private bool EnsureMobileToken()
    {
        if (!Request.Headers.TryGetValue("X-SmartPort-Mobile-Token", out var value)) return false;
        var token = value.ToString();
        if (!DemoTokens.TryGetValue(token, out var expires) || expires <= DateTimeOffset.UtcNow) return false;
        return true;
    }

    private bool IsMobileDemoCodeValid(string? accessCode, string? role)
    {
        if (string.IsNullOrWhiteSpace(accessCode)) return false;
        var candidates = new[]
        {
            _configuration["SMARTPORT_DRIVER_DEMO_CODE"],
            _configuration["SMARTPORT_DEMO_ACCESS_CODE"],
            _configuration["SMARTPORT_JUDGE_DEMO_CODE"],
            "smartport2026"
        }.Where(c => !string.IsNullOrWhiteSpace(c));
        return candidates.Any(c => string.Equals(c, accessCode.Trim(), StringComparison.Ordinal));
    }

    private IActionResult MobileUnauthorized() => Unauthorized(new { message = "Demo access required", signIn = "/api/mobile/auth/demo-login" });

    private static DataProvenanceType ResolveDriverSource(string? sourceLabel) => (sourceLabel ?? string.Empty).ToLowerInvariant() switch
    {
        var s when s.Contains("web") => DataProvenanceType.WebDriverCompanion,
        var s when s.Contains("mobile api") || s == "api" => DataProvenanceType.MobileApi,
        var s when s.Contains("whatsapp") => DataProvenanceType.WhatsAppDriverCheckIn,
        var s when s.Contains("android") || s.Contains("driver companion") => DataProvenanceType.AndroidDriverApp,
        _ => DataProvenanceType.MobileApi
    };

    private static string SourceActor(DataProvenanceType source) => source switch
    {
        DataProvenanceType.WebDriverCompanion => "Web Driver Companion",
        DataProvenanceType.MobileApi => "Mobile API",
        DataProvenanceType.WhatsAppDriverCheckIn => "Optional WhatsApp connector",
        _ => "Android Driver Companion"
    };

    [HttpGet("/api/mobile/truck/status/{reference}")]
    [HttpGet("/api/mobile/driver/status/{reference}")]
    public async Task<IActionResult> TruckStatus(string reference)
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var truck = await _queue.GetTruckAsync(reference);
        if (truck == null) return NotFound(new { message = "Truck/reference not found" });
        truck.NotificationHistory = (await _notifications.GetHistoryAsync(reference)).ToList();
        return Ok(truck);
    }

    [HttpPost("/api/mobile/truck/check")]
    public async Task<IActionResult> TruckCheck([FromBody] DriverCheckRequestDto request) { if (!EnsureMobileToken()) return MobileUnauthorized(); return await TruckStatus(request.Reference); }

    [HttpGet("/api/mobile/notifications/{reference}")]
    [HttpGet("/api/mobile/driver/notifications/{reference}")]
    public async Task<IActionResult> Notifications(string reference) { if (!EnsureMobileToken()) return MobileUnauthorized(); return Ok(await _notifications.GetHistoryAsync(reference)); }

    [HttpPost("/api/mobile/driver/acknowledge")]
    public async Task<IActionResult> Acknowledge([FromBody] DriverAcknowledgementRequestDto request)
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var truck = await _queue.AcknowledgeAsync(request.Reference, request.Acknowledgement);
        return truck == null ? NotFound(new { message = "Truck/reference not found" }) : Ok(truck);
    }

    [HttpPost("/api/mobile/driver/confirm-status")]
    public async Task<IActionResult> ConfirmStatus([FromBody] DriverEventRequestDto request)
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var source = ResolveDriverSource(request.SourceLabel);
        var truck = await _queue.RecordDriverEventAsync(request.Reference, request.EventType, source, request.Action, SourceActor(source));
        return truck == null ? NotFound(new { message = "Truck/reference not found" }) : Ok(truck);
    }

    [HttpPost("/api/mobile/driver/location-checkin")]
    public async Task<IActionResult> LocationCheckIn([FromBody] DriverEventRequestDto request)
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var source = ResolveDriverSource(request.SourceLabel);
        var truck = await _queue.RecordLocationCheckInAsync(request.Reference, request.Latitude, request.Longitude, request.LocationLabel, source, SourceActor(source));
        return truck == null ? NotFound(new { message = "Truck/reference not found" }) : Ok(truck);
    }

    [HttpPost("/api/mobile/driver/command")]
    public async Task<IActionResult> DriverCommand([FromBody] DriverCommandRequestDto request) { if (!EnsureMobileToken()) return MobileUnauthorized(); return Ok(await _commands.HandleCommandAsync(request)); }


    [HttpPost("/api/mobile/driver/report-incident")]
    public async Task<IActionResult> ReportIncident([FromBody] DriverEventRequestDto request)
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var truck = await _queue.RecordDriverEventAsync(request.Reference, DriverEventType.DriverIssueReported, ResolveDriverSource(request.SourceLabel), request.Action ?? "Driver reported incident from mobile app", SourceActor(ResolveDriverSource(request.SourceLabel)));
        return truck == null ? NotFound(new { message = "Truck/reference not found" }) : Ok(new { status = "incident-recorded", truck, audit = truck.AuditTrail.OrderByDescending(a => a.Timestamp).FirstOrDefault() });
    }

    [HttpGet("/api/mobile/fleet/summary")]
    public async Task<IActionResult> FleetSummary() { if (!EnsureMobileToken()) return MobileUnauthorized(); return Ok(await _queue.GetFleetSummaryAsync()); }

    [HttpGet("/api/mobile/fleet/trucks")]
    [HttpGet("/api/mobile/fleet/tracker")]
    public async Task<IActionResult> FleetTracker()
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var trucks = await _queue.GetTrucksAsync();
        return Ok(trucks.Select(t => new { t.BookingReference, t.TruckRegistration, t.DriverName, t.FleetOperatorName, t.JobReference, t.ContainerReference, t.CurrentStatus, t.QueueNumber, t.AssignedGate, t.BerthYardStagingZone, t.EtaCallForwardTime, t.DelayRisk, t.LastKnownLocationLabel, t.LastLocationCheckIn, t.CurrentInstruction, t.EstimatedIdlingMinutesAvoided, t.EstimatedCo2KgAvoided }));
    }

    [HttpPost("/api/mobile/fleet/request-location")]
    public async Task<IActionResult> FleetRequestLocation([FromBody] DriverEventRequestDto request)
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var truck = await _queue.RecordDriverEventAsync(request.Reference, DriverEventType.DriverAcknowledgedInstruction, DataProvenanceType.MobileApi, "Fleet requested a fresh Driver Companion location check-in.", "Fleet mobile API");
        return truck == null ? NotFound(new { message = "Truck/reference not found" }) : Ok(truck);
    }

    [HttpPost("/api/mobile/fleet/send-instruction")]
    public async Task<IActionResult> FleetSendInstruction([FromBody] FleetInstructionRequestDto request)
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var truck = await _queue.RecordDriverEventAsync(request.Reference, DriverEventType.DriverAcknowledgedInstruction, DataProvenanceType.MobileApi, request.Instruction, "Fleet mobile API");
        return truck == null ? NotFound(new { message = "Truck/reference not found" }) : Ok(truck);
    }

    [HttpGet("/api/mobile/execution/plans")]
    public async Task<IActionResult> ExecutionPlans() { if (!EnsureMobileToken()) return MobileUnauthorized(); return Ok(await _plans.GetPlansAsync()); }

    [HttpPost("/api/mobile/execution/approve")]
    public async Task<IActionResult> ExecutionApprove([FromBody] ExecutionPlanActionRequestDto request) { if (!EnsureMobileToken()) return MobileUnauthorized(); return Ok(await _plans.UpdateStatusAsync(request.PlanId, ExecutionPlanStatus.Approved, "Mobile API", "Execution plan approved from mobile API.")); }

    [HttpPost("/api/mobile/execution/dispatch")]
    public async Task<IActionResult> ExecutionDispatch([FromBody] ExecutionPlanActionRequestDto request) { if (!EnsureMobileToken()) return MobileUnauthorized(); return Ok(await _plans.UpdateStatusAsync(request.PlanId, ExecutionPlanStatus.Dispatching, "Mobile API", "Execution plan dispatched from mobile API.")); }

    [HttpPost("/api/mobile/execution/complete")]
    public async Task<IActionResult> ExecutionComplete([FromBody] ExecutionPlanActionRequestDto request) { if (!EnsureMobileToken()) return MobileUnauthorized(); return Ok(await _plans.UpdateStatusAsync(request.PlanId, ExecutionPlanStatus.Completed, "Mobile API", "Execution plan completed from mobile API.")); }

    [HttpPost("/api/mobile/copilot/driver")]
    public async Task<IActionResult> DriverCopilot([FromBody] CopilotQuestionRequestDto request)
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var truck = await _queue.GetTruckAsync(request.Reference);
        var prompt = $"Driver asks about Smart Port queue job {request.Reference}: {request.Question}. Keep answer scoped to status, ETA, gate, staging and instruction.";
        var response = await _copilot.GenerateResponseAsync(prompt);
        return Ok(new CopilotResponseDto { Answer = response.ShortAnswer, SuggestedAction = response.RecommendedAction, Source = response.GeneratedBy, RelatedTruckStatus = truck });
    }

    [HttpPost("/api/mobile/copilot/fleet")]
    public async Task<IActionResult> FleetCopilot([FromBody] CopilotQuestionRequestDto request)
    {
        if (!EnsureMobileToken()) return MobileUnauthorized();
        var response = await _copilot.GenerateResponseAsync($"Fleet owner asks: {request.Question}. Summarize fleet queue plan and driver actions only.");
        return Ok(new CopilotResponseDto { Answer = response.ShortAnswer, SuggestedAction = response.RecommendedAction, Source = response.GeneratedBy });
    }

    [HttpGet("/api/mobile/driver/demo")]
    public async Task<IActionResult> Demo() { if (!EnsureMobileToken()) return MobileUnauthorized(); return Ok(new { references = await _queue.GetDemoReferencesAsync(), backend = "Smart Port Fleet & Driver Companion" }); }

    [HttpPost("/api/mobile/device/register")]
    public async Task<IActionResult> Register([FromBody] MobileDeviceRegistrationDto request) { if (!EnsureMobileToken()) return MobileUnauthorized(); await _devices.RegisterAsync(request); return Ok(new { status = "registered", mode = "placeholder" }); }

    [HttpPost("/api/mobile/device/unregister")]
    public async Task<IActionResult> Unregister([FromBody] MobileDeviceRegistrationDto request) { if (!EnsureMobileToken()) return MobileUnauthorized(); await _devices.UnregisterAsync(request.Reference, request.DeviceToken); return Ok(new { status = "unregistered" }); }
}

public sealed class MobileDemoLoginRequest { public string Role { get; set; } = "Driver Demo"; public string AccessCode { get; set; } = string.Empty; }

[ApiController]
[IgnoreAntiforgeryToken]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IFleetDriverQueueService _queue;
    private readonly IDriverStatusCommandService _commands;
    private readonly INotificationService _notifications;
    private readonly IWhatsAppConnectorService _whatsApp;
    private readonly IWhatsAppWebhookParser _parser;
    public WhatsAppWebhookController(IConfiguration config, IFleetDriverQueueService queue, IDriverStatusCommandService commands, INotificationService notifications, IWhatsAppConnectorService whatsApp, IWhatsAppWebhookParser parser) { _config = config; _queue = queue; _commands = commands; _notifications = notifications; _whatsApp = whatsApp; _parser = parser; }

    [HttpGet("/webhooks/whatsapp")]
    public IActionResult Verify([FromQuery(Name = "hub.mode")] string? mode, [FromQuery(Name = "hub.verify_token")] string? token, [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        return mode == "subscribe" && !string.IsNullOrWhiteSpace(token) && token == _config["SMARTPORT_WHATSAPP_VERIFY_TOKEN"] ? Content(challenge ?? string.Empty, "text/plain") : Unauthorized();
    }

    [HttpPost("/webhooks/whatsapp")]
    public async Task<IActionResult> Inbound([FromBody] JsonElement payload)
    {
        var handled = 0;
        foreach (var msg in _parser.Parse(payload))
        {
            var contacts = await _queue.GetDriverContactsAsync();
            var sender = NormalizeInbound(msg.FromWaId);
            var driver = contacts.FirstOrDefault(c => c.NormalizedWhatsAppNumber.Replace("+", string.Empty) == sender && c.IsLiveWhatsAppSafe);
            if (driver == null)
            {
                var fallbackRef = (await _queue.GetDemoReferencesAsync()).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(fallbackRef)) await _notifications.RecordAsync(fallbackRef, NotificationChannel.WhatsApp, NotificationStatus.IgnoredUnapprovedSender, NotificationEventType.InboundWhatsAppCommand, "Ignored unapproved WhatsApp sender. No truck state was changed.", DataProvenanceType.WhatsAppDriverCheckIn, "WhatsApp webhook");
                handled++;
                continue;
            }
            var truck = (await _queue.GetTrucksAsync()).FirstOrDefault(t => t.TruckRegistration == driver.AssignedTruckRegistration);
            if (truck == null) continue;
            DriverCommandResultDto result;
            if (msg.Latitude.HasValue || msg.Longitude.HasValue)
            {
                var updated = await _queue.RecordLocationCheckInAsync(truck.BookingReference, msg.Latitude, msg.Longitude, msg.TextBody, DataProvenanceType.WhatsAppDriverCheckIn, "WhatsApp LiveTest");
                var reply = updated == null ? "Location received, but Smart Port could not match the active truck job." : $"Location received. Updated ETA to {updated.AssignedGate}: {Math.Max(1, (int)Math.Round((updated.EtaCallForwardTime - DateTime.UtcNow).TotalMinutes))} minutes. Current instruction: {updated.CurrentInstruction.Instruction}";
                await SendInboundReplyAsync(updated ?? truck, reply, NotificationEventType.WhatsAppLocationCheckIn);
            }
            else
            {
                result = await _commands.HandleCommandAsync(new DriverCommandRequestDto { Reference = truck.BookingReference, SenderWhatsAppNumber = sender, CommandText = msg.TextBody, Actor = driver.DriverName, Source = DataProvenanceType.WhatsAppDriverCheckIn });
                await SendInboundReplyAsync(result.Truck ?? truck, result.ReplyMessage, NotificationEventType.InboundWhatsAppCommand);
            }
            handled++;
        }
        return Ok(new { status = "received", handled });
    }

    private async Task SendInboundReplyAsync(FleetTruckDto truck, string reply, NotificationEventType eventType)
    {
        var sendResult = await _whatsApp.SendAsync(truck, reply);
        await _notifications.RecordAsync(truck.BookingReference, NotificationChannel.WhatsApp, sendResult.Status, eventType, reply, DataProvenanceType.WhatsAppDriverCheckIn, "Smart Port WhatsApp webhook", sendResult.ExternalMessageId);
    }
    private static string NormalizeInbound(string value) => new(value.Where(char.IsDigit).ToArray());
}
