using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPort.Application.Interfaces;
using SmartPort.Application.DTOs;
using SmartPort.Domain.Enums;
using SmartPort.Infrastructure.Services;
using SmartPort.Shared.Constants;

namespace SmartPort.Web.Controllers;

// ─── AI Agent Controller ──────────────────────────────────────────────────────

[Authorize(Policy = Policies.CanAccessGeminiAgent)]
public class AgentController : Controller
{
    private readonly IAiAgentService _agent;
    private readonly ISmartPortIntelligenceService _intelligence;
    private readonly IOperationalReportService _reports;

    public AgentController(IAiAgentService agent, ISmartPortIntelligenceService intelligence, IOperationalReportService reports)
    {
        _agent = agent;
        _intelligence = intelligence;
        _reports = reports;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.SuggestedQuestions = _agent.GetSuggestedQuestions();
        ViewBag.Context = await _agent.GetContextAsync();
        ViewBag.Snapshot = await _intelligence.GetSnapshotAsync();
        ViewBag.AgentModeStatus = _reports.GetStatus();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return RedirectToAction(nameof(Index));

        var answer = await _agent.AskAsync(question);
        ViewBag.Answer = answer;
        ViewBag.SuggestedQuestions = _agent.GetSuggestedQuestions();
        ViewBag.Context = await _agent.GetContextAsync();
        ViewBag.Snapshot = await _intelligence.GetSnapshotAsync();
        ViewBag.AgentModeStatus = _reports.GetStatus();
        return View("Index");
    }

    [HttpGet]
    public async Task<IActionResult> AskJson(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(new { error = "No question provided" });
        var answer = await _agent.AskAsync(q);
        return Json(answer);
    }
}

// ─── Scenario Simulator Controller ───────────────────────────────────────────

[Authorize]
public class SimulatorController : Controller
{
    private readonly IScenarioSimulatorService _sim;

    public SimulatorController(IScenarioSimulatorService sim) => _sim = sim;

    public IActionResult Index()
    {
        ViewBag.Presets = _sim.GetPresetScenarios();
        return View(new ScenarioInput());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(ScenarioInput input)
    {
        var result = await _sim.SimulateAsync(input);
        ViewBag.Presets = _sim.GetPresetScenarios();
        ViewBag.Input = input;
        return View("Result", result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunPreset(string name)
    {
        var presets = _sim.GetPresetScenarios();
        var input = presets.FirstOrDefault(p => p.ScenarioName == name)
                    ?? PresetScenarios.DurbanHighCongestion;
        var result = await _sim.SimulateAsync(input);
        ViewBag.Presets = presets;
        ViewBag.Input = input;
        return View("Result", result);
    }
}

// ─── Organisations Controller ─────────────────────────────────────────────────

[Authorize]
public class OrganisationsController : Controller
{
    private readonly IOrganisationService _orgs;
    public OrganisationsController(IOrganisationService orgs) => _orgs = orgs;

    public async Task<IActionResult> Index()
        => View(await _orgs.GetAllAsync());

    public async Task<IActionResult> Detail(int id)
    {
        var org = await _orgs.GetByIdAsync(id);
        if (org == null) return NotFound();
        return View(org);
    }

    [Authorize(Roles = "Admin,PortOperationsManager")]
    public IActionResult Create() => View(new SaveOrganisationDto());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,PortOperationsManager")]
    public async Task<IActionResult> Create(SaveOrganisationDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var id = await _orgs.CreateAsync(dto, User.Identity!.Name!);
        TempData["Success"] = "Organisation registered.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize(Roles = "Admin,PortOperationsManager")]
    public async Task<IActionResult> Edit(int id)
    {
        var org = await _orgs.GetByIdAsync(id);
        if (org == null) return NotFound();
        ViewBag.OrgId = id;
        return View(new SaveOrganisationDto {
            Name = org.Name, OrganisationType = org.OrganisationType,
            RegistrationNumber = org.RegistrationNumber, ContactPerson = org.ContactPerson,
            ContactEmail = org.ContactEmail, ContactPhone = org.ContactPhone,
            Address = org.Address, Province = org.Province
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,PortOperationsManager")]
    public async Task<IActionResult> Edit(int id, SaveOrganisationDto dto)
    {
        if (!ModelState.IsValid) { ViewBag.OrgId = id; return View(dto); }
        await _orgs.UpdateAsync(id, dto, User.Identity!.Name!);
        TempData["Success"] = "Organisation updated.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}

// ─── Fleet Controller ─────────────────────────────────────────────────────────

[Authorize(Policy = Policies.CanAccessFleet)]
public class FleetController : Controller
{
    private readonly IFleetVehicleService _fleet;
    private readonly IOrganisationService _orgs;
    private readonly IFleetDriverQueueService _queue;
    private readonly INotificationService _notifications;
    private readonly IDriverStatusCommandService _commands;
    private readonly ISmartPortCopilotChatService _copilot;
    private readonly IOperationalActionService _actions;
    public FleetController(IFleetVehicleService fleet, IOrganisationService orgs, IFleetDriverQueueService queue, INotificationService notifications, IDriverStatusCommandService commands, ISmartPortCopilotChatService copilot, IOperationalActionService actions)
    {
        _fleet = fleet; _orgs = orgs; _queue = queue; _notifications = notifications; _commands = commands; _copilot = copilot; _actions = actions;
    }

    [HttpGet("/fleet")]
    public async Task<IActionResult> Index(string? fleetOperatorId)
    {
        var summary = await _queue.GetFleetSummaryAsync(fleetOperatorId);
        return View(summary);
    }

    [HttpGet("/fleet/trucks")]
    public async Task<IActionResult> Trucks(string? fleetOperatorId)
    {
        var trucks = await _queue.GetTrucksAsync(fleetOperatorId);
        return View(trucks);
    }

    [HttpGet("/fleet/tracker")]
    public async Task<IActionResult> Tracker(string? fleetOperatorId)
    {
        ViewBag.FleetOperatorId = fleetOperatorId;
        return View(await _queue.GetTrucksAsync(fleetOperatorId));
    }

    [HttpGet("/fleet/trucks/{id}")]
    public async Task<IActionResult> TruckDetail(string id)
    {
        var truck = await _queue.GetTruckAsync(id);
        if (truck == null) return NotFound();
        truck.NotificationHistory = (await _notifications.GetHistoryAsync(id)).ToList();
        ViewBag.WhatsAppStatus = _queue.GetWhatsAppConnectorStatus();
        return View(truck);
    }

    [HttpGet("/fleet/owner-demo")]
    public async Task<IActionResult> OwnerDemo() => View("Index", await _queue.GetFleetSummaryAsync("durban-freight"));

    [HttpGet("/fleet/drivers")]
    public async Task<IActionResult> Drivers()
    {
        ViewBag.FleetOperators = (await _queue.GetFleetSummaryAsync()).FleetOperators;
        ViewBag.Trucks = await _queue.GetTrucksAsync();
        ViewBag.WhatsAppStatus = _queue.GetWhatsAppConnectorStatus();
        return View(await _queue.GetDriverContactsAsync());
    }

    [Authorize(Policy = Policies.CanManageSettings)]
    [HttpGet("/fleet/drivers/create")]
    public async Task<IActionResult> CreateDriver()
    {
        ViewBag.FleetOperators = (await _queue.GetFleetSummaryAsync()).FleetOperators;
        ViewBag.Trucks = await _queue.GetTrucksAsync();
        ViewBag.WhatsAppStatus = _queue.GetWhatsAppConnectorStatus();
        return View("DriverForm", new SaveDriverContactRequestDto());
    }

    [Authorize(Policy = Policies.CanManageSettings)]
    [HttpPost("/fleet/drivers")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveDriver(SaveDriverContactRequestDto request)
    {
        try { await _queue.SaveDriverContactAsync(request); TempData["Success"] = "Driver contact and companion-app readiness saved."; }
        catch (ArgumentException ex) { TempData["Warning"] = ex.Message; }
        return RedirectToAction(nameof(Drivers));
    }

    [HttpGet("/fleet/trucks/create")]
    public async Task<IActionResult> CreateQueueTruck()
    {
        ViewBag.FleetOperators = (await _queue.GetFleetSummaryAsync()).FleetOperators;
        ViewBag.Drivers = await _queue.GetDriverContactsAsync();
        return View("TruckForm", new SaveFleetQueueTruckRequestDto());
    }

    [HttpPost("/fleet/trucks/save")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveQueueTruck(SaveFleetQueueTruckRequestDto request)
    {
        var truck = await _queue.SaveTruckAsync(request);
        TempData["Success"] = "Truck/job assignment saved.";
        return Redirect($"/fleet/trucks/{truck.BookingReference}");
    }

    [Authorize(Policy = Policies.CanManageSettings)]
    [HttpGet("/fleet/settings")]
    public async Task<IActionResult> Settings() { ViewBag.WhatsAppStatus = _queue.GetWhatsAppConnectorStatus(); ViewBag.GeminiStatus = (await _copilot.BuildPageAsync()).AgentModeStatus; return View(); }

    [Authorize(Policy = Policies.CanManageSettings)]
    [HttpPost("/fleet/settings/test-gemini")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TestGemini()
    {
        var response = await _copilot.GenerateResponseAsync("Smart Port settings test: explain the next safe action for the current demo queue in two sentences.");
        TempData["GeminiTest"] = $"{response.GeneratedBy}: {response.ShortAnswer}";
        return Redirect("/fleet/settings");
    }

    [HttpGet("/fleet/download-app")]
    [HttpGet("/mobile/download")]
    public async Task<IActionResult> DownloadApp() { ViewBag.DemoReferences = await _queue.GetDemoReferencesAsync(); ViewBag.ApkExists = System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "downloads", "SmartPortDriverCompanion-debug.apk")); return View(); }

    [HttpGet("/fleet/data-sources")]
    public async Task<IActionResult> DataSources() { ViewBag.WhatsAppStatus = _queue.GetWhatsAppConnectorStatus(); return View(await _queue.GetDataSourcesAsync()); }

    [HttpGet("/fleet/notifications")]
    public async Task<IActionResult> Notifications(string? reference)
    {
        var refs = await _queue.GetDemoReferencesAsync();
        var selected = string.IsNullOrWhiteSpace(reference) ? refs.First() : reference;
        ViewBag.DemoReferences = refs;
        ViewBag.SelectedReference = selected;
        ViewBag.WhatsAppStatus = _queue.GetWhatsAppConnectorStatus();
        return View(await _notifications.GetHistoryAsync(selected));
    }

    [HttpPost("/fleet/notify/{reference}/{channel}")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SendNotification(string reference, NotificationChannel channel)
    {
        var forceDemo = string.Equals(Request.Query["mode"], "demo", StringComparison.OrdinalIgnoreCase);
        if (forceDemo && channel == NotificationChannel.WhatsApp)
        {
            var truck = await _queue.GetTruckAsync(reference);
            if (truck != null) await _notifications.RecordAsync(reference, NotificationChannel.WhatsApp, NotificationStatus.SimulatedSent, NotificationEventType.AiInstructionUpdated, $"Simulated WhatsApp: {truck.LatestNotification}", DataProvenanceType.SyntheticDemoData, "Fleet owner");
            TempData["Success"] = $"Demo WhatsApp notification recorded for {reference}.";
        }
        else
        {
            var action = await _actions.ExecuteAsync(new OperationalActionRequest(reference, OperationalActionType.RefreshStatus, "Fleet owner", DataProvenanceType.ManualOperatorInput, channel));
            TempData[action.Success ? "Success" : "Warning"] = $"{action.Message} Source: {action.Source}. Meta ID: {action.WhatsAppMetaMessageId}";
        }
        return Redirect(Request.Headers.Referer.ToString() ?? "/fleet/notifications");
    }

    [HttpPost("/fleet/trucks/{reference}/action")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TruckAction(string reference, string commandText)
    {
        var actionType = commandText switch { "ARRIVED_STAGING" => OperationalActionType.MoveToStaging, "PROCEEDING_GATE" => OperationalActionType.ReleaseToGate, "DELAYED 20" => OperationalActionType.Reschedule, "ISSUE" => OperationalActionType.MarkException, _ => OperationalActionType.RefreshStatus };
        var result = await _actions.ExecuteAsync(new OperationalActionRequest(reference, actionType, "Fleet operations console", DataProvenanceType.ManualOperatorInput, CommandText: commandText));
        TempData[result.Success ? "Success" : "Warning"] = $"{result.Message} Audit: {result.AuditTimestampUtc:u}. Next: {result.NextRecommendedAction}";
        return Redirect($"/fleet/trucks/{reference}");
    }

    [HttpPost("/fleet/trucks/{reference}/request-location")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RequestLocation(string reference)
    {
        var result = await _actions.ExecuteAsync(new OperationalActionRequest(reference, OperationalActionType.RequestLocation, "Fleet operations console", DataProvenanceType.ManualOperatorInput, NotificationChannel.InApp));
        TempData[result.Success ? "Success" : "Warning"] = $"App check-in request recorded. {result.Message} Audit: {result.AuditTimestampUtc:u}.";
        return Redirect($"/fleet/trucks/{reference}");
    }

    [HttpPost("/fleet/trucks/{reference}/copilot")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TruckCopilot(string reference, string question)
    {
        var response = await _copilot.GenerateResponseAsync($"Fleet owner asks about truck {reference}: {question}. Keep the answer to Smart Port queue operations and next driver action.");
        TempData["CopilotAnswer"] = response.ShortAnswer;
        return Redirect($"/fleet/trucks/{reference}");
    }

    [HttpGet("/fleet/vehicles")]
    public async Task<IActionResult> Vehicles(FleetVehicleFilterDto filter)
    {
        var result = await _fleet.GetVehiclesAsync(filter);
        ViewBag.Filter = filter;
        ViewBag.Organisations = await _orgs.GetAllAsync();
        return View("Vehicles", result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var v = await _fleet.GetByIdAsync(id);
        if (v == null) return NotFound();
        return View(v);
    }

    [Authorize(Roles = "Admin,PortOperationsManager")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Organisations = await _orgs.GetAllAsync();
        return View(new SaveFleetVehicleDto());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,PortOperationsManager")]
    public async Task<IActionResult> Create(SaveFleetVehicleDto dto)
    {
        if (!ModelState.IsValid) { ViewBag.Organisations = await _orgs.GetAllAsync(); return View(dto); }
        var id = await _fleet.CreateAsync(dto, User.Identity!.Name!);
        TempData["Success"] = "Vehicle registered.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize(Roles = "Admin,PortOperationsManager")]
    public async Task<IActionResult> Edit(int id)
    {
        var v = await _fleet.GetByIdAsync(id);
        if (v == null) return NotFound();
        ViewBag.VehicleId = id; ViewBag.Organisations = await _orgs.GetAllAsync();
        return View(new SaveFleetVehicleDto {
            OrganisationId = v.OrganisationId, RegistrationNumber = v.RegistrationNumber,
            FleetNumber = v.FleetNumber, VehicleType = v.VehicleType, CargoType = v.CargoType,
            CapacityTons = v.CapacityTons, Status = v.Status, CurrentLocation = v.CurrentLocation
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,PortOperationsManager")]
    public async Task<IActionResult> Edit(int id, SaveFleetVehicleDto dto)
    {
        if (!ModelState.IsValid) { ViewBag.VehicleId = id; ViewBag.Organisations = await _orgs.GetAllAsync(); return View(dto); }
        await _fleet.UpdateAsync(id, dto, User.Identity!.Name!);
        TempData["Success"] = "Vehicle updated.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}

// ─── Dispatch Controller ──────────────────────────────────────────────────────

[Authorize]
public class DispatchController : Controller
{
    private readonly IDispatchTripService _trips;
    private readonly IOrganisationService _orgs;
    private readonly IFleetVehicleService _fleet;
    private readonly IDispatchRecommendationService _recSvc;
    private readonly IIdlingEmissionService _emSvc;
    private readonly SmartPort.Infrastructure.Persistence.SmartPortDbContext _db;

    public DispatchController(IDispatchTripService trips, IOrganisationService orgs,
        IFleetVehicleService fleet, IDispatchRecommendationService recSvc,
        IIdlingEmissionService emSvc,
        SmartPort.Infrastructure.Persistence.SmartPortDbContext db)
    {
        _trips = trips; _orgs = orgs; _fleet = fleet; _recSvc = recSvc; _emSvc = emSvc; _db = db;
    }

    public async Task<IActionResult> Index(DispatchTripFilterDto filter)
    {
        var result = await _trips.GetTripsAsync(filter);
        ViewBag.Filter = filter;
        ViewBag.Organisations = await _orgs.GetAllAsync();
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var trip = await _trips.GetByIdAsync(id);
        if (trip == null) return NotFound();
        return View(trip);
    }

    [Authorize(Roles = "Admin,PortOperationsManager,TerminalStaff")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new SaveDispatchTripDto { PlannedDispatchTime = DateTime.Now.AddHours(1), PlannedArrivalWindowStart = DateTime.Now.AddHours(2), PlannedArrivalWindowEnd = DateTime.Now.AddHours(3) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,PortOperationsManager,TerminalStaff")]
    public async Task<IActionResult> Create(SaveDispatchTripDto dto)
    {
        if (!ModelState.IsValid) { await PopulateDropdowns(); return View(dto); }
        var id = await _trips.CreateAsync(dto, User.Identity!.Name!);
        TempData["Success"] = "Dispatch trip created.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecommendation(int id)
    {
        var tripDetail = await _trips.GetByIdAsync(id);
        if (tripDetail == null) return NotFound();

        var tripEntity = await _db.DispatchTrips.FindAsync(id);
        if (tripEntity == null) return NotFound();

        var rec = await _recSvc.GenerateRecommendationAsync(tripEntity);

        var flowRec = new SmartPort.Domain.Entities.FlowRecommendation {
            DispatchTripId = id, OrganisationId = tripEntity.OrganisationId,
            RecommendationType = rec.RecommendationType, RiskLevel = rec.RiskLevel,
            ConfidenceLevel = rec.ConfidenceLevel, RecommendationText = rec.RecommendationText,
            Reason = rec.Reason, ExpectedBenefit = rec.ExpectedBenefit, CongestionScore = rec.CongestionScore,
            GeneratedAt = DateTime.UtcNow, CreatedBy = User.Identity!.Name!
        };
        _db.FlowRecommendations.Add(flowRec);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Recommendation generated: {rec.RecommendationType} — Risk: {rec.RiskLevel}";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateEmissions(int id)
    {
        var tripEntity = await _db.DispatchTrips.FindAsync(id);
        if (tripEntity == null) return NotFound();

        var existing = await _db.IdlingEmissionEstimates.FirstOrDefaultAsync(e => e.DispatchTripId == id);
        if (existing != null) { _db.IdlingEmissionEstimates.Remove(existing); }

        var est = await _emSvc.CalculateEstimateAsync(tripEntity);
        _db.IdlingEmissionEstimates.Add(new SmartPort.Domain.Entities.IdlingEmissionEstimate {
            DispatchTripId = id, EstimatedIdlingMinutes = est.EstimatedIdlingMinutes,
            EstimatedDieselLitres = est.EstimatedDieselLitres, EstimatedFuelCost = est.EstimatedFuelCost,
            EstimatedCo2Kg = est.EstimatedCo2Kg, AvoidableIdlingFlag = est.AvoidableIdlingFlag,
            CalculationNotes = est.Notes, CreatedBy = User.Identity!.Name!
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Emissions calculated: {est.EstimatedIdlingMinutes:F0} min idling, {est.EstimatedCo2Kg:F1} kg CO₂, R{est.EstimatedFuelCost:F2} fuel.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, TripStatus status)
    {
        await _trips.UpdateStatusAsync(id, status, User.Identity!.Name!);
        TempData["Success"] = $"Trip status updated to {status}.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Organisations = await _orgs.GetAllAsync();
        ViewBag.Vehicles = await _fleet.GetVehiclesAsync(new FleetVehicleFilterDto { PageSize = 200 });
        ViewBag.Drivers = await _db.Drivers.Where(d => d.IsActive && !d.IsDeleted).ToListAsync();
    }
}

// ─── Flow Intelligence Controller ────────────────────────────────────────────

[Authorize]
public class FlowController : Controller
{
    private readonly IFlowIntelligenceService _flow;
    private readonly IDisruptionService _disruptions;
    private readonly IPilotMetricsService _pilot;
    private readonly IDispatchTripService _trips;

    public FlowController(IFlowIntelligenceService flow, IDisruptionService disruptions,
        IPilotMetricsService pilot, IDispatchTripService trips)
    { _flow = flow; _disruptions = disruptions; _pilot = pilot; _trips = trips; }

    public async Task<IActionResult> Index()
    {
        var summary = await _flow.GetSummaryAsync();
        return View(summary);
    }

    public async Task<IActionResult> Recommendations()
    {
        var recs = await _flow.GetActiveRecommendationsAsync(50);
        return View(recs);
    }

    public async Task<IActionResult> Recommendation(int id)
    {
        var rec = await _flow.GetRecommendationByIdAsync(id);
        if (rec == null) return NotFound();
        return View(rec);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id, string? feedback)
    {
        await _flow.AcceptRecommendationAsync(id, feedback ?? "Accepted", User.Identity!.Name!);
        TempData["Success"] = "Recommendation accepted and actioned.";
        return RedirectToAction(nameof(Recommendations));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(int id, string? feedback)
    {
        await _flow.DismissRecommendationAsync(id, feedback ?? "Dismissed", User.Identity!.Name!);
        TempData["Info"] = "Recommendation dismissed.";
        return RedirectToAction(nameof(Recommendations));
    }
}

// ─── Disruptions Controller ───────────────────────────────────────────────────

[Authorize]
public class DisruptionsController : Controller
{
    private readonly IDisruptionService _disruptions;
    public DisruptionsController(IDisruptionService disruptions) => _disruptions = disruptions;

    public async Task<IActionResult> Index()
    {
        ViewBag.Active = await _disruptions.GetActiveAsync();
        ViewBag.All    = await _disruptions.GetAllAsync(30);
        return View();
    }

    [Authorize(Roles = "Admin,PortOperationsManager,TerminalStaff")]
    public IActionResult Create() => View(new SaveDisruptionDto { StartTime = DateTime.Now });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,PortOperationsManager,TerminalStaff")]
    public async Task<IActionResult> Create(SaveDisruptionDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _disruptions.CreateAsync(dto, User.Identity!.Name!);
        TempData["Success"] = "Disruption event created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,PortOperationsManager,TerminalStaff")]
    public async Task<IActionResult> Resolve(int id)
    {
        await _disruptions.ResolveAsync(id, User.Identity!.Name!);
        TempData["Success"] = "Disruption resolved.";
        return RedirectToAction(nameof(Index));
    }
}

// ─── Emissions Controller ─────────────────────────────────────────────────────

[Authorize]
public class EmissionsController : Controller
{
    private readonly IEmissionsSummaryService _emissions;
    public EmissionsController(IEmissionsSummaryService emissions) => _emissions = emissions;

    public async Task<IActionResult> Index()
    {
        var summary = await _emissions.GetSummaryAsync();
        ViewBag.TopTrips = await _emissions.GetTopIdlingTripsAsync(20);
        return View(summary);
    }
}

// ─── Pilot Metrics Controller ─────────────────────────────────────────────────

[Authorize]
public class PilotController : Controller
{
    private readonly IPilotMetricsService _pilot;
    private readonly SmartPort.Infrastructure.Persistence.SmartPortDbContext _db;

    public PilotController(IPilotMetricsService pilot,
        SmartPort.Infrastructure.Persistence.SmartPortDbContext db) { _pilot = pilot; _db = db; }

    public async Task<IActionResult> Index()
    {
        var comparison = await _pilot.GetPilotComparisonAsync();
        ViewBag.AllSnapshots = await _db.PilotMetricSnapshots
            .OrderByDescending(p => p.SnapshotDate).ToListAsync();
        return View(comparison);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,PortOperationsManager")]
    public async Task<IActionResult> GenerateSnapshot(string periodLabel)
    {
        await _pilot.GeneratePilotSnapshotAsync(periodLabel);
        TempData["Success"] = $"Pilot snapshot generated for: {periodLabel}";
        return RedirectToAction(nameof(Index));
    }
}

// ─── Reports Controller ───────────────────────────────────────────────────────

[Authorize]
public class ReportsController : Controller
{
    private readonly IEmissionsSummaryService _emissions;
    private readonly IPilotMetricsService _pilot;
    private readonly IDisruptionService _disruptions;
    private readonly IFlowIntelligenceService _flow;
    private readonly IDispatchTripService _trips;
    private readonly IOperationalReportService _agentReports;

    public ReportsController(IEmissionsSummaryService emissions, IPilotMetricsService pilot,
        IDisruptionService disruptions, IFlowIntelligenceService flow, IDispatchTripService trips,
        IOperationalReportService agentReports)
    { _emissions = emissions; _pilot = pilot; _disruptions = disruptions; _flow = flow; _trips = trips; _agentReports = agentReports; }

    public IActionResult Index()
    {
        ViewBag.AgentModeStatus = _agentReports.GetStatus();
        ViewBag.AgentReportTypes = _agentReports.GetReportTypes();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgentReport(string reportType, AgentMode mode = AgentMode.Hybrid, string? scenarioSummary = null, CancellationToken cancellationToken = default)
    {
        var result = await _agentReports.GenerateAsync(new OperationalReportRequest
        {
            ReportType = reportType,
            Mode = mode,
            ScenarioSummary = scenarioSummary ?? string.Empty
        }, cancellationToken);
        ViewBag.AgentModeStatus = _agentReports.GetStatus();
        ViewBag.AgentReportTypes = _agentReports.GetReportTypes();
        return View("AgentReport", result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadAgentReport(string reportType, AgentMode mode = AgentMode.Hybrid, string? scenarioSummary = null, CancellationToken cancellationToken = default)
    {
        var result = await _agentReports.GenerateAsync(new OperationalReportRequest
        {
            ReportType = reportType,
            Mode = mode,
            ScenarioSummary = scenarioSummary ?? string.Empty
        }, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(result.Markdown), "text/markdown", $"SmartPort_{result.ReportType.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMddHHmm}.md");
    }

    public async Task<IActionResult> Operational()
    {
        ViewBag.Summary = await _flow.GetSummaryAsync();
        ViewBag.ActiveTrips = await _trips.GetActiveTripsAsync();
        ViewBag.HighRisk = await _trips.GetHighRiskTripsAsync(10);
        ViewBag.Disruptions = await _disruptions.GetActiveAsync();
        return View();
    }

    public async Task<IActionResult> Emissions()
    {
        var summary = await _emissions.GetSummaryAsync();
        ViewBag.TopTrips = await _emissions.GetTopIdlingTripsAsync(30);
        return View(summary);
    }

    public async Task<IActionResult> Pilot()
    {
        ViewBag.Comparison = await _pilot.GetPilotComparisonAsync();
        return View();
    }

    public async Task<IActionResult> Disruptions()
    {
        ViewBag.All = await _disruptions.GetAllAsync(100);
        return View();
    }

    public async Task<IActionResult> Recommendations()
    {
        ViewBag.Recs = await _flow.GetActiveRecommendationsAsync(100);
        return View();
    }

    public async Task<IActionResult> ExportEmissionsCsv()
    {
        var trips = await _emissions.GetTopIdlingTripsAsync(500);
        var csv = "Trip ID,Vehicle,Organisation,Route,Idling (min),Diesel (L),Fuel Cost (R),CO2 (kg),Avoidable,Date\n";
        foreach (var t in trips)
            csv += $"{t.TripId},{t.VehicleRegistration},{t.OrganisationName},{t.RouteName},{t.IdlingMinutes:F1},{t.DieselLitres:F2},{t.FuelCost:F2},{t.Co2Kg:F2},{t.AvoidableFlag},{t.TripDate:yyyy-MM-dd}\n";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"SmartPort_Emissions_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
