using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.Interfaces;
using SmartPort.Infrastructure.Services;
using SmartPort.Shared.Constants;

namespace SmartPort.Web.Controllers;

[Authorize(Policy = Policies.CanAccessGeminiAgent)]
public class OpsIngestController : Controller
{
    private static readonly List<OpsIngestAuditItem> IngestHistory = new();
    private static readonly object IngestLock = new();
    private readonly ISmartPortCopilotChatService _copilot;
    private readonly IAgentNarrativeService _narrative;

    public OpsIngestController(ISmartPortCopilotChatService copilot, IAgentNarrativeService narrative) { _copilot = copilot; _narrative = narrative; }

    [HttpGet("/ops-ingest")]
    [HttpGet("/agent/ingest")]
    public IActionResult Index()
    {
        ViewBag.Sample = "Three trucks delayed at staging, gate 4 congestion, vessel berth window moved by 45 minutes, power disruption expected between 14:00 and 15:00.";
        ViewBag.History = Snapshot();
        return View();
    }

    [HttpPost("/ops-ingest/analyze")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Analyze(string operationalNote)
    {
        var note = string.IsNullOrWhiteSpace(operationalNote) ? "Three trucks delayed at staging, gate 4 congestion, vessel berth window moved by 45 minutes, power disruption expected between 14:00 and 15:00." : operationalNote.Trim();
        var signals = ExtractSignals(note);
        var prompt = $"Classify this Smart Port operational note for a pilot-ready execution plan. Return structured fields when possible. Do not claim live integration. Note: {note}";
        var narrative = await _narrative.GenerateAsync(new AgentNarrativeRequest
        {
            Purpose = "ops ingest structured extraction",
            ReportType = "Ops Ingest Structured Extraction",
            UserPrompt = prompt,
            CurrentPage = "/ops-ingest",
            RequestedMode = AgentMode.Hybrid,
            TaskCategory = GeminiTaskCategory.Routine,
            ActionType = "ops-ingest",
            Context = new OperationalContext { TrucksInQueue = signals.StructuredSignals.Contains("truck_delay") ? 3 : 1, GateDelayActive = signals.StructuredSignals.Contains("gate_pressure"), ActiveDisruptions = signals.StructuredSignals.Contains("power") ? 2 : 1, TopRecommendations = new() { signals.PlanPreview } },
            DeterministicRecommendations = new[] { signals.PlanPreview }
        });
        var response = string.IsNullOrWhiteSpace(narrative.Narrative) ? await _copilot.GenerateResponseAsync(prompt) : new CopilotChatResponse { ShortAnswer = narrative.Narrative, GeneratedBy = string.IsNullOrWhiteSpace(narrative.SourceLabel) ? (narrative.UsedGemini ? "Gemini" : narrative.FallbackActive ? "Hybrid" : "Local Fallback") : narrative.SourceLabel };
        var item = new OpsIngestAuditItem(DateTime.UtcNow, note, signals.DisruptionType, signals.StructuredSignals, signals.PlanPreview, response.ShortAnswer, response.GeneratedBy, "Audit entry created; preview only until operator approves", "IPMS/TOS, berth schedule, gate OCR, weighbridge, fleet GPS, driver app, WhatsApp, ERP, weather/disruption feeds");
        lock (IngestLock)
        {
            IngestHistory.Add(item);
            if (IngestHistory.Count > 25) IngestHistory.RemoveRange(0, IngestHistory.Count - 25);
        }
        TempData["Success"] = "Operational note analyzed. Structured signals and pilot connector path created.";
        return Redirect("/ops-ingest");
    }

    private static IReadOnlyList<OpsIngestAuditItem> Snapshot()
    {
        lock (IngestLock)
        {
            return IngestHistory.OrderByDescending(h => h.TimestampUtc).Take(8).ToList();
        }
    }

    private static (string DisruptionType, string StructuredSignals, string PlanPreview) ExtractSignals(string note)
    {
        var lower = note.ToLowerInvariant();
        var signals = new List<string>();
        if (lower.Contains("truck")) signals.Add("truck_delay=true");
        if (lower.Contains("gate")) signals.Add("gate_pressure=high");
        if (lower.Contains("berth") || lower.Contains("vessel")) signals.Add("berth_window_changed=true");
        if (lower.Contains("power")) signals.Add("power_disruption_risk=true");
        if (lower.Contains("45")) signals.Add("berth_delay_minutes=45");
        var disruption = lower.Contains("power") ? "Power-risk and gate congestion" : lower.Contains("gate") ? "Gate congestion" : "Operational note";
        var plan = lower.Contains("gate") ? "Hold non-critical trucks, move SPQ-2026-0042 to staging, delay gate release until berth window is stable, and draft driver updates for approval." : "Create operator review task, classify queue risk, and request fleet confirmation before sends.";
        return (disruption, string.Join("; ", signals.DefaultIfEmpty("manual_review_required=true")), plan);
    }
}

public sealed record OpsIngestAuditItem(DateTime TimestampUtc, string SourceNote, string DisruptionType, string StructuredSignals, string PlanPreview, string GeminiOrFallbackReasoning, string SourceModel, string ApprovalStatus, string ConnectorRoadmap);
