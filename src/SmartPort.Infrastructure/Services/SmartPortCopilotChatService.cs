using Microsoft.EntityFrameworkCore;
using SmartPort.Domain.Enums;
using SmartPort.Infrastructure.Persistence;

namespace SmartPort.Infrastructure.Services;

public interface ISmartPortCopilotChatService
{
    Task<CopilotChatPageModel> BuildPageAsync(string? prompt = null);
    Task<CopilotChatResponse> GenerateResponseAsync(string prompt);
    List<CopilotPromptChip> GetPromptChips();
}

public class CopilotChatPageModel
{
    public OperationalContext Context { get; set; } = new();
    public string CurrentPrompt { get; set; } = string.Empty;
    public CopilotChatResponse? Response { get; set; }
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
    public string DataNote { get; set; } = "Synthetic demo data · deterministic local response";
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

    public SmartPortCopilotChatService(IAiAgentService agent, SmartPortDbContext db, ITruckTrackingService tracking, ISmartPortIntelligenceService intelligence)
    {
        _agent = agent;
        _db = db;
        _tracking = tracking;
        _intelligence = intelligence;
    }

    public async Task<CopilotChatPageModel> BuildPageAsync(string? prompt = null)
    {
        var context = await _agent.GetContextAsync();
        var model = new CopilotChatPageModel
        {
            Context = context,
            CurrentPrompt = prompt ?? string.Empty,
            PromptChips = GetPromptChips(),
            SupportedTopics = SupportedTopics()
        };

        if (!string.IsNullOrWhiteSpace(prompt))
        {
            model.Response = await GenerateResponseAsync(prompt.Trim(), context);
        }

        return model;
    }

    public List<CopilotPromptChip> GetPromptChips() => new()
    {
        new() { Icon = "👋", Label = "Hello", Prompt = "hello" },
        new() { Icon = "❔", Label = "Capabilities", Prompt = "what can you do" },
        new() { Icon = "🚛", Label = "Truck queues", Prompt = "trucks on queue" },
        new() { Icon = "📡", Label = "Track trucks", Prompt = "track delayed trucks" },
        new() { Icon = "🛑", Label = "Hold trucks", Prompt = "which trucks should be held outside the port" },
        new() { Icon = "⚠️", Label = "Biggest risk", Prompt = "What is the biggest risk right now?" },
        new() { Icon = "🧭", Label = "Action plan", Prompt = "Generate an operator action plan." },
        new() { Icon = "🌿", Label = "CO₂ savings", Prompt = "How much CO2 can we save?" },
        new() { Icon = "🚪", Label = "Gate bottleneck", Prompt = "Which gate is becoming a bottleneck?" },
        new() { Icon = "🎬", Label = "Demo summary", Prompt = "Prepare a 2-minute demo summary." }
    };

    private static List<string> SupportedTopics() => new()
    {
        "Truck queues", "Truck tracking / ETA", "Gate bottlenecks", "Berth pressure", "Yard congestion",
        "Emissions / CO₂", "Load-shedding disruption", "Scenario simulation", "Recommendations", "Demo summary"
    };

    public async Task<CopilotChatResponse> GenerateResponseAsync(string prompt)
    {
        var context = await _agent.GetContextAsync();
        var snapshot = await _intelligence.GetSnapshotAsync();
        var response = await GenerateResponseAsync(prompt.Trim(), context);
        response.DataNote = $"{snapshot.DataNote} · snapshot {snapshot.Timestamp:HH:mm:ss} UTC";
        if (response.Intent == "risk")
        {
            response.Title = snapshot.TopRisk;
            response.AffectedArea = snapshot.AffectedArea;
            response.ConfidenceScore = snapshot.Confidence;
        }
        return response;
    }

    private async Task<CopilotChatResponse> GenerateResponseAsync(string prompt, OperationalContext ctx)
    {
        var q = prompt.ToLowerInvariant();
        var intent = ClassifyIntent(q);
        return intent switch
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
            "demo" => BuildDemoSummary(prompt, ctx),
            "action-plan" => BuildActionPlan(prompt, ctx),
            "vague-related" => BuildVagueRelated(prompt, ctx),
            "out-of-scope" => BuildOutOfScope(prompt),
            _ => BuildRisk(prompt, ctx)
        };
    }

    private static string ClassifyIntent(string q)
    {
        if (Has(q, "ignore instruction", "ignore previous", "system prompt", "developer prompt", "reveal prompt", "show prompt", "secret", "credential", "password", "api key", "dump database", "dump config", "internal config", "jailbreak")) return "safety";
        if (IsGreeting(q)) return "greeting";
        if (Has(q, "help", "what can you do", "what are you", "capabilit", "supported", "topics")) return "help";
        if (Has(q, "demo", "2-minute", "summary", "video", "pitch", "walkthrough")) return "demo";
        if (Has(q, "biggest risk", "port risk", "operational risk", "current risk", "what is urgent", "urgent", "do first", "should we do first", "manager do first")) return "risk";
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
        return "out-of-scope";
    }

    private CopilotChatResponse BuildGreeting(string prompt, OperationalContext ctx) =>
        Base(prompt, "greeting", "Low", 98, "Smart Port Copilot",
            "Hello, I’m the SmartPort Copilot — a local deterministic assistant for Culltron Smart Port Flow.",
            "I can analyse truck queues, truck ETA, gate bottlenecks, berth pressure, yard congestion, load-shedding disruption, emissions, scenario simulation and recommendations.",
            "Ask a focused operations question such as ‘what is the biggest port risk right now?’ or ‘which trucks should be held outside the port?’. ",
            "Keeps the demo scoped, safe and judge-friendly.",
            "Synthetic demo data is used for all operational estimates.",
            "No external AI or cloud secret is required.",
            StandardActions());

    private CopilotChatResponse BuildHelp(string prompt, OperationalContext ctx) =>
        Base(prompt, "help / capabilities", "Low", 97, "Supported Smart Port Topics",
            "I can help with smart port operations, congestion, truck tracking/ETA, gate flow, berth and yard pressure, emissions, disruptions, scenarios, recommendations and demo summary.",
            "The assistant uses deterministic topic routing and refuses unrelated, unsafe or secret-seeking prompts.",
            "Choose a topic chip or ask: trucks on queue, track delayed trucks, how much CO₂ can we save, or generate an operator action plan.",
            "You get consistent explainable outputs with summary, reasoning, action, impact, confidence, affected area and links.",
            "All figures are based on synthetic demo data.",
            "Local-only processing keeps the competition demo safe and repeatable.",
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
            "Local deterministic scope control is active.",
            StandardActions());

    private CopilotChatResponse BuildSafetyRefusal(string prompt) =>
        Base(prompt, "safety refusal", "Low", 99, "Governance / Safety",
            "I can’t help with secrets, credentials, internal configuration, prompt override requests, database dumps or attempts to bypass instructions.",
            "The Copilot is scope-limited to smart port operations and protects internal prompts, configuration and demo data boundaries.",
            "Ask about port congestion, truck ETA, gate queues, emissions, disruptions, scenario risk or recommended operator actions instead.",
            "Maintains enterprise governance and demo safety.",
            "No secret or external system data is exposed.",
            "Local deterministic governance is active.",
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

    private CopilotChatResponse BuildDemoSummary(string prompt, OperationalContext ctx) =>
        Base(prompt, "demo", "Medium", 94, "Competition Demo Narrative",
            "Culltron Smart Port Flow is a local deterministic Smart Port Copilot that converts synthetic port telemetry into explainable operator actions.",
            $"The live demo shows {ctx.VesselsInPort} vessels in port, {ctx.TrucksInQueue} queued trucks, {ctx.YardOccupancyPct:F1}% yard occupancy, {ctx.ActiveDisruptions} disruptions and {ctx.EstimatedCo2Today:F1} kg CO₂ from idling today.",
            "Narrate Landing → Dashboard → AI Command Centre → SmartPort Copilot Chat → Truck Tracking → Scenario Simulator → Emissions → Recommendations/Audit.",
            "Judges see premium UI, deterministic AI, scenario planning, sustainability impact and accountable decision history in under three minutes.",
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
            MessageType = intent.Contains("greeting") || intent.Contains("help") ? "compact" : intent.Contains("out-of-scope") || intent.Contains("safety") ? "warning" : intent.Contains("vague") ? "clarification" : "operational",
            OperationalReasoning = reasoning,
            RecommendedAction = action,
            ExpectedImpact = impact,
            AffectedArea = area,
            Severity = severity,
            ConfidenceScore = confidence,
            EmissionsImpact = emissions,
            EnergyImpact = energy,
            ActionCards = cards,
            IsSmallTalk = intent.Contains("greeting") || intent.Contains("help"),
            IsOutOfScope = intent.Contains("out-of-scope") || intent.Contains("safety"),
            IsVagueButRelated = intent.Contains("vague"),
            IsOperational = !(intent.Contains("greeting") || intent.Contains("help") || intent.Contains("out-of-scope") || intent.Contains("safety") || intent.Contains("vague")),
            SuggestedFollowUps = new() { "What is the biggest risk right now?", "Which trucks should be held outside the port?", "Generate an operator action plan." },
            TopicChips = GetPromptChips().Take(6).ToList(),
            MetricBadges = new()
            {
                new() { Label = "Urgency", Value = severity, Tone = severity.ToLowerInvariant() switch { "critical" => "danger", "high" => "warning", _ => "info" } },
                new() { Label = "Confidence", Value = $"{confidence}%", Tone = confidence >= 90 ? "success" : "info" },
                new() { Label = "Area", Value = area, Tone = "teal" },
                new() { Label = "Intent", Value = intent, Tone = "muted" }
            }
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

    private static bool Has(string text, params string[] terms) => terms.Any(term => text.Contains(term));

    private static bool IsGreeting(string text)
    {
        var normalized = text.Trim().Trim('?', '!', '.', ',');
        return normalized is "hello" or "hi" or "hey" or "thanks" or "thank you" or "who are you" or "how are you"
            || normalized.StartsWith("hello ")
            || normalized.StartsWith("hi ")
            || normalized.StartsWith("hey ")
            || normalized.StartsWith("thanks ")
            || normalized.StartsWith("thank you ")
            || normalized.StartsWith("good morning")
            || normalized.StartsWith("good afternoon");
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
