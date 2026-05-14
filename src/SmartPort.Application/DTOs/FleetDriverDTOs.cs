using System.Text.Json.Serialization;

namespace SmartPort.Application.DTOs;

public enum QueueTruckStatus { Scheduled, Waiting, HoldPosition, ProceedToGate, AtGate, InYard, LoadingUnloading, Completed, Delayed, Rescheduled }
public enum QueueDelayRisk { Low, Medium, High, Critical }
public enum NotificationChannel { InApp, WhatsApp, AndroidPush }
public enum NotificationStatus { Pending, SimulatedSent, Sent, Failed, ConnectorNotConfigured }
public enum NotificationEventType { QueueStatusChanged, ProceedToGate, HoldPosition, GateChanged, AppointmentRescheduled, DelayRiskIncreased, IncidentDisruptionWarning, AppointmentCompleted, AiInstructionUpdated }
public enum DriverAcknowledgement { Seen, Holding, Proceeding }

public class FleetOperatorDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
}

public class QueueInstructionDto
{
    public string Reference { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

public class TruckStatusTimelineDto
{
    public DateTime Timestamp { get; set; }
    public QueueTruckStatus Status { get; set; }
    public string Note { get; set; } = string.Empty;
}

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
}

public class FleetTruckDto
{
    public string Id { get; set; } = string.Empty;
    public string FleetOperatorId { get; set; } = string.Empty;
    public string FleetOperatorName { get; set; } = string.Empty;
    public string TruckRegistration { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DriverContactPlaceholder { get; set; } = string.Empty;
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
    public string LatestNotification { get; set; } = string.Empty;
    public NotificationStatus WhatsAppNotificationStatus { get; set; }
    public NotificationStatus InAppNotificationStatus { get; set; }
    public DateTime LastUpdated { get; set; }
    public DriverAcknowledgement? AcknowledgementState { get; set; }
    public List<TruckStatusTimelineDto> Timeline { get; set; } = new();
    public List<DriverNotificationDto> NotificationHistory { get; set; } = new();
}

public class FleetQueueSummaryDto
{
    public FleetOperatorDto SelectedFleet { get; set; } = new();
    public List<FleetOperatorDto> FleetOperators { get; set; } = new();
    public List<FleetTruckDto> Trucks { get; set; } = new();
    public int TotalTrucks { get; set; }
    public int TrucksWaiting { get; set; }
    public int TrucksHolding { get; set; }
    public int TrucksProceeding { get; set; }
    public int DelayedOrRescheduledTrucks { get; set; }
    public int TotalIdlingMinutesAvoided { get; set; }
    public decimal TotalCo2KgAvoided { get; set; }
    public string LatestAiQueueRecommendation { get; set; } = string.Empty;
    public List<DriverNotificationDto> LatestNotifications { get; set; } = new();
    public List<FleetTruckDto> HighRiskTrucks { get; set; } = new();
}

public class MobileDeviceRegistrationDto
{
    public string Reference { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "Android";
    public string AppVersion { get; set; } = string.Empty;
}

public class DriverCheckRequestDto
{
    public string Reference { get; set; } = string.Empty;
}

public class DriverAcknowledgementRequestDto
{
    public string Reference { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DriverAcknowledgement Acknowledgement { get; set; }
}
