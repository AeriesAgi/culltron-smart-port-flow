using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.DTOs;
using SmartPort.Application.Interfaces;
using SmartPort.Infrastructure.Services;

namespace SmartPort.Web.Controllers;

[AllowAnonymous]
public class DriverAppController : Controller
{
    private readonly IFleetDriverQueueService _queue;
    private readonly INotificationService _notifications;
    private readonly ISmartPortCopilotChatService _copilot;
    private readonly IConfiguration _configuration;
    public DriverAppController(IFleetDriverQueueService queue, INotificationService notifications, ISmartPortCopilotChatService copilot, IConfiguration configuration) { _queue = queue; _notifications = notifications; _copilot = copilot; _configuration = configuration; }


    [HttpGet("/driver-app/login")]
    public async Task<IActionResult> Login()
    {
        ViewBag.DemoReferences = await _queue.GetDemoReferencesAsync();
        ViewBag.DemoCode = "smartport2026";
        return View("~/Views/DriverApp/Login.cshtml");
    }

    [HttpPost("/driver-app/login")]
    [IgnoreAntiforgeryToken]
    public IActionResult LoginSubmit(string accessCode, string reference = "SPQ-2026-0042")
    {
        var allowed = new[] { _configuration["SMARTPORT_DRIVER_DEMO_CODE"], _configuration["SMARTPORT_DEMO_ACCESS_CODE"], "smartport2026" }
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Any(code => string.Equals(code, accessCode?.Trim(), StringComparison.Ordinal));
        if (!allowed)
        {
            TempData["Warning"] = "Driver demo code was not accepted. Use the judging demo code or the private driver code.";
            return Redirect("/driver-app/login");
        }

        Response.Cookies.Append("SmartPort.DriverAppAccess", "granted", new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true, Expires = DateTimeOffset.UtcNow.AddHours(8) });
        TempData["Success"] = "Driver demo access granted. Location is sent only when you tap Check In.";
        return Redirect(string.IsNullOrWhiteSpace(reference) ? "/driver-app" : $"/driver-app/{Uri.EscapeDataString(reference)}");
    }

    [HttpGet("/app/driver")]
    public IActionResult DriverAlias() => Redirect("/driver-app");

    [HttpGet("/driver-app")]
    [HttpGet("/driver-app/{reference}")]
    public async Task<IActionResult> Index(string? reference)
    {
        var refs = await _queue.GetDemoReferencesAsync();
        var selected = string.IsNullOrWhiteSpace(reference) ? refs.FirstOrDefault() ?? "SPQ-2026-0042" : reference;
        var truck = await _queue.GetTruckAsync(selected) ?? await _queue.GetTruckAsync(refs.FirstOrDefault() ?? "SPQ-2026-0042");
        if (truck == null) return NotFound();
        truck.NotificationHistory = (await _notifications.GetHistoryAsync(truck.BookingReference)).ToList();
        ViewBag.DemoReferences = refs;
        return View("~/Views/DriverApp/Index.cshtml", truck);
    }

    [HttpPost("/driver-app/action")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Action(string reference, DriverEventType eventType, string? note)
    {
        var truck = await _queue.RecordDriverEventAsync(reference, eventType, DataProvenanceType.WebDriverCompanion, note ?? $"Driver app action: {eventType}", "Driver Companion App");
        TempData[truck == null ? "Warning" : "Success"] = truck == null ? "Truck/reference not found." : $"Action recorded: {eventType}. Fleet tracker and audit trail updated.";
        return Redirect($"/driver-app/{reference}");
    }

    [HttpPost("/driver-app/checkin")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CheckIn(string reference, decimal? latitude, decimal? longitude, string? locationLabel)
    {
        var truck = await _queue.RecordLocationCheckInAsync(reference, latitude, longitude, locationLabel, DataProvenanceType.WebDriverCompanion, "Driver Companion App");
        TempData[truck == null ? "Warning" : "Success"] = truck == null ? "Truck/reference not found." : $"Location check-in received: {truck.LastKnownLocationLabel}. ETA and fleet tracker refreshed.";
        return Redirect($"/driver-app/{reference}");
    }

    [HttpPost("/driver-app/copilot")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Copilot(string reference, string question)
    {
        var response = await _copilot.GenerateResponseAsync($"Driver Companion App question for truck/job {reference}: {question}. Answer only from current queue, gate, staging, ETA, driver instruction and safety state.");
        TempData["CopilotAnswer"] = response.ShortAnswer;
        TempData["CopilotSource"] = response.GeneratedBy;
        return Redirect($"/driver-app/{reference}");
    }
}
