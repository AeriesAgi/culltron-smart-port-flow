using SmartPort.Domain.Enums;

namespace SmartPort.Domain.Entities;

/// <summary>
/// Tracks a compliance document in the port workflow.
/// </summary>
public class Document : BaseEntity
{
    public string DocumentNumber { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; }
    public ComplianceStatus ComplianceStatus { get; set; }

    // ─── Association ─────────────────────────────────────────────────────
    public int? VesselId { get; set; }
    public int? CargoRecordId { get; set; }
    public string? ContainerNumber { get; set; }

    // ─── Metadata ────────────────────────────────────────────────────────
    public string Title { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }

    // ─── Dates ───────────────────────────────────────────────────────────
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? RequiredByDate { get; set; }

    public bool IsExpired => ExpiryDate.HasValue && DateTime.UtcNow > ExpiryDate;
    public bool IsOverdue => RequiredByDate.HasValue && DateTime.UtcNow > RequiredByDate
                             && Status != DocumentStatus.Approved;

    // ─── File / Storage ──────────────────────────────────────────────────
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ContentType { get; set; }

    public string? Notes { get; set; }

    // ─── Navigation ──────────────────────────────────────────────────────
    public virtual Vessel? Vessel { get; set; }
    public virtual CargoRecord? CargoRecord { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a vessel's scheduled visit to the port (from shipping line schedule).
/// </summary>
public class VesselScheduleVisit : BaseEntity
{
    public int VesselId { get; set; }
    public string ServiceName { get; set; } = string.Empty;    // e.g. MSC Saracen Service
    public string LoopCode { get; set; } = string.Empty;
    public string VoyageNumber { get; set; } = string.Empty;
    public int? BerthId { get; set; }

    public DateTime EstimatedArrival { get; set; }
    public DateTime EstimatedDeparture { get; set; }
    public DateTime? ActualArrival { get; set; }
    public DateTime? ActualDeparture { get; set; }
    public DateTime? WindowStart { get; set; }
    public DateTime? WindowEnd { get; set; }

    public int PlannedDischargeCount { get; set; }
    public int PlannedLoadCount { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }

    public virtual Vessel Vessel { get; set; } = null!;
    public virtual Berth? Berth { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Persists computed operational metrics for analytics and trend analysis.
/// </summary>
public class OperationalMetric : BaseEntity
{
    public DateTime MetricDate { get; set; }
    public string MetricType { get; set; } = string.Empty;     // e.g. Throughput, Turnaround, etc.
    public string Terminal { get; set; } = string.Empty;
    public string? BerthCode { get; set; }
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;           // TEU, Hours, Count, Percent
    public string? Dimension { get; set; }                     // e.g. VesselType, Direction
    public string? DimensionValue { get; set; }
    public string? Source { get; set; }                        // Calculated, Manual, AIS
    public string? Notes { get; set; }
}
