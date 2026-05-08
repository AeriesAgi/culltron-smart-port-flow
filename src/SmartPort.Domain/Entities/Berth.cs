using SmartPort.Domain.Enums;

namespace SmartPort.Domain.Entities;

/// <summary>
/// Represents a physical berth (quay position) within the port terminal.
/// </summary>
public class Berth : BaseEntity
{
    public string Code { get; set; } = string.Empty;       // e.g. B-01, T2-04
    public string Name { get; set; } = string.Empty;
    public string Terminal { get; set; } = string.Empty;   // e.g. Durban Container Terminal

    public BerthType BerthType { get; set; }
    public BerthStatus Status { get; set; }

    // ─── Physical Specs ──────────────────────────────────────────────────
    public decimal Length { get; set; }             // metres alongside
    public decimal MaxDraught { get; set; }         // max vessel draught
    public decimal MaxLOA { get; set; }             // max length overall
    public int MaxTEUPerCall { get; set; }          // for container berths
    public bool HasReeferPlugs { get; set; }
    public int ReeferPlugCount { get; set; }
    public bool HasShorepower { get; set; }
    public bool HasCranes { get; set; }
    public int CraneCount { get; set; }

    // ─── Location ────────────────────────────────────────────────────────
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string QuayDescription { get; set; } = string.Empty;

    // ─── Maintenance ─────────────────────────────────────────────────────
    public DateTime? MaintenanceStartDate { get; set; }
    public DateTime? MaintenanceEndDate { get; set; }
    public string? MaintenanceNotes { get; set; }

    // ─── Utilisation Metrics (rolling) ──────────────────────────────────
    public decimal UtilisationPercent30Day { get; set; }
    public decimal AverageTurnaroundHours { get; set; }

    // ─── Navigation ──────────────────────────────────────────────────────
    public virtual ICollection<BerthAssignment> Assignments { get; set; } = new List<BerthAssignment>();

    public bool IsAvailable => Status == BerthStatus.Available;
}
