using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartPort.Application.Interfaces;
using SmartPort.Domain.Enums;
using SmartPort.Infrastructure.Persistence;

namespace SmartPort.Infrastructure.Services;

// ─── Scenario Input ───────────────────────────────────────────────────────────

public class ScenarioInput
{
    public string ScenarioName { get; set; } = "Custom Scenario";
    public bool LoadSheddingAt16 { get; set; }
    public int LoadSheddingStage { get; set; } = 0;
    public int GateCapacityDropPercent { get; set; } = 0;
    public int VesselEtaSlipHours { get; set; } = 0;
    public int BerthOccupancyIncreasePercent { get; set; } = 0;
    public int CraneAvailabilityDropPercent { get; set; } = 0;
    public int TruckArrivalSpikePercent { get; set; } = 0;
    public int ContainerBacklogIncreasePercent { get; set; } = 0;
    public string TimeOfDay { get; set; } = "Current";
}

// ─── Scenario Result ──────────────────────────────────────────────────────────

public class ScenarioResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public DateTime SimulatedAt { get; set; } = DateTime.UtcNow;

    // Risk scores 0-100
    public int OverallCongestionRisk { get; set; }
    public int GateDelayRisk { get; set; }
    public int BerthDelayRisk { get; set; }
    public int EnergyDisruptionRisk { get; set; }
    public int VesselRisk { get; set; }
    public int YardPressureRisk { get; set; }

    // Risk levels
    public string OverallRiskLevel { get; set; } = "Low";
    public string GateRiskLevel { get; set; } = "Low";
    public string BerthRiskLevel { get; set; } = "Low";
    public string EnergyRiskLevel { get; set; } = "Low";

    // Impact estimates
    public decimal EstimatedWaitingTimeMinutes { get; set; }
    public decimal EstimatedIdlingMinutes { get; set; }
    public decimal EstimatedDieselLitres { get; set; }
    public decimal EstimatedFuelCostRand { get; set; }
    public decimal EstimatedCo2Kg { get; set; }
    public decimal BaselineWaitingMinutes { get; set; }
    public decimal WaitingTimeIncreasePct { get; set; }

    // Explanations
    public List<string> RiskFactors { get; set; } = new();
    public List<ScenarioRecommendation> Recommendations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    // Colour helpers
    public string OverallRiskColour => OverallCongestionRisk switch { > 80 => "danger", > 60 => "high", > 30 => "warning", _ => "success" };
    public string GateRiskColour    => GateDelayRisk    switch { > 80 => "danger", > 60 => "high", > 30 => "warning", _ => "success" };
    public string BerthRiskColour   => BerthDelayRisk   switch { > 80 => "danger", > 60 => "high", > 30 => "warning", _ => "success" };
    public string EnergyRiskColour  => EnergyDisruptionRisk switch { > 80 => "danger", > 60 => "high", > 30 => "warning", _ => "success" };
}

public class ScenarioRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ExpectedImpact { get; set; } = string.Empty;
    public string Urgency { get; set; } = "Medium";
    public string UrgencyColour => Urgency switch { "Critical" => "danger", "High" => "high", "Medium" => "warning", _ => "success" };
}

// ─── Preset Scenarios ─────────────────────────────────────────────────────────

public static class PresetScenarios
{
    public static ScenarioInput DurbanHighCongestion => new()
    {
        ScenarioName = "Durban High Congestion Day",
        LoadSheddingAt16 = true, LoadSheddingStage = 3,
        GateCapacityDropPercent = 20,
        VesselEtaSlipHours = 2,
        BerthOccupancyIncreasePercent = 15,
        TruckArrivalSpikePercent = 40,
        ContainerBacklogIncreasePercent = 20,
        TimeOfDay = "Peak (07:00-09:00)"
    };

    public static ScenarioInput AfricanPortsHighSim => new()
    {
        ScenarioName = "African Ports Network: High Congestion Simulation",
        LoadSheddingAt16 = true, LoadSheddingStage = 4,
        GateCapacityDropPercent = 30,
        VesselEtaSlipHours = 3,
        BerthOccupancyIncreasePercent = 25,
        CraneAvailabilityDropPercent = 20,
        TruckArrivalSpikePercent = 50,
        ContainerBacklogIncreasePercent = 35,
        TimeOfDay = "Peak (16:00-18:00)"
    };

    public static ScenarioInput LoadSheddingOnly => new()
    {
        ScenarioName = "Load-Shedding Stage 4 at 16:00",
        LoadSheddingAt16 = true, LoadSheddingStage = 4,
        TimeOfDay = "Afternoon"
    };

    public static ScenarioInput GateCapacityDrop => new()
    {
        ScenarioName = "Gate Capacity Drops 30%",
        GateCapacityDropPercent = 30,
        TruckArrivalSpikePercent = 20,
        TimeOfDay = "Current"
    };
}

// ─── Interface ────────────────────────────────────────────────────────────────

public interface IScenarioSimulatorService
{
    Task<ScenarioResult> SimulateAsync(ScenarioInput input);
    List<ScenarioInput> GetPresetScenarios();
}

// ─── Implementation ───────────────────────────────────────────────────────────

public class ScenarioSimulatorService : IScenarioSimulatorService
{
    private readonly SmartPortDbContext _db;
    private readonly FlowIntelligenceSettings _settings;

    public ScenarioSimulatorService(SmartPortDbContext db, IOptions<FlowIntelligenceSettings> settings)
    {
        _db = db; _settings = settings.Value;
    }

    public List<ScenarioInput> GetPresetScenarios() => new()
    {
        PresetScenarios.DurbanHighCongestion,
        PresetScenarios.AfricanPortsHighSim,
        PresetScenarios.LoadSheddingOnly,
        PresetScenarios.GateCapacityDrop,
        new() { ScenarioName = "Vessel ETA Slip +3 Hours", VesselEtaSlipHours = 3, BerthOccupancyIncreasePercent = 10 },
        new() { ScenarioName = "Crane Availability -20%",  CraneAvailabilityDropPercent = 20, VesselEtaSlipHours = 1 },
        new() { ScenarioName = "Container Backlog +35%",   ContainerBacklogIncreasePercent = 35, TruckArrivalSpikePercent = 15 },
    };

    public async Task<ScenarioResult> SimulateAsync(ScenarioInput input)
    {
        // Read current operational baseline
        var vessels    = await _db.Vessels.Where(v => !v.IsDeleted).ToListAsync();
        var berths     = await _db.Berths.Where(b => !b.IsDeleted).ToListAsync();
        var gates      = await _db.Gates.Where(g => !g.IsDeleted).ToListAsync();
        var containers = await _db.Containers.Where(c => !c.IsDeleted).ToListAsync();
        var disrupts   = await _db.DisruptionEvents.Where(d => d.IsActive && !d.IsDeleted).ToListAsync();

        // Baseline values
        var baseGateQueue       = gates.Sum(g => g.CurrentQueueCount);
        var baseBerthUtil       = berths.Count > 0 ? (decimal)berths.Count(b => b.Status == BerthStatus.Occupied) / berths.Count * 100 : 75m;
        var baseVesselsDelayed  = vessels.Count(v => v.DelayMinutes > 0);
        var baseYardPct         = 72m;
        var baseWaiting         = 68m;

        var result = new ScenarioResult { ScenarioName = input.ScenarioName, SimulatedAt = DateTime.UtcNow };
        var factors = new List<string>();
        var warnings = new List<string>();
        var recs = new List<ScenarioRecommendation>();

        // ── Simulate each variable ────────────────────────────────────────────

        int gateScore   = 0;
        int berthScore  = 0;
        int energyScore = 0;
        int vesselScore = 0;
        int yardScore   = 0;

        // Gate capacity drop
        if (input.GateCapacityDropPercent > 0)
        {
            var effectiveCapDrop = input.GateCapacityDropPercent;
            gateScore += effectiveCapDrop;
            factors.Add($"Gate capacity reduced by {effectiveCapDrop}% — effective processing throughput decreases proportionally");
            if (effectiveCapDrop >= 30)
            {
                warnings.Add($"CRITICAL: Gate capacity at -{effectiveCapDrop}% will create severe backlogs during peak periods");
                recs.Add(new() { Title = "Activate Emergency Gate Procedures", Reason = $"Gate capacity at -{effectiveCapDrop}% is unsustainable during normal operations", ExpectedImpact = "Opens alternative processing lanes — estimated 40% backlog reduction", Urgency = "Critical" });
            }
        }

        // Truck arrival spike
        if (input.TruckArrivalSpikePercent > 0)
        {
            var spikePts = (int)(input.TruckArrivalSpikePercent * 0.6);
            gateScore += spikePts;
            factors.Add($"Truck arrivals up {input.TruckArrivalSpikePercent}% — gate queue will grow to approximately {(int)(baseGateQueue * (1 + input.TruckArrivalSpikePercent / 100.0))} vehicles");
            recs.Add(new() { Title = "Implement Appointment Smoothing", Reason = $"Truck spike of +{input.TruckArrivalSpikePercent}% will overwhelm gate processing capacity", ExpectedImpact = "Redistributes arrivals — reduces peak queue by an estimated 25-35%", Urgency = input.TruckArrivalSpikePercent > 30 ? "High" : "Medium" });
        }

        // Vessel ETA slip
        if (input.VesselEtaSlipHours > 0)
        {
            vesselScore += input.VesselEtaSlipHours * 12;
            berthScore  += input.VesselEtaSlipHours * 8;
            factors.Add($"Vessel ETA slipping {input.VesselEtaSlipHours}h — berth schedule disrupted, pilot/tug re-allocation required");
            recs.Add(new() { Title = "Notify Berth Planner and Pilot Services", Reason = $"ETA slip of {input.VesselEtaSlipHours}h cascades into berth window conflicts", ExpectedImpact = "Early notification prevents idle berth time and tug scheduling conflicts", Urgency = input.VesselEtaSlipHours >= 3 ? "High" : "Medium" });
        }

        // Berth occupancy increase
        if (input.BerthOccupancyIncreasePercent > 0)
        {
            berthScore += (int)(input.BerthOccupancyIncreasePercent * 1.2);
            var simBerthUtil = Math.Min(100m, baseBerthUtil + input.BerthOccupancyIncreasePercent);
            factors.Add($"Berth utilisation rises to {simBerthUtil:F0}% — anchor queue pressure increases");
            if (simBerthUtil > 90)
                warnings.Add($"CRITICAL: Berth utilisation at {simBerthUtil:F0}% — incoming vessels will experience extended anchorage waiting");
        }

        // Crane availability drop
        if (input.CraneAvailabilityDropPercent > 0)
        {
            berthScore  += (int)(input.CraneAvailabilityDropPercent * 0.8);
            vesselScore += (int)(input.CraneAvailabilityDropPercent * 0.6);
            factors.Add($"Crane availability at -{input.CraneAvailabilityDropPercent}% — cargo operations productivity reduced, turnaround hours increase");
            recs.Add(new() { Title = "Reallocate Available Cranes to Priority Vessels", Reason = $"Crane capacity down {input.CraneAvailabilityDropPercent}% — prioritise highest-impact vessels", ExpectedImpact = "Reduces turnaround time impact on priority calls by 20-30%", Urgency = "High" });
        }

        // Container backlog
        if (input.ContainerBacklogIncreasePercent > 0)
        {
            yardScore += (int)(input.ContainerBacklogIncreasePercent * 0.9);
            gateScore += (int)(input.ContainerBacklogIncreasePercent * 0.4);
            var simYard = Math.Min(100m, baseYardPct + input.ContainerBacklogIncreasePercent * 0.7m);
            factors.Add($"Container backlog +{input.ContainerBacklogIncreasePercent}% — yard density rises to approx {simYard:F0}%");
            if (simYard > 90)
                warnings.Add($"CRITICAL: Yard density approaching {simYard:F0}% — receiving operations may be suspended");
        }

        // Load shedding
        if (input.LoadSheddingAt16 || input.LoadSheddingStage > 0)
        {
            var stage = Math.Max(input.LoadSheddingStage, 1);
            energyScore = Math.Min(100, 20 + stage * 15);
            factors.Add($"Eskom Stage {stage} load-shedding active — cold-chain, reefer zones, and terminal equipment on backup power");
            if (stage >= 3)
            {
                warnings.Add($"HIGH: Stage {stage} load-shedding affects reefer plug capacity, OCR cameras, and gate kiosk systems");
                recs.Add(new() { Title = "Priority Cold-Chain Dispatch Now", Reason = $"Stage {stage} load-shedding will compromise reefer storage and refrigerated cargo integrity", ExpectedImpact = "Prevents temperature exceedance on all cold-chain cargo in transit", Urgency = stage >= 4 ? "Critical" : "High" });
                recs.Add(new() { Title = "Switch to Manual Gate Processing Backup", Reason = "OCR and RFID gate systems may lose power during shedding window", ExpectedImpact = "Maintains gate throughput at ~70% of normal capacity", Urgency = "High" });
            }
        }

        // Time of day modifier
        var timeScore = input.TimeOfDay switch {
            "Peak (07:00-09:00)" => 18,
            "Peak (16:00-18:00)" => 22,
            "Afternoon"          => 10,
            _ => 0
        };
        if (timeScore > 0)
            factors.Add($"Time of day ({input.TimeOfDay}) adds +{timeScore} pts to congestion risk");

        // ── Aggregate scores ──────────────────────────────────────────────────

        gateScore   = Math.Min(100, gateScore + timeScore);
        berthScore  = Math.Min(100, berthScore);
        energyScore = Math.Min(100, energyScore);
        vesselScore = Math.Min(100, vesselScore);
        yardScore   = Math.Min(100, yardScore);

        var overall = Math.Min(100, (gateScore + berthScore + energyScore + vesselScore + yardScore) / 4);

        // ── Waiting time estimate ─────────────────────────────────────────────

        var waitMultiplier  = 1.0m + (gateScore / 100.0m * 1.5m) + (berthScore / 100.0m * 0.8m) + (energyScore / 100.0m * 0.4m);
        var simWaiting      = Math.Round(baseWaiting * waitMultiplier, 1);
        var waitIncreasePct = Math.Round((simWaiting - baseWaiting) / baseWaiting * 100, 1);

        // ── Emissions estimate ────────────────────────────────────────────────

        var simTrucks       = baseGateQueue * (1 + input.TruckArrivalSpikePercent / 100.0m);
        var idlingMins      = simWaiting * simTrucks * 0.6m;
        var dieselL         = idlingMins / 60m * _settings.IdlingLitresPerHour;
        var fuelCost        = dieselL * _settings.DieselPricePerLitre;
        var co2             = dieselL * _settings.Co2KgPerLitreDiesel;

        // ── Populate result ───────────────────────────────────────────────────

        result.OverallCongestionRisk         = overall;
        result.GateDelayRisk                 = gateScore;
        result.BerthDelayRisk                = berthScore;
        result.EnergyDisruptionRisk          = energyScore;
        result.VesselRisk                    = vesselScore;
        result.YardPressureRisk              = yardScore;
        result.OverallRiskLevel              = ScoreToLevel(overall);
        result.GateRiskLevel                 = ScoreToLevel(gateScore);
        result.BerthRiskLevel                = ScoreToLevel(berthScore);
        result.EnergyRiskLevel               = ScoreToLevel(energyScore);
        result.EstimatedWaitingTimeMinutes   = simWaiting;
        result.BaselineWaitingMinutes        = baseWaiting;
        result.WaitingTimeIncreasePct        = waitIncreasePct;
        result.EstimatedIdlingMinutes        = Math.Round(idlingMins, 1);
        result.EstimatedDieselLitres         = Math.Round(dieselL, 2);
        result.EstimatedFuelCostRand         = Math.Round(fuelCost, 2);
        result.EstimatedCo2Kg               = Math.Round(co2, 2);
        result.RiskFactors                   = factors;
        result.Warnings                      = warnings;
        result.Recommendations               = recs.Any() ? recs : new List<ScenarioRecommendation> {
            new() { Title = "No Critical Actions Required", Reason = "Selected parameters are within manageable range", ExpectedImpact = "Continue standard monitoring", Urgency = "Low" }
        };

        return result;
    }

    private static string ScoreToLevel(int score) => score switch { > 80 => "Critical", > 60 => "High", > 30 => "Medium", _ => "Low" };
}
