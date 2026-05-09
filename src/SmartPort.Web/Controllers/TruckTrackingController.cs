using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Infrastructure.Services;

namespace SmartPort.Web.Controllers;

[Authorize]
public class TruckTrackingController : Controller
{
    private readonly ITruckTrackingService _tracking;

    public TruckTrackingController(ITruckTrackingService tracking) => _tracking = tracking;

    [Route("TruckTracking")]
    [Route("Tracking")]
    public async Task<IActionResult> Index()
    {
        var model = await _tracking.GetDashboardAsync();
        return View(model);
    }
}
