using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.Interfaces;
using SmartPort.Domain.Enums;
using SmartPort.Infrastructure.Persistence;
using SmartPort.Shared.Constants;

namespace SmartPort.Web.Controllers;

// ─── Dashboard ────────────────────────────────────────────────────────────────

[Authorize(Policy = Policies.CanAccessControlRoom)]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly IAlertService _alerts;

    public DashboardController(IDashboardService dashboard, IAlertService alerts)
    {
        _dashboard = dashboard;
        _alerts = alerts;
    }

    public async Task<IActionResult> Index()
    {
        var summary = await _dashboard.GetDashboardSummaryAsync();
        return View(summary);
    }
}

// ─── Vessels ──────────────────────────────────────────────────────────────────

[Authorize]
public class VesselsController : Controller
{
    private readonly IVesselService _vessels;
    private readonly IBerthService _berths;

    public VesselsController(IVesselService vessels, IBerthService berths)
    {
        _vessels = vessels;
        _berths = berths;
    }

    public async Task<IActionResult> Index(VesselFilterDto filter)
    {
        var result = await _vessels.GetVesselsAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var vessel = await _vessels.GetVesselDetailAsync(id);
        if (vessel == null) return NotFound();
        return View(vessel);
    }

    [Authorize(Policy = Policies.CanManageVessels)]
    public IActionResult Create() => View(new CreateVesselDto());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanManageVessels)]
    public async Task<IActionResult> Create(CreateVesselDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var id = await _vessels.CreateVesselAsync(dto, User.Identity!.Name!);
        TempData["Success"] = "Vessel registered successfully.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize(Policy = Policies.CanManageVessels)]
    public async Task<IActionResult> Edit(int id)
    {
        var v = await _vessels.GetVesselDetailAsync(id);
        if (v == null) return NotFound();
        var dto = new UpdateVesselDto
        {
            IMONumber = v.IMONumber, Name = v.Name, ShippingLine = v.ShippingLine,
            Agent = v.Agent, VesselType = v.VesselType, LengthOverall = v.LengthOverall,
            MaxDraught = v.MaxDraught, GrossTonnage = v.GrossTonnage, TEUCapacity = v.TEUCapacity ?? 0,
            VoyageNumber = v.VoyageNumber, Status = v.Status,
            EstimatedTimeOfArrival = v.ETA, EstimatedTimeOfDeparture = v.ETD,
            DelayMinutes = v.DelayMinutes, DelayReason = v.DelayReason
        };
        ViewBag.VesselId = id;
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanManageVessels)]
    public async Task<IActionResult> Edit(int id, UpdateVesselDto dto)
    {
        if (!ModelState.IsValid) { ViewBag.VesselId = id; return View(dto); }
        await _vessels.UpdateVesselAsync(id, dto, User.Identity!.Name!);
        TempData["Success"] = "Vessel updated.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Schedule()
    {
        var berths = await _berths.GetBerthOccupancyAsync();
        return View(berths);
    }
}

// ─── Berths ───────────────────────────────────────────────────────────────────

[Authorize]
public class BerthsController : Controller
{
    private readonly IBerthService _berths;
    private readonly IVesselService _vessels;

    public BerthsController(IBerthService berths, IVesselService vessels)
    {
        _berths = berths;
        _vessels = vessels;
    }

    public async Task<IActionResult> Index()
    {
        var berths = await _berths.GetAllBerthsAsync();
        return View(berths);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var berth = await _berths.GetBerthDetailAsync(id);
        if (berth == null) return NotFound();
        return View(berth);
    }

    public async Task<IActionResult> Schedule()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(7);
        var schedule = await _berths.GetBerthScheduleAsync(from, to);
        return View(schedule);
    }

    [Authorize(Policy = Policies.CanManageVessels)]
    public async Task<IActionResult> Assign()
    {
        var vessels = await _vessels.GetExpectedVesselsAsync(72);
        var berths = await _berths.GetAllBerthsAsync();
        ViewBag.Vessels = vessels;
        ViewBag.Berths = berths;
        return View(new CreateBerthAssignmentDto());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanManageVessels)]
    public async Task<IActionResult> Assign(CreateBerthAssignmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Vessels = await _vessels.GetExpectedVesselsAsync(72);
            ViewBag.Berths = await _berths.GetAllBerthsAsync();
            return View(dto);
        }
        await _berths.CreateBerthAssignmentAsync(dto, User.Identity!.Name!);
        TempData["Success"] = "Berth assignment created.";
        return RedirectToAction(nameof(Schedule));
    }
}

// ─── Containers ───────────────────────────────────────────────────────────────

[Authorize]
public class ContainersController : Controller
{
    private readonly IContainerService _containers;
    private readonly IYardService _yard;

    public ContainersController(IContainerService containers, IYardService yard)
    {
        _containers = containers;
        _yard = yard;
    }

    public async Task<IActionResult> Index(ContainerFilterDto filter)
    {
        var result = await _containers.GetContainersAsync(filter);
        ViewBag.Filter = filter;
        ViewBag.YardBlocks = await _yard.GetYardBlockStatusAsync();
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var container = await _containers.GetContainerDetailAsync(id);
        if (container == null) return NotFound();
        return View(container);
    }

    public async Task<IActionResult> Track(string? number)
    {
        if (!string.IsNullOrWhiteSpace(number))
        {
            var container = await _containers.GetContainerByNumberAsync(number);
            ViewBag.SearchNumber = number;
            ViewBag.Result = container;
        }
        return View();
    }

    public async Task<IActionResult> Yard()
    {
        var blocks = await _yard.GetYardBlockStatusAsync();
        return View(blocks);
    }

    public async Task<IActionResult> YardBlock(int id)
    {
        var block = await _yard.GetYardBlockDetailAsync(id);
        if (block == null) return NotFound();
        return View(block);
    }

    public async Task<IActionResult> DwellAlerts()
    {
        var containers = await _containers.GetDwellAlertContainersAsync();
        return View(containers);
    }
}

// ─── Gates / Trucks ───────────────────────────────────────────────────────────

[Authorize]
public class GatesController : Controller
{
    private readonly IGateService _gates;

    public GatesController(IGateService gates) => _gates = gates;

    public async Task<IActionResult> Index()
    {
        var gates = await _gates.GetGateStatusAsync();
        return View(gates);
    }

    public async Task<IActionResult> Trucks(TruckFilterDto filter)
    {
        var trucks = await _gates.GetTrucksAsync(filter);
        ViewBag.Filter = filter;
        return View(trucks);
    }

    public async Task<IActionResult> Transactions()
    {
        var txns = await _gates.GetRecentTransactionsAsync(100);
        return View(txns);
    }
}

// ─── Incidents ────────────────────────────────────────────────────────────────

[Authorize]
public class IncidentsController : Controller
{
    private readonly IIncidentService _incidents;
    private readonly IAlertService _alerts;
    private readonly IRecommendationService _recommendations;

    public IncidentsController(IIncidentService incidents, IAlertService alerts, IRecommendationService recommendations)
    {
        _incidents = incidents;
        _alerts = alerts;
        _recommendations = recommendations;
    }

    public async Task<IActionResult> Index(IncidentFilterDto filter)
    {
        var result = await _incidents.GetIncidentsAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var incident = await _incidents.GetIncidentDetailAsync(id);
        if (incident == null) return NotFound();
        return View(incident);
    }

    [Authorize(Policy = Policies.CanManageIncidents)]
    public IActionResult Create() => View(new CreateIncidentDto());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanManageIncidents)]
    public async Task<IActionResult> Create(CreateIncidentDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var id = await _incidents.CreateIncidentAsync(dto, User.Identity!.Name!);
        TempData["Success"] = "Incident created and reference number assigned.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanManageIncidents)]
    public async Task<IActionResult> Acknowledge(int id)
    {
        await _incidents.AcknowledgeIncidentAsync(id, User.Identity!.Name!);
        TempData["Success"] = "Incident acknowledged.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanManageIncidents)]
    public async Task<IActionResult> Resolve(int id, ResolveIncidentDto dto)
    {
        await _incidents.ResolveIncidentAsync(id, dto, User.Identity!.Name!);
        TempData["Success"] = "Incident resolved.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Alerts()
    {
        var alerts = await _alerts.GetActiveAlertsAsync();
        return View(alerts);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanAcknowledgeAlerts)]
    public async Task<IActionResult> AcknowledgeAlert(int id)
    {
        await _alerts.AcknowledgeAlertAsync(id, User.Identity!.Name!);
        TempData["Success"] = "Alert acknowledged.";
        return RedirectToAction(nameof(Alerts));
    }

    public async Task<IActionResult> Recommendations()
    {
        var recs = await _recommendations.GetActiveRecommendationsAsync();
        return View(recs);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptRecommendation(int id, string? notes)
    {
        await _recommendations.AcceptRecommendationAsync(id, User.Identity!.Name!, notes);
        TempData["Success"] = "Recommendation accepted and actioned.";
        return RedirectToAction(nameof(Recommendations));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissRecommendation(int id, string? notes)
    {
        await _recommendations.DismissRecommendationAsync(id, User.Identity!.Name!, notes);
        TempData["Info"] = "Recommendation dismissed.";
        return RedirectToAction(nameof(Recommendations));
    }
}

// ─── Documents ────────────────────────────────────────────────────────────────

[Authorize]
public class DocumentsController : Controller
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents) => _documents = documents;

    public async Task<IActionResult> Index(DocumentFilterDto filter)
    {
        var result = await _documents.GetDocumentsAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var doc = await _documents.GetDocumentDetailAsync(id);
        if (doc == null) return NotFound();
        return View(doc);
    }

    [Authorize(Policy = Policies.CanManageVessels)]
    public IActionResult Create() => View(new CreateDocumentDto());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanManageVessels)]
    public async Task<IActionResult> Create(CreateDocumentDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var id = await _documents.CreateDocumentAsync(dto, User.Identity!.Name!);
        TempData["Success"] = "Document record created.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanApproveDocuments)]
    public async Task<IActionResult> Approve(int id)
    {
        await _documents.UpdateDocumentStatusAsync(id, DocumentStatus.Approved, User.Identity!.Name!);
        TempData["Success"] = "Document approved.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.CanApproveDocuments)]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        await _documents.UpdateDocumentStatusAsync(id, DocumentStatus.Rejected, User.Identity!.Name!, reason);
        TempData["Warning"] = "Document rejected.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Overdue()
    {
        var docs = await _documents.GetOverdueDocumentsAsync();
        return View(docs);
    }
}

// ─── Analytics ────────────────────────────────────────────────────────────────

[Authorize(Policy = Policies.CanAccessReports)]
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    public async Task<IActionResult> Index(AnalyticsFilterDto filter)
    {
        if (filter.FromDate == default) filter.FromDate = DateTime.UtcNow.AddDays(-30);
        if (filter.ToDate == default) filter.ToDate = DateTime.UtcNow;

        var throughput = await _analytics.GetThroughputAnalyticsAsync(filter);
        var turnaround = await _analytics.GetTurnaroundAnalyticsAsync(filter);
        var berth = await _analytics.GetBerthEfficiencyAsync(filter);
        var yard = await _analytics.GetYardAnalyticsAsync(filter);

        ViewBag.Throughput = throughput;
        ViewBag.Turnaround = turnaround;
        ViewBag.Berth = berth;
        ViewBag.Yard = yard;
        ViewBag.Filter = filter;

        return View();
    }
}

// ─── Admin ────────────────────────────────────────────────────────────────────

[Authorize(Policy = Policies.CanManageUsers)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.Where(u => u.IsActive).ToList();
        var userVms = new List<UserAdminViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            userVms.Add(new UserAdminViewModel
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email ?? "",
                JobTitle = u.JobTitle, Organisation = u.Organisation, Terminal = u.Terminal,
                IsActive = u.IsActive, Roles = roles.ToList(), LastLoginAt = u.LastLoginAt
            });
        }
        return View(userVms);
    }

    public async Task<IActionResult> UserDetail(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        return View(new UserAdminViewModel
        {
            Id = user.Id, FullName = user.FullName, Email = user.Email ?? "",
            JobTitle = user.JobTitle, Organisation = user.Organisation, Terminal = user.Terminal,
            IsActive = user.IsActive, Roles = roles.ToList(), LastLoginAt = user.LastLoginAt
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUserActive(string id, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);
        TempData["Success"] = $"User {(isActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Settings() => View();

    public IActionResult Roles()
    {
        var roles = _roleManager.Roles.ToList();
        return View(roles);
    }
}

public class UserAdminViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Organisation { get; set; }
    public string? Terminal { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime? LastLoginAt { get; set; }
}
