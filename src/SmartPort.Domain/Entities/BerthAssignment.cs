using SmartPort.Domain.Enums;

namespace SmartPort.Domain.Entities;

/// <summary>
/// Records a single berth allocation: one vessel at one berth for one call.
/// </summary>
public class BerthAssignment : BaseEntity
{
    public int VesselId { get; set; }
    public int BerthId { get; set; }

    // ─── Planned Window ──────────────────────────────────────────────────
    public DateTime PlannedArrival { get; set; }
    public DateTime PlannedDeparture { get; set; }

    // ─── Actuals ─────────────────────────────────────────────────────────
    public DateTime? ActualArrival { get; set; }
    public DateTime? ActualDeparture { get; set; }

    // ─── Operations ──────────────────────────────────────────────────────
    public string? BollardFrom { get; set; }
    public string? BollardTo { get; set; }
    public bool CargoPlanApproved { get; set; }
    public string? AssignedPilot { get; set; }
    public string? AssignedTugs { get; set; }
    public string? GangsAllocated { get; set; }       // stevedore gangs

    // ─── Cargo Ops Targets ───────────────────────────────────────────────
    public int PlannedDischarge { get; set; }          // TEU or tonnes
    public int PlannedLoad { get; set; }
    public int ActualDischarge { get; set; }
    public int ActualLoad { get; set; }

    // ─── Status / Delay ──────────────────────────────────────────────────
    public VesselStatus OperationalStatus { get; set; }
    public int? DelayMinutes { get; set; }
    public string? DelayCategory { get; set; }
    public string? Notes { get; set; }

    // ─── Computed ────────────────────────────────────────────────────────
    public decimal? PlannedDurationHours =>
        (decimal)(PlannedDeparture - PlannedArrival).TotalHours;

    public decimal? ActualDurationHours =>
        (ActualDeparture.HasValue && ActualArrival.HasValue)
            ? (decimal)(ActualDeparture.Value - ActualArrival.Value).TotalHours
            : null;

    public bool HasConflict { get; set; }

    // ─── Navigation ──────────────────────────────────────────────────────
    public virtual Vessel Vessel { get; set; } = null!;
    public virtual Berth Berth { get; set; } = null!;
}
