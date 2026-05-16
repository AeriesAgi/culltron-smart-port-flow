using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.DTOs;
using SmartPort.Application.Interfaces;
using SmartPort.Infrastructure.Services;
using SmartPort.Shared.Constants;

namespace SmartPort.Web.Controllers;

[Authorize(Policy = Policies.CanAccessGeminiAgent)]
public class GeminiAgentController : Controller
{
    private static readonly List<GeminiAgentHistoryItem> History = new();
    private static readonly object HistoryLock = new();
    private readonly IConfiguration _configuration;
    private readonly IAgentNarrativeService _narrative;
    private readonly ISmartPortCopilotChatService _copilot;
    private readonly IExecutionPlanService _plans;
    private readonly IFleetDriverQueueService _queue;

    public GeminiAgentController(IConfiguration configuration, IAgentNarrativeService narrative, ISmartPortCopilotChatService copilot, IExecutionPlanService plans, IFleetDriverQueueService queue)
    {
        _configuration = configuration;
        _narrative = narrative;
        _copilot = copilot;
        _plans = plans;
        _queue = queue;
    }

    [HttpGet("/gemini-agent")]
    [HttpGet("/agent/gemini")]
    public async Task<IActionResult> Index()
    {
        var summary = await _queue.GetFleetSummaryAsync();
        var readiness = BuildReadiness();
        ViewBag.Status = _narrative.GetStatus();
        ViewBag.Readiness = readiness;
        ViewBag.Intelligence = BuildIntelligence(summary);
        ViewBag.History = SnapshotHistory();
        ViewBag.LastHistory = SnapshotHistory().FirstOrDefault();
        return View(summary);
    }

    [HttpPost("/gemini-agent/generate")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Generate(string briefType = "Gemini Operations Brief")
    {
        var started = DateTime.UtcNow;
        var summary = await _queue.GetFleetSummaryAsync();
        var plan = await _plans.GeneratePlanAsync($"{briefType} · human-approved demo plan");
        var prompt = BuildPrompt(briefType, summary);
        var narrative = await _narrative.GenerateAsync(new AgentNarrativeRequest
        {
            Purpose = "gemini operations console",
            ReportType = NormalizeBriefType(briefType),
            UserPrompt = prompt,
            CurrentPage = "/gemini-agent",
            RequestedMode = AgentMode.Hybrid,
            Context = BuildOperationalContext(summary),
            DeterministicRecommendations = new[] { DeterministicRecommendation(summary), summary.LatestAiQueueRecommendation }
        });
        var response = new CopilotChatResponse
        {
            ShortAnswer = string.IsNullOrWhiteSpace(narrative.Narrative) ? DeterministicRecommendation(summary) : narrative.Narrative,
            RecommendedAction = DeterministicRecommendation(summary),
            GeneratedBy = narrative.UsedGemini ? "Gemini" : narrative.FallbackActive ? "Hybrid" : "Local Fallback"
        };
        var elapsed = (int)Math.Max(1, (DateTime.UtcNow - started).TotalMilliseconds);
        var generatedBy = narrative.UsedGemini ? "Gemini" : narrative.FallbackActive ? "Hybrid / Local Fallback" : "Local Fallback";
        var recommendation = string.IsNullOrWhiteSpace(response.RecommendedAction) ? DeterministicRecommendation(summary) : response.RecommendedAction;

        var item = new GeminiAgentHistoryItem(
            DateTime.UtcNow,
            NormalizeBriefType(briefType),
            generatedBy,
            $"queue={summary.TotalTrucks}; waiting={summary.TrucksWaiting}; holding={summary.TrucksHolding}; disruptions={summary.DelayedOrRescheduledTrucks}; notifications={summary.LatestNotifications.Count}; idlingAvoided={summary.TotalIdlingMinutesAvoided}",
            response.ShortAnswer,
            recommendation,
            DeterministicRecommendation(summary),
            narrative.UsedGemini ? response.ShortAnswer : "Gemini not configured or failed; deterministic recommendation retained.",
            narrative.UsedGemini ? "Gemini added live narrative reasoning; fallback kept executable action constraints." : $"Fallback retained because: {narrative.Status}.",
            "Human approval required before external fleet/driver send",
            plan.PlanId,
            elapsed);

        lock (HistoryLock)
        {
            History.Add(item);
            if (History.Count > 50) History.RemoveRange(0, History.Count - 50);
        }

        TempData["Success"] = $"{item.BriefType} generated via {generatedBy}. Audit event saved for this demo session in {elapsed} ms.";
        return Redirect("/gemini-agent");
    }

    internal static IReadOnlyList<GeminiAgentHistoryItem> SnapshotHistory()
    {
        lock (HistoryLock)
        {
            return History.OrderByDescending(h => h.GeneratedAtUtc).Take(12).ToList();
        }
    }

    private GeminiReadinessViewModel BuildReadiness()
    {
        var apiKeyConfigured = !string.IsNullOrWhiteSpace(_configuration["GEMINI_API_KEY"] ?? _configuration["Gemini:ApiKey"]);
        var enabledText = _configuration["Gemini:Enabled"] ?? _configuration["GEMINI_ENABLED"];
        var enabled = bool.TryParse(enabledText, out var parsed) && parsed;
        var mode = _configuration["Gemini:Mode"] ?? _configuration["GEMINI_MODE"] ?? "Hybrid demo / deterministic fallback";
        var model = _configuration["Gemini:Model"] ?? _configuration["GEMINI_MODEL"] ?? "gemini-2.5-flash";
        var last = SnapshotHistory().FirstOrDefault();
        return new GeminiReadinessViewModel(apiKeyConfigured, enabled, mode, model, apiKeyConfigured && enabled ? "Gemini-enhanced when provider responds; deterministic fallback retained" : "Local deterministic fallback active", last?.BriefType ?? "No generation yet", last?.LatencyMs);
    }

    private static OperationalIntelligenceViewModel BuildIntelligence(FleetQueueSummaryDto summary)
    {
        var congestionRisk = Math.Min(100, 30 + summary.TrucksWaiting * 8 + summary.TrucksHolding * 5 + summary.DelayedOrRescheduledTrucks * 10);
        var executionReadiness = Math.Clamp(82 - summary.DelayedOrRescheduledTrucks * 7 + summary.TrucksProceeding * 4, 0, 100);
        var emissionsImpact = Math.Clamp(55 + summary.TotalIdlingMinutesAvoided / 3, 0, 100);
        var driverResponse = Math.Clamp(70 + summary.LatestNotifications.Count * 3 - summary.HighRiskTrucks.Count * 5, 0, 100);
        var integrationConfidence = summary.DataSources.Count == 0 ? 76 : Math.Clamp(68 + summary.DataSources.Count * 4, 0, 92);
        var overall = (congestionRisk + executionReadiness + emissionsImpact + driverResponse + integrationConfidence) / 5;
        return new OperationalIntelligenceViewModel(overall, congestionRisk, executionReadiness, emissionsImpact, driverResponse, integrationConfidence, summary.HighRiskTrucks.Count, summary.DelayedOrRescheduledTrucks > 0 ? "Gate and berth timing variance" : "Gate pressure with controlled staging", summary.TotalIdlingMinutesAvoided * 18, 91, 84);
    }

    private static string BuildPrompt(string briefType, FleetQueueSummaryDto summary) => NormalizeBriefType(briefType) switch
    {
        "Fleet Owner Action Plan" => $"Generate a fleet owner action plan for {summary.TotalTrucks} Smart Port demo trucks. Include driver notifications, approvals, and WhatsApp/mobile caveats.",
        "Driver Instruction Brief" => "Generate safe driver instructions for the current Smart Port queue, staging, gate call-forward, and human-approved actions.",
        "Disruption Recovery Plan" => "Generate a disruption recovery plan for delayed staging, gate congestion, berth window movement, and power-risk context.",
        "Emissions/Idling Impact Brief" => $"Explain idling and CO2 impact using {summary.TotalIdlingMinutesAvoided} avoided minutes and {summary.TotalCo2KgAvoided} kg CO2 avoided.",
        "Executive Judge Summary" => "Prepare a two-minute judge demo summary showing public site, Gemini agent, execution plan, driver/WhatsApp simulation, governance, and audit trail.",
        "Risk & Governance Review" => "Review AI governance, human approval, prompt safety, synthetic data boundaries, secrets handling, and pilot connector readiness.",
        "Integration/Pilot Readiness Brief" => "Generate a pilot readiness brief for IPMS/TOS/gate/fleet/GPS/WhatsApp connector mapping without claiming live integration.",
        "Live Gemini Connector Test" => "Run a concise live Gemini connector test using sanitized Smart Port context. Return status, source, and a safe one-paragraph operations observation.",
        _ => "Generate a Gemini operations brief for gate pressure, berth readiness, idling impact, fleet/driver actions, audit trail, and next approvals."
    };

    private static string NormalizeBriefType(string briefType) => briefType switch
    {
        "Driver Instructions" => "Driver Instruction Brief",
        "Judge Demo Summary" => "Executive Judge Summary",
        "Gemini Operations Brief" => "Gemini Operations Brief",
        "Fleet Owner Action Plan" => "Fleet Owner Action Plan",
        "Disruption Recovery Plan" => "Disruption Recovery Plan",
        "Emissions/Idling Impact Brief" => "Emissions/Idling Impact Brief",
        "Executive Judge Summary" => "Executive Judge Summary",
        "Risk & Governance Review" => "Risk & Governance Review",
        "Integration/Pilot Readiness Brief" => "Integration/Pilot Readiness Brief",
        "Live Gemini Connector Test" => "Live Gemini Connector Test",
        _ => briefType
    };

    private static OperationalContext BuildOperationalContext(FleetQueueSummaryDto summary) => new()
    {
        TrucksInQueue = summary.TotalTrucks,
        ActiveTrips = summary.TrucksProceeding,
        HighRiskTrips = summary.HighRiskTrucks.Count,
        BerthUtilisationPct = summary.BerthReadiness.Count == 0 ? 78 : 84,
        YardOccupancyPct = 82,
        OpenIncidents = summary.DelayedOrRescheduledTrucks,
        ActiveDisruptions = summary.DelayedOrRescheduledTrucks,
        CriticalDisruptions = summary.HighRiskTrucks.Count(t => t.DelayRisk == QueueDelayRisk.Critical),
        GatesOperational = summary.GateCapacities.Count(g => g.Status != CapacityStatus.Closed),
        TotalIdlingMinutesToday = summary.TotalIdlingMinutesAvoided,
        EstimatedCo2Today = summary.TotalCo2KgAvoided,
        GateDelayActive = summary.TrucksWaiting > 0 || summary.TrucksHolding > 0,
        RoadCongestionActive = summary.DelayedOrRescheduledTrucks > 0,
        TopDisruptions = summary.BerthReadiness.Select(b => $"{b.BerthName}: {b.ReadinessStatus}").Take(4).ToList(),
        TopRecommendations = new() { summary.LatestAiQueueRecommendation, DeterministicRecommendation(summary) }
    };

    private static string DeterministicRecommendation(FleetQueueSummaryDto summary)
        => $"Hold {summary.TrucksHolding} trucks outside the gate, move high-risk trucks to staging, release only approved vehicles to gate, and keep WhatsApp/mobile sends simulated until a human approves the execution plan.";
}

public sealed record GeminiReadinessViewModel(bool ApiKeyConfigured, bool Enabled, string Mode, string Model, string FallbackStatus, string LastGenerationStatus, int? LastLatencyMs);
public sealed record OperationalIntelligenceViewModel(int OverallScore, int CongestionRisk, int ExecutionReadiness, int EmissionsImpact, int DriverResponseState, int IntegrationConfidence, int TrucksNeedingAction, string BottleneckReason, int ExecutionPlanRoiRand, int DataQualityScore, int ConnectorReadinessScore);
public sealed record GeminiAgentHistoryItem(DateTime GeneratedAtUtc, string BriefType, string SourceModel, string InputSignalsUsed, string Summary, string RecommendedAction, string DeterministicRecommendation, string GeminiEnhancedRecommendation, string Differences, string ApprovalStatus, string PlanId, int LatencyMs);

[Authorize]
public class DemoTourController : Controller
{
    [HttpGet("/demo-tour")]
    public IActionResult Index() => View();
}

[Authorize(Policy = Policies.CanAccessReports)]
public class EnterpriseReadinessController : Controller
{
    [HttpGet("/enterprise-readiness")]
    [HttpGet("/pilot-readiness/enhanced")]
    public IActionResult Index() => View();
}
