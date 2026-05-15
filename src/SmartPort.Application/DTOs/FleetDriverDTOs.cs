using System.Text.Json.Serialization;

namespace SmartPort.Application.DTOs;

public enum QueueTruckStatus { Scheduled, EnRoute, Waiting, LocationRequested, LocationShared, HoldPosition, Holding, AtStaging, AtStagingHolding, ProceedToGate, AtGate, InYard, LoadingUnloading, Completed, Delayed, Rescheduled, Exception }
public enum QueueDelayRisk { Low, Medium, High, Critical }
public enum NotificationChannel { InApp, WhatsApp, AndroidPush }
public enum NotificationStatus { Pending, SimulatedSent, LiveTestSent, Sent, Failed, ConnectorNotConfigured, BlockedSafety, IgnoredUnapprovedSender }
public enum NotificationEventType { QueueStatusChanged, ProceedToGate, HoldPosition, GateChanged, AppointmentRescheduled, DelayRiskIncreased, IncidentDisruptionWarning, AppointmentCompleted, AiInstructionUpdated, DriverCheckIn, DriverConfirmation, WhatsAppLocationCheckIn, ExecutionPlanGenerated, InboundWhatsAppCommand, CopilotAnswerGenerated, ExceptionReported }
public enum DriverAcknowledgement { Seen, Holding, Proceeding }
public enum DriverNotificationPreference { InApp, WhatsApp, Both }
public enum WhatsAppMode { Demo, ConnectorReady, LiveTest }
public enum DataProvenanceType { SyntheticDemoData, ExternalPortFeedPlaceholder, ManualOperatorInput, WhatsAppDriverCheckIn, AndroidDriverApp, GeminiAiRecommendation, DeterministicFallback, FutureLiveConnector }
public enum DriverEventType { WhatsAppLocationRequested, WhatsAppLocationShared, AndroidAppStatusChecked, DriverAcknowledgedInstruction, DriverConfirmedHolding, DriverArrivedAtStaging, DriverProceedingToGate, DriverArrivedAtGate, DriverCompletedJob, DriverReady, DriverBreak, DriverLunch, DriverDelayed, DriverIssueReported }
public enum OperationalActionType { CheckEta, RequestLocation, ShareLocation, ConfirmHolding, ArrivedAtStaging, ProceedingToGate, ArrivedAtGate, CompleteJob, Ready, Break15, Lunch30, Delayed20, MoveToStaging, ReleaseToGate, Reschedule, MarkException, ReportIssue, RefreshStatus }
public enum ExecutionTruckActionType { ProceedToGate, HoldPosition, MoveToStaging, DelayArrival, Reschedule, MarkException, RequestLocation, AwaitBerthReadiness }
public enum CapacityStatus { Open, Available, NearFull, Full, Congested, Closed, Incident }

public class FleetOperatorDto { public string Id { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string ContactPerson { get; set; } = string.Empty; }

public class DriverContactProfileDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DriverName { get; set; } = string.Empty;
    public string LicencePermitPlaceholder { get; set; } = string.Empty;
    public string WhatsAppCountryCode { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string NormalizedWhatsAppNumber { get; set; } = string.Empty;
    public string BackupContact { get; set; } = string.Empty;
    public DriverNotificationPreference NotificationPreference { get; set; } = DriverNotificationPreference.Both;
    public bool WhatsAppConsentConfirmed { get; set; }
    public bool TestApproved { get; set; }
    public bool IsActive { get; set; } = true;
    public string AssignedTruckRegistration { get; set; } = string.Empty;
    public string AssignedFleetOperatorId { get; set; } = string.Empty;
    public string AssignedFleetOperatorName { get; set; } = string.Empty;
    public bool IsDemoSeedRecord { get; set; }
    public bool IsLiveWhatsAppSafe => IsActive && !IsDemoSeedRecord && WhatsAppConsentConfirmed && TestApproved && !string.IsNullOrWhiteSpace(NormalizedWhatsAppNumber) && NormalizedWhatsAppNumber.StartsWith('+');
    public string ValidationMessage { get; set; } = string.Empty;
    public DataProvenanceType Source { get; set; } = DataProvenanceType.ManualOperatorInput;
}

public class SaveDriverContactRequestDto
{
    public string DriverName { get; set; } = string.Empty;
    public string LicencePermitPlaceholder { get; set; } = string.Empty;
    public string WhatsAppCountryCode { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string BackupContact { get; set; } = string.Empty;
    public DriverNotificationPreference NotificationPreference { get; set; } = DriverNotificationPreference.Both;
    public bool WhatsAppConsentConfirmed { get; set; }
    public bool TestApproved { get; set; }
    public bool IsActive { get; set; } = true;
    public string AssignedTruckRegistration { get; set; } = string.Empty;
    public string AssignedFleetOperatorId { get; set; } = string.Empty;
}

public class SaveFleetQueueTruckRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string FleetOperatorId { get; set; } = string.Empty;
    public string TruckRegistration { get; set; } = string.Empty;
    public string AssignedDriverName { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "Container tractor";
    public string ContainerReference { get; set; } = string.Empty;
    public string JobReference { get; set; } = string.Empty;
    public string BookingReference { get; set; } = string.Empty;
    public QueueTruckStatus CurrentStatus { get; set; } = QueueTruckStatus.Scheduled;
    public string AssignedGate { get; set; } = string.Empty;
    public string BerthYardStagingZone { get; set; } = string.Empty;
    public DateTime AppointmentTime { get; set; } = DateTime.UtcNow.AddHours(1);
    public DateTime EtaCallForwardTime { get; set; } = DateTime.UtcNow.AddHours(1);
}

public class QueueInstructionDto { public string Reference { get; set; } = string.Empty; public string Instruction { get; set; } = string.Empty; public string Reason { get; set; } = string.Empty; public string Explanation { get; set; } = string.Empty; public DataProvenanceType Source { get; set; } = DataProvenanceType.DeterministicFallback; public DateTime GeneratedAt { get; set; } }
public class TruckStatusTimelineDto { public DateTime Timestamp { get; set; } public QueueTruckStatus Status { get; set; } public string Note { get; set; } = string.Empty; public DriverEventType? DriverEventType { get; set; } public DataProvenanceType Source { get; set; } = DataProvenanceType.SyntheticDemoData; public string Actor { get; set; } = "Smart Port"; }
public class LocationCheckInDto { public decimal Latitude { get; set; } public decimal Longitude { get; set; } public string LocationLabel { get; set; } = string.Empty; public DateTime Timestamp { get; set; } = DateTime.UtcNow; public decimal DistanceToAssignedGateKm { get; set; } public decimal DistanceToStagingKm { get; set; } public int EstimatedTravelMinutes { get; set; } public decimal EtaConfidence { get; set; } = .78m; public DateTime EstimatedArrivalTime { get; set; } public DataProvenanceType Source { get; set; } = DataProvenanceType.WhatsAppDriverCheckIn; }

public class DriverNotificationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TruckReference { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientContact { get; set; } = string.Empty;
    public string FleetOperator { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public NotificationEventType EventType { get; set; }
    public string RelatedInstructionReference { get; set; } = string.Empty;
    public DataProvenanceType Source { get; set; } = DataProvenanceType.SyntheticDemoData;
    public string ExternalMessageId { get; set; } = string.Empty;
}

public class OperationalAuditEntryDto { public DateTime Timestamp { get; set; } = DateTime.UtcNow; public string Actor { get; set; } = string.Empty; public string EventType { get; set; } = string.Empty; public QueueTruckStatus? OldStatus { get; set; } public QueueTruckStatus? NewStatus { get; set; } public string Reason { get; set; } = string.Empty; public DataProvenanceType Source { get; set; } public string PublicSafeNote { get; set; } = string.Empty; }

public class FleetTruckDto
{
    public string Id { get; set; } = string.Empty;
    public string FleetOperatorId { get; set; } = string.Empty;
    public string FleetOperatorName { get; set; } = string.Empty;
    public string TruckRegistration { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "Container tractor";
    public string DriverContactPlaceholder { get; set; } = "DEMO-ONLY-NOT-LIVE";
    public DriverContactProfileDto DriverContact { get; set; } = new();
    public string ContainerReference { get; set; } = string.Empty;
    public string JobReference { get; set; } = string.Empty;
    public string BookingReference { get; set; } = string.Empty;
    public DateTime AppointmentTime { get; set; }
    public int QueueNumber { get; set; }
    public string AssignedGate { get; set; } = string.Empty;
    public string BerthYardStagingZone { get; set; } = string.Empty;
    public QueueTruckStatus CurrentStatus { get; set; }
    public DateTime EtaCallForwardTime { get; set; }
    public QueueInstructionDto CurrentInstruction { get; set; } = new();
    public QueueDelayRisk DelayRisk { get; set; }
    public int EstimatedIdlingMinutesAvoided { get; set; }
    public decimal EstimatedCo2KgAvoided { get; set; }
    public decimal QueueEfficiencyScore { get; set; } = .76m;
    public string LatestNotification { get; set; } = string.Empty;
    public NotificationStatus WhatsAppNotificationStatus { get; set; }
    public NotificationStatus InAppNotificationStatus { get; set; }
    public DateTime LastUpdated { get; set; }
    public DriverAcknowledgement? AcknowledgementState { get; set; }
    public string DriverAvailabilityStatus { get; set; } = "Available";
    public string LastKnownLocationLabel { get; set; } = string.Empty;
    public LocationCheckInDto? LastLocationCheckIn { get; set; }
    public DataProvenanceType Source { get; set; } = DataProvenanceType.SyntheticDemoData;
    public List<OperationalActionType> AllowedNextActions { get; set; } = new();
    public List<TruckStatusTimelineDto> Timeline { get; set; } = new();
    public List<OperationalAuditEntryDto> AuditTrail { get; set; } = new();
    public List<DriverNotificationDto> NotificationHistory { get; set; } = new();
}

public class GateCapacityDto { public string GateName { get; set; } = string.Empty; public int CurrentQueueCount { get; set; } public int CapacityPerHour { get; set; } public string CongestionLevel { get; set; } = string.Empty; public CapacityStatus Status { get; set; } public List<string> AssignedTrucks { get; set; } = new(); public DateTime NextAvailableTime { get; set; } }
public class StagingAreaDto { public string AreaName { get; set; } = string.Empty; public int Capacity { get; set; } public int CurrentTrucks { get; set; } public decimal DistanceToAssignedGateKm { get; set; } public CapacityStatus Status { get; set; } }
public class YardZoneCapacityDto { public string ZoneName { get; set; } = string.Empty; public int Capacity { get; set; } public int CurrentLoad { get; set; } public CapacityStatus Status { get; set; } }
public class BerthReadinessStatusDto { public string BerthName { get; set; } = string.Empty; public string ReadinessStatus { get; set; } = string.Empty; public string CurrentOperation { get; set; } = string.Empty; public DateTime NextAvailableTime { get; set; } public QueueDelayRisk DelayRisk { get; set; } public List<string> LinkedTruckReferences { get; set; } = new(); }

public class FleetQueueSummaryDto
{
    public FleetOperatorDto SelectedFleet { get; set; } = new(); public List<FleetOperatorDto> FleetOperators { get; set; } = new(); public List<FleetTruckDto> Trucks { get; set; } = new();
    public int TotalTrucks { get; set; } public int TrucksWaiting { get; set; } public int TrucksHolding { get; set; } public int TrucksProceeding { get; set; } public int DelayedOrRescheduledTrucks { get; set; }
    public int TotalIdlingMinutesAvoided { get; set; } public decimal TotalCo2KgAvoided { get; set; } public string LatestAiQueueRecommendation { get; set; } = string.Empty;
    public List<DriverNotificationDto> LatestNotifications { get; set; } = new(); public List<FleetTruckDto> HighRiskTrucks { get; set; } = new(); public List<DataSourceReadinessDto> DataSources { get; set; } = new(); public WhatsAppConnectorStatusDto WhatsAppStatus { get; set; } = new();
    public List<GateCapacityDto> GateCapacities { get; set; } = new(); public List<StagingAreaDto> StagingAreas { get; set; } = new(); public List<BerthReadinessStatusDto> BerthReadiness { get; set; } = new(); public ExecutionPlanDto? LatestExecutionPlan { get; set; }
}

public class DataSourceReadinessDto { public string Badge { get; set; } = string.Empty; public DataProvenanceType SourceType { get; set; } public string CurrentDemoSource { get; set; } = string.Empty; public string RealWorldSource { get; set; } = string.Empty; public string RequiredData { get; set; } = string.Empty; public string SmartPortPlugIn { get; set; } = string.Empty; public string PilotReadinessLevel { get; set; } = string.Empty; }
public class WhatsAppConnectorStatusDto
{
    public WhatsAppMode Mode { get; set; } = WhatsAppMode.Demo;
    public bool Enabled { get; set; }
    public bool AccessTokenConfigured { get; set; }
    public bool PhoneNumberIdConfigured { get; set; }
    public bool BusinessAccountIdConfigured { get; set; }
    public bool VerifyTokenConfigured { get; set; }
    public bool PublicBaseUrlConfigured { get; set; }
    public string GraphVersion { get; set; } = "v20.0";
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string WebhookCallbackUrl { get; set; } = string.Empty;
    public bool ApprovedDriverAvailable { get; set; }
    public bool CredentialsConfigured { get; set; }
    public bool LiveSendingAllowed { get; set; }
    public string SafetyMessage { get; set; } = "Live WhatsApp messages are only sent to approved test numbers. Demo records use simulated WhatsApp notifications.";
    public List<string> LiveTestReadinessChecks { get; set; } = new();
}
public class MobileDeviceRegistrationDto { public string Reference { get; set; } = string.Empty; public string DeviceToken { get; set; } = string.Empty; public string Platform { get; set; } = "Android"; public string AppVersion { get; set; } = string.Empty; }
public class DriverCheckRequestDto { public string Reference { get; set; } = string.Empty; }
public class DriverAcknowledgementRequestDto { public string Reference { get; set; } = string.Empty; [JsonConverter(typeof(JsonStringEnumConverter))] public DriverAcknowledgement Acknowledgement { get; set; } }
public class DriverEventRequestDto { public string Reference { get; set; } = string.Empty; [JsonConverter(typeof(JsonStringEnumConverter))] public DriverEventType EventType { get; set; } public string SourceLabel { get; set; } = string.Empty; public decimal? Latitude { get; set; } public decimal? Longitude { get; set; } public string? LocationLabel { get; set; } }
public class DriverCommandRequestDto { public string Reference { get; set; } = string.Empty; public string SenderWhatsAppNumber { get; set; } = string.Empty; public string CommandText { get; set; } = string.Empty; public string Actor { get; set; } = "Driver"; public DataProvenanceType Source { get; set; } = DataProvenanceType.AndroidDriverApp; }
public class DriverCommandResultDto { public bool Success { get; set; } public string ReplyMessage { get; set; } = string.Empty; public FleetTruckDto? Truck { get; set; } public string Source { get; set; } = "Fallback"; }
public class CopilotQuestionRequestDto { public string Reference { get; set; } = string.Empty; public string UserRole { get; set; } = "Driver"; public string Question { get; set; } = string.Empty; }
public class CopilotResponseDto { public string Answer { get; set; } = string.Empty; public string SuggestedAction { get; set; } = string.Empty; public string Source { get; set; } = "Fallback"; public FleetTruckDto? RelatedTruckStatus { get; set; } }

public class StateTransitionResultDto { public bool Success { get; set; } public string Message { get; set; } = string.Empty; public QueueTruckStatus OldStatus { get; set; } public QueueTruckStatus NewStatus { get; set; } public FleetTruckDto? Truck { get; set; } public IReadOnlyList<OperationalActionType> AllowedNextActions { get; set; } = Array.Empty<OperationalActionType>(); }
public class QueueOptimizationResultDto { public QueueTruckStatus RecommendedStatus { get; set; } public int QueuePositionUpdate { get; set; } public string GateAssignmentSuggestion { get; set; } = string.Empty; public string StagingInstruction { get; set; } = string.Empty; public DateTime EtaCallForwardTime { get; set; } public int IdlingMinutesAvoided { get; set; } public decimal Co2KgAvoided { get; set; } public QueueDelayRisk DelayRisk { get; set; } public string Reason { get; set; } = string.Empty; public string Explanation { get; set; } = string.Empty; public decimal ConfidenceScore { get; set; } public string DriverFriendlyInstruction { get; set; } = string.Empty; public string NotificationMessage { get; set; } = string.Empty; public List<OperationalActionType> AllowedNextActions { get; set; } = new(); }
public class ExecutionPlanTruckActionDto { public string TruckReference { get; set; } = string.Empty; public string TruckRegistration { get; set; } = string.Empty; public string DriverName { get; set; } = string.Empty; public ExecutionTruckActionType ActionType { get; set; } public string Reason { get; set; } = string.Empty; public DateTime EtaCallForwardTime { get; set; } public string NotificationText { get; set; } = string.Empty; public List<OperationalActionType> AllowedNextActions { get; set; } = new(); public int IdlingMinutesAvoided { get; set; } public decimal Co2KgAvoided { get; set; } }
public class ExecutionPlanDto { public string PlanId { get; set; } = Guid.NewGuid().ToString("N"); public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public string ScenarioName { get; set; } = "Demo gate congestion execution plan"; public string AffectedFleetOperator { get; set; } = "All demo fleets"; public List<ExecutionPlanTruckActionDto> TruckActions { get; set; } = new(); public int ExpectedIdlingMinutesAvoided { get; set; } public decimal ExpectedCo2KgAvoided { get; set; } public int ExpectedDelayReductionMinutes { get; set; } public decimal ConfidenceScore { get; set; } public string Explanation { get; set; } = string.Empty; public List<OperationalAuditEntryDto> AuditEntries { get; set; } = new(); }
