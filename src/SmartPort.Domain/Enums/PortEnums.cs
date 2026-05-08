namespace SmartPort.Domain.Enums;

// ─── Vessel ─────────────────────────────────────────────────────────────────

public enum VesselStatus
{
    Expected,       // AIS/schedule indicates inbound
    Approaching,    // Within 50 nm of port
    AtAnchor,       // Waiting for berth
    BerthingInProgress,
    Berthed,
    CargoOperations,
    Departing,
    Departed,
    Delayed,
    Diverted,
    Unknown
}

public enum VesselType
{
    ContainerShip,
    BulkCarrier,
    TankerCrude,
    TankerProduct,
    RoRo,           // Roll-on/Roll-off
    GeneralCargo,
    Multipurpose,
    Reefer,
    LNG,
    LPG,
    Passenger,
    NavyOther
}

// ─── Berth ──────────────────────────────────────────────────────────────────

public enum BerthStatus
{
    Available,
    Reserved,
    Occupied,
    UnderMaintenance,
    Restricted
}

public enum BerthType
{
    Container,
    BulkLiquid,
    BulkDry,
    RoRo,
    GeneralCargo,
    MultiPurpose,
    Passenger,
    Naval
}

// ─── Container ──────────────────────────────────────────────────────────────

public enum ContainerStatus
{
    Expected,
    GateIn,
    InYard,
    LoadingInProgress,
    OnVessel,
    DischargeInProgress,
    GateOut,
    OnHold,
    Damaged,
    Lost
}

public enum ContainerSize
{
    TEU20 = 20,
    FEU40 = 40,
    HC45 = 45
}

public enum ContainerType
{
    DryGeneral,
    Reefer,
    OpenTop,
    FlatRack,
    TankContainer,
    Hazardous,
    OverDimension
}

public enum ContainerDirection
{
    Import,
    Export,
    Transhipment,
    Empty
}

// ─── Cargo ──────────────────────────────────────────────────────────────────

public enum CargoCategory
{
    General,
    Bulk,
    Liquid,
    Hazardous,
    Refrigerated,
    HighValue,
    Oversized,
    Livestock
}

public enum CargoStatus
{
    Manifested,
    Loaded,
    InTransit,
    Discharged,
    InCustoms,
    Released,
    Held,
    Rejected
}

// ─── Gate / Truck ────────────────────────────────────────────────────────────

public enum TruckStatus
{
    Approaching,
    InQueue,
    AtGate,
    GateInComplete,
    InYard,
    Loading,
    Unloading,
    GateOutComplete,
    Departed,
    Rejected
}

public enum GateTransactionType
{
    Entry,
    Exit
}

public enum GateTransactionStatus
{
    Pending,
    Processing,
    Approved,
    Rejected,
    Exception
}

// ─── Incident / Alert ────────────────────────────────────────────────────────

public enum IncidentSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum IncidentStatus
{
    Open,
    Acknowledged,
    InProgress,
    Resolved,
    Closed,
    Escalated
}

public enum IncidentCategory
{
    Operational,
    Safety,
    Security,
    Environmental,
    Equipment,
    Cyber,
    Weather,
    Compliance,
    Other
}

public enum AlertType
{
    VesselDelay,
    BerthConflict,
    ContainerDwellExceeded,
    HazardousCargoAlert,
    TruckQueueCritical,
    DocumentOverdue,
    CustomsHold,
    WeatherAdvisory,
    EquipmentFailure,
    SecurityIncident,
    SystemAlert
}

public enum AlertStatus
{
    Active,
    Acknowledged,
    Resolved,
    Suppressed
}

// ─── Document / Compliance ───────────────────────────────────────────────────

public enum DocumentType
{
    BillOfLading,
    Manifest,
    CustomsDeclaration,
    PhytosanitaryCertificate,
    DangerousGoodsDeclaration,
    VesselClearance,
    PortStateControl,
    CargoRelease,
    ImportPermit,
    ExportPermit,
    InsuranceCertificate,
    PackingList,
    ProofOfDelivery
}

public enum DocumentStatus
{
    Required,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Expired,
    NotRequired
}

public enum ComplianceStatus
{
    Compliant,
    NonCompliant,
    PendingReview,
    Waived,
    Unknown
}

// ─── AI / Recommendation ────────────────────────────────────────────────────

public enum RecommendationType
{
    BerthReallocation,
    TruckQueueMitigation,
    ContainerPrioritisation,
    DelayEscalation,
    ResourceOptimisation,
    DocumentChase,
    MaintenanceWindow,
    AlertConsolidation
}

public enum RecommendationStatus
{
    Pending,
    Accepted,
    Dismissed,
    Applied,
    Expired
}

// ─── Culltron Smart Port Flow — Organisation ─────────────────────────────────

public enum OrganisationType
{
    LogisticsCompany,
    Haulier,
    Depot,
    Warehouse,
    PortOperator,
    Municipality,
    ResearchPartner,
    FunderViewer,
    Other
}

// ─── FleetVehicle ─────────────────────────────────────────────────────────────

public enum FleetVehicleType
{
    RigidTruck,
    ArticulatedTruck,
    Interlink,
    Flatbed,
    SideLoader,
    TankerTruck,
    ReeferTruck,
    LCV,
    Other
}

public enum FleetCargoType
{
    Container,
    Bulk,
    BreakBulk,
    ColdChain,
    Fuel,
    GeneralFreight,
    Other
}

public enum FleetVehicleStatus
{
    Available,
    Dispatched,
    Waiting,
    AtGate,
    Delayed,
    Completed,
    OutOfService
}

// ─── DispatchTrip ─────────────────────────────────────────────────────────────

public enum TripUrgencyLevel
{
    Low,
    Normal,
    High,
    Critical
}

public enum TripStatus
{
    Planned,
    RecommendedHold,
    ReadyForDispatch,
    Dispatched,
    Waiting,
    AtGate,
    Completed,
    Cancelled,
    Delayed
}

// ─── DisruptionEvent ──────────────────────────────────────────────────────────

public enum DisruptionType
{
    LoadShedding,
    GateDelay,
    RoadCongestion,
    YardDelay,
    Weather,
    SystemOutage,
    EquipmentFailure,
    ManualAlert,
    Other
}

public enum DisruptionSeverity
{
    Low,
    Medium,
    High,
    Critical
}

// ─── FlowRecommendation ───────────────────────────────────────────────────────

public enum FlowRecommendationType
{
    ReleaseNow,
    DelayDispatch,
    HoldAtDepot,
    PrioritiseCargo,
    MonitorOnly,
    EscalateDisruption,
    RerouteIfPossible
}

public enum FlowRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum FlowConfidenceLevel
{
    Low,
    Medium,
    High
}

// ─── PilotMetricSnapshot ──────────────────────────────────────────────────────

public enum PilotMetricType
{
    Baseline,
    Current,
    Target
}
