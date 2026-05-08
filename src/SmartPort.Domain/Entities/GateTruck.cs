using SmartPort.Domain.Enums;

namespace SmartPort.Domain.Entities;

/// <summary>
/// Represents a physical gate at the port perimeter for landside access.
/// </summary>
public class Gate : BaseEntity
{
    public string Code { get; set; } = string.Empty;        // e.g. G1, G2, MAINGATE
    public string Name { get; set; } = string.Empty;
    public string Terminal { get; set; } = string.Empty;
    public bool IsOperational { get; set; } = true;
    public bool IsEntryGate { get; set; }
    public bool IsExitGate { get; set; }
    public int LaneCount { get; set; }
    public bool HasWeighbridge { get; set; }
    public bool HasOCR { get; set; }                        // OCR camera for plate/container
    public bool HasRFID { get; set; }
    public int CurrentQueueCount { get; set; }
    public int AverageProcessingMinutes { get; set; }
    public DateTime? LastQueueUpdated { get; set; }

    public virtual ICollection<GateTransaction> Transactions { get; set; } = new List<GateTransaction>();
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a truck booked or arriving at the port for cargo operations.
/// </summary>
public class Truck : BaseEntity
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? FleetNumber { get; set; }
    public string TransporterName { get; set; } = string.Empty;
    public string TransporterCode { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DriverIDNumber { get; set; } = string.Empty;
    public string? DriverContactNumber { get; set; }

    public TruckStatus Status { get; set; }

    // ─── Booking ────────────────────────────────────────────────────────
    public string? BookingReference { get; set; }
    public DateTime? AppointmentDateTime { get; set; }
    public string? TargetContainerNumber { get; set; }
    public string? TargetYardBlock { get; set; }
    public ContainerDirection? TruckOperation { get; set; }

    // ─── Timing ─────────────────────────────────────────────────────────
    public DateTime? EstimatedArrivalTime { get; set; }
    public DateTime? ActualArrivalTime { get; set; }
    public DateTime? GateInTime { get; set; }
    public DateTime? GateOutTime { get; set; }
    public decimal? PortDwellMinutes { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<GateTransaction> GateTransactions { get; set; } = new List<GateTransaction>();
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Records an individual gate passage event for a truck.
/// </summary>
public class GateTransaction : BaseEntity
{
    public int TruckId { get; set; }
    public int GateId { get; set; }

    public GateTransactionType TransactionType { get; set; }
    public GateTransactionStatus Status { get; set; }
    public DateTime TransactionTime { get; set; }

    public string? ContainerNumber { get; set; }
    public decimal? VehicleWeightKg { get; set; }
    public decimal? TareWeightKg { get; set; }
    public string? BookingReference { get; set; }
    public string? CustomsStatus { get; set; }
    public bool DocumentsVerified { get; set; }
    public string? LaneName { get; set; }
    public string? OperatorId { get; set; }
    public string? Notes { get; set; }
    public string? ExceptionReason { get; set; }

    public virtual Truck Truck { get; set; } = null!;
    public virtual Gate Gate { get; set; } = null!;
}
