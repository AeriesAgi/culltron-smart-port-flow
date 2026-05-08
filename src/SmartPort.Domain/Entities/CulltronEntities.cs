using SmartPort.Domain.Enums;

namespace SmartPort.Domain.Entities;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Represents a logistics company, haulier, depot, municipality, or partner
/// organisation using the Culltron Smart Port Flow platform.
/// </summary>
public class Organisation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public OrganisationType OrganisationType { get; set; }
    public string? RegistrationNumber { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string Province { get; set; } = "KwaZulu-Natal";
    public bool IsActive { get; set; } = true;

    public virtual ICollection<FleetVehicle> FleetVehicles { get; set; } = new List<FleetVehicle>();
    public virtual ICollection<Driver> Drivers { get; set; } = new List<Driver>();
    public virtual ICollection<DispatchTrip> DispatchTrips { get; set; } = new List<DispatchTrip>();
    public virtual ICollection<FlowRecommendation> FlowRecommendations { get; set; } = new List<FlowRecommendation>();
    public virtual ICollection<PilotMetricSnapshot> PilotMetrics { get; set; } = new List<PilotMetricSnapshot>();
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Represents a freight vehicle in a logistics organisation's fleet.
/// This is distinct from the gate-management Truck entity — it is used for
/// dispatch planning, congestion risk scoring, and idling analysis.
/// </summary>
public class FleetVehicle : BaseEntity
{
    public int OrganisationId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? FleetNumber { get; set; }
    public FleetVehicleType VehicleType { get; set; }
    public FleetCargoType CargoType { get; set; }
    public decimal? CapacityTons { get; set; }
    public FleetVehicleStatus Status { get; set; }
    public string? CurrentLocation { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Organisation Organisation { get; set; } = null!;
    public virtual ICollection<DispatchTrip> DispatchTrips { get; set; } = new List<DispatchTrip>();
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Represents a truck driver associated with an organisation.
/// </summary>
public class Driver : BaseEntity
{
    public int OrganisationId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? LicenceNumber { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Organisation Organisation { get; set; } = null!;
    public virtual ICollection<DispatchTrip> DispatchTrips { get; set; } = new List<DispatchTrip>();
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Represents a planned or active truck dispatch movement.
/// This is the core operational unit of the Flow Intelligence Engine.
/// </summary>
public class DispatchTrip : BaseEntity
{
    public int OrganisationId { get; set; }
    public int FleetVehicleId { get; set; }
    public int? DriverId { get; set; }

    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;

    public FleetCargoType CargoType { get; set; }
    public string? CargoDescription { get; set; }
    public TripUrgencyLevel UrgencyLevel { get; set; }

    public DateTime PlannedDispatchTime { get; set; }
    public DateTime? ActualDispatchTime { get; set; }
    public DateTime PlannedArrivalWindowStart { get; set; }
    public DateTime PlannedArrivalWindowEnd { get; set; }
    public DateTime? ActualArrivalTime { get; set; }
    public DateTime? GateInTime { get; set; }
    public DateTime? GateOutTime { get; set; }

    public TripStatus Status { get; set; }
    public string? Notes { get; set; }

    // ─── Computed ─────────────────────────────────────────────────────────
    public bool IsArrivalWindowMissed =>
        ActualArrivalTime.HasValue && ActualArrivalTime.Value > PlannedArrivalWindowEnd;

    public decimal? PortDwellMinutes =>
        (GateInTime.HasValue && GateOutTime.HasValue)
            ? (decimal)(GateOutTime.Value - GateInTime.Value).TotalMinutes
            : null;

    public virtual Organisation Organisation { get; set; } = null!;
    public virtual FleetVehicle FleetVehicle { get; set; } = null!;
    public virtual Driver? Driver { get; set; }
    public virtual ICollection<FlowRecommendation> FlowRecommendations { get; set; } = new List<FlowRecommendation>();
    public virtual IdlingEmissionEstimate? EmissionEstimate { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Represents an active operational disruption affecting port access routes,
/// gate operations, or dispatch corridors.
/// </summary>
public class DisruptionEvent : BaseEntity
{
    public DisruptionType DisruptionType { get; set; }
    public DisruptionSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AffectedLocation { get; set; }
    public string? AffectedRoute { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = string.Empty;

    public bool IsOngoing => IsActive && (EndTime == null || EndTime > DateTime.UtcNow);
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Represents a decision-support recommendation generated by the
/// Culltron Flow Intelligence Engine for a specific dispatch trip or
/// operational situation.
/// </summary>
public class FlowRecommendation : BaseEntity
{
    public int? DispatchTripId { get; set; }
    public int OrganisationId { get; set; }

    public FlowRecommendationType RecommendationType { get; set; }
    public FlowRiskLevel RiskLevel { get; set; }
    public FlowConfidenceLevel ConfidenceLevel { get; set; }

    public string RecommendationText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ExpectedBenefit { get; set; }
    public int? CongestionScore { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool? AcceptedByUser { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? UserFeedback { get; set; }

    public bool IsPending => AcceptedByUser == null;
    public string RiskColourClass => RiskLevel switch {
        FlowRiskLevel.Low      => "success",
        FlowRiskLevel.Medium   => "warning",
        FlowRiskLevel.High     => "high",
        FlowRiskLevel.Critical => "danger",
        _ => "muted"
    };

    public virtual DispatchTrip? DispatchTrip { get; set; }
    public virtual Organisation Organisation { get; set; } = null!;
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Stores estimated fuel and emissions impact for a dispatch trip,
/// based on configurable per-litre and CO2 assumptions.
/// </summary>
public class IdlingEmissionEstimate : BaseEntity
{
    public int DispatchTripId { get; set; }
    public decimal EstimatedIdlingMinutes { get; set; }
    public decimal EstimatedDieselLitres { get; set; }
    public decimal EstimatedFuelCost { get; set; }
    public decimal EstimatedCo2Kg { get; set; }
    public bool AvoidableIdlingFlag { get; set; }
    public string? CalculationNotes { get; set; }

    public virtual DispatchTrip DispatchTrip { get; set; } = null!;
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Records a point-in-time snapshot of pilot KPIs for a given period.
/// Used to compare baseline vs current vs target performance.
/// </summary>
public class PilotMetricSnapshot : BaseEntity
{
    public int? OrganisationId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;   // e.g. "June 2025 Week 1"
    public PilotMetricType MetricType { get; set; }

    public decimal AverageWaitingMinutes { get; set; }
    public decimal TotalIdlingMinutes { get; set; }
    public decimal EstimatedDieselLitres { get; set; }
    public decimal EstimatedFuelCost { get; set; }
    public decimal EstimatedCo2Kg { get; set; }
    public int MissedArrivalWindows { get; set; }
    public decimal DispatchReliabilityPercent { get; set; }
    public int RecommendationsGenerated { get; set; }
    public int HighRiskTrips { get; set; }
    public string? Notes { get; set; }

    public virtual Organisation? Organisation { get; set; }
}
