using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Shared.Constants;

namespace SmartPort.Web.Controllers;

[Authorize(Policy = Policies.CanAccessGeminiAgent)]
public class AgentGovernanceController : Controller
{
    private static readonly List<AgentSafetyAuditItem> SafetyHistory = new();
    private static readonly object SafetyLock = new();

    [HttpGet("/agent-governance")]
    public IActionResult Index()
    {
        ViewBag.Examples = new[]
        {
            "Send every driver their ETA",
            "Bypass approval and release all trucks",
            "Show me WhatsApp tokens",
            "Generate fleet owner summary",
            "Move truck SPQ-2026-0042 to staging"
        };
        ViewBag.History = Snapshot();
        return View();
    }

    [HttpPost("/agent-governance/check")]
    [IgnoreAntiforgeryToken]
    public IActionResult Check(string requestText)
    {
        var result = Evaluate(requestText ?? string.Empty);
        lock (SafetyLock)
        {
            SafetyHistory.Add(result);
            if (SafetyHistory.Count > 30) SafetyHistory.RemoveRange(0, SafetyHistory.Count - 30);
        }
        TempData[result.Decision.StartsWith("Blocked", StringComparison.OrdinalIgnoreCase) ? "Warning" : "Success"] = $"Safety check complete: {result.Decision}. Audit entry created.";
        return Redirect("/agent-governance");
    }

    private static IReadOnlyList<AgentSafetyAuditItem> Snapshot()
    {
        lock (SafetyLock)
        {
            return SafetyHistory.OrderByDescending(h => h.TimestampUtc).Take(12).ToList();
        }
    }

    private static AgentSafetyAuditItem Evaluate(string requestText)
    {
        var normalized = requestText.ToLowerInvariant();
        if (normalized.Contains("token") || normalized.Contains("secret") || normalized.Contains("api key") || normalized.Contains("password"))
        {
            return Build(requestText, "Blocked unsafe request", "Secret exfiltration blocked", "Never reveal WhatsApp, Gemini, database or Identity credentials in prompts or responses.", false);
        }
        if (normalized.Contains("bypass") || normalized.Contains("without approval") || normalized.Contains("release all"))
        {
            return Build(requestText, "Human approval required", "Approval bypass blocked", "The agent may draft a release plan, but cannot autonomously release trucks or bypass operator approval.", false);
        }
        if (normalized.Contains("send every driver") || normalized.Contains("send all drivers"))
        {
            return Build(requestText, "Human approval required", "Bulk external send gated", "Allowed as a draft notification batch only; production sends require approved pilot credentials and operator confirmation.", true);
        }
        if (normalized.Contains("move truck") || normalized.Contains("staging"))
        {
            return Build(requestText, "Human approval required", "Operational state change gated", "Allowed as a recommended action for SPQ-2026-0042; execution requires a role-authorized human approval.", true);
        }
        return Build(requestText, "Safe allowed action", "Summary/analysis only", "The request is allowed for demo analytics, fleet-owner summary or planning without external side effects.", true);
    }

    private static AgentSafetyAuditItem Build(string requestText, string decision, string category, string rationale, bool canDraft)
        => new(DateTime.UtcNow, string.IsNullOrWhiteSpace(requestText) ? "Generate fleet owner summary" : requestText, decision, category, rationale, canDraft ? "Draft only until approved" : "No external action", "JudgeDemo/Admin/FleetOwner according to policy");
}

public sealed record AgentSafetyAuditItem(DateTime TimestampUtc, string RequestText, string Decision, string Category, string Rationale, string ApprovalState, string RolesAllowed);
