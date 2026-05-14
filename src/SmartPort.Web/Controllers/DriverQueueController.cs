using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.DTOs;
using SmartPort.Application.Interfaces;

namespace SmartPort.Web.Controllers;

public class DriverController : Controller
{
    private readonly IFleetDriverQueueService _queue;
    private readonly INotificationService _notifications;
    public DriverController(IFleetDriverQueueService queue, INotificationService notifications) { _queue = queue; _notifications = notifications; }

    [HttpGet("/driver")]
    public async Task<IActionResult> Index()
    {
        ViewBag.DemoReferences = await _queue.GetDemoReferencesAsync();
        return View();
    }

    [HttpGet("/driver/demo")]
    public async Task<IActionResult> Demo()
    {
        var reference = (await _queue.GetDemoReferencesAsync()).First();
        return Redirect($"/driver/status/{reference}");
    }

    [HttpGet("/driver/status/{reference}")]
    public async Task<IActionResult> Status(string reference)
    {
        var truck = await _queue.GetTruckAsync(reference);
        if (truck == null) return NotFound();
        truck.NotificationHistory = (await _notifications.GetHistoryAsync(reference)).ToList();
        return View(truck);
    }

    [HttpPost("/driver/acknowledge")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Acknowledge(string reference, DriverAcknowledgement acknowledgement)
    {
        await _queue.AcknowledgeAsync(reference, acknowledgement);
        return Redirect($"/driver/status/{reference}");
    }
}

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
    private readonly IFleetDriverQueueService _queue;
    private readonly INotificationService _notifications;
    private readonly IMobileDeviceRegistrationService _devices;
    public MobileApiController(IFleetDriverQueueService queue, INotificationService notifications, IMobileDeviceRegistrationService devices)
    { _queue = queue; _notifications = notifications; _devices = devices; }

    [HttpGet("/api/mobile/truck/status/{reference}")]
    public async Task<IActionResult> TruckStatus(string reference)
    {
        var truck = await _queue.GetTruckAsync(reference);
        if (truck == null) return NotFound(new { message = "Truck/reference not found" });
        truck.NotificationHistory = (await _notifications.GetHistoryAsync(reference)).ToList();
        return Ok(truck);
    }

    [HttpPost("/api/mobile/truck/check")]
    public async Task<IActionResult> TruckCheck([FromBody] DriverCheckRequestDto request) => await TruckStatus(request.Reference);

    [HttpGet("/api/mobile/notifications/{reference}")]
    public async Task<IActionResult> Notifications(string reference) => Ok(await _notifications.GetHistoryAsync(reference));

    [HttpPost("/api/mobile/driver/acknowledge")]
    public async Task<IActionResult> Acknowledge([FromBody] DriverAcknowledgementRequestDto request)
    {
        var truck = await _queue.AcknowledgeAsync(request.Reference, request.Acknowledgement);
        return truck == null ? NotFound(new { message = "Truck/reference not found" }) : Ok(truck);
    }

    [HttpGet("/api/mobile/driver/demo")]
    public async Task<IActionResult> Demo() => Ok(new { references = await _queue.GetDemoReferencesAsync(), backend = "Smart Port Fleet & Driver Queue Companion" });

    [HttpPost("/api/mobile/device/register")]
    public async Task<IActionResult> Register([FromBody] MobileDeviceRegistrationDto request) { await _devices.RegisterAsync(request); return Ok(new { status = "registered", mode = "placeholder" }); }

    [HttpPost("/api/mobile/device/unregister")]
    public async Task<IActionResult> Unregister([FromBody] MobileDeviceRegistrationDto request) { await _devices.UnregisterAsync(request.Reference, request.DeviceToken); return Ok(new { status = "unregistered" }); }
}
