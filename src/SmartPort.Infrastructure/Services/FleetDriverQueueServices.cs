using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SmartPort.Application.DTOs;
using SmartPort.Application.Interfaces;

namespace SmartPort.Infrastructure.Services;

public class DemoFleetDriverQueueService : IFleetDriverQueueService
{
    private static readonly ConcurrentDictionary<string, DriverAcknowledgement> Acknowledgements = new(StringComparer.OrdinalIgnoreCase);

    public async Task<FleetQueueSummaryDto> GetFleetSummaryAsync(string? fleetOperatorId = null)
    {
        var operators = BuildOperators();
        var trucks = (await GetTrucksAsync(fleetOperatorId)).ToList();
        var selected = operators.FirstOrDefault(o => string.Equals(o.Id, fleetOperatorId, StringComparison.OrdinalIgnoreCase)) ?? operators.First();
        var latestNotifications = trucks.SelectMany(t => t.NotificationHistory).OrderByDescending(n => n.Timestamp).Take(8).ToList();
        return new FleetQueueSummaryDto
        {
            SelectedFleet = selected,
            FleetOperators = operators,
            Trucks = trucks,
            TotalTrucks = trucks.Count,
            TrucksWaiting = trucks.Count(t => t.CurrentStatus == QueueTruckStatus.Waiting),
            TrucksHolding = trucks.Count(t => t.CurrentStatus == QueueTruckStatus.HoldPosition),
            TrucksProceeding = trucks.Count(t => t.CurrentStatus == QueueTruckStatus.ProceedToGate),
            DelayedOrRescheduledTrucks = trucks.Count(t => t.CurrentStatus is QueueTruckStatus.Delayed or QueueTruckStatus.Rescheduled),
            TotalIdlingMinutesAvoided = trucks.Sum(t => t.EstimatedIdlingMinutesAvoided),
            TotalCo2KgAvoided = trucks.Sum(t => t.EstimatedCo2KgAvoided),
            LatestAiQueueRecommendation = "Keep 3 high-risk trucks off-gate, call forward Gate 2 reefer traffic in 12 minutes, and stage export containers at Yard B to cut avoidable idling.",
            LatestNotifications = latestNotifications,
            HighRiskTrucks = trucks.Where(t => t.DelayRisk is QueueDelayRisk.High or QueueDelayRisk.Critical).ToList()
        };
    }

    public async Task<IReadOnlyList<FleetTruckDto>> GetTrucksAsync(string? fleetOperatorId = null)
    {
        var trucks = BuildTrucks();
        foreach (var truck in trucks)
        {
            if (Acknowledgements.TryGetValue(truck.BookingReference, out var ack) || Acknowledgements.TryGetValue(truck.TruckRegistration, out ack) || Acknowledgements.TryGetValue(truck.JobReference, out ack))
            {
                truck.AcknowledgementState = ack;
            }
            truck.NotificationHistory = SeedNotifications(truck);
            truck.LatestNotification = truck.NotificationHistory.OrderByDescending(n => n.Timestamp).FirstOrDefault()?.Message ?? truck.LatestNotification;
        }

        return string.IsNullOrWhiteSpace(fleetOperatorId)
            ? trucks
            : trucks.Where(t => string.Equals(t.FleetOperatorId, fleetOperatorId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<FleetTruckDto?> GetTruckAsync(string referenceOrId)
    {
        if (string.IsNullOrWhiteSpace(referenceOrId)) return null;
        var key = referenceOrId.Trim();
        return (await GetTrucksAsync()).FirstOrDefault(t =>
            string.Equals(t.Id, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.TruckRegistration, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.BookingReference, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.JobReference, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.ContainerReference, key, StringComparison.OrdinalIgnoreCase));
    }

    public Task<IReadOnlyList<string>> GetDemoReferencesAsync() => Task.FromResult<IReadOnlyList<string>>(BuildTrucks().Select(t => t.BookingReference).Take(6).ToList());

    public async Task<FleetTruckDto?> AcknowledgeAsync(string reference, DriverAcknowledgement acknowledgement)
    {
        var truck = await GetTruckAsync(reference);
        if (truck == null) return null;
        Acknowledgements[truck.BookingReference] = acknowledgement;
        truck.AcknowledgementState = acknowledgement;
        return truck;
    }

    private static List<FleetOperatorDto> BuildOperators() => new()
    {
        new() { Id = "durban-freight", Name = "Durban Freight Logistics", ContactPerson = "Naledi Mokoena" },
        new() { Id = "kzn-cold-chain", Name = "KZN Cold Chain Carriers", ContactPerson = "Asha Pillay" },
        new() { Id = "bayline-container", Name = "Bayline Container Transport", ContactPerson = "Warren Jacobs" },
        new() { Id = "harborlink", Name = "HarborLink Haulage", ContactPerson = "Sibusiso Dlamini" }
    };

    private static List<FleetTruckDto> BuildTrucks()
    {
        var now = DateTime.UtcNow;
        return new List<FleetTruckDto>
        {
            Truck("SPQ-2026-0042", "durban-freight", "Durban Freight Logistics", "KZN-482-TR", "Thabo Ndlovu", "+27 600 000 042", "CONT-DUR-88421", "JOB-DUR-0042", now.AddMinutes(35), 14, "Gate 3", "Staging Area A", QueueTruckStatus.HoldPosition, now.AddMinutes(18), "Hold position for 18 minutes to avoid Gate 3 congestion.", "Gate 3 is above the safe queue threshold while Berth 204 is still clearing imports.", QueueDelayRisk.High, 31, 42.8m),
            Truck("SPQ-2026-0043", "kzn-cold-chain", "KZN Cold Chain Carriers", "KZN-771-RF", "Amina Khan", "+27 600 000 043", "REEF-2219-ZA", "JOB-REEF-0043", now.AddMinutes(20), 7, "Gate 2", "Reefer Yard R2", QueueTruckStatus.ProceedToGate, now.AddMinutes(12), "Proceed to Gate 2 within the next 12 minutes.", "Berth slot is ready and Gate 2 queue has dropped below the reefer priority threshold.", QueueDelayRisk.Medium, 19, 24.6m),
            Truck("SPQ-2026-0044", "bayline-container", "Bayline Container Transport", "ND-119-CT", "Musa Zulu", "+27 600 000 044", "MSKU-781244-3", "JOB-BAY-0044", now.AddMinutes(55), 22, "Gate 4", "Yard Block C", QueueTruckStatus.Delayed, now.AddMinutes(30), "Delay arrival by 30 minutes due to berth congestion.", "A late vessel move has constrained Yard Block C and delaying arrival prevents unnecessary gate idling.", QueueDelayRisk.Critical, 45, 63.1m),
            Truck("SPQ-2026-0045", "harborlink", "HarborLink Haulage", "DBN-908-HL", "Priya Naidoo", "+27 600 000 045", "HLX-EXP-4450", "JOB-HLX-0045", now.AddMinutes(15), 3, "Gate 1", "Export Stack E1", QueueTruckStatus.AtGate, now.AddMinutes(5), "Remain in Gate 1 lane and prepare export reference for check-in.", "Gate OCR pre-check is complete and the export stack is available.", QueueDelayRisk.Low, 12, 15.4m),
            Truck("SPQ-2026-0046", "durban-freight", "Durban Freight Logistics", "KZN-652-TR", "Lerato Dube", "+27 600 000 046", "CONT-DUR-88422", "JOB-DUR-0046", now.AddMinutes(70), 29, "Gate 3", "Off-site Holding", QueueTruckStatus.Rescheduled, now.AddMinutes(40), "Remain off-site until called forward.", "Road congestion and terminal stack pressure make off-site holding the cleanest option.", QueueDelayRisk.High, 38, 51.9m),
            Truck("SPQ-2026-0047", "kzn-cold-chain", "KZN Cold Chain Carriers", "KZN-314-RF", "Sipho Mthembu", "+27 600 000 047", "REEF-9981-ZA", "JOB-REEF-0047", now.AddMinutes(42), 11, "Gate 2", "Staging Area B", QueueTruckStatus.Waiting, now.AddMinutes(24), "Move to Staging Area B to reduce idling.", "Reefer plug capacity is constrained; staging keeps the truck close without blocking the gate.", QueueDelayRisk.Medium, 22, 29.7m),
            Truck("SPQ-2026-0048", "bayline-container", "Bayline Container Transport", "ND-774-CT", "Johan Botha", "+27 600 000 048", "MSKU-555001-2", "JOB-BAY-0048", now.AddMinutes(95), 35, "Gate 5", "Yard Block D", QueueTruckStatus.Scheduled, now.AddMinutes(62), "Arrive at the pre-advice window; do not enter the gate queue early.", "Later appointment is aligned to vessel discharge completion and keeps gate demand flat.", QueueDelayRisk.Low, 16, 20.2m)
        };
    }

    private static FleetTruckDto Truck(string booking, string opId, string op, string reg, string driver, string contact, string container, string job, DateTime appt, int q, string gate, string zone, QueueTruckStatus status, DateTime eta, string instruction, string reason, QueueDelayRisk risk, int idle, decimal co2)
    {
        var updated = DateTime.UtcNow.AddMinutes(-q);
        var truck = new FleetTruckDto
        {
            Id = booking,
            FleetOperatorId = opId,
            FleetOperatorName = op,
            TruckRegistration = reg,
            DriverName = driver,
            DriverContactPlaceholder = contact,
            ContainerReference = container,
            JobReference = job,
            BookingReference = booking,
            AppointmentTime = appt,
            QueueNumber = q,
            AssignedGate = gate,
            BerthYardStagingZone = zone,
            CurrentStatus = status,
            EtaCallForwardTime = eta,
            CurrentInstruction = new QueueInstructionDto { Reference = $"QI-{booking[^4..]}", Instruction = instruction, Reason = reason, Explanation = BuildFallbackExplanation(status, risk, idle, co2), GeneratedAt = updated },
            DelayRisk = risk,
            EstimatedIdlingMinutesAvoided = idle,
            EstimatedCo2KgAvoided = co2,
            LatestNotification = $"Smart Port Update: Truck {reg} should {instruction.ToLowerInvariant()} Estimated gate call-forward: {eta:HH:mm}. Ref: {booking}.",
            WhatsAppNotificationStatus = NotificationStatus.SimulatedSent,
            InAppNotificationStatus = NotificationStatus.SimulatedSent,
            LastUpdated = updated
        };
        truck.Timeline = new()
        {
            new() { Timestamp = appt.AddMinutes(-60), Status = QueueTruckStatus.Scheduled, Note = "Appointment confirmed by Smart Port." },
            new() { Timestamp = appt.AddMinutes(-35), Status = QueueTruckStatus.Waiting, Note = "Truck visible in queue planning window." },
            new() { Timestamp = updated, Status = status, Note = instruction }
        };
        truck.NotificationHistory = SeedNotifications(truck);
        return truck;
    }

    private static string BuildFallbackExplanation(QueueTruckStatus status, QueueDelayRisk risk, int idle, decimal co2) => status switch
    {
        QueueTruckStatus.HoldPosition => $"Gate congestion is active, so holding prevents queue spillback and avoids about {idle} idling minutes ({co2:N1} kg CO2).",
        QueueTruckStatus.ProceedToGate => $"Berth readiness and a low gate queue make this the cleanest time to call the truck forward.",
        QueueTruckStatus.Delayed or QueueTruckStatus.Rescheduled => $"A disruption or berth/yard constraint is active. Rescheduling reduces gate pressure and avoids about {idle} minutes of diesel idling.",
        _ when risk == QueueDelayRisk.High => "High idling risk detected; Smart Port is keeping the vehicle staged until terminal capacity improves.",
        _ => "Queue, berth, gate, and emissions signals are balanced for the current instruction."
    };

    private static List<DriverNotificationDto> SeedNotifications(FleetTruckDto truck) => new()
    {
        new() { TruckReference = truck.BookingReference, RecipientName = truck.DriverName, RecipientContact = truck.DriverContactPlaceholder, FleetOperator = truck.FleetOperatorName, Channel = NotificationChannel.InApp, Message = truck.LatestNotification, Status = NotificationStatus.SimulatedSent, EventType = NotificationEventType.AiInstructionUpdated, RelatedInstructionReference = truck.CurrentInstruction.Reference, Timestamp = truck.LastUpdated },
        new() { TruckReference = truck.BookingReference, RecipientName = truck.DriverName, RecipientContact = truck.DriverContactPlaceholder, FleetOperator = truck.FleetOperatorName, Channel = NotificationChannel.WhatsApp, Message = truck.LatestNotification, Status = NotificationStatus.SimulatedSent, EventType = NotificationEventType.AiInstructionUpdated, RelatedInstructionReference = truck.CurrentInstruction.Reference, Timestamp = truck.LastUpdated.AddSeconds(15) }
    };
}

public class NotificationTemplateService : INotificationTemplateService
{
    public string BuildMessage(FleetTruckDto truck, NotificationEventType eventType, NotificationChannel channel)
    {
        if (eventType == NotificationEventType.ProceedToGate)
            return $"Smart Port Update: Truck {truck.TruckRegistration} may proceed to {truck.AssignedGate} within {Math.Max(1, (int)(truck.EtaCallForwardTime - DateTime.UtcNow).TotalMinutes)} minutes. Have reference {truck.BookingReference} ready.";
        return $"Smart Port Update: Truck {truck.TruckRegistration} should {truck.CurrentInstruction.Instruction.ToLowerInvariant()} Estimated gate call-forward: {truck.EtaCallForwardTime:HH:mm}. Ref: {truck.BookingReference}.";
    }
}

public class DriverNotificationService : INotificationService
{
    private readonly IFleetDriverQueueService _queue;
    private readonly INotificationTemplateService _templates;
    private readonly IInAppNotificationService _inApp;
    private readonly IWhatsAppNotificationSender _whatsApp;
    private readonly IPushNotificationSender _push;
    private static readonly ConcurrentDictionary<string, List<DriverNotificationDto>> History = new(StringComparer.OrdinalIgnoreCase);

    public DriverNotificationService(IFleetDriverQueueService queue, INotificationTemplateService templates, IInAppNotificationService inApp, IWhatsAppNotificationSender whatsApp, IPushNotificationSender push)
    { _queue = queue; _templates = templates; _inApp = inApp; _whatsApp = whatsApp; _push = push; }

    public async Task<DriverNotificationDto> SendAsync(string reference, NotificationChannel channel, NotificationEventType eventType)
    {
        var truck = await _queue.GetTruckAsync(reference) ?? throw new InvalidOperationException("Truck/reference not found.");
        var message = _templates.BuildMessage(truck, eventType, channel);
        var status = channel switch
        {
            NotificationChannel.InApp => await _inApp.SendAsync(truck, message),
            NotificationChannel.WhatsApp => await _whatsApp.SendAsync(truck, message),
            NotificationChannel.AndroidPush => await _push.SendAsync(truck, message),
            _ => NotificationStatus.Failed
        };
        var notification = new DriverNotificationDto
        {
            TruckReference = truck.BookingReference,
            RecipientName = truck.DriverName,
            RecipientContact = truck.DriverContactPlaceholder,
            FleetOperator = truck.FleetOperatorName,
            Channel = channel,
            Message = message,
            Status = status,
            EventType = eventType,
            RelatedInstructionReference = truck.CurrentInstruction.Reference,
            Timestamp = DateTime.UtcNow
        };
        History.AddOrUpdate(truck.BookingReference, _ => new List<DriverNotificationDto> { notification }, (_, list) => { lock (list) list.Add(notification); return list; });
        return notification;
    }

    public async Task<IReadOnlyList<DriverNotificationDto>> GetHistoryAsync(string reference)
    {
        var truck = await _queue.GetTruckAsync(reference);
        if (truck == null) return Array.Empty<DriverNotificationDto>();
        var seeded = truck.NotificationHistory;
        if (!History.TryGetValue(truck.BookingReference, out var sent)) return seeded;
        lock (sent) return seeded.Concat(sent).OrderByDescending(n => n.Timestamp).ToList();
    }
}

public class InAppNotificationService : IInAppNotificationService
{
    public Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message) => Task.FromResult(NotificationStatus.SimulatedSent);
}

public class SimulatedWhatsAppNotificationSender : IWhatsAppNotificationSender
{
    public Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message) => Task.FromResult(NotificationStatus.SimulatedSent);
}

public class WhatsAppCloudApiNotificationSender : IWhatsAppNotificationSender
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    public WhatsAppCloudApiNotificationSender(HttpClient http, IConfiguration config) { _http = http; _config = config; }
    public async Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message)
    {
        var enabled = string.Equals(_config["SMARTPORT_WHATSAPP_ENABLED"], "true", StringComparison.OrdinalIgnoreCase);
        var token = _config["SMARTPORT_WHATSAPP_ACCESS_TOKEN"];
        var phoneNumberId = _config["SMARTPORT_WHATSAPP_PHONE_NUMBER_ID"];
        if (!enabled || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneNumberId)) return NotificationStatus.ConnectorNotConfigured;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v19.0/{phoneNumberId}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(new { messaging_product = "whatsapp", to = truck.DriverContactPlaceholder.Replace("+", string.Empty).Replace(" ", string.Empty), type = "text", text = new { body = message } }), Encoding.UTF8, "application/json");
            using var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode ? NotificationStatus.Sent : NotificationStatus.Failed;
        }
        catch { return NotificationStatus.Failed; }
    }
}

public class SimulatedPushNotificationSender : IPushNotificationSender
{
    public Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message) => Task.FromResult(NotificationStatus.SimulatedSent);
}

public class MobileDeviceRegistrationService : IMobileDeviceRegistrationService
{
    private static readonly ConcurrentDictionary<string, MobileDeviceRegistrationDto> Devices = new(StringComparer.OrdinalIgnoreCase);
    public Task RegisterAsync(MobileDeviceRegistrationDto registration) { Devices[$"{registration.Reference}:{registration.DeviceToken}"] = registration; return Task.CompletedTask; }
    public Task UnregisterAsync(string reference, string deviceToken) { Devices.TryRemove($"{reference}:{deviceToken}", out _); return Task.CompletedTask; }
}
