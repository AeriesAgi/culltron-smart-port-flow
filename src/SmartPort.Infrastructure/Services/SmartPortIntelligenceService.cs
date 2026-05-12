namespace SmartPort.Infrastructure.Services;

public interface ISmartPortIntelligenceService
{
    Task<SmartPortOperationsSnapshot> GetSnapshotAsync();
}

public class SmartPortOperationsSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string DataNote { get; set; } = "Synthetic demo data · hybrid Gemini/local fallback rules and scoring";
    public int VesselsInPort { get; set; }
    public int VesselsAtAnchor { get; set; }
    public decimal BerthUtilisation { get; set; }
    public decimal YardOccupancy { get; set; }
    public int TrucksInQueue { get; set; }
    public int ActiveDisruptions { get; set; }
    public string LoadSheddingEnergyRisk { get; set; } = "Stable";
    public decimal EmissionsIdlingEstimateKg { get; set; }
    public decimal IdlingMinutes { get; set; }
    public string TopGateBottleneck { get; set; } = "Primary gate queue";
    public List<string> DelayedTrucks { get; set; } = new();
    public List<string> HoldOutsidePortCandidates { get; set; } = new();
    public List<string> PriorityReleaseCandidates { get; set; } = new();
    public string TopRisk { get; set; } = "Gate queue pressure";
    public string TopRiskSeverity { get; set; } = "Medium";
    public string TopRiskReason { get; set; } = "Queue, berth, yard, energy and incident signals are scored locally.";
    public string AffectedArea { get; set; } = "Port Operations";
    public string ExpectedConsequence { get; set; } = "Avoidable queue growth and idling if no operator action is taken.";
    public int Confidence { get; set; } = 88;
    public List<string> RecommendedActions { get; set; } = new();
    public List<string> ImmediateActions { get; set; } = new();
    public List<string> Next30Minutes { get; set; } = new();
    public List<string> Next2Hours { get; set; } = new();
    public List<string> EscalationActions { get; set; } = new();
    public List<CopilotActionCard> ActionLinks { get; set; } = new();
    public List<MonitoringLane> MonitoringLanes { get; set; } = new();
    public string ShiftBrief { get; set; } = string.Empty;
}

public class MonitoringLane
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Watch";
    public string Metric { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Confidence { get; set; } = 85;
}

public class SmartPortIntelligenceService : ISmartPortIntelligenceService
{
    private readonly IAiAgentService _agent;
    private readonly ITruckTrackingService _tracking;

    public SmartPortIntelligenceService(IAiAgentService agent, ITruckTrackingService tracking)
    {
        _agent = agent;
        _tracking = tracking;
    }

    public async Task<SmartPortOperationsSnapshot> GetSnapshotAsync()
    {
        var ctx = await _agent.GetContextAsync();
        var trucks = await _tracking.GetDashboardAsync();
        var score = Score(ctx, trucks);
        var severity = Severity(score);
        var topRisk = ResolveTopRisk(ctx, trucks);
        var hold = trucks.Trucks.Where(t => t.Status == "Hold Outside Port").Select(t => $"{t.FleetIdentifier} · {t.CurrentCheckpoint} · ETA {t.EtaMinutesToGate} min").Take(4).ToList();
        var delayed = trucks.Trucks.Where(t => t.Status is "Delayed" or "Hold Outside Port").Select(t => $"{t.FleetIdentifier} · risk {t.DelayRiskScore}/100 · {t.RecommendedAction}").Take(4).ToList();
        var priority = trucks.Trucks.Where(t => t.Status == "Priority Release").Select(t => $"{t.FleetIdentifier} · {t.RouteCorridor}").Take(4).ToList();

        var snapshot = new SmartPortOperationsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            VesselsInPort = ctx.VesselsInPort,
            VesselsAtAnchor = ctx.VesselsAtAnchor,
            BerthUtilisation = ctx.BerthUtilisationPct,
            YardOccupancy = ctx.YardOccupancyPct,
            TrucksInQueue = ctx.TrucksInQueue,
            ActiveDisruptions = ctx.ActiveDisruptions,
            LoadSheddingEnergyRisk = ctx.LoadSheddingActive ? "Active load-shedding / energy disruption" : "Stable, monitor next disruption window",
            EmissionsIdlingEstimateKg = Math.Max(ctx.EstimatedCo2Today, trucks.EstimatedCo2Kg),
            IdlingMinutes = Math.Max(ctx.TotalIdlingMinutesToday, trucks.TotalIdlingMinutes),
            TopGateBottleneck = trucks.GatePressureScore >= 70 ? "Gate approach / outer staging interface" : "Primary gate queue under watch",
            DelayedTrucks = delayed.Count > 0 ? delayed : new() { "No delayed truck fallback beyond demo thresholds." },
            HoldOutsidePortCandidates = hold.Count > 0 ? hold : new() { "No current hold candidate above risk threshold." },
            PriorityReleaseCandidates = priority.Count > 0 ? priority : new() { "No priority release candidate above urgency threshold." },
            TopRisk = topRisk.title,
            TopRiskSeverity = severity,
            TopRiskReason = topRisk.reason,
            AffectedArea = topRisk.area,
            ExpectedConsequence = topRisk.consequence,
            Confidence = Math.Clamp((trucks.AiConfidenceScore + 86 + (ctx.TopRecommendations.Count > 0 ? 4 : 0)) / 2, 75, 96),
            RecommendedActions = new()
            {
                "Meter truck arrivals against gate pressure and keep high-risk trucks outside the port perimeter.",
                "Prioritise pre-cleared or urgent cargo through the fastest available lane.",
                "Protect berth and yard recovery windows before dispatching new non-critical trips.",
                "Record accepted recommendations for a visible decision/audit trail."
            },
            ImmediateActions = new()
            {
                "Confirm gate queue depth and activate overflow / pre-clearance lane if queue exceeds threshold.",
                "Send hold advisories to the highest-risk approaching trucks.",
                "Brief shift lead on top risk, confidence and expected consequence."
            },
            Next30Minutes = new()
            {
                "Re-score delayed trucks and priority release candidates.",
                "Run a scenario if load-shedding or vessel delay is expected within the next window.",
                "Check emissions/idling exposure after gate metering."
            },
            Next2Hours = new()
            {
                "Review berth handover and yard dwell pressure before releasing queued dispatches.",
                "Update the decision trail with accepted/dismissed recommendations.",
                "Prepare a short shift brief for the next operations handover."
            },
            EscalationActions = new()
            {
                ctx.LoadSheddingActive ? "Escalate energy playbook and reefer protection." : "Keep energy disruption playbook ready for the next simulated window.",
                "Notify dispatchers where ETA risk exceeds the demo threshold.",
                "Escalate persistent gate or berth pressure to operations manager."
            },
            ActionLinks = new()
            {
                new() { Icon = "📡", Title = "Truck Tracking", Description = "Inspect ETA and hold candidates.", Url = "/TruckTracking" },
                new() { Icon = "🔬", Title = "Run Simulator", Description = "Model energy, gate or vessel disruption.", Url = "/Simulator" },
                new() { Icon = "🌿", Title = "Emissions", Description = "Review indicative idling impact.", Url = "/Emissions" },
                new() { Icon = "🧠", Title = "Recommendations", Description = "Accept/review action trail.", Url = "/Flow/Recommendations" },
                new() { Icon = "✦", Title = "Copilot Chat", Description = "Ask a scoped follow-up question.", Url = "/Copilot" }
            },
            MonitoringLanes = BuildLanes(ctx, trucks)
        };

        snapshot.ShiftBrief = $"Shift brief: {severity} risk. {snapshot.TopRisk} affecting {snapshot.AffectedArea}. Current synthetic state: {ctx.VesselsInPort} vessels in port, {ctx.VesselsAtAnchor} at anchor, {ctx.TrucksInQueue} queued trucks, {ctx.BerthUtilisationPct:F0}% berth utilisation, {ctx.YardOccupancyPct:F1}% yard occupancy, {ctx.ActiveDisruptions} active disruptions and {snapshot.EmissionsIdlingEstimateKg:F1} kg indicative CO₂ exposure. First action: {snapshot.ImmediateActions.First()}";
        return snapshot;
    }

    private static List<MonitoringLane> BuildLanes(OperationalContext ctx, TruckTrackingDashboardDto trucks) => new()
    {
        new() { Name = "Gate pressure", Status = ctx.TrucksInQueue > 14 ? "Critical" : ctx.TrucksInQueue > 8 ? "High" : "Watch", Metric = $"{ctx.TrucksInQueue} queued trucks", Reason = "Queue depth and processing load are scored against demo thresholds.", Confidence = 90 },
        new() { Name = "Berth pressure", Status = ctx.BerthUtilisationPct > 85 || ctx.VesselsAtAnchor > 0 ? "High" : "Watch", Metric = $"{ctx.BerthUtilisationPct:F0}% utilised · {ctx.VesselsAtAnchor} at anchor", Reason = "High occupancy can cascade into yard and gate appointments.", Confidence = 87 },
        new() { Name = "Yard pressure", Status = ctx.YardOccupancyPct > 88 ? "Critical" : ctx.YardOccupancyPct > 76 ? "High" : "Watch", Metric = $"{ctx.YardOccupancyPct:F1}% occupancy", Reason = "Yard density and dwell alerts constrain dispatch flow.", Confidence = 86 },
        new() { Name = "Truck queue", Status = trucks.HoldOutsidePortCount > 0 ? "High" : trucks.DelayedCount > 0 ? "Watch" : "Stable", Metric = $"{trucks.ActiveTrucks} active · {trucks.HoldOutsidePortCount} holds", Reason = "ETA, checkpoint and risk thresholds drive hold/release advice.", Confidence = trucks.AiConfidenceScore },
        new() { Name = "Emissions / idling", Status = trucks.EstimatedCo2Kg > 18 ? "High" : "Watch", Metric = $"{trucks.TotalIdlingMinutes:F0} min · {trucks.EstimatedCo2Kg:F1} kg CO₂", Reason = "Indicative diesel/CO₂ factors are applied to queue idling minutes.", Confidence = 84 },
        new() { Name = "Load-shedding / energy", Status = ctx.LoadSheddingActive ? "Critical" : "Stable", Metric = ctx.LoadSheddingActive ? "Active disruption" : "No active energy event", Reason = "Energy state influences gate automation, crane productivity and reefer priority.", Confidence = 85 },
        new() { Name = "Incidents / disruptions", Status = ctx.CriticalDisruptions > 0 ? "Critical" : ctx.ActiveDisruptions > 0 ? "High" : "Stable", Metric = $"{ctx.ActiveDisruptions} active", Reason = "Open disruptions and incidents are included in the top-risk score.", Confidence = 88 }
    };

    private static (string title, string reason, string area, string consequence) ResolveTopRisk(OperationalContext ctx, TruckTrackingDashboardDto trucks)
    {
        if (ctx.LoadSheddingActive) return ("Energy disruption may slow gate and reefer operations", "Load-shedding is active, so gate automation and cold-chain handoffs need protection.", "Energy / Gate / Reefers", "Manual processing and reefer priority issues can increase truck waits.");
        if (trucks.HoldOutsidePortCount > 0 || ctx.TrucksInQueue > 14) return ("Gate queue pressure with avoidable truck idling", "Queue depth and approaching-truck ETA risk exceed hold thresholds.", "Gate / Truck Flow", "Trucks may join the live queue too early, increasing diesel waste and CO₂ exposure.");
        if (ctx.BerthUtilisationPct > 85 || ctx.VesselsAtAnchor > 0) return ("Berth pressure can cascade into yard and gate congestion", "Berth utilisation or anchorage pressure is high in the snapshot.", "Berths / Yard", "Late discharge windows may shift appointment plans and grow yard density.");
        if (ctx.YardOccupancyPct > 82) return ("Yard density is constraining dispatch reliability", "Yard occupancy and dwell alerts reduce usable ground slots.", "Yard / Dispatch", "Container availability issues can force truck rework and extra idling.");
        return ("Monitor gate queue and dispatch pacing", "No critical threshold is active, but baseline scoring keeps gate, berth, yard and emissions under watch.", "Port Operations", "Without pacing, normal peaks can still become queues.");
    }

    private static int Score(OperationalContext ctx, TruckTrackingDashboardDto trucks)
    {
        var score = 22 + Math.Min(24, ctx.TrucksInQueue * 2) + Math.Min(18, trucks.HoldOutsidePortCount * 8 + trucks.DelayedCount * 4);
        score += ctx.BerthUtilisationPct > 85 ? 18 : ctx.BerthUtilisationPct > 70 ? 9 : 0;
        score += ctx.YardOccupancyPct > 88 ? 16 : ctx.YardOccupancyPct > 76 ? 8 : 0;
        score += ctx.LoadSheddingActive ? 16 : 0;
        score += Math.Min(12, ctx.ActiveDisruptions * 3 + ctx.CriticalDisruptions * 5);
        return Math.Clamp(score, 0, 100);
    }

    private static string Severity(int score) => score switch
    {
        >= 85 => "Critical",
        >= 65 => "High",
        >= 40 => "Medium",
        _ => "Low"
    };
}
