using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.Interfaces;
using SmartPort.Infrastructure.Services;

namespace SmartPort.Web.Controllers;

[Authorize]
public class ImpactController : Controller
{
    private readonly ISmartPortIntelligenceService _intelligence;
    private readonly ITruckTrackingService _tracking;
    private readonly IExternalIntegrationHealthService _integrationHealth;

    public ImpactController(ISmartPortIntelligenceService intelligence, ITruckTrackingService tracking, IExternalIntegrationHealthService integrationHealth)
    {
        _intelligence = intelligence;
        _tracking = tracking;
        _integrationHealth = integrationHealth;
    }

    [Route("Impact")]
    [Route("ExecutiveImpact")]
    public async Task<IActionResult> Index()
    {
        var snapshot = await _intelligence.GetSnapshotAsync();
        var trucks = await _tracking.GetDashboardAsync();
        ViewBag.Snapshot = snapshot;
        ViewBag.Trucks = trucks;
        ViewBag.Integrations = await _integrationHealth.GetIntegrationHealthAsync();

        var avoidableMinutes = Math.Round(Math.Max(snapshot.IdlingMinutes, trucks.TotalIdlingMinutes) * 0.32m, 0);
        var co2 = Math.Round(avoidableMinutes / 60m * 3.0m * 2.68m, 1);
        var diesel = Math.Round(avoidableMinutes / 60m * 3.0m, 1);
        var fuelCost = Math.Round(diesel * 24.0m, 0);
        ViewBag.Kpis = new[]
        {
            new ImpactKpi("Idling minutes avoided", $"{avoidableMinutes:F0}", "Indicative shift opportunity", "cyan", 72),
            new ImpactKpi("CO₂ reduction potential", $"{co2:F1} kg", "Indicative demo estimate", "teal", 64),
            new ImpactKpi("Diesel cost saving", $"R{fuelCost:F0}", $"{diesel:F1} L diesel avoided", "green", 58),
            new ImpactKpi("Average gate queue reduction", "18–32%", "With dispatch metering", "blue", 61),
            new ImpactKpi("Berth utilisation upside", "6–11%", "Scenario-based potential", "purple", 49),
            new ImpactKpi("Truck turnaround improvement", "14–24 min", "Per high-risk truck", "cyan", 68),
            new ImpactKpi("Energy exposure", snapshot.LoadSheddingEnergyRisk.Contains("Active") ? "High" : "Watch", "Load-shedding playbook", "amber", snapshot.LoadSheddingEnergyRisk.Contains("Active") ? 84 : 38),
            new ImpactKpi("Operator actions generated", snapshot.RecommendedActions.Count.ToString(), "Explainable local rules", "teal", 76),
            new ImpactKpi("High-risk items", (trucks.HoldOutsidePortCount + snapshot.ActiveDisruptions).ToString(), "Requires attention", "red", 70),
            new ImpactKpi("Operational hours saved", $"{Math.Round(avoidableMinutes / 60m, 1):F1} h", "Demo estimate", "green", 54),
            new ImpactKpi("Dispatch coordination", "+28%", "Indicative improvement", "blue", 66)
        };
        return View();
    }
}

[Authorize]
public class CleanLogisticsController : Controller
{
    [Route("CleanLogistics")]
    [Route("Sustainability")]
    public IActionResult Index() => View();
}

[Authorize]
public class PilotReadinessController : Controller
{
    private readonly IExternalIntegrationHealthService _integrationHealth;
    public PilotReadinessController(IExternalIntegrationHealthService integrationHealth) => _integrationHealth = integrationHealth;

    [Route("PilotReadiness")]
    [Route("Roadmap")]
    public async Task<IActionResult> Index()
    {
        ViewBag.Integrations = await _integrationHealth.GetIntegrationHealthAsync();
        return View();
    }
}

[Authorize]
public class StakeholdersController : Controller
{
    [Route("Stakeholders")]
    public IActionResult Index() => View();
}

[Authorize]
public class BriefController : Controller
{
    private readonly ISmartPortIntelligenceService _intelligence;
    private readonly ITruckTrackingService _tracking;

    public BriefController(ISmartPortIntelligenceService intelligence, ITruckTrackingService tracking)
    {
        _intelligence = intelligence;
        _tracking = tracking;
    }

    [Route("Brief")]
    [Route("Reports/ExecutiveBrief")]
    public async Task<IActionResult> Index()
    {
        ViewBag.Snapshot = await _intelligence.GetSnapshotAsync();
        ViewBag.Trucks = await _tracking.GetDashboardAsync();
        return View();
    }
}

public record ImpactKpi(string Label, string Value, string Note, string Tone, int Progress);
