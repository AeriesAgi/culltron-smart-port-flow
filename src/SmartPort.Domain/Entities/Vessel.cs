using SmartPort.Domain.Enums;

namespace SmartPort.Domain.Entities;

/// <summary>
/// Represents a vessel (ship) operating within or expected at the port.
/// </summary>
public class Vessel : BaseEntity
{
    // ─── Identity ────────────────────────────────────────────────────────
    public string IMONumber { get; set; } = string.Empty;     // e.g. IMO9321483
    public string MMSINumber { get; set; } = string.Empty;    // AIS identifier
    public string CallSign { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FlagCountry { get; set; } = string.Empty;
    public string FlagCode { get; set; } = string.Empty;      // ISO-3166 alpha-2
    public string PortOfRegistry { get; set; } = string.Empty;

    // ─── Classification ───────────────────────────────────────────────────
    public VesselType VesselType { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string ShippingLine { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;         // Port agent

    // ─── Physical ────────────────────────────────────────────────────────
    public decimal GrossTonnage { get; set; }
    public decimal DeadweightTonnage { get; set; }
    public decimal LengthOverall { get; set; }          // metres
    public decimal Beam { get; set; }                   // metres
    public decimal MaxDraught { get; set; }             // metres
    public int TEUCapacity { get; set; }                // for container ships
    public int YearBuilt { get; set; }

    // ─── Current State ───────────────────────────────────────────────────
    public VesselStatus Status { get; set; }
    public decimal? CurrentDraught { get; set; }
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public decimal? SpeedOverGround { get; set; }
    public string? LastPositionSource { get; set; }         // AIS, Manual, etc.
    public DateTime? LastPositionUpdated { get; set; }
    public string? NextDestination { get; set; }

    // ─── Arrival/Departure ───────────────────────────────────────────────
    public DateTime? EstimatedTimeOfArrival { get; set; }
    public DateTime? ActualTimeOfArrival { get; set; }
    public DateTime? EstimatedTimeOfDeparture { get; set; }
    public DateTime? ActualTimeOfDeparture { get; set; }
    public DateTime? RequestedBerthTime { get; set; }

    // ─── Port Service ────────────────────────────────────────────────────
    public string? VoyageNumber { get; set; }
    public string? PreviousPort { get; set; }
    public string? NextPort { get; set; }
    public bool PilotageRequired { get; set; } = true;
    public bool TugAssistanceRequired { get; set; } = false;
    public string? SpecialRequirements { get; set; }

    // ─── Metrics ─────────────────────────────────────────────────────────
    public decimal? TurnaroundHours { get; set; }
    public int? DelayMinutes { get; set; }
    public string? DelayReason { get; set; }

    // ─── Navigation ──────────────────────────────────────────────────────
    public virtual ICollection<BerthAssignment> BerthAssignments { get; set; } = new List<BerthAssignment>();
    public virtual ICollection<VesselScheduleVisit> ScheduleVisits { get; set; } = new List<VesselScheduleVisit>();
    public virtual ICollection<Container> Containers { get; set; } = new List<Container>();
    public virtual ICollection<CargoRecord> CargoRecords { get; set; } = new List<CargoRecord>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual ICollection<Incident> Incidents { get; set; } = new List<Incident>();

    // ─── Computed Helpers ────────────────────────────────────────────────
    public bool IsCurrentlyInPort =>
        Status is VesselStatus.Berthed or VesselStatus.CargoOperations or VesselStatus.BerthingInProgress;

    public string StatusDisplay => Status.ToString().Replace("InProgress", " In Progress");
    public string TypeDisplay => VesselType.ToString().Replace("Tanker", "Tanker ");
}
