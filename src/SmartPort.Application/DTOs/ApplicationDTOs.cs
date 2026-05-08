using SmartPort.Domain.Enums;

namespace SmartPort.Application.Interfaces;

// ─── Pagination ───────────────────────────────────────────────────────────────

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

// ─── Dashboard DTOs ──────────────────────────────────────────────────────────

public class DashboardSummaryDto
{
    // Vessel counts
    public int VesselsInPort { get; set; }
    public int VesselsExpectedToday { get; set; }
    public int VesselsAtAnchor { get; set; }
    public int VesselsDelayed { get; set; }

    // Berth
    public int BerthsOccupied { get; set; }
    public int BerthsAvailable { get; set; }
    public int BerthsUnderMaintenance { get; set; }
    public decimal BerthUtilisationPercent { get; set; }

    // Container/Yard
    public int ContainersInYard { get; set; }
    public int DwellAlertContainers { get; set; }
    public decimal YardOccupancyPercent { get; set; }
    public int ThroughputTodayTEU { get; set; }

    // Gate/Truck
    public int TrucksInQueue { get; set; }
    public int TrucksInYard { get; set; }
    public int GatesOperational { get; set; }

    // Incidents/Alerts
    public int OpenIncidents { get; set; }
    public int CriticalIncidents { get; set; }
    public int ActiveAlerts { get; set; }
    public int PendingRecommendations { get; set; }

    // KPIs
    public decimal AverageTurnaroundHours { get; set; }
    public decimal CraneProductivity { get; set; }
    public decimal TruckTurnaroundMinutes { get; set; }

    // Lists for dashboard widgets
    public IEnumerable<VesselListDto> VesselsInPortList { get; set; } = [];
    public IEnumerable<VesselListDto> ArrivingVessels { get; set; } = [];
    public IEnumerable<AlertDto> ActiveAlertsList { get; set; } = [];
    public IEnumerable<RecommendationDto> TopRecommendations { get; set; } = [];
    public IEnumerable<BerthStatusDto> BerthStatuses { get; set; } = [];
    public IEnumerable<YardBlockStatusDto> YardBlockStatuses { get; set; } = [];
    public IEnumerable<KpiTrendDto> ThroughputTrend { get; set; } = [];
}

public class KpiTrendDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
}

// ─── Vessel DTOs ──────────────────────────────────────────────────────────────

public class VesselListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IMONumber { get; set; } = string.Empty;
    public string ShippingLine { get; set; } = string.Empty;
    public VesselType VesselType { get; set; }
    public VesselStatus Status { get; set; }
    public string FlagCode { get; set; } = string.Empty;
    public string? VoyageNumber { get; set; }
    public string? BerthCode { get; set; }
    public DateTime? ETA { get; set; }
    public DateTime? ETD { get; set; }
    public int? DelayMinutes { get; set; }
    public decimal LengthOverall { get; set; }
    public decimal MaxDraught { get; set; }
    public int? TEUCapacity { get; set; }
    public string Agent { get; set; } = string.Empty;
}

public class VesselDetailDto : VesselListDto
{
    public string MMSINumber { get; set; } = string.Empty;
    public string CallSign { get; set; } = string.Empty;
    public string FlagCountry { get; set; } = string.Empty;
    public string PortOfRegistry { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public decimal GrossTonnage { get; set; }
    public decimal DeadweightTonnage { get; set; }
    public decimal Beam { get; set; }
    public int YearBuilt { get; set; }
    public DateTime? ActualTimeOfArrival { get; set; }
    public DateTime? ActualTimeOfDeparture { get; set; }
    public decimal? TurnaroundHours { get; set; }
    public string? DelayReason { get; set; }
    public string? PreviousPort { get; set; }
    public string? NextPort { get; set; }
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public IEnumerable<BerthAssignmentDto> BerthAssignments { get; set; } = [];
    public IEnumerable<DocumentListDto> Documents { get; set; } = [];
    public IEnumerable<IncidentListDto> Incidents { get; set; } = [];
}

public class VesselFilterDto
{
    public VesselStatus? Status { get; set; }
    public VesselType? VesselType { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateVesselDto
{
    public string IMONumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShippingLine { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;
    public VesselType VesselType { get; set; }
    public decimal LengthOverall { get; set; }
    public decimal MaxDraught { get; set; }
    public decimal GrossTonnage { get; set; }
    public int TEUCapacity { get; set; }
    public string? VoyageNumber { get; set; }
    public DateTime? EstimatedTimeOfArrival { get; set; }
}

public class UpdateVesselDto : CreateVesselDto
{
    public VesselStatus Status { get; set; }
    public DateTime? EstimatedTimeOfDeparture { get; set; }
    public int? DelayMinutes { get; set; }
    public string? DelayReason { get; set; }
}

// ─── Berth DTOs ───────────────────────────────────────────────────────────────

public class BerthStatusDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Terminal { get; set; } = string.Empty;
    public BerthType BerthType { get; set; }
    public BerthStatus Status { get; set; }
    public decimal Length { get; set; }
    public decimal MaxDraught { get; set; }
    public string? CurrentVesselName { get; set; }
    public string? CurrentVesselIMO { get; set; }
    public DateTime? CurrentVesselETA { get; set; }
    public DateTime? CurrentVesselETD { get; set; }
    public decimal UtilisationPercent30Day { get; set; }
    public bool HasCranes { get; set; }
    public int CraneCount { get; set; }
}

public class BerthDetailDto : BerthStatusDto
{
    public decimal AverageTurnaroundHours { get; set; }
    public IEnumerable<BerthAssignmentDto> UpcomingAssignments { get; set; } = [];
    public IEnumerable<BerthAssignmentDto> RecentAssignments { get; set; } = [];
}

public class BerthOccupancyDto
{
    public int BerthId { get; set; }
    public string BerthCode { get; set; } = string.Empty;
    public IEnumerable<BerthAssignmentDto> Assignments { get; set; } = [];
}

public class BerthAssignmentDto
{
    public int Id { get; set; }
    public int BerthId { get; set; }
    public string BerthCode { get; set; } = string.Empty;
    public int VesselId { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string ShippingLine { get; set; } = string.Empty;
    public DateTime PlannedArrival { get; set; }
    public DateTime PlannedDeparture { get; set; }
    public DateTime? ActualArrival { get; set; }
    public DateTime? ActualDeparture { get; set; }
    public VesselStatus OperationalStatus { get; set; }
    public int PlannedDischarge { get; set; }
    public int PlannedLoad { get; set; }
    public int ActualDischarge { get; set; }
    public int ActualLoad { get; set; }
    public int? DelayMinutes { get; set; }
    public bool CargoPlanApproved { get; set; }
}

public class CreateBerthAssignmentDto
{
    public int VesselId { get; set; }
    public int BerthId { get; set; }
    public DateTime PlannedArrival { get; set; }
    public DateTime PlannedDeparture { get; set; }
    public int PlannedDischarge { get; set; }
    public int PlannedLoad { get; set; }
}

// ─── Container DTOs ───────────────────────────────────────────────────────────

public class ContainerListDto
{
    public int Id { get; set; }
    public string ContainerNumber { get; set; } = string.Empty;
    public ContainerSize Size { get; set; }
    public ContainerType ContainerType { get; set; }
    public ContainerDirection Direction { get; set; }
    public ContainerStatus Status { get; set; }
    public string? ShippingLine { get; set; }
    public string? YardSlot { get; set; }
    public string? YardBlockCode { get; set; }
    public decimal? DwellTimeHours { get; set; }
    public bool IsDwellAlertRaised { get; set; }
    public bool IsHazardous { get; set; }
    public bool IsReefer { get; set; }
    public bool IsOnHold { get; set; }
    public ComplianceStatus CustomsStatus { get; set; }
    public string? VesselName { get; set; }
}

public class ContainerDetailDto : ContainerListDto
{
    public string? PortOfLoading { get; set; }
    public string? PortOfDischarge { get; set; }
    public string? FinalDestination { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public string? HazardClass { get; set; }
    public decimal? ReeferSetTemp { get; set; }
    public string? CustomsReferenceNumber { get; set; }
    public string? HoldReason { get; set; }
    public DateTime? GateInDateTime { get; set; }
    public DateTime? GateOutDateTime { get; set; }
    public decimal? FreeTimeLimitHours { get; set; }
}

public class ContainerFilterDto
{
    public ContainerStatus? Status { get; set; }
    public ContainerDirection? Direction { get; set; }
    public bool? IsHazardous { get; set; }
    public bool? IsDwellAlert { get; set; }
    public bool? IsOnHold { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

// ─── Yard DTOs ────────────────────────────────────────────────────────────────

public class YardBlockStatusDto
{
    public int Id { get; set; }
    public string BlockCode { get; set; } = string.Empty;
    public string Terminal { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public int TotalCapacityTEU { get; set; }
    public int CurrentOccupancyTEU { get; set; }
    public decimal OccupancyPercent { get; set; }
    public bool IsReeferBlock { get; set; }
    public bool IsHazardousBlock { get; set; }
    public bool IsNearCapacity { get; set; }
    public bool IsCritical { get; set; }
}

public class YardBlockDetailDto : YardBlockStatusDto
{
    public int Rows { get; set; }
    public int Bays { get; set; }
    public int Tiers { get; set; }
    public IEnumerable<ContainerListDto> Containers { get; set; } = [];
}

// ─── Gate / Truck DTOs ───────────────────────────────────────────────────────

public class GateStatusDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsOperational { get; set; }
    public int CurrentQueueCount { get; set; }
    public int AverageProcessingMinutes { get; set; }
    public bool IsEntryGate { get; set; }
    public bool IsExitGate { get; set; }
    public int LaneCount { get; set; }
    public bool HasOCR { get; set; }
    public int EstimatedWaitMinutes => CurrentQueueCount * AverageProcessingMinutes / Math.Max(1, LaneCount);
}

public class TruckListDto
{
    public int Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string TransporterName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public TruckStatus Status { get; set; }
    public string? BookingReference { get; set; }
    public DateTime? AppointmentDateTime { get; set; }
    public string? TargetContainerNumber { get; set; }
    public DateTime? GateInTime { get; set; }
    public decimal? PortDwellMinutes { get; set; }
}

public class TruckFilterDto
{
    public TruckStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class GateTransactionDto
{
    public int Id { get; set; }
    public string GateCode { get; set; } = string.Empty;
    public string TruckRegistration { get; set; } = string.Empty;
    public string TransporterName { get; set; } = string.Empty;
    public GateTransactionType TransactionType { get; set; }
    public GateTransactionStatus Status { get; set; }
    public DateTime TransactionTime { get; set; }
    public string? ContainerNumber { get; set; }
    public bool DocumentsVerified { get; set; }
    public string? ExceptionReason { get; set; }
}

// ─── Incident DTOs ───────────────────────────────────────────────────────────

public class IncidentListDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public IncidentCategory Category { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public string? Location { get; set; }
    public string? Terminal { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? TargetResolutionTime { get; set; }
    public bool IsOverdue { get; set; }
    public string? VesselName { get; set; }
}

public class IncidentDetailDto : IncidentListDto
{
    public string Description { get; set; } = string.Empty;
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? RootCause { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? CorrectiveAction { get; set; }
    public IEnumerable<IncidentUpdateDto> Updates { get; set; } = [];
}

public class IncidentUpdateDto
{
    public string UpdatedBy { get; set; } = string.Empty;
    public IncidentStatus NewStatus { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateIncidentDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentCategory Category { get; set; }
    public IncidentSeverity Severity { get; set; }
    public int? VesselId { get; set; }
    public int? BerthId { get; set; }
    public string? Location { get; set; }
    public string? Terminal { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? TargetResolutionTime { get; set; }
}

public class UpdateIncidentDto
{
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public string? Notes { get; set; }
    public DateTime? TargetResolutionTime { get; set; }
}

public class ResolveIncidentDto
{
    public string RootCause { get; set; } = string.Empty;
    public string ResolutionNotes { get; set; } = string.Empty;
    public string? CorrectiveAction { get; set; }
    public bool RequiresFollowUp { get; set; }
}

public class IncidentFilterDto
{
    public IncidentStatus? Status { get; set; }
    public IncidentSeverity? Severity { get; set; }
    public IncidentCategory? Category { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ─── Alert DTOs ───────────────────────────────────────────────────────────────

public class AlertDto
{
    public int Id { get; set; }
    public AlertType AlertType { get; set; }
    public AlertStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
    public string? VesselName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool IsAutoGenerated { get; set; }
}

// ─── Document DTOs ───────────────────────────────────────────────────────────

public class DocumentListDto
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; }
    public ComplianceStatus ComplianceStatus { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public string? VesselName { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsExpired { get; set; }
}

public class DocumentDetailDto : DocumentListDto
{
    public string? ReviewedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? Notes { get; set; }
    public string? FileName { get; set; }
}

public class DocumentFilterDto
{
    public DocumentType? DocumentType { get; set; }
    public DocumentStatus? Status { get; set; }
    public int? VesselId { get; set; }
    public bool? IsOverdue { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateDocumentDto
{
    public DocumentType DocumentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public int? VesselId { get; set; }
    public string? ContainerNumber { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

// ─── Analytics DTOs ──────────────────────────────────────────────────────────

public class AnalyticsFilterDto
{
    public DateTime FromDate { get; set; } = DateTime.UtcNow.AddDays(-30);
    public DateTime ToDate { get; set; } = DateTime.UtcNow;
    public string Terminal { get; set; } = "Durban Container Terminal";
}

public class ThroughputAnalyticsDto
{
    public decimal TotalTEU { get; set; }
    public decimal AverageDailyTEU { get; set; }
    public decimal PeakDailyTEU { get; set; }
    public IEnumerable<KpiTrendDto> DailyTrend { get; set; } = [];
}

public class TurnaroundAnalyticsDto
{
    public decimal AverageTurnaroundHours { get; set; }
    public decimal MedianTurnaroundHours { get; set; }
    public decimal AverageCraneProductivity { get; set; }
    public IEnumerable<KpiTrendDto> DailyTrend { get; set; } = [];
}

public class BerthEfficiencyDto
{
    public decimal AverageUtilisationPercent { get; set; }
    public IEnumerable<BerthEfficiencyRowDto> ByBerth { get; set; } = [];
}

public class BerthEfficiencyRowDto
{
    public string BerthCode { get; set; } = string.Empty;
    public decimal UtilisationPercent { get; set; }
    public decimal AverageTurnaroundHours { get; set; }
    public int VesselCallCount { get; set; }
}

public class YardAnalyticsDto
{
    public decimal AverageYardDensity { get; set; }
    public decimal AverageDwellTimeHours { get; set; }
    public IEnumerable<KpiTrendDto> DailyDensityTrend { get; set; } = [];
}

// ─── Recommendation DTOs ──────────────────────────────────────────────────────

public class RecommendationDto
{
    public int Id { get; set; }
    public RecommendationType Type { get; set; }
    public RecommendationStatus Status { get; set; }
    public IncidentSeverity Priority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public string? ImpactEstimate { get; set; }
    public string? DetailedRationale { get; set; }
    public string? VesselName { get; set; }
    public bool IsAIGenerated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ActedOnBy { get; set; }
    public string? ActedOnNotes { get; set; }
}
