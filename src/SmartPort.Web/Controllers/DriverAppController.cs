using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.DTOs;
using SmartPort.Application.Interfaces;

namespace SmartPort.Web.Controllers;

[AllowAnonymous]
public class DriverAppController : Controller
{
    private readonly IFleetDriverQueueService _queue;
    private readonly INotificationService _notifications;
    private readonly ISmartPortCopilotChatService _copilot;
    public DriverAppController(IFleetDriverQueueService queue, INotificationService notifications, ISmartPortCopilotChatService copilot) { _queue = queue; _notifications = notifications; _copilot = copilot; }

    [HttpGet("/driver-app")]
    [HttpGet("/app/driver")]
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
