using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartPort.Application.Interfaces;
using SmartPort.Domain.Entities;
using SmartPort.Domain.Enums;
using SmartPort.Infrastructure.Persistence;

namespace SmartPort.Infrastructure.Services;

// ─── Settings ─────────────────────────────────────────────────────────────────

public class AiAgentSettings
{
    public bool Enabled { get; set; } = true;
    public bool FallbackMode { get; set; } = true;
    public string SystemPrompt { get; set; } = string.Empty;
}

// ─── Question/Answer Model ────────────────────────────────────────────────────

public class AgentQuestion
{
    public string Question { get; set; } = string.Empty;
}

public class AgentAnswer
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "info";
    public List<string> RelatedActions { get; set; } = new();
    public List<string> DataPoints { get; set; } = new();
    public bool IsFromLlm { get; set; } = false;
    public string AnsweredAt { get; set; } = DateTime.UtcNow.ToString("HH:mm:ss");
    public string DataDisclaimer { get; set; } = "Response based on synthetic operational data generated for demonstration purposes only.";
}

// ─── Operational Context snapshot passed to the engine ───────────────────────

public class OperationalContext
{
    public int VesselsInPort { get; set; }
    public int VesselsDelayed { get; set; }
    public int VesselsAtAnchor { get; set; }
    public int BerthsOccupied { get; set; }
    public int BerthsAvailable { get; set; }
    public decimal BerthUtilisationPct { get; set; }
    public int ActiveDisruptions { get; set; }
    public int CriticalDisruptions { get; set; }
    public int ContainersInYard { get; set; }
    public int DwellAlerts { get; set; }
    public decimal YardOccupancyPct { get; set; }
    public int TrucksInQueue { get; set; }
    public int GatesOperational { get; set; }
    public int OpenIncidents { get; set; }
    public int ActiveAlerts { get; set; }
    public decimal AverageTurnaroundHours { get; set; }
    public int ActiveTrips { get; set; }
    public int HighRiskTrips { get; set; }
    public decimal TotalIdlingMinutesToday { get; set; }
    public decimal EstimatedCo2Today { get; set; }
    public decimal DispatchReliabilityPct { get; set; }
    public bool LoadSheddingActive { get; set; }
    public bool RoadCongestionActive { get; set; }
    public bool GateDelayActive { get; set; }
    public List<string> TopDisruptions { get; set; } = new();
    public List<string> TopVessels { get; set; } = new();
    public List<string> TopRecommendations { get; set; } = new();
}

// ─── AI Agent Service Interface ───────────────────────────────────────────────

public interface IAiAgentService
{
    Task<AgentAnswer> AskAsync(string question);
    Task<OperationalContext> GetContextAsync();
    List<AgentQuestion> GetSuggestedQuestions();
}

// ─── AI Agent Service Implementation ─────────────────────────────────────────

public class AiAgentService : IAiAgentService
{
    private readonly SmartPortDbContext _db;
    private readonly AiAgentSettings _settings;

    public AiAgentService(SmartPortDbContext db, IOptions<AiAgentSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
    }

    public async Task<AgentAnswer> AskAsync(string question)
    {
        var ctx = await GetContextAsync();

        // Always use the local deterministic engine. This project intentionally avoids
        // paid AI APIs, cloud secrets, and internet-dependent inference paths.
        return BuildDeterministicAnswer(question.ToLower().Trim(), ctx);
    }

    public async Task<OperationalContext> GetContextAsync()
    {
        var vessels    = await _db.Vessels.Where(v => !v.IsDeleted).ToListAsync();
        var berths     = await _db.Berths.Where(b => !b.IsDeleted).ToListAsync();
        var containers = await _db.Containers.Where(c => !c.IsDeleted).ToListAsync();
        var gates      = await _db.Gates.Where(g => !g.IsDeleted).ToListAsync();
        var incidents  = await _db.Incidents.Where(i => !i.IsDeleted).ToListAsync();
        var alerts     = await _db.Alerts.Where(a => !a.IsDeleted).ToListAsync();
        var disrupts   = await _db.DisruptionEvents.Where(d => !d.IsDeleted).ToListAsync();
        var trips      = await _db.DispatchTrips.Include(t => t.FleetVehicle).Where(t => !t.IsDeleted).ToListAsync();
        var emissions  = await _db.IdlingEmissionEstimates.Where(e => !e.IsDeleted && e.CreatedAt >= DateTime.UtcNow.Date).ToListAsync();
        var recs       = await _db.FlowRecommendations.Where(r => !r.IsDeleted).ToListAsync();
        var metrics    = await _db.OperationalMetrics
            .Where(m => m.MetricDate >= DateTime.UtcNow.AddDays(-7)).ToListAsync();

        var activeDisrupts = disrupts.Where(d => d.IsActive).ToList();
        var totalCap  = (decimal)berths.Sum(b => b.TotalCapacityTEU > 0 ? b.TotalCapacityTEU : 3000);
        var occupied  = (decimal)containers.Count(c => c.Status == ContainerStatus.InYard);
        var berthsOcc = berths.Count(b => b.Status == BerthStatus.Occupied);
        var avgTurnaround = metrics.Where(m => m.MetricType == "AverageTurnaroundHours").Any()
            ? metrics.Where(m => m.MetricType == "AverageTurnaroundHours").Average(m => m.Value)
            : 32m;

        var activeFlowRecs = recs.Where(r => r.AcceptedByUser == null).ToList();
        var today = DateTime.UtcNow.Date;
        var highRiskTrips = recs.Where(r => r.RiskLevel >= FlowRiskLevel.High && r.GeneratedAt >= today).ToList();

        return new OperationalContext
        {
            VesselsInPort          = vessels.Count(v => v.IsCurrentlyInPort),
            VesselsDelayed         = vessels.Count(v => v.DelayMinutes > 0),
            VesselsAtAnchor        = vessels.Count(v => v.Status == VesselStatus.AtAnchor),
            BerthsOccupied         = berthsOcc,
            BerthsAvailable        = berths.Count(b => b.Status == BerthStatus.Available),
            BerthUtilisationPct    = berths.Count > 0 ? Math.Round((decimal)berthsOcc / berths.Count * 100, 1) : 0,
            ActiveDisruptions      = activeDisrupts.Count,
            CriticalDisruptions    = activeDisrupts.Count(d => d.Severity == DisruptionSeverity.Critical),
            ContainersInYard       = (int)occupied,
            DwellAlerts            = containers.Count(c => c.IsDwellAlertRaised),
            YardOccupancyPct       = totalCap > 0 ? Math.Round(occupied / totalCap * 100, 1) : 0,
            TrucksInQueue          = gates.Sum(g => g.CurrentQueueCount),
            GatesOperational       = gates.Count(g => g.IsOperational),
            OpenIncidents          = incidents.Count(i => i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed),
            ActiveAlerts           = alerts.Count(a => a.Status == AlertStatus.Active),
            AverageTurnaroundHours = Math.Round((decimal)avgTurnaround, 1),
            ActiveTrips            = trips.Count(t => t.Status == TripStatus.Dispatched || t.Status == TripStatus.Waiting || t.Status == TripStatus.AtGate),
            HighRiskTrips          = highRiskTrips.Count,
            TotalIdlingMinutesToday = emissions.Sum(e => e.EstimatedIdlingMinutes),
            EstimatedCo2Today      = emissions.Sum(e => e.EstimatedCo2Kg),
            DispatchReliabilityPct = 76.2m,
            LoadSheddingActive     = activeDisrupts.Any(d => d.DisruptionType == DisruptionType.LoadShedding),
            RoadCongestionActive   = activeDisrupts.Any(d => d.DisruptionType == DisruptionType.RoadCongestion),
            GateDelayActive        = activeDisrupts.Any(d => d.DisruptionType == DisruptionType.GateDelay),
            TopDisruptions         = activeDisrupts.OrderByDescending(d => d.Severity).Take(3).Select(d => $"{d.Title} ({d.Severity})").ToList(),
            TopVessels             = vessels.Where(v => v.IsCurrentlyInPort || v.Status == VesselStatus.AtAnchor).Take(4).Select(v => $"{v.Name} ({v.Status})").ToList(),
            TopRecommendations     = activeFlowRecs.OrderByDescending(r => r.RiskLevel).Take(3).Select(r => r.RecommendationText).ToList()
        };
    }

    public List<AgentQuestion> GetSuggestedQuestions() => new()
    {
        new() { Question = "What is causing congestion today?" },
        new() { Question = "Which berths should we prioritise?" },
        new() { Question = "What happens if load-shedding starts at 16:00?" },
        new() { Question = "How do we reduce truck idle time today?" },
        new() { Question = "Which vessels are at highest risk?" },
        new() { Question = "What should the operations manager do first?" },
        new() { Question = "How can we reduce emissions impact today?" },
        new() { Question = "Is the yard nearing capacity?" },
        new() { Question = "How long are trucks waiting at the gate?" },
        new() { Question = "What is the current berth utilisation?" },
        new() { Question = "Which routes have active disruptions?" },
        new() { Question = "What is the energy risk level right now?" },
    };

    // ─── Deterministic Engine ─────────────────────────────────────────────────

    private AgentAnswer BuildDeterministicAnswer(string q, OperationalContext ctx)
    {
        // Route question to best matching handler
        if (Contains(q, "congest", "delay", "slow", "traffic", "causing"))
            return AnswerCongestion(q, ctx);
        if (Contains(q, "berth", "dock", "priorit"))
            return AnswerBerths(q, ctx);
        if (Contains(q, "load.shedd", "power", "energy", "eskom", "electric"))
            return AnswerLoadShedding(q, ctx);
        if (Contains(q, "truck", "idle", "wait", "queue", "gate"))
            return AnswerTrucks(q, ctx);
        if (Contains(q, "vessel", "ship", "risk", "high.risk", "anchor"))
            return AnswerVessels(q, ctx);
        if (Contains(q, "first", "priorit", "manager", "do", "action", "recommend"))
            return AnswerPriority(q, ctx);
        if (Contains(q, "emiss", "co2", "carbon", "sustainab", "fuel", "diesel"))
            return AnswerEmissions(q, ctx);
        if (Contains(q, "yard", "capac", "container", "dwell", "storage"))
            return AnswerYard(q, ctx);
        if (Contains(q, "utilisa", "utiliza", "occupan", "berth status"))
            return AnswerBerthUtil(q, ctx);
        if (Contains(q, "route", "disrupt", "incident", "active"))
            return AnswerDisruptions(q, ctx);

        return AnswerGeneral(q, ctx);
    }

    private AgentAnswer AnswerCongestion(string q, OperationalContext ctx)
    {
        var factors = new List<string>();
        var actions = new List<string>();
        var risk    = "medium";

        if (ctx.LoadSheddingActive)  { factors.Add("Eskom load-shedding is active — cold-chain and refrigerated operations are under power risk"); }
        if (ctx.RoadCongestionActive){ factors.Add("Road congestion reported on a key freight corridor — trucks are experiencing extended travel times"); }
        if (ctx.GateDelayActive)     { factors.Add("Gate processing delays at the terminal — average processing time has increased significantly"); }
        if (ctx.TrucksInQueue > 8)   { factors.Add($"{ctx.TrucksInQueue} trucks are currently queued — above normal threshold of 8"); }
        if (ctx.VesselsAtAnchor > 0) { factors.Add($"{ctx.VesselsAtAnchor} vessel(s) waiting at anchorage — berth pressure is contributing to congestion"); }

        if (!factors.Any()) factors.Add("No major congestion triggers are currently active — conditions are within normal range");

        if (ctx.GateDelayActive)   { actions.Add("Activate overflow gate lanes to reduce vehicle processing backlog"); risk = "high"; }
        if (ctx.TrucksInQueue > 10){ actions.Add("Implement appointment-based gate entry to smooth truck arrival curve"); risk = "high"; }
        if (ctx.RoadCongestionActive){ actions.Add("Issue route advisory to dispatchers — recommend alternative corridors"); }
        actions.Add("Monitor berth turnaround times — high utilisation amplifies downstream congestion");

        var level = ctx.ActiveDisruptions > 2 ? "high" : ctx.ActiveDisruptions > 0 ? "medium" : "low";

        return new AgentAnswer
        {
            Question  = q,
            Category  = "Congestion Analysis",
            RiskLevel = level,
            Answer    = factors.Any()
                ? $"**Congestion is currently {level.ToUpper()} risk.** The following {factors.Count} factor(s) are contributing:\n\n" +
                  string.Join("\n", factors.Select((f, i) => $"{i+1}. {f}")) +
                  $"\n\nCurrent operational state: {ctx.VesselsInPort} vessels in port, {ctx.TrucksInQueue} trucks queued, {ctx.BerthsOccupied}/{ctx.BerthsOccupied + ctx.BerthsAvailable} berths occupied, {ctx.ActiveDisruptions} active disruption(s)."
                : "Congestion risk is currently LOW. No major disruptions are active. All gates and berths are operating within normal parameters.",
            RelatedActions = actions,
            DataPoints = new() {
                $"Active disruptions: {ctx.ActiveDisruptions}",
                $"Trucks in queue: {ctx.TrucksInQueue}",
                $"Gate delay active: {ctx.GateDelayActive}",
                $"Road congestion active: {ctx.RoadCongestionActive}",
                $"Vessels at anchor: {ctx.VesselsAtAnchor}"
            }
        };
    }

    private AgentAnswer AnswerBerths(string q, OperationalContext ctx)
    {
        var actions = new List<string>();
        var risk    = ctx.BerthUtilisationPct > 80 ? "high" : ctx.BerthUtilisationPct > 60 ? "medium" : "low";

        if (ctx.BerthsAvailable == 0)
        {
            actions.Add("All berths are currently occupied — prioritise earliest departure vessel for discharge completion");
            actions.Add($"Vessels at anchor ({ctx.VesselsAtAnchor}) are waiting — confirm ETDs and sequence berth handovers");
        }
        else
        {
            actions.Add($"{ctx.BerthsAvailable} berth(s) currently available — review incoming vessel ETAs and assign immediately");
            actions.Add("Prioritise highest-urgency vessel call for next available berth slot");
        }

        if (ctx.VesselsDelayed > 0)
            actions.Add($"{ctx.VesselsDelayed} vessel(s) delayed — reassess berth windows to prevent cascading schedule impact");

        return new AgentAnswer
        {
            Question  = q,
            Category  = "Berth Planning",
            RiskLevel = risk,
            Answer    = $"**Berth utilisation is currently {ctx.BerthUtilisationPct}% ({ctx.BerthsOccupied} occupied, {ctx.BerthsAvailable} available).**\n\n" +
                        (ctx.BerthUtilisationPct > 85
                            ? "Utilisation is HIGH. Berth pressure is elevated and may cause cascading delays for incoming vessels. Prioritise efficient discharge and departure to free capacity."
                            : ctx.BerthUtilisationPct > 60
                                ? "Utilisation is MODERATE. Monitor incoming ETAs carefully. Ensure berth handover sequences are confirmed with pilot and tug services."
                                : "Berth utilisation is LOW. Capacity is available — ensure incoming vessels are scheduled to available berths promptly to maintain throughput.") +
                        $"\n\nAverage vessel turnaround: {ctx.AverageTurnaroundHours} hours. Delayed vessels: {ctx.VesselsDelayed}.",
            RelatedActions = actions,
            DataPoints = new() {
                $"Berths occupied: {ctx.BerthsOccupied}",
                $"Berths available: {ctx.BerthsAvailable}",
                $"Utilisation: {ctx.BerthUtilisationPct}%",
                $"Vessels delayed: {ctx.VesselsDelayed}",
                $"Average turnaround: {ctx.AverageTurnaroundHours}h"
            }
        };
    }

    private AgentAnswer AnswerLoadShedding(string q, OperationalContext ctx)
    {
        var actions = new List<string>();
        var risk    = ctx.LoadSheddingActive ? "critical" : "low";

        if (ctx.LoadSheddingActive)
        {
            actions.Add("Immediately prioritise all cold-chain and reefer dispatches before next load-shedding window");
            actions.Add("Confirm backup power status at all cold stores and reefer zones");
            actions.Add("Notify dispatchers: avoid dispatching temperature-sensitive cargo during power-risk window");
            actions.Add("Check terminal crane and equipment backup power — reduced crane availability will extend turnaround");
        }
        else
        {
            actions.Add("No immediate load-shedding risk. Monitor Eskom schedule for upcoming stages.");
            actions.Add("Review afternoon window (16:00–20:00) — historically high load-shedding risk period.");
        }

        var answer = ctx.LoadSheddingActive
            ? $"⚡ **ACTIVE LOAD-SHEDDING RISK DETECTED.** Eskom load-shedding is currently active in the operational area.\n\n" +
              $"This affects: cold-chain/reefer operations, terminal power-dependent equipment, backup storage facilities, and gate OCR/RFID systems.\n\n" +
              $"Current operational exposure: {ctx.ActiveTrips} active trips. Prioritise cold-chain and temperature-sensitive cargo immediately."
            : "No active load-shedding risk at this time. The current operational window is clear. However, afternoon windows (16:00–20:00) carry higher historical risk — plan cold-chain dispatches accordingly.";

        return new AgentAnswer
        {
            Question  = q,
            Category  = "Energy & Load-Shedding Risk",
            RiskLevel = risk,
            Answer    = answer,
            RelatedActions = actions,
            DataPoints = new() {
                $"Load-shedding active: {ctx.LoadSheddingActive}",
                $"Active disruptions: {ctx.ActiveDisruptions}",
                $"Active trips exposed: {ctx.ActiveTrips}",
                $"Cold-chain trips at risk: check Dispatch module"
            }
        };
    }

    private AgentAnswer AnswerTrucks(string q, OperationalContext ctx)
    {
        var risk = ctx.TrucksInQueue > 12 ? "critical" : ctx.TrucksInQueue > 8 ? "high" : ctx.TrucksInQueue > 4 ? "medium" : "low";
        var estWait = ctx.TrucksInQueue * 7; // 7 min avg per truck per lane
        var actions = new List<string>();

        if (ctx.TrucksInQueue > 8)
        {
            actions.Add($"Activate additional gate lanes — current queue of {ctx.TrucksInQueue} trucks exceeds threshold");
            actions.Add("Implement pre-booking slot management to smooth truck arrival curve");
        }
        if (ctx.GateDelayActive)
            actions.Add("Gate processing delay is active — escalate to terminal operations for additional staffing");
        actions.Add("Issue estimated wait-time advisory to transport operators");
        actions.Add("Review appointment window distribution — cluster arrivals are amplifying queue spikes");

        return new AgentAnswer
        {
            Question  = q,
            Category  = "Gate & Truck Operations",
            RiskLevel = risk,
            Answer    = $"**Gate queue level: {ctx.TrucksInQueue} trucks** (Risk: {risk.ToUpper()}).\n\n" +
                        $"Estimated current wait time: approximately {estWait} minutes at {ctx.GatesOperational} operational gate(s).\n\n" +
                        (ctx.TrucksInQueue > 8
                            ? $"Queue is ABOVE threshold. Gate delay risk is {(ctx.GateDelayActive ? "CONFIRMED" : "elevated")}. Immediate intervention is recommended to prevent idling escalation.\n\nIdling impact today: {ctx.TotalIdlingMinutesToday:F0} total minutes estimated across active fleet."
                            : $"Queue is within normal range. No immediate intervention required. Continue monitoring."),
            RelatedActions = actions,
            DataPoints = new() {
                $"Trucks in queue: {ctx.TrucksInQueue}",
                $"Gates operational: {ctx.GatesOperational}",
                $"Gate delay active: {ctx.GateDelayActive}",
                $"Est. wait time: ~{estWait} min",
                $"Total idling today: {ctx.TotalIdlingMinutesToday:F0} min"
            }
        };
    }

    private AgentAnswer AnswerVessels(string q, OperationalContext ctx)
    {
        var risk = ctx.VesselsDelayed > 2 ? "high" : ctx.VesselsDelayed > 0 ? "medium" : "low";
        var actions = new List<string>();

        if (ctx.VesselsAtAnchor > 0)
            actions.Add($"Review berth allocation for {ctx.VesselsAtAnchor} vessel(s) at anchor — assign next available compatible berth");
        if (ctx.VesselsDelayed > 0)
            actions.Add($"Issue delay notifications for {ctx.VesselsDelayed} delayed vessel(s) — update ETA with agents and berth planner");
        actions.Add("Confirm pilot and tug availability for all ETAs within next 6 hours");
        actions.Add("Review cargo plan approvals — incomplete plans delay cargo operations");

        var vesselList = ctx.TopVessels.Any()
            ? "\n\nActive vessels:\n" + string.Join("\n", ctx.TopVessels.Select(v => $"• {v}"))
            : "";

        return new AgentAnswer
        {
            Question  = q,
            Category  = "Vessel Risk",
            RiskLevel = risk,
            Answer    = $"**Vessel status overview:** {ctx.VesselsInPort} vessel(s) in port, {ctx.VesselsAtAnchor} at anchor, {ctx.VesselsDelayed} delayed.\n\n" +
                        (ctx.VesselsDelayed > 0
                            ? $"DELAY RISK IS {risk.ToUpper()}. Delayed vessels create downstream berth scheduling pressure and may cause congestion at pilot boarding areas."
                            : "No significant vessel delay risk at this time. Berth schedule appears stable.") +
                        vesselList,
            RelatedActions = actions,
            DataPoints = new() {
                $"In port: {ctx.VesselsInPort}",
                $"At anchor: {ctx.VesselsAtAnchor}",
                $"Delayed: {ctx.VesselsDelayed}",
                $"Avg turnaround: {ctx.AverageTurnaroundHours}h",
                $"Berth utilisation: {ctx.BerthUtilisationPct}%"
            }
        };
    }

    private AgentAnswer AnswerPriority(string q, OperationalContext ctx)
    {
        var priorities = new List<string>();
        var actions    = new List<string>();
        var risk       = "medium";
        int p = 1;

        if (ctx.CriticalDisruptions > 0) { priorities.Add($"P{p++}. 🔴 CRITICAL: Address {ctx.CriticalDisruptions} critical disruption(s) immediately — these are the highest-impact items"); risk = "critical"; }
        if (ctx.LoadSheddingActive)       { priorities.Add($"P{p++}. ⚡ Prioritise all cold-chain dispatches now — load-shedding is active"); }
        if (ctx.TrucksInQueue > 10)       { priorities.Add($"P{p++}. 🚛 Open additional gate lanes — {ctx.TrucksInQueue} trucks queued"); risk = risk == "medium" ? "high" : risk; }
        if (ctx.VesselsAtAnchor > 0)      { priorities.Add($"P{p++}. ⚓ Assign berth to {ctx.VesselsAtAnchor} vessel(s) at anchor"); }
        if (ctx.OpenIncidents > 0)        { priorities.Add($"P{p++}. ⚠️ Review {ctx.OpenIncidents} open incident(s) — check for overdue items"); }
        if (ctx.DwellAlerts > 0)          { priorities.Add($"P{p++}. 📦 Action {ctx.DwellAlerts} container dwell alert(s) — demurrage is accruing"); }
        if (ctx.HighRiskTrips > 0)        { priorities.Add($"P{p++}. 🛣️ Review {ctx.HighRiskTrips} high-risk dispatch trip(s) in Flow Intelligence"); }

        if (!priorities.Any()) priorities.Add("P1. ✅ No critical items. Continue standard monitoring. Review upcoming vessel ETAs and dispatch windows.");

        actions.Add("Open the Flow Intelligence module for detailed dispatch recommendations");
        actions.Add("Check AI Recommendations panel for pending system-generated actions");
        actions.Add("Review vessel schedule for next 6 hours");

        return new AgentAnswer
        {
            Question  = q,
            Category  = "Priority Actions",
            RiskLevel = risk,
            Answer    = $"**Recommended priority order for operations manager ({DateTime.UtcNow:HH:mm} SAST):**\n\n" +
                        string.Join("\n", priorities),
            RelatedActions = actions,
            DataPoints = new() {
                $"Critical disruptions: {ctx.CriticalDisruptions}",
                $"Open incidents: {ctx.OpenIncidents}",
                $"Trucks in queue: {ctx.TrucksInQueue}",
                $"Vessels at anchor: {ctx.VesselsAtAnchor}",
                $"Dwell alerts: {ctx.DwellAlerts}",
                $"High-risk trips: {ctx.HighRiskTrips}"
            }
        };
    }

    private AgentAnswer AnswerEmissions(string q, OperationalContext ctx)
    {
        var fuelCost    = ctx.TotalIdlingMinutesToday / 60m * 3.0m * 24.0m;
        var actions     = new List<string>();
        var risk        = ctx.EstimatedCo2Today > 50 ? "high" : ctx.EstimatedCo2Today > 20 ? "medium" : "low";

        actions.Add("Apply dispatch timing recommendations to reduce unnecessary waiting at gates and routes");
        actions.Add("Prioritise high-urgency loads to reduce total fleet dwell time");
        if (ctx.TrucksInQueue > 6)
            actions.Add($"Reduce gate queue from current {ctx.TrucksInQueue} trucks — each 10 min wait ≈ 0.5L diesel and 1.3kg CO2 per truck");
        actions.Add("Review route options during active congestion windows to reduce idling exposure");

        return new AgentAnswer
        {
            Question  = q,
            Category  = "Emissions & Sustainability",
            RiskLevel = risk,
            Answer    = $"**Estimated emissions impact today (synthetic data, indicative only):**\n\n" +
                        $"• Total estimated idling: {ctx.TotalIdlingMinutesToday:F0} minutes across active fleet\n" +
                        $"• Estimated CO₂ from idling: {ctx.EstimatedCo2Today:F1} kg\n" +
                        $"• Estimated fuel cost from idling: R{fuelCost:F0}\n\n" +
                        $"Assumptions: 3.0 L/hour idle consumption, R24.00/L diesel, 2.68 kg CO₂ per litre.\n\n" +
                        "Applying optimised dispatch timing could reduce avoidable idling by an estimated 20–35%. " +
                        "These are synthetic estimates for demonstration purposes — actual measurements require GPS/telematics integration.",
            RelatedActions = actions,
            DataPoints = new() {
                $"Idling minutes today: {ctx.TotalIdlingMinutesToday:F0}",
                $"CO₂ estimate: {ctx.EstimatedCo2Today:F1} kg",
                $"Est. fuel cost: R{fuelCost:F0}",
                $"Trucks in queue: {ctx.TrucksInQueue}",
                $"Assumptions: synthetic/indicative only"
            }
        };
    }

    private AgentAnswer AnswerYard(string q, OperationalContext ctx)
    {
        var risk = ctx.YardOccupancyPct > 90 ? "critical" : ctx.YardOccupancyPct > 80 ? "high" : ctx.YardOccupancyPct > 65 ? "medium" : "low";
        var actions = new List<string>();

        if (ctx.DwellAlerts > 0)
            actions.Add($"Action {ctx.DwellAlerts} overdue container(s) — notify agents and escalate customs holds");
        if (ctx.YardOccupancyPct > 80)
            actions.Add("Yard density is high — consider stacking reallocation and early removal of cleared cargo");
        actions.Add("Review customs hold containers — releasing these will free premium yard space");

        return new AgentAnswer
        {
            Question  = q,
            Category  = "Yard & Container Operations",
            RiskLevel = risk,
            Answer    = $"**Yard occupancy: {ctx.YardOccupancyPct:F1}%** (Risk: {risk.ToUpper()}).\n\n" +
                        $"Containers in yard: {ctx.ContainersInYard}. Dwell time alerts: {ctx.DwellAlerts}.\n\n" +
                        (ctx.YardOccupancyPct > 85
                            ? "Yard is NEAR CAPACITY. Dwell-time containers are consuming premium space. Immediate agent notification and customs escalation recommended."
                            : "Yard capacity is within acceptable range. Monitor dwell alerts and ensure free-time containers are collected before escalation."),
            RelatedActions = actions,
            DataPoints = new() {
                $"Occupancy: {ctx.YardOccupancyPct:F1}%",
                $"Containers in yard: {ctx.ContainersInYard}",
                $"Dwell alerts: {ctx.DwellAlerts}"
            }
        };
    }

    private AgentAnswer AnswerBerthUtil(string q, OperationalContext ctx) =>
        AnswerBerths(q, ctx);

    private AgentAnswer AnswerDisruptions(string q, OperationalContext ctx)
    {
        var risk = ctx.CriticalDisruptions > 0 ? "critical" : ctx.ActiveDisruptions > 2 ? "high" : ctx.ActiveDisruptions > 0 ? "medium" : "low";
        var body = ctx.ActiveDisruptions > 0
            ? $"**{ctx.ActiveDisruptions} active disruption(s) in the network:**\n\n" +
              string.Join("\n", ctx.TopDisruptions.Select(d => $"• {d}")) +
              "\n\nSee the Disruption Response Centre for full details and affected routes."
            : "No active disruptions at this time. All corridors and terminal operations are reporting normal status.";

        return new AgentAnswer
        {
            Question  = q,
            Category  = "Disruption Status",
            RiskLevel = risk,
            Answer    = body,
            RelatedActions = new() {
                "Open Disruption Response Centre for full event details",
                "Check Flow Intelligence for disruption-linked recommendations"
            },
            DataPoints = new() {
                $"Active disruptions: {ctx.ActiveDisruptions}",
                $"Critical: {ctx.CriticalDisruptions}",
                $"Load-shedding: {ctx.LoadSheddingActive}",
                $"Road congestion: {ctx.RoadCongestionActive}",
                $"Gate delay: {ctx.GateDelayActive}"
            }
        };
    }

    private AgentAnswer AnswerGeneral(string q, OperationalContext ctx)
    {
        var riskScore = 0;
        if (ctx.LoadSheddingActive)   riskScore += 25;
        if (ctx.RoadCongestionActive) riskScore += 20;
        if (ctx.GateDelayActive)      riskScore += 15;
        if (ctx.TrucksInQueue > 8)    riskScore += 15;
        if (ctx.VesselsAtAnchor > 0)  riskScore += 10;
        if (ctx.BerthUtilisationPct > 80) riskScore += 15;
        var riskLevel = riskScore > 70 ? "critical" : riskScore > 50 ? "high" : riskScore > 30 ? "medium" : "low";

        return new AgentAnswer
        {
            Question  = q,
            Category  = "General Operations",
            RiskLevel = riskLevel,
            Answer    = $"**SmartPort AI Agent — Operational Summary ({DateTime.UtcNow:HH:mm} SAST)**\n\n" +
                        $"Overall risk level: **{riskLevel.ToUpper()}** (score: {riskScore}/100)\n\n" +
                        $"• Vessels in port: {ctx.VesselsInPort} | Delayed: {ctx.VesselsDelayed} | At anchor: {ctx.VesselsAtAnchor}\n" +
                        $"• Berth utilisation: {ctx.BerthUtilisationPct}% ({ctx.BerthsOccupied} occupied, {ctx.BerthsAvailable} available)\n" +
                        $"• Yard occupancy: {ctx.YardOccupancyPct:F1}% | Dwell alerts: {ctx.DwellAlerts}\n" +
                        $"• Gate queue: {ctx.TrucksInQueue} trucks | Gates operational: {ctx.GatesOperational}\n" +
                        $"• Active disruptions: {ctx.ActiveDisruptions} | Open incidents: {ctx.OpenIncidents}\n" +
                        $"• Estimated idling today: {ctx.TotalIdlingMinutesToday:F0} min | CO₂: {ctx.EstimatedCo2Today:F1} kg\n\n" +
                        "Try asking a specific question from the suggestions below for more detailed analysis.",
            RelatedActions = new() {
                "Ask about congestion, berths, load-shedding, or emissions for detailed guidance",
                "Open the Scenario Simulator to model what-if situations"
            },
            DataPoints = new() { $"Overall risk score: {riskScore}/100" }
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static bool Contains(string text, params string[] terms) =>
        terms.Any(t => System.Text.RegularExpressions.Regex.IsMatch(text, t));

}
