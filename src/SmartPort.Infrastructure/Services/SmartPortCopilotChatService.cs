using Microsoft.EntityFrameworkCore;
using SmartPort.Domain.Enums;
using SmartPort.Infrastructure.Persistence;

namespace SmartPort.Infrastructure.Services;

public interface ISmartPortCopilotChatService
{
    Task<CopilotChatPageModel> BuildPageAsync(string? prompt = null);
    Task<CopilotChatResponse> GenerateResponseAsync(string prompt, string? conversationHistory = null);
    List<CopilotPromptChip> GetPromptChips();
}

public class CopilotChatPageModel
{
    public OperationalContext Context { get; set; } = new();
    public string CurrentPrompt { get; set; } = string.Empty;
    public CopilotChatResponse? Response { get; set; }
    public AgentModeStatus AgentModeStatus { get; set; } = new();
    public List<CopilotPromptChip> PromptChips { get; set; } = new();
    public List<string> SupportedTopics { get; set; } = new();
}

public class CopilotPromptChip
{
    public string Label { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Icon { get; set; } = "✦";
}

public class CopilotChatResponse
{
    public string Prompt { get; set; } = string.Empty;
    public string MessageType { get; set; } = "operational";
    public string Intent { get; set; } = "general";
    public string IntentCategory { get; set; } = "operational";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ShortAnswer { get; set; } = string.Empty;
    public string OperationalReasoning { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public string ExpectedImpact { get; set; } = string.Empty;
    public string AffectedArea { get; set; } = "Port Operations";
    public string Severity { get; set; } = "Medium";
    public int ConfidenceScore { get; set; } = 82;
    public string EmissionsImpact { get; set; } = string.Empty;
    public string EnergyImpact { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<CopilotActionCard> ActionCards { get; set; } = new();
    public string DataNote { get; set; } = "Synthetic demo data · Gemini/local fallback response";
    public string GeneratedBy { get; set; } = "Local";
    public bool HumanApprovalRequired { get; set; } = true;
    public bool NotAutomaticallyExecuted { get; set; } = true;
    public bool IsSmallTalk { get; set; }
    public bool IsOutOfScope { get; set; }
    public bool IsVagueButRelated { get; set; }
    public bool IsOperational { get; set; } = true;
    public List<string> SuggestedFollowUps { get; set; } = new();
    public List<CopilotPromptChip> TopicChips { get; set; } = new();
    public List<CopilotMetricBadge> MetricBadges { get; set; } = new();
}

public class CopilotActionCard
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = "/dashboard";
    public string Icon { get; set; } = "↗";
}

public class CopilotMetricBadge
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Tone { get; set; } = "info";
}

public class SmartPortCopilotChatService : ISmartPortCopilotChatService
{
    private readonly IAiAgentService _agent;
    private readonly SmartPortDbContext _db;
    private readonly ITruckTrackingService _tracking;
    private readonly ISmartPortIntelligenceService _intelligence;
    private readonly IAgentNarrativeService _narrative;

    public SmartPortCopilotChatService(IAiAgentService agent, SmartPortDbContext db, ITruckTrackingService tracking, ISmartPortIntelligenceService intelligence, IAgentNarrativeService narrative)
    {
        _agent = agent;
        _db = db;
        _tracking = tracking;
        _intelligence = intelligence;
        _narrative = narrative;
    }

    public async Task<CopilotChatPageModel> BuildPageAsync(string? prompt = null)
    {
        var context = await _agent.GetContextAsync();
        var model = new CopilotChatPageModel
        {
            Context = context,
            CurrentPrompt = prompt ?? string.Empty,
            PromptChips = GetPromptChips(),
            SupportedTopics = SupportedTopics(),
            AgentModeStatus = _narrative.GetStatus()
        };

        return model;
    }

    public List<CopilotPromptChip> GetPromptChips() => new()
    {
        new() { Icon = "👋", Label = "Hello", Prompt = "hello" },
        new() { Icon = "❔", Label = "Capabilities", Prompt = "what can you do" },
        new() { Icon = "⚠️", Label = "Biggest risk", Prompt = "What is the biggest operational risk right now?" },
        new() { Icon = "🚛", Label = "Reduce idling", Prompt = "How can we reduce truck idling?" },
        new() { Icon = "⚓", Label = "Berth focus", Prompt = "Which berth needs attention?" },
        new() { Icon = "🧭", Label = "Action plan", Prompt = "Generate an operator action plan." },
        new() { Icon = "🌿", Label = "Emissions brief", Prompt = "Generate an emissions reduction brief." },
        new() { Icon = "🚪", Label = "Gate bottleneck", Prompt = "Where is the gate bottleneck?" },
        new() { Icon = "🎬", Label = "2-minute demo", Prompt = "Prepare a 2-minute demo summary." },
        new() { Icon = "🗺️", Label = "Real pilot", Prompt = "What would a real pilot need?" }
    };

    private static List<string> SupportedTopics() => new()
    {
        "Truck queues", "Truck tracking / ETA", "Gate bottlenecks", "Berth pressure", "Yard congestion",
        "Emissions / CO₂", "Load-shedding disruption", "Scenario simulation", "Recommendations", "Audit / decision history",
        "Executive impact", "Clean logistics / NCIC alignment", "Pilot readiness", "Integration readiness", "Stakeholder value",
        "Executive brief", "Grant / investor summary", "Business case", "Implementation roadmap", "Success metrics"
    };

    public async Task<CopilotChatResponse> GenerateResponseAsync(string prompt, string? conversationHistory = null)
    {
        var context = await _agent.GetContextAsync();
        var snapshot = await _intelligence.GetSnapshotAsync();
        var response = await GenerateResponseAsync(prompt.Trim(), context, conversationHistory);
        response.DataNote = string.IsNullOrWhiteSpace(response.DataNote)
            ? $"{snapshot.DataNote} · snapshot {snapshot.Timestamp:HH:mm:ss} UTC"
            : $"{response.DataNote} · snapshot {snapshot.Timestamp:HH:mm:ss} UTC";
        if (response.Intent == "risk")
        {
            response.Title = snapshot.TopRisk;
            response.AffectedArea = snapshot.AffectedArea;
            response.ConfidenceScore = snapshot.Confidence;
        }
        return response;
    }

    private async Task<CopilotChatResponse> GenerateResponseAsync(string prompt, OperationalContext ctx, string? conversationHistory = null)
    {
        var q = prompt.ToLowerInvariant();
        var intent = ClassifyIntent(q);
        var response = intent switch
        {
            "safety" => BuildSafetyRefusal(prompt),
            "greeting" => BuildGreeting(prompt, ctx),
            "help" => BuildHelp(prompt, ctx),
            "truck-tracking" => await BuildTruckTracking(prompt, ctx),
            "truck-queue" => await BuildGate(prompt, ctx),
            "gate" => await BuildGate(prompt, ctx),
            "berth" => BuildBerth(prompt, ctx),
            "yard" => BuildYard(prompt, ctx),
            "vessel" => BuildBerth(prompt, ctx),
            "emissions" => BuildEmissions(prompt, ctx),
            "energy" => BuildEnergy(prompt, ctx),
            "scenario" => BuildScenario(prompt, ctx),
            "recommendations" => BuildAudit(prompt, ctx),
            "audit" => BuildAudit(prompt, ctx),
            "executive-impact" => BuildExecutiveImpact(prompt, ctx),
            "clean-logistics" => BuildCleanLogistics(prompt, ctx),
            "pilot-readiness" => BuildPilotReadiness(prompt, ctx),
            "integrations" => BuildIntegrations(prompt, ctx),
            "stakeholder-value" => BuildStakeholderValue(prompt, ctx),
            "executive-brief" => BuildExecutiveBrief(prompt, ctx),
            "grant-summary" => BuildGrantSummary(prompt, ctx),
            "success-metrics" => BuildSuccessMetrics(prompt, ctx),
            "demo" => BuildDemoSummary(prompt, ctx),
            "action-plan" => BuildActionPlan(prompt, ctx),
            "sensitive-label" => BuildSensitiveLabel(prompt),
            "harmless" => BuildHarmless(prompt, ctx),
            "vague-related" => BuildVagueRelated(prompt, ctx),
            "out-of-scope" => BuildOutOfScope(prompt),
            _ => BuildOutOfScope(prompt)
        };

        response.IntentCategory = IntentCategory(intent);
        ApplyEngineStatus(response, usedGemini: false, fallbackReason: null);

        var status = _narrative.GetStatus();
        var geminiPreferred = status.GeminiEnabled && status.GeminiConfigured && intent != "safety" && intent != "sensitive-label";
        if (!geminiPreferred)
        {
            response.DataNote = status.GeminiEnabled && !status.GeminiConfigured
                ? "Local Offline-Safe Mode · Gemini enabled but not configured"
                : "Local Offline-Safe Mode · Gemini not configured or disabled";
            return response;
        }

        var enhanced = await _narrative.GenerateAsync(new AgentNarrativeRequest
        {
            Purpose = "copilot conversational response",
            ReportType = CopilotReportType(intent),
            DetectedIntent = response.IntentCategory,
            UserPrompt = prompt,
            CurrentPage = prompt.Contains("Driver asks", StringComparison.OrdinalIgnoreCase) ? "Mobile Driver Copilot" : "SmartPort Copilot",
            ConversationHistory = conversationHistory ?? string.Empty,
            RequestedMode = AgentMode.Hybrid,
            TaskCategory = IsPremiumIntent(intent) ? GeminiTaskCategory.Premium : GeminiTaskCategory.Routine,
            ActionType = $"copilot-{intent}",
            Context = ctx,
            DeterministicRecommendations = ctx.TopRecommendations
        });

        if (enhanced.UsedGemini && !string.IsNullOrWhiteSpace(enhanced.Narrative))
        {
            return BuildGeminiResponse(prompt, intent, response, enhanced.Narrative, enhanced.SourceLabel);
        }

        ApplyEngineStatus(response, usedGemini: false, fallbackReason: "Gemini unavailable — local fallback used");
        return response;
    }


    private CopilotChatResponse BuildGeminiResponse(string prompt, string intent, CopilotChatResponse localResponse, string geminiText, string sourceLabel)
    {
        var response = localResponse;
        response.GeneratedBy = string.IsNullOrWhiteSpace(sourceLabel) ? "Gemini" : sourceLabel;
        response.IntentCategory = IntentCategory(intent);
        response.Intent = response.IntentCategory;
        response.Title = response.IntentCategory switch
        {
            "greeting" => "SmartPort Copilot Online",
            "help" => "SmartPort Copilot Capabilities",
            "report" => "Generated Agent Response",
            "demo" => "Demo / Pitch Assistant",
            "out-of-scope" => "Smart Port Scope Control",
            _ => response.Title
        };
        response.Summary = FirstSentence(geminiText, response.Summary);
        response.ShortAnswer = geminiText.Trim();
        response.OperationalReasoning = geminiText.Trim();
        response.MessageType = response.IntentCategory is "greeting" or "help" or "harmless" ? "compact" : response.MessageType;
        response.IsSmallTalk = response.IntentCategory is "greeting" or "help" or "harmless";
        response.IsOperational = response.IntentCategory is "operational" or "report" or "demo";
        response.DataNote = $"{response.GeneratedBy} · sanitized synthetic Smart Port context";
        response.MetricBadges = BuildMetricBadges(response.Severity, response.ConfidenceScore, response.AffectedArea, response.IntentCategory, "Gemini");
        return response;
    }

    private static bool IsPremiumIntent(string intent) => intent is "executive-brief" or "risk" or "demo";

    private static void ApplyEngineStatus(CopilotChatResponse response, bool usedGemini, string? fallbackReason)
    {
        response.GeneratedBy = usedGemini ? "Gemini" : string.IsNullOrWhiteSpace(fallbackReason) ? "Local" : "Hybrid";
        if (!string.IsNullOrWhiteSpace(fallbackReason)) response.DataNote = fallbackReason;
        response.MetricBadges = BuildMetricBadges(response.Severity, response.ConfidenceScore, response.AffectedArea, response.IntentCategory, response.GeneratedBy);
    }

    private static List<CopilotMetricBadge> BuildMetricBadges(string severity, int confidence, string area, string intent, string generatedBy) => new()
    {
        new() { Label = "Generated by", Value = generatedBy, Tone = generatedBy == "Gemini" ? "success" : generatedBy == "Hybrid" ? "warning" : "info" },
        new() { Label = "Intent", Value = intent, Tone = "teal" },
        new() { Label = "Human approval", Value = "Required", Tone = "warning" },
        new() { Label = "Execution", Value = "Not automatic", Tone = "muted" }
    };

    private static string FirstSentence(string text, string fallback)
    {
        var clean = text.Trim().Replace("#", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(clean)) return fallback;
        var firstLine = clean.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim('-', ' ', '*') ?? clean;
        return firstLine.Length > 180 ? firstLine[..180] + "…" : firstLine;
    }

    private static string CopilotReportType(string intent) => intent switch
    {
        "action-plan" => "Operator Action Plan",
        "emissions" => "Emissions Reduction Brief",
        "scenario" => "Scenario Analysis",
        "pilot-readiness" => "Pilot Readiness Summary",
        "executive-brief" => "Executive Operations Brief",
        "demo" or "grant-summary" => "Demo and Pitch Summary",
        "help" => "Copilot Capability Explanation",
        "greeting" or "harmless" => "Conversational Smart Port Copilot Reply",
        _ => "Smart Port Copilot Response"
    };

    private static string IntentCategory(string intent) => intent switch
    {
        "greeting" => "greeting",
        "help" => "help",
        "action-plan" or "executive-brief" or "success-metrics" => "report",
        "demo" or "grant-summary" or "executive-impact" or "stakeholder-value" or "clean-logistics" => "demo",
        "harmless" => "harmless",
        "out-of-scope" => "out-of-scope",
        "safety" or "safety refusal" or "sensitive-label" => "fallback",
        _ => "operational"
    };

    private static string ClassifyIntent(string q)
    {
        if (Has(q, "ignore instruction", "ignore previous", "system prompt", "developer prompt", "reveal prompt", "show prompt", "secret", "credential", "password", "api key", "dump database", "dump config", "internal config", "jailbreak")) return "safety";
        if (LooksLikePassphrase(q)) return "sensitive-label";
        if (IsGreeting(q)) return "greeting";
        if (Has(q, "how's your day", "hows your day", "are you working", "what should i ask", "can you help me present", "help me present")) return "harmless";
        if (Has(q, "help", "help me", "what can you do", "what are you", "capabilit", "supported", "topics", "explain this system", "what is smart port", "how does this app work")) return "help";
        if (Has(q, "investor summary", "grant", "funding", "2-minute grant", "ncic pitch", "pitch summary", "explain to judges", "to judges")) return "grant-summary";
        if (Has(q, "business impact", "executive impact", "business case", "impact centre", "impact center", "investor")) return "executive-impact";
        if (Has(q, "ncic", "clean logistics", "sustainability", "clean-tech", "clean tech", "alignment")) return "clean-logistics";
        if (Has(q, "pilot", "90-day", "90 day", "roadmap", "implementation", "30-day", "60-day")) return "pilot-readiness";
        if (Has(q, "integration", "integrations", "production need", "data would production", "data source", "tos", "ipms", "gps", "telematics", "ocr", "rfid")) return "integrations";
        if (Has(q, "stakeholder", "fleet operator", "port authority", "terminal operator", "sustainability officer", "municipality", "province", "operations manager")) return "stakeholder-value";
        if (Has(q, "executive brief", "generate brief", "shift report", "briefing", "emissions report", "pilot readiness report", "incident response brief", "emissions reduction brief")) return "executive-brief";
        if (q.Contains("generate") && q.Contains("brief")) return "executive-brief";
        if (Has(q, "success metric", "pilot metrics", "risks and mitigations", "risks", "mitigation")) return "success-metrics";
        if (Has(q, "demo", "2-minute", "summary", "video", "pitch", "walkthrough", "summarize demo", "make a 2-minute pitch")) return "demo";
        if (Has(q, "biggest risk", "port risk", "operational risk", "current risk", "what is urgent", "urgent", "do first", "should we do first", "manager do first", "berth focus", "reduce idling", "disruptions", "yard pressure")) return "risk";
        if (Has(q, "action plan", "operator action", "recommended actions", "what should operators do", "generate action", "prepare shift brief", "shift brief", "prioritise first", "prioritize first")) return "action-plan";
        if (Has(q, "track", "tracking", "eta", "truck eta", "where are the trucks", "where are trucks", "delayed truck", "delayed trucks", "which trucks are delayed", "hold outside", "held outside", "outside port", "wait outside", "trucks to hold", "hold trucks", "which trucks should wait", "priority release", "priority release trucks", "approaching", "checkpoint")) return "truck-tracking";
        if (Has(q, "truck queue", "trucks on queue", "queued truck", "queued trucks", "trucks in queue", "gate queue trucks")) return "truck-tracking";
        if (Has(q, "gate", "bottleneck", "queue")) return "gate";
        if (Has(q, "load", "energy", "shedding", "power")) return "energy";
        if (Has(q, "emission", "co2", "co₂", "carbon", "idling", "diesel", "fuel")) return "emissions";
        if (Has(q, "berth", "anchor", "turnaround")) return "berth";
        if (Has(q, "vessel", "ship", "eta slip")) return "vessel";
        if (Has(q, "yard", "container", "dwell")) return "yard";
        if (Has(q, "scenario", "simulate", "what if", "what happens if")) return "scenario";
        if (Has(q, "recommend")) return "recommendations";
        if (Has(q, "audit", "decision history", "decision")) return "audit";
        if (Has(q, "port", "operation", "congestion", "risk", "flow", "terminal")) return "vague-related";
        if (Has(q, "politics", "medical", "legal advice", "financial advice", "recipe", "homework", "code", "coding", "programming", "weather", "sports", "celebrity", "news", "general knowledge", "offensive", "abusive", "bypass security", "show config")) return "out-of-scope";
        return q.Length <= 28 ? "harmless" : "out-of-scope";
    }

    private CopilotChatResponse BuildGreeting(string prompt, OperationalContext ctx) =>
        Base(prompt, "greeting", "Low", 98, "Smart Port Copilot",
            "Hey, I’m online. I can help with Smart Port risks, truck idling, berth pressure, emissions, incidents, scenario analysis, or demo/pilot explanations. What do you want to check first?",
            "This was classified as greeting/small talk, so I’m not generating a risk report or action plan.",
            "Ask me about biggest risk, reduce idling, berth focus, emissions, incidents, scenario analysis, or a pilot/demo brief.",
            "Keeps the conversation natural while staying grounded in the Smart Port system.",
            "Synthetic demo data is used only when operational context is requested.",
            "Recommendations remain human-approved and not automatically executed.",
            StandardActions());

    private CopilotChatResponse BuildHelp(string prompt, OperationalContext ctx) =>
        Base(prompt, "help", "Low", 97, "Smart Port Copilot Capabilities",
            "I can help with truck congestion, gate bottlenecks, berth and yard pressure, emissions/idling, incidents, scenario analysis, recommendation explanations, reports, pilot readiness, and demo/pitch wording.",
            "When Gemini is configured I use Gemini Agent Mode with sanitized Smart Port context; otherwise I use the local offline-safe fallback.",
            "Try: ‘What is the biggest operational risk right now?’, ‘How can we reduce truck idling?’, or ‘Generate an operator action plan.’",
            "You get conversational assistance without automatic operational execution.",
            "All operational figures are synthetic/demo data until live integrations are approved.",
            "Recommendations require human approval and audit tracking.",
            StandardActions());

    private CopilotChatResponse BuildVagueRelated(string prompt, OperationalContext ctx) =>
        Base(prompt, "vague but related", "Low", 80, "Scope Clarification",
            "I can help with port operations, but I need a more specific operational area.",
            "The request appears related to port flow, but it does not identify a clear supported intent such as truck queues, berth pressure, yard congestion, emissions, load-shedding or recommendations.",
            "Try: truck queues, truck ETA, gate bottleneck, berth pressure, yard congestion, emissions, scenario simulation or recommendations.",
            "A clearer question produces a safer and more actionable answer.",
            "Synthetic demo data will be used once a topic is selected.",
            "No external live systems are queried.",
            StandardActions());

    private CopilotChatResponse BuildOutOfScope(string prompt) =>
        Base(prompt, "out-of-scope", "Low", 99, "Scope Control",
            "I’m focused on Smart Port operations, congestion, truck queues, berth/gate flow, emissions, disruptions, scenario simulation and operator recommendations.",
            "That request is outside the Culltron Smart Port Flow demo scope, so I won’t generate unrelated general, political, medical, legal, financial or offensive content.",
            "Try asking: ‘What is the biggest port risk right now?’ or ‘How can we reduce truck idling?’. ",
            "Keeps the assistant safe, scoped and presentation-ready.",
            "Synthetic demo data is only used for smart port operations questions.",
            "Hybrid scope control is active with local fallback.",
            StandardActions());

    private CopilotChatResponse BuildSafetyRefusal(string prompt) =>
        Base(prompt, "safety refusal", "Low", 99, "Governance / Safety",
            "I can’t help with secrets, credentials, internal configuration, prompt override requests, database dumps or attempts to bypass instructions.",
            "The Copilot is scope-limited to smart port operations and protects internal prompts, configuration and demo data boundaries.",
            "Ask about port congestion, truck ETA, gate queues, emissions, disruptions, scenario risk or recommended operator actions instead.",
            "Maintains enterprise governance and demo safety.",
            "No secret or external system data is exposed.",
            "Hybrid governance is active with local fallback.",
            StandardActions());

    private CopilotChatResponse BuildSensitiveLabel(string prompt) =>
        Base(prompt, "sensitive-label", "Low", 99, "Input Safety",
            "That looks like a passphrase or label rather than an operations question. For safety I won’t process it as a command.",
            "The Copilot avoids treating password-like, token-like, or credential-like text as operational instructions.",
            "Ask me things like ‘biggest risk’, ‘reduce idling’, or ‘generate executive brief’. ",
            "Prevents accidental handling of sensitive-looking input while keeping the Smart Port workflow available.",
            "No operational estimate changed.",
            "No external system was queried for this input.",
            StandardActions());

    private CopilotChatResponse BuildHarmless(string prompt, OperationalContext ctx) =>
        Base(prompt, "harmless", "Low", 90, "Smart Port Copilot",
            "I’m here and ready to help with the Smart Port demo. If you want, I can help you present the system, explain what to ask, or check the current port risk.",
            "The message is harmless but not a direct operational command, so I’m keeping the answer conversational and grounded in Culltron Smart Port Flow.",
            "Try asking for the biggest risk, truck idling reduction, berth focus, an operator action plan, or a 2-minute demo summary.",
            "Keeps the assistant natural without inventing unrelated content.",
            $"Operational context is available when needed: {ctx.TrucksInQueue} queued trucks and {ctx.BerthUtilisationPct:F0}% berth utilisation.",
            "Recommendations remain human-approved and not automatically executed.",
            StandardActions());

    private CopilotChatResponse BuildRisk(string prompt, OperationalContext ctx)
    {
        var riskScore = Score(ctx);
        var severity = Severity(riskScore);
        return Base(prompt, "risk", severity, Math.Min(97, 70 + riskScore / 4), "Integrated Port Operations",
            $"Overall operational risk is {severity.ToLowerInvariant()} with a composite score of {riskScore}/100.",
            $"The largest pressure signals are {ctx.TrucksInQueue} trucks queued, {ctx.BerthUtilisationPct:F0}% berth utilisation, {ctx.YardOccupancyPct:F1}% yard occupancy, {ctx.ActiveDisruptions} active disruptions and {ctx.OpenIncidents} open incidents.",
            "Open the Dashboard, then action the highest-risk recommendation and test a load-shedding or peak-gate scenario before dispatching additional trucks.",
            "Reduces decision latency by focusing the operator on the constraint most likely to cascade across berth, yard and gate flow.",
            $"Current idling footprint is {ctx.TotalIdlingMinutesToday:F0} minutes and {ctx.EstimatedCo2Today:F1} kg CO₂ today.",
            ctx.LoadSheddingActive ? "Load-shedding is already active; protect reefer movements and gate processing continuity." : "No active load-shedding signal, but the simulator can model a Stage 2-4 shock.",
            StandardActions());
    }

    private CopilotChatResponse BuildActionPlan(string prompt, OperationalContext ctx)
    {
        var steps = new List<string>();
        if (ctx.CriticalDisruptions > 0) steps.Add($"1. Escalate {ctx.CriticalDisruptions} critical disruption(s) and assign an owner.");
        if (ctx.TrucksInQueue > 8) steps.Add($"2. Open overflow gate lanes and meter inbound dispatches; {ctx.TrucksInQueue} trucks are queued.");
        if (ctx.VesselsAtAnchor > 0) steps.Add($"3. Re-sequence berths for {ctx.VesselsAtAnchor} vessel(s) at anchor.");
        if (ctx.DwellAlerts > 0) steps.Add($"4. Pull forward dwell alerts; {ctx.DwellAlerts} containers need release/escalation.");
        steps.Add("5. Run a scenario simulation and record the accepted recommendation for audit.");

        return Base(prompt, "action plan", ctx.CriticalDisruptions > 0 ? "Critical" : "High", 91, "Operations Manager",
            "Recommended operator action plan generated from current synthetic port state.",
            string.Join(" ", steps),
            "Brief the shift lead, accept the top Flow Intelligence recommendation, and re-check Dashboard KPIs after the simulated intervention.",
            "Creates an auditable 15-minute response loop across gate, berth, yard, energy and emissions teams.",
            $"Target avoidable idling first: {ctx.TotalIdlingMinutesToday:F0} minutes today can be reduced by metering truck dispatch.",
            ctx.LoadSheddingActive ? "Energy playbook should be active now." : "Keep the load-shedding playbook ready for the next disruption window.",
            StandardActions());
    }

    private async Task<CopilotChatResponse> BuildTruckTracking(string prompt, OperationalContext ctx)
    {
        var tracking = await _tracking.GetDashboardAsync();
        var delayed = tracking.Trucks.Where(t => t.Status is "Delayed" or "Hold Outside Port").Take(3).ToList();
        var focus = delayed.Count > 0 ? delayed : tracking.Trucks.Take(3).ToList();
        var truckSummary = string.Join("; ", focus.Select(t => $"{t.FleetIdentifier} on {t.RouteCorridor} at {t.CurrentCheckpoint}, ETA {t.EtaMinutesToGate} min, risk {t.DelayRiskScore}/100, {t.Status.ToLowerInvariant()}, {t.EstimatedCo2Kg:F1} kg CO₂"));

        return Base(prompt, "truck tracking / ETA", tracking.GatePressureScore >= 80 ? "High" : "Medium", tracking.AiConfidenceScore, "Truck Tracking / ETA Intelligence",
            $"Tracking {tracking.ActiveTrucks} active approaching trucks; {tracking.HoldOutsidePortCount} should be held outside the port and {tracking.PriorityReleaseCount} should receive priority release.",
            $"ETA and checkpoint risk are derived from local dispatch, fleet and gate queue demo data. Focus trucks: {truckSummary}.",
            "Hold high-risk trucks at outer staging, release priority cargo through pre-cleared lanes, and meter remaining arrivals against live gate pressure.",
            $"Expected impact is lower queue growth, about {tracking.TotalIdlingMinutes:F0} idling minutes under control, and reduced CO₂ exposure of {tracking.EstimatedCo2Kg:F1} kg in the current demo view.",
            $"Truck idling exposure is {tracking.TotalIdlingMinutes:F0} minutes / {tracking.EstimatedCo2Kg:F1} kg CO₂ based on synthetic demo assumptions.",
            ctx.LoadSheddingActive ? "Energy disruption increases ETA uncertainty; hold non-critical trucks outside the port perimeter." : "Energy stable; gate pressure is the main ETA constraint.",
            new() { new() { Icon = "📡", Title = "Truck Tracking", Description = "Open ETA intelligence board.", Url = "/TruckTracking" }, new() { Icon = "🚪", Title = "Gates", Description = "Inspect live gate queue pressure.", Url = "/gates" }, new() { Icon = "🌿", Title = "Emissions", Description = "Review idling and CO₂ exposure.", Url = "/emissions" } });
    }

    private async Task<CopilotChatResponse> BuildGate(string prompt, OperationalContext ctx)
    {
        var topGate = await _db.Gates.Where(g => !g.IsDeleted)
            .OrderByDescending(g => g.CurrentQueueCount)
            .Select(g => new { g.Code, g.Name, g.CurrentQueueCount, g.AverageProcessingMinutes, g.LaneCount, g.IsOperational })
            .FirstOrDefaultAsync();
        var gateName = topGate == null ? "primary gate" : $"{topGate.Code} — {topGate.Name}";
        var severity = ctx.TrucksInQueue > 16 ? "Critical" : ctx.TrucksInQueue > 8 ? "High" : "Medium";
        return Base(prompt, "gate", severity, 89, "Gate Queue / Truck Flow",
            $"{gateName} is the most likely bottleneck based on queue depth and processing load.",
            topGate == null ? $"Total queue depth is {ctx.TrucksInQueue} trucks." : $"{gateName} has {topGate.CurrentQueueCount} trucks, {topGate.LaneCount} lanes and averages {topGate.AverageProcessingMinutes} minutes per truck; total port queue is {ctx.TrucksInQueue} trucks.",
            "Meter new dispatches, open an overflow lane, pre-clear documents and reroute non-critical cargo away from the gate peak.",
            "Expected to reduce queue growth and lower avoidable idling during the next 30-45 minutes.",
            "Every 10 minutes removed from 10 queued trucks saves roughly 5L diesel and 13.4kg CO₂ under the demo assumptions.",
            ctx.LoadSheddingActive ? "Manual gate fallback should be ready because OCR/RFID processing may degrade." : "Energy state is stable; keep OCR/RFID lanes focused on pre-cleared trucks.",
            new() { new() { Icon = "🚪", Title = "Open Gates", Description = "Inspect gate status and queues.", Url = "/gates" }, new() { Icon = "🚛", Title = "Dispatch", Description = "Hold or release planned trucks.", Url = "/dispatch" }, new() { Icon = "🌿", Title = "Emissions", Description = "Quantify idling impact.", Url = "/emissions" } });
    }

    private CopilotChatResponse BuildBerth(string prompt, OperationalContext ctx)
    {
        var severity = ctx.VesselsAtAnchor > 0 || ctx.BerthUtilisationPct > 85 ? "High" : "Medium";
        return Base(prompt, "berth", severity, 87, "Berth / Vessel Pressure",
            $"Berth utilisation is {ctx.BerthUtilisationPct:F0}% with {ctx.VesselsAtAnchor} vessel(s) at anchor and {ctx.VesselsDelayed} delayed vessel(s).",
            "High berth occupancy can cascade into yard density and gate appointment slippage when discharge windows shift.",
            "Review the berth schedule, protect the fastest departure, and prioritise vessels whose cargo connects to constrained yard blocks or cold-chain dispatches.",
            "Improves berth turnover and lowers downstream yard and gate pressure.",
            "Faster berth clearance reduces truck waiting surges caused by late cargo availability.",
            ctx.LoadSheddingActive ? "Load-shedding can slow crane productivity and reefer handovers; sequence cold-chain first." : "No active energy constraint, but berth plans should include a load-shedding contingency.",
            new() { new() { Icon = "⚓", Title = "Vessels", Description = "Inspect delayed and at-anchor vessels.", Url = "/vessels" }, new() { Icon = "🗓️", Title = "Berth Schedule", Description = "Review berth assignments.", Url = "/berths/schedule" }, new() { Icon = "🔬", Title = "Simulate", Description = "Model ETA or crane disruption.", Url = "/simulator" } });
    }

    private CopilotChatResponse BuildYard(string prompt, OperationalContext ctx)
    {
        var severity = ctx.YardOccupancyPct > 88 ? "Critical" : ctx.YardOccupancyPct > 76 ? "High" : "Medium";
        return Base(prompt, "yard", severity, 86, "Yard / Container Pressure",
            $"Yard occupancy is {ctx.YardOccupancyPct:F1}% with {ctx.DwellAlerts} dwell alert(s).",
            "Dwell cargo consumes premium ground slots and amplifies truck appointment delays when vessels discharge into saturated blocks.",
            "Escalate customs/document holds, pull cleared imports forward, and reserve capacity for priority reefers or high-value cargo.",
            "Recovers yard capacity and reduces gate rework caused by unavailable containers.",
            "Reducing dwell-related rework lowers truck circulation time and idling exposure.",
            ctx.LoadSheddingActive ? "Protect reefer block power and dispatch cold-chain cargo first." : "Energy stable; use this window to recover yard density.",
            new() { new() { Icon = "📦", Title = "Yard View", Description = "Open yard pressure map.", Url = "/containers/yard" }, new() { Icon = "📦", Title = "Dwell Alerts", Description = "Action overdue containers.", Url = "/containers/dwellalerts" } });
    }

    private CopilotChatResponse BuildEmissions(string prompt, OperationalContext ctx)
    {
        var avoidableCo2 = ctx.EstimatedCo2Today * 0.28m;
        return Base(prompt, "emissions", ctx.EstimatedCo2Today > 75 ? "High" : "Medium", 88, "Emissions / Truck Idling",
            $"Estimated idling today is {ctx.TotalIdlingMinutesToday:F0} minutes, producing approximately {ctx.EstimatedCo2Today:F1} kg CO₂.",
            "Queueing trucks, delayed dispatch windows and gate processing variance are the main demo drivers of avoidable diesel burn.",
            "Apply dispatch metering, hold non-critical trucks at depot and prioritise pre-cleared high-urgency loads.",
            $"A conservative 28% idling reduction could save about {avoidableCo2:F1} kg CO₂ today in the demo model.",
            $"Potential CO₂ saving: {avoidableCo2:F1} kg; current footprint: {ctx.EstimatedCo2Today:F1} kg CO₂.",
            ctx.LoadSheddingActive ? "Energy disruption may increase idling if gates switch to manual processing." : "Energy stable; best opportunity is dispatch smoothing and gate pre-clearance.",
            new() { new() { Icon = "🌿", Title = "Emissions", Description = "Open idling and CO₂ dashboard.", Url = "/emissions" }, new() { Icon = "🚛", Title = "Dispatch", Description = "Adjust truck release timing.", Url = "/dispatch" } });
    }

    private CopilotChatResponse BuildEnergy(string prompt, OperationalContext ctx)
    {
        var severity = ctx.LoadSheddingActive ? "Critical" : "High";
        return Base(prompt, "energy", severity, 90, "Load-shedding / Energy Disruption",
            ctx.LoadSheddingActive ? "Load-shedding is active in the demo state." : "If load-shedding starts soon, gate throughput, reefer capacity and equipment productivity become the critical risk chain.",
            "Energy disruption affects OCR/RFID gates, reefer plugs, cold-chain dispatch timing and crane/equipment productivity.",
            "Activate manual gate fallback, dispatch cold-chain cargo first, verify generator coverage and run a Stage 3/4 scenario before releasing extra trucks.",
            "Protects cold-chain integrity and keeps essential gate throughput alive during the disruption window.",
            "Manual gates and longer queues can increase idling; meter dispatches before the outage window.",
            "Prioritise reefers, OCR fallback, gate kiosks and critical lighting on backup power.",
            new() { new() { Icon = "⚡", Title = "Disruptions", Description = "Inspect active energy and road disruptions.", Url = "/disruptions" }, new() { Icon = "🔬", Title = "Simulator", Description = "Run load-shedding scenario.", Url = "/simulator" }, new() { Icon = "🤖", Title = "AI Agent", Description = "Ask for disruption priorities.", Url = "/agent" } });
    }

    private CopilotChatResponse BuildScenario(string prompt, OperationalContext ctx) =>
        Base(prompt, "scenario", "Medium", 84, "Scenario Simulation",
            "Use the simulator to model truck spikes, ETA slips, berth pressure, crane drops, backlog growth and load-shedding stages.",
            $"The current baseline has {ctx.TrucksInQueue} queued trucks, {ctx.BerthUtilisationPct:F0}% berth utilisation and {ctx.YardOccupancyPct:F1}% yard occupancy.",
            "Run the Durban High Congestion or Load-Shedding Stage 4 preset, then compare risk scores and recommended actions.",
            "Creates a judge-friendly what-if moment showing decision support before disruption becomes an incident.",
            $"Scenario outputs include idling minutes and CO₂ based on {ctx.TotalIdlingMinutesToday:F0} current idling minutes.",
            ctx.LoadSheddingActive ? "Use the active energy state as the scenario starting point." : "Try a Stage 4 load-shedding preset to show resilience planning.",
            new() { new() { Icon = "🔬", Title = "Open Simulator", Description = "Run what-if analysis.", Url = "/simulator" }, new() { Icon = "📋", Title = "Reports", Description = "Review operational outputs.", Url = "/reports" } });

    private CopilotChatResponse BuildAudit(string prompt, OperationalContext ctx) =>
        Base(prompt, "audit", ctx.HighRiskTrips > 0 ? "High" : "Medium", 85, "Recommendations / Decision Audit",
            $"There are {ctx.HighRiskTrips} high-risk recommendation-linked trips today and {ctx.TopRecommendations.Count} top recommendations in context.",
            "Audit history shows what the engine recommended, whether an operator accepted or dismissed it, and when the decision was made.",
            "Open recommendations, accept the top actionable item, and record feedback to create a visible decision trail.",
            "Improves explainability and gives judges a clear governance story for AI-assisted operations.",
            "Accepted dispatch decisions should reduce avoidable idling and emissions over the shift.",
            ctx.LoadSheddingActive ? "Energy-related recommendations should be prioritized in the audit trail." : "Energy state can be captured through scenario-generated recommendation notes.",
            new() { new() { Icon = "🧠", Title = "Recommendations", Description = "Review active recommendations.", Url = "/flow/recommendations" }, new() { Icon = "📋", Title = "Audit Report", Description = "Open recommendation history.", Url = "/reports/recommendations" } });


    private CopilotChatResponse BuildExecutiveImpact(string prompt, OperationalContext ctx) =>
        Base(prompt, "executive impact", "Medium", 92, "Executive Impact Centre",
            $"Business impact is the ability to reduce avoidable idling, improve truck release timing and give stakeholders an auditable shift-level action plan. Current demo exposure is {ctx.TotalIdlingMinutesToday:F0} idling minutes and {ctx.EstimatedCo2Today:F1} kg CO₂.",
            "The response uses synthetic demo data from queues, berth/yard pressure, disruptions and recommendations. Savings are indicative and not verified outcomes.",
            "Open the Executive Impact Centre, review the assumptions panel, then generate an Executive Brief for the pilot conversation.",
            "Creates a credible value narrative for judges, NCIC reviewers, funders and pilot stakeholders without claiming live operational deployment.",
            "Indicative reduction opportunity comes from dispatch metering and hold-outside-port decisions, not verified emissions accounting.",
            ctx.LoadSheddingActive ? "Energy disruption strengthens the business case for resilience planning." : "Energy scenarios can be modelled before a disruption window.",
            BusinessActions());

    private CopilotChatResponse BuildCleanLogistics(string prompt, OperationalContext ctx) =>
        Base(prompt, "clean logistics / NCIC alignment", "Medium", 93, "Clean Logistics Impact",
            "NCIC alignment is the clean-logistics direction: reduce avoidable truck idling, diesel waste and congestion through explainable local decision support.",
            "Culltron Smart Port Flow demonstrates queue intelligence, ETA tracking, hold-outside-port recommendations, scenario simulation, emissions/idling estimates and audit-style operator plans using synthetic data.",
            "Use the Clean Logistics Impact page to frame the problem, solution, pilot outcomes and demo-first advantage.",
            "Positions the platform as a pilot-ready direction for cleaner, more resilient logistics while keeping claims realistic.",
            $"Current demo emissions context: {ctx.EstimatedCo2Today:F1} kg CO₂ from idling; estimates are indicative and not verified outcomes.",
            ctx.LoadSheddingActive ? "Load-shedding is part of the clean logistics story because energy disruption increases queue and idling risk." : "The simulator can show energy disruption resilience without live feeds.",
            BusinessActions());

    private CopilotChatResponse BuildPilotReadiness(string prompt, OperationalContext ctx) =>
        Base(prompt, "pilot readiness", "Medium", 91, "Pilot Readiness",
            "A 90-day pilot would move from 30-day setup, to 60-day integration prototype, to 90-day operational pilot with daily shift briefs, recommendations, scenario simulation and impact reporting.",
            "The current demonstrator is demo-safe: synthetic data, no paid external AI API and integration adapters disabled/demo-backed by default.",
            "Confirm pilot site and stakeholders, identify data owners, validate baseline metrics, then prototype approved gate, telematics, berth/vessel, energy and emissions-factor integrations.",
            "Shows Culltron has thought beyond the demo while avoiding false claims of live port deployment.",
            "Pilot metrics should include idling, diesel cost and CO₂ avoided as indicative or partner-validated measures.",
            "Energy schedules are a required pilot data source for resilience testing.",
            new() { new() { Icon = "🗺️", Title = "Pilot Readiness", Description = "Open 30/60/90-day roadmap.", Url = "/PilotReadiness" }, new() { Icon = "🤝", Title = "Stakeholders", Description = "Review persona value.", Url = "/Stakeholders" }, new() { Icon = "🧾", Title = "Executive Brief", Description = "Generate stakeholder brief.", Url = "/Brief" } });

    private CopilotChatResponse BuildIntegrations(string prompt, OperationalContext ctx) =>
        Base(prompt, "integration readiness", "Medium", 90, "Integration Readiness",
            "Production would require partner-approved TOS/IPMS or port-operations data, gate OCR/RFID, truck GPS/telematics, fleet dispatch, energy/load-shedding schedules, emissions factors, incident logs and reporting/export pathways.",
            "The app includes integration-ready interfaces with SyntheticDemo providers active by default, so the demo runs without internet, API keys or external systems.",
            "Use the Integration Readiness wall to discuss data needed, business value, complexity and risk per integration.",
            "Creates a credible technical path from demonstrator to pilot without claiming live integration.",
            "Emissions factors would need partner-approved assumptions or verified datasets in production.",
            "Energy data is treated as a future integration source, not a live feed in demo mode.",
            new() { new() { Icon = "🔌", Title = "Integration Wall", Description = "Open Pilot Readiness integrations.", Url = "/PilotReadiness#integration-readiness" }, new() { Icon = "📈", Title = "Impact", Description = "See value from integrations.", Url = "/Impact" } });

    private CopilotChatResponse BuildStakeholderValue(string prompt, OperationalContext ctx) =>
        Base(prompt, "stakeholder value", "Medium", 89, "Stakeholder Value",
            "Fleet operators get hold/release guidance and lower avoidable idling; port authorities get congestion visibility and clean-logistics reporting; sustainability officers get indicative idling and CO₂ evidence.",
            "Each persona maps pain points to existing modules: command centre, tracking, simulator, impact reporting, recommendations and executive brief.",
            "Open Stakeholder Value and select the persona relevant to the meeting.",
            "Helps turn the demo into a pilot discussion with clear stakeholder-specific value.",
            "Sustainability value remains indicative until production data and validated factors are connected.",
            "Municipal/provincial value includes resilience during energy disruption and congestion visibility.",
            new() { new() { Icon = "🤝", Title = "Stakeholder Value", Description = "Open persona cards.", Url = "/Stakeholders" }, new() { Icon = "📡", Title = "Truck Tracking", Description = "Fleet operator value.", Url = "/TruckTracking" } });

    private CopilotChatResponse BuildExecutiveBrief(string prompt, OperationalContext ctx) =>
        Base(prompt, "executive brief", "Medium", 92, "Executive Brief",
            $"Executive brief: current demo state has {ctx.VesselsInPort} vessels in port, {ctx.TrucksInQueue} queued trucks, {ctx.BerthUtilisationPct:F0}% berth utilisation, {ctx.YardOccupancyPct:F1}% yard occupancy and {ctx.ActiveDisruptions} disruptions.",
            "The brief is print-friendly and includes top risk, queue state, delayed/held trucks, gate/berth/yard pressure, idling estimate, recommended action plan and demo disclaimer.",
            "Open /Brief, print/save as PDF, or copy the brief into the meeting notes.",
            "Useful for judges, grant reviewers, pilot stakeholders and shift handover.",
            $"Indicative emissions/idling: {ctx.TotalIdlingMinutesToday:F0} minutes and {ctx.EstimatedCo2Today:F1} kg CO₂.",
            ctx.LoadSheddingActive ? "Include active energy risk in the handover." : "Include the energy integration readiness note.",
            new() { new() { Icon = "🧾", Title = "Executive Brief", Description = "Open print-friendly brief.", Url = "/Brief" }, new() { Icon = "📈", Title = "Impact Centre", Description = "Show executive KPIs.", Url = "/Impact" } });

    private CopilotChatResponse BuildGrantSummary(string prompt, OperationalContext ctx) =>
        Base(prompt, "grant / investor summary", "Medium", 91, "Grant / Investor Summary",
            "Culltron Smart Port Flow is a pilot-ready smart-port demonstrator for cleaner logistics: Gemini Agent Mode and local fallback turn synthetic port, queue, ETA, disruption and emissions signals into explainable operator actions.",
            "The value proposition is reduced avoidable idling, better truck flow, improved resilience during energy disruption, transparent recommendations and a clear integration pathway for future production pilots.",
            "Use a 2-minute narrative: problem, solution, demo-safe architecture, indicative impact, 90-day pilot plan, required integrations and stakeholder value.",
            "Gives grant and investor audiences a realistic path from demonstrator to partner-validated pilot.",
            "Emissions outcomes are potential/indicative until validated with production data and approved factors.",
            "Energy resilience is addressed through scenario planning and future energy data integration.",
            BusinessActions());

    private CopilotChatResponse BuildSuccessMetrics(string prompt, OperationalContext ctx) =>
        Base(prompt, "success metrics / risks", "Medium", 88, "Pilot Success Metrics",
            "Pilot success metrics should include truck waiting-time reduction, queue-length reduction, idling reduction, CO₂ avoided, diesel cost avoided, average turnaround time, recommendation adoption, operator response time and disruption recovery time.",
            "Risks include data availability, operator adoption, integration complexity, network/power reliability, change management, data quality and stakeholder alignment.",
            "Mitigate by starting with synthetic/demo workflow validation, agreeing baseline metrics, connecting one data source at a time and reviewing operator feedback weekly.",
            "Keeps the pilot measurable, auditable and realistic.",
            "CO₂ avoided should be reported as indicative until a partner-validated methodology exists.",
            "Energy disruption recovery time should be tracked during load-shedding scenarios or approved data feeds.",
            new() { new() { Icon = "🗺️", Title = "Pilot Readiness", Description = "Open metrics and risks.", Url = "/PilotReadiness" }, new() { Icon = "📈", Title = "Impact", Description = "Open value dashboard.", Url = "/Impact" } });

    private CopilotChatResponse BuildDemoSummary(string prompt, OperationalContext ctx) =>
        Base(prompt, "demo", "Medium", 94, "Competition Demo Narrative",
            "Culltron Smart Port Flow is a hybrid Smart Port Copilot that converts synthetic port telemetry into explainable operator actions.",
            $"The live demo shows {ctx.VesselsInPort} vessels in port, {ctx.TrucksInQueue} queued trucks, {ctx.YardOccupancyPct:F1}% yard occupancy, {ctx.ActiveDisruptions} disruptions and {ctx.EstimatedCo2Today:F1} kg CO₂ from idling today.",
            "Narrate Landing → Dashboard → AI Command Centre → SmartPort Copilot Chat → Truck Tracking → Scenario Simulator → Emissions → Recommendations/Audit.",
            "Judges see premium UI, Gemini/local fallback AI, scenario planning, sustainability impact and accountable decision history in under three minutes.",
            "Highlight CO₂/idling savings as a measurable business and sustainability outcome.",
            ctx.LoadSheddingActive ? "Mention active load-shedding response as a South African port reality." : "Mention that load-shedding can be simulated without live external feeds.",
            StandardActions());

    private CopilotChatResponse Base(string prompt, string intent, string severity, int confidence, string area,
        string summary, string reasoning, string action, string impact, string emissions, string energy,
        List<CopilotActionCard> cards)
    {
        return new CopilotChatResponse
        {
            Prompt = prompt,
            Intent = intent,
            Title = area,
            Summary = summary,
            ShortAnswer = summary,
            MessageType = IntentCategory(intent) is "greeting" or "help" or "harmless" ? "compact" : intent.Contains("out-of-scope") || intent.Contains("safety") || intent.Contains("sensitive") ? "warning" : intent.Contains("vague") ? "clarification" : "operational",
            OperationalReasoning = reasoning,
            RecommendedAction = action,
            ExpectedImpact = impact,
            AffectedArea = area,
            Severity = severity,
            ConfidenceScore = confidence,
            EmissionsImpact = emissions,
            EnergyImpact = energy,
            ActionCards = cards,
            IntentCategory = IntentCategory(intent),
            IsSmallTalk = IntentCategory(intent) is "greeting" or "help" or "harmless",
            IsOutOfScope = intent.Contains("out-of-scope") || intent.Contains("safety"),
            IsVagueButRelated = intent.Contains("vague"),
            IsOperational = IntentCategory(intent) is "operational" or "report" or "demo",
            SuggestedFollowUps = new() { "What is the biggest operational risk right now?", "How can we reduce truck idling?", "Generate an operator action plan." },
            TopicChips = GetPromptChips().Take(6).ToList(),
            MetricBadges = BuildMetricBadges(severity, confidence, area, IntentCategory(intent), "Local")
        };
    }

    private static List<CopilotActionCard> StandardActions() => new()
    {
        new() { Icon = "⬡", Title = "Dashboard", Description = "Return to the command center.", Url = "/dashboard" },
        new() { Icon = "📡", Title = "Truck Tracking", Description = "Inspect truck ETA and holds.", Url = "/TruckTracking" },
        new() { Icon = "🧠", Title = "Recommendations", Description = "Review/accept actions.", Url = "/flow/recommendations" },
        new() { Icon = "🔬", Title = "Simulator", Description = "Model what-if pressure.", Url = "/simulator" },
        new() { Icon = "🌿", Title = "Emissions", Description = "Quantify idling impact.", Url = "/emissions" }
    };

    private static List<CopilotActionCard> BusinessActions() => new()
    {
        new() { Icon = "📈", Title = "Executive Impact", Description = "Open business impact KPIs.", Url = "/Impact" },
        new() { Icon = "🌍", Title = "Clean Logistics", Description = "Review NCIC-style alignment.", Url = "/CleanLogistics" },
        new() { Icon = "🗺️", Title = "Pilot Readiness", Description = "Open 30/60/90-day plan.", Url = "/PilotReadiness" },
        new() { Icon = "🤝", Title = "Stakeholders", Description = "Review stakeholder value.", Url = "/Stakeholders" },
        new() { Icon = "🧾", Title = "Executive Brief", Description = "Generate print-friendly brief.", Url = "/Brief" }
    };

    private static bool Has(string text, params string[] terms) => terms.Any(term => text.Contains(term));

    private static bool LooksLikePassphrase(string text)
    {
        var normalized = text.Trim();
        if (normalized.Length < 6 || normalized.Contains(' ')) return false;
        var hasLetter = normalized.Any(char.IsLetter);
        var hasDigit = normalized.Any(char.IsDigit);
        var hasSymbol = normalized.Any(ch => !char.IsLetterOrDigit(ch));
        return hasLetter && hasDigit && hasSymbol;
    }

    private static bool IsGreeting(string text)
    {
        var normalized = text.Trim().Trim('?', '!', '.', ',');
        return normalized is "hello" or "hi" or "hey" or "thanks" or "thank you" or "who are you" or "how are you" or "lol" or "okay" or "ok" or "nice"
            || normalized.StartsWith("hello ")
            || normalized.StartsWith("hi ")
            || normalized.StartsWith("hey ")
            || normalized.StartsWith("thanks ")
            || normalized.StartsWith("thank you ")
            || normalized.StartsWith("good morning")
            || normalized.StartsWith("good afternoon")
            || normalized.StartsWith("good evening");
    }

    private static int Score(OperationalContext ctx)
    {
        var score = 20;
        score += Math.Min(25, ctx.TrucksInQueue * 2);
        score += ctx.BerthUtilisationPct > 80 ? 20 : ctx.BerthUtilisationPct > 60 ? 10 : 0;
        score += ctx.YardOccupancyPct > 85 ? 20 : ctx.YardOccupancyPct > 70 ? 10 : 0;
        score += ctx.LoadSheddingActive ? 18 : 0;
        score += Math.Min(15, ctx.OpenIncidents * 2 + ctx.CriticalDisruptions * 5);
        return Math.Min(100, score);
    }

    private static string Severity(int score) => score switch
    {
        >= 85 => "Critical",
        >= 65 => "High",
        >= 40 => "Medium",
        _ => "Low"
    };
}
