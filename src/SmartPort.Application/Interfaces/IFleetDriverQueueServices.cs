using SmartPort.Application.DTOs;

namespace SmartPort.Application.Interfaces;

public interface IFleetDriverQueueService
{
    Task<FleetQueueSummaryDto> GetFleetSummaryAsync(string? fleetOperatorId = null);
    Task<IReadOnlyList<FleetTruckDto>> GetTrucksAsync(string? fleetOperatorId = null);
    Task<FleetTruckDto?> GetTruckAsync(string referenceOrId);
    Task<IReadOnlyList<string>> GetDemoReferencesAsync();
    Task<FleetTruckDto?> AcknowledgeAsync(string reference, DriverAcknowledgement acknowledgement);
}

public interface INotificationTemplateService
{
    string BuildMessage(FleetTruckDto truck, NotificationEventType eventType, NotificationChannel channel);
}

public interface INotificationService
{
    Task<DriverNotificationDto> SendAsync(string reference, NotificationChannel channel, NotificationEventType eventType);
    Task<IReadOnlyList<DriverNotificationDto>> GetHistoryAsync(string reference);
}

public interface IWhatsAppNotificationSender
{
    Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message);
}

public interface IInAppNotificationService
{
    Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message);
}

public interface IPushNotificationSender
{
    Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message);
}

public interface IMobileDeviceRegistrationService
{
    Task RegisterAsync(MobileDeviceRegistrationDto registration);
    Task UnregisterAsync(string reference, string deviceToken);
}
