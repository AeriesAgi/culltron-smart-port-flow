using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.Interfaces;
using SmartPort.Shared.Constants;

namespace SmartPort.Web.Controllers;

[Authorize(Policy = Policies.CanAccessControlRoom)]
public class IntegrationController : Controller
{
    private readonly ISyntheticSmartPortConnector _syntheticConnector;
    private readonly ISmartPortIntegrationHealthService _healthService;
    private readonly ISmartPortFieldMappingService _fieldMappingService;
    private readonly ISmartPortReadinessScoringService _readinessScoringService;

    public IntegrationController(
        ISyntheticSmartPortConnector syntheticConnector,
        ISmartPortIntegrationHealthService healthService,
        ISmartPortFieldMappingService fieldMappingService,
        ISmartPortReadinessScoringService readinessScoringService)
    {
        _syntheticConnector = syntheticConnector;
        _healthService = healthService;
        _fieldMappingService = fieldMappingService;
        _readinessScoringService = readinessScoringService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var report = await _readinessScoringService.GetPilotReadinessReportAsync(cancellationToken);
        return View(report);
    }

    public async Task<IActionResult> DataSources(CancellationToken cancellationToken)
    {
        ViewBag.Health = await _healthService.GetConnectorHealthAsync(cancellationToken);
        var snapshot = await _syntheticConnector.GetSnapshotAsync(cancellationToken);
        return View(snapshot);
    }

    public async Task<IActionResult> FieldMapping(CancellationToken cancellationToken)
    {
        ViewBag.MissingMappings = await _fieldMappingService.GetMissingPilotMappingsAsync(cancellationToken);
        var mappings = await _fieldMappingService.GetSeedMappingsAsync(cancellationToken);
        return View(mappings);
    }

    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var health = await _healthService.GetConnectorHealthAsync(cancellationToken);
        return View(health);
    }

    public async Task<IActionResult> PilotReport(CancellationToken cancellationToken)
    {
        var report = await _readinessScoringService.GetPilotReadinessReportAsync(cancellationToken);
        return View(report);
    }
}
