using SmartPort.Application.DTOs;

namespace SmartPort.Application.Interfaces;

public interface IFleetDriverQueueService
{
    Task<FleetQueueSummaryDto> GetFleetSummaryAsync(string? fleetOperatorId = null);
    Task<IReadOnlyList<FleetTruckDto>> GetTrucksAsync(string? fleetOperatorId = null);
    Task<FleetTruckDto?> GetTruckAsync(string referenceOrId);
    Task<IReadOnlyList<string>> GetDemoReferencesAsync();
    Task<IReadOnlyList<DriverContactProfileDto>> GetDriverContactsAsync();
    Task<DriverContactProfileDto> SaveDriverContactAsync(SaveDriverContactRequestDto request);
    Task<FleetTruckDto> SaveTruckAsync(SaveFleetQueueTruckRequestDto request);
    Task<IReadOnlyList<DataSourceReadinessDto>> GetDataSourcesAsync();
    WhatsAppConnectorStatusDto GetWhatsAppConnectorStatus();
    Task<FleetTruckDto?> AcknowledgeAsync(string reference, DriverAcknowledgement acknowledgement);
    Task<FleetTruckDto?> RecordDriverEventAsync(string reference, DriverEventType eventType, DataProvenanceType source, string? note = null, string actor = "Driver");
    Task<FleetTruckDto?> RecordLocationCheckInAsync(string reference, decimal? latitude, decimal? longitude, string? label, DataProvenanceType source, string actor = "Driver");
}

public interface IOperationalStateMachineService
{
    bool CanTransition(QueueTruckStatus currentStatus, OperationalActionType requestedAction, out QueueTruckStatus newStatus, out string message);
    Task<StateTransitionResultDto> ApplyTransitionAsync(string jobReference, OperationalActionType action, string actor, DataProvenanceType source);
    Task<IReadOnlyList<OperationalActionType>> GetAllowedNextActionsAsync(string jobReference);
}

public interface IQueueOptimizationService { QueueOptimizationResultDto Optimize(FleetTruckDto truck, FleetQueueSummaryDto? context = null); }
public interface IExecutionPlanService { Task<ExecutionPlanDto> GeneratePlanAsync(string scenarioName = "Demo gate congestion execution plan", string? fleetOperatorId = null); Task<ExecutionPlanDto?> GetPlanAsync(string id); Task<IReadOnlyList<ExecutionPlanDto>> GetPlansAsync(); }
public interface IDriverStatusCommandService { Task<DriverCommandResultDto> HandleCommandAsync(DriverCommandRequestDto request); }
public interface ILocationEtaService { LocationCheckInDto Estimate(string reference, string assignedGate, string stagingArea, decimal? latitude, decimal? longitude, string? label, DataProvenanceType source); }
public interface INotificationTemplateService { string BuildMessage(FleetTruckDto truck, NotificationEventType eventType, NotificationChannel channel); }
public interface INotificationService { Task<DriverNotificationDto> SendAsync(string reference, NotificationChannel channel, NotificationEventType eventType); Task<IReadOnlyList<DriverNotificationDto>> GetHistoryAsync(string reference); }
public interface IWhatsAppNotificationSender { Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message); }
public interface IInAppNotificationService { Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message); }
public interface IPushNotificationSender { Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message); }
public interface IMobileDeviceRegistrationService { Task RegisterAsync(MobileDeviceRegistrationDto registration); Task UnregisterAsync(string reference, string deviceToken); }
