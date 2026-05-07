using SmartPort.Domain.Enums;

namespace SmartPort.Domain.Entities;

/// <summary>
/// Represents a single intermodal container being tracked through the port.
/// </summary>
public class Container : BaseEntity
{
    // ─── Identity ────────────────────────────────────────────────────────
    public string ContainerNumber { get; set; } = string.Empty;   // ISO 6346  e.g. MAEU2345678
    public ContainerSize Size { get; set; }
    public ContainerType ContainerType { get; set; }
    public ContainerDirection Direction { get; set; }
    public ContainerStatus Status { get; set; }

    // ─── Assignment ──────────────────────────────────────────────────────
    public int? VesselId { get; set; }
    public string? VoyageNumber { get; set; }
    public int? YardBlockId { get; set; }
    public string? YardSlot { get; set; }   // e.g. A01-03-4  (row-bay-tier)

    // ─── Cargo ───────────────────────────────────────────────────────────
    public string? ShippingLine { get; set; }
    public string? PortOfLoading { get; set; }
    public string? PortOfDischarge { get; set; }
    public string? FinalDestination { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public bool IsHazardous { get; set; }
    public string? HazardClass { get; set; }      // IMDG class
    public bool IsReefer { get; set; }
    public decimal? ReeferSetTemp { get; set; }

    // ─── Customs ─────────────────────────────────────────────────────────
    public string? CustomsReferenceNumber { get; set; }
    public ComplianceStatus CustomsStatus { get; set; }
    public bool IsOnHold { get; set; }
    public string? HoldReason { get; set; }

    // ─── Timeline ────────────────────────────────────────────────────────
    public DateTime? GateInDateTime { get; set; }
    public DateTime? GateOutDateTime { get; set; }
    public DateTime? LoadedOnVesselDateTime { get; set; }
    public DateTime? DischargedFromVesselDateTime { get; set; }

    // ─── Dwell ───────────────────────────────────────────────────────────
    public decimal? DwellTimeHours { get; set; }
    public decimal? FreeTimeLimitHours { get; set; }
    public bool IsDwellAlertRaised { get; set; }

    // ─── Navigation ──────────────────────────────────────────────────────
    public virtual Vessel? Vessel { get; set; }
    public virtual YardBlock? YardBlock { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a yard block (storage zone) within a terminal.
/// </summary>
public class YardBlock : BaseEntity
{
    public string BlockCode { get; set; } = string.Empty;    // e.g. A, B, C / CT01
    public string Terminal { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;         // e.g. Import, Export, Reefer

    public int Rows { get; set; }
    public int Bays { get; set; }
    public int Tiers { get; set; }
    public int TotalCapacityTEU { get; set; }
    public int CurrentOccupancyTEU { get; set; }
    public bool IsReeferBlock { get; set; }
    public bool IsHazardousBlock { get; set; }
    public bool IsActive { get; set; } = true;

    public decimal OccupancyPercent =>
        TotalCapacityTEU > 0
            ? Math.Round((decimal)CurrentOccupancyTEU / TotalCapacityTEU * 100, 1)
            : 0;

    public bool IsNearCapacity => OccupancyPercent >= 85;
    public bool IsCritical => OccupancyPercent >= 95;

    public virtual ICollection<Container> Containers { get; set; } = new List<Container>();
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a cargo record tied to a vessel call — the manifest line level.
/// </summary>
public class CargoRecord : BaseEntity
{
    public int VesselId { get; set; }
    public string VoyageNumber { get; set; } = string.Empty;

    public CargoCategory Category { get; set; }
    public CargoStatus Status { get; set; }
    public ContainerDirection Direction { get; set; }

    public string BillOfLadingNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Consignee { get; set; } = string.Empty;
    public string Shipper { get; set; } = string.Empty;
    public string PortOfLoading { get; set; } = string.Empty;
    public string PortOfDischarge { get; set; } = string.Empty;

    public decimal GrossWeightKg { get; set; }
    public decimal VolumeM3 { get; set; }
    public int TEUCount { get; set; }
    public bool IsHazardous { get; set; }
    public string? HazardClass { get; set; }

    public string? CustomsReferenceNumber { get; set; }
    public ComplianceStatus CustomsStatus { get; set; }

    public string? Notes { get; set; }

    public virtual Vessel Vessel { get; set; } = null!;
}
