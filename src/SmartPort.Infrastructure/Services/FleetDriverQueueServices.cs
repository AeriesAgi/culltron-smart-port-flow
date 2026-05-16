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
    private readonly IConfiguration _config;
    private readonly IQueueOptimizationService _optimizer;
    private readonly ILocationEtaService _eta;
    private static readonly ConcurrentDictionary<string, DriverAcknowledgement> Acknowledgements = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, FleetTruckDto> StateOverrides = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, DriverContactProfileDto> ManualContacts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, FleetTruckDto> ManualTrucks = new(StringComparer.OrdinalIgnoreCase);

    public DemoFleetDriverQueueService(IConfiguration config, IQueueOptimizationService optimizer, ILocationEtaService eta) { _config = config; _optimizer = optimizer; _eta = eta; }

    public async Task<FleetQueueSummaryDto> GetFleetSummaryAsync(string? fleetOperatorId = null)
    {
        var operators = BuildOperators();
        var trucks = (await GetTrucksAsync(fleetOperatorId)).ToList();
        var selected = operators.FirstOrDefault(o => string.Equals(o.Id, fleetOperatorId, StringComparison.OrdinalIgnoreCase)) ?? operators.First();
        var plan = BuildExecutionPlan(trucks, "Current control-room congestion snapshot");
        return new FleetQueueSummaryDto
        {
            SelectedFleet = selected, FleetOperators = operators, Trucks = trucks, TotalTrucks = trucks.Count,
            TrucksWaiting = trucks.Count(t => t.CurrentStatus is QueueTruckStatus.Waiting or QueueTruckStatus.EnRoute or QueueTruckStatus.LocationShared),
            TrucksHolding = trucks.Count(t => t.CurrentStatus is QueueTruckStatus.HoldPosition or QueueTruckStatus.Holding or QueueTruckStatus.AtStaging or QueueTruckStatus.AtStagingHolding),
            TrucksProceeding = trucks.Count(t => t.CurrentStatus == QueueTruckStatus.ProceedToGate),
            DelayedOrRescheduledTrucks = trucks.Count(t => t.CurrentStatus is QueueTruckStatus.Delayed or QueueTruckStatus.Rescheduled or QueueTruckStatus.Exception),
            TotalIdlingMinutesAvoided = trucks.Sum(t => t.EstimatedIdlingMinutesAvoided), TotalCo2KgAvoided = trucks.Sum(t => t.EstimatedCo2KgAvoided),
            LatestAiQueueRecommendation = "Execution plan: release ready trucks to uncongested gates, hold high-risk arrivals, request location check-ins, and use staging to reduce idling.",
            LatestNotifications = trucks.SelectMany(t => t.NotificationHistory).OrderByDescending(n => n.Timestamp).Take(10).ToList(),
            HighRiskTrucks = trucks.Where(t => t.DelayRisk is QueueDelayRisk.High or QueueDelayRisk.Critical).ToList(), DataSources = (await GetDataSourcesAsync()).ToList(), WhatsAppStatus = GetWhatsAppConnectorStatus(),
            GateCapacities = BuildGateCapacities(trucks), StagingAreas = BuildStagingAreas(), BerthReadiness = BuildBerthReadiness(trucks), LatestExecutionPlan = plan
        };
    }

    public Task<IReadOnlyList<FleetTruckDto>> GetTrucksAsync(string? fleetOperatorId = null)
    {
        var trucks = BuildTrucks().Concat(ManualTrucks.Values.Select(CloneTruck)).ToList();
        foreach (var truck in trucks)
        {
            ApplyManualContact(truck); truck.NotificationHistory = SeedNotifications(truck); ApplyStateOverride(truck);
            if (Acknowledgements.TryGetValue(truck.BookingReference, out var ack) || Acknowledgements.TryGetValue(truck.TruckRegistration, out ack) || Acknowledgements.TryGetValue(truck.JobReference, out ack)) truck.AcknowledgementState = ack;
            truck.AllowedNextActions = OperationalStateMachineService.GetAllowedActions(truck.CurrentStatus).ToList();
            truck.LatestNotification = truck.NotificationHistory.OrderByDescending(n => n.Timestamp).FirstOrDefault()?.Message ?? truck.LatestNotification;
        }
        IReadOnlyList<FleetTruckDto> result = string.IsNullOrWhiteSpace(fleetOperatorId) ? trucks : trucks.Where(t => string.Equals(t.FleetOperatorId, fleetOperatorId, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult(result);
    }

    public async Task<FleetTruckDto?> GetTruckAsync(string referenceOrId) => string.IsNullOrWhiteSpace(referenceOrId) ? null : (await GetTrucksAsync()).FirstOrDefault(t => IsTruckMatch(t, referenceOrId.Trim()));
    public Task<IReadOnlyList<string>> GetDemoReferencesAsync() => Task.FromResult<IReadOnlyList<string>>(BuildTrucks().Select(t => t.BookingReference).Take(6).ToList());
    public Task<IReadOnlyList<DriverContactProfileDto>> GetDriverContactsAsync() => Task.FromResult<IReadOnlyList<DriverContactProfileDto>>(BuildTrucks().Select(t => t.DriverContact).Concat(ManualContacts.Values).OrderBy(c => c.DriverName).ToList());

    public Task<DriverContactProfileDto> SaveDriverContactAsync(SaveDriverContactRequestDto request)
    {
        var normalized = NormalizeWhatsApp(request.WhatsAppCountryCode, request.WhatsAppNumber, out var validation); if (!string.IsNullOrEmpty(validation)) throw new ArgumentException(validation);
        var fleet = BuildOperators().FirstOrDefault(o => string.Equals(o.Id, request.AssignedFleetOperatorId, StringComparison.OrdinalIgnoreCase));
        var profile = new DriverContactProfileDto { DriverName = Clean(request.DriverName), LicencePermitPlaceholder = Clean(request.LicencePermitPlaceholder), WhatsAppCountryCode = Clean(request.WhatsAppCountryCode), WhatsAppNumber = Clean(request.WhatsAppNumber), NormalizedWhatsAppNumber = normalized, BackupContact = Clean(request.BackupContact), NotificationPreference = request.NotificationPreference, WhatsAppConsentConfirmed = request.WhatsAppConsentConfirmed, TestApproved = request.TestApproved, IsActive = request.IsActive, AssignedTruckRegistration = Clean(request.AssignedTruckRegistration).ToUpperInvariant(), AssignedFleetOperatorId = request.AssignedFleetOperatorId, AssignedFleetOperatorName = fleet?.Name ?? request.AssignedFleetOperatorId, IsDemoSeedRecord = false, Source = DataProvenanceType.ManualOperatorInput };
        ManualContacts[profile.AssignedTruckRegistration] = profile; return Task.FromResult(profile);
    }

    public Task<FleetTruckDto> SaveTruckAsync(SaveFleetQueueTruckRequestDto request)
    {
        var fleet = BuildOperators().FirstOrDefault(o => string.Equals(o.Id, request.FleetOperatorId, StringComparison.OrdinalIgnoreCase)) ?? BuildOperators().First();
        var booking = string.IsNullOrWhiteSpace(request.BookingReference) ? $"SPQ-DEMO-{DateTime.UtcNow:HHmmss}" : Clean(request.BookingReference).ToUpperInvariant();
        var truck = new FleetTruckDto { Id = string.IsNullOrWhiteSpace(request.Id) ? booking : Clean(request.Id), FleetOperatorId = fleet.Id, FleetOperatorName = fleet.Name, TruckRegistration = Clean(request.TruckRegistration).ToUpperInvariant(), DriverName = string.IsNullOrWhiteSpace(request.AssignedDriverName) ? "Unassigned demo driver" : Clean(request.AssignedDriverName), VehicleType = string.IsNullOrWhiteSpace(request.VehicleType) ? "Container tractor" : Clean(request.VehicleType), DriverContactPlaceholder = "MANUAL-OPERATOR-INPUT", DriverContact = new DriverContactProfileDto { DriverName = string.IsNullOrWhiteSpace(request.AssignedDriverName) ? "Unassigned demo driver" : Clean(request.AssignedDriverName), AssignedTruckRegistration = Clean(request.TruckRegistration).ToUpperInvariant(), AssignedFleetOperatorId = fleet.Id, AssignedFleetOperatorName = fleet.Name, IsDemoSeedRecord = false, Source = DataProvenanceType.ManualOperatorInput }, ContainerReference = Clean(request.ContainerReference).ToUpperInvariant(), JobReference = Clean(request.JobReference).ToUpperInvariant(), BookingReference = booking, AppointmentTime = request.AppointmentTime, EtaCallForwardTime = request.EtaCallForwardTime, QueueNumber = ManualTrucks.Count + 50, AssignedGate = Clean(request.AssignedGate), BerthYardStagingZone = Clean(request.BerthYardStagingZone), CurrentStatus = request.CurrentStatus, DelayRisk = QueueDelayRisk.Medium, EstimatedIdlingMinutesAvoided = 10, EstimatedCo2KgAvoided = 13.5m, LatestNotification = $"Manual truck/job record saved for {booking}.", WhatsAppNotificationStatus = NotificationStatus.Pending, InAppNotificationStatus = NotificationStatus.SimulatedSent, LastUpdated = DateTime.UtcNow, LastKnownLocationLabel = "Manual operator input", Source = DataProvenanceType.ManualOperatorInput };
        var opt = _optimizer.Optimize(truck); truck.CurrentInstruction = BuildInstruction(truck, opt); truck.AllowedNextActions = opt.AllowedNextActions;
        truck.Timeline.Add(new TruckStatusTimelineDto { Timestamp = truck.LastUpdated, Status = truck.CurrentStatus, Source = DataProvenanceType.ManualOperatorInput, Actor = "Fleet owner", Note = "Truck/job created or edited by fleet owner demo workflow." });
        truck.AuditTrail.Add(Audit("Fleet owner", "ManualTruckSaved", null, truck.CurrentStatus, "Truck/job created or edited.", DataProvenanceType.ManualOperatorInput));
        truck.NotificationHistory.Add(Notification(truck, NotificationChannel.InApp, NotificationStatus.SimulatedSent, NotificationEventType.QueueStatusChanged, truck.LatestNotification, DataProvenanceType.ManualOperatorInput));
        ManualTrucks[truck.BookingReference] = truck; StateOverrides[truck.BookingReference] = truck; return Task.FromResult(truck);
    }

    public Task<IReadOnlyList<DataSourceReadinessDto>> GetDataSourcesAsync() => Task.FromResult<IReadOnlyList<DataSourceReadinessDto>>(new List<DataSourceReadinessDto>
    {
        Source("Demo", DataProvenanceType.SyntheticDemoData, "Synthetic demo queue, truck, berth, gate, and fleet records", "IPMS / PCS / TOS / appointment / berth / yard / fleet systems", "Jobs, booking refs, capacity, appointment windows", "Map connector feeds into queue DTOs/services", "Demo-ready"),
        Source("Gate System Placeholder", DataProvenanceType.ExternalPortFeedPlaceholder, "Synthetic gate queue and capacity signals", "Gate appointment/OCR/RFID systems", "Queue length, truck pre-advice, check-in/out", "Webhook/API connector updates truck status timeline", "Connector-ready"),
        Source("Berth Feed Placeholder", DataProvenanceType.ExternalPortFeedPlaceholder, "Synthetic berth readiness signals", "Berth planning / terminal operating systems", "Berth readiness, crane windows, stack availability", "Feeds QueueOptimizationService", "Connector-ready"),
        Source("Yard Feed Placeholder", DataProvenanceType.ExternalPortFeedPlaceholder, "Synthetic yard/staging capacity", "Yard management systems", "Zone capacity, stack readiness, staging load", "Capacity model influences execution plan", "Connector-ready"),
        Source("Fleet Feed Placeholder", DataProvenanceType.ExternalPortFeedPlaceholder, "Demo trucks and drivers", "Fleet TMS/ERP and future GPS/telematics", "Truck, driver, ETA, assignment", "Fleet connector updates assignments", "Pilot mapping required"),
        Source("WhatsApp Check-In", DataProvenanceType.WhatsAppDriverCheckIn, "Simulated WhatsApp location check-ins", "WhatsApp Cloud API inbound location webhook", "Driver-shared location and timestamp", "Webhook updates ETA and timeline", "Demo-ready, LiveTest gated"),
        Source("Android App", DataProvenanceType.AndroidDriverApp, "Android companion API calls", "Smart Port Driver Companion", "Reference lookup, acknowledgements, commands", "Mobile APIs update shared backend state", "Demo-ready"),
        Source("AI", DataProvenanceType.GeminiAiRecommendation, "Gemini explanation when configured", "Gemini Agent Mode", "Operational context and queue plan", "Phrases explanations only; fallback decides", "Optional"),
        Source("Fallback", DataProvenanceType.DeterministicFallback, "Rule-based queue optimizer", "Operational business rules", "Gate congestion, berth readiness, location, risk", "Always-on decision engine", "Demo-ready")
    });

    public WhatsAppConnectorStatusDto GetWhatsAppConnectorStatus()
    {
        var mode = ParseMode(_config["SMARTPORT_WHATSAPP_MODE"] ?? _config["SMARTPORT_NOTIFICATION_MODE"]);
        var enabled = IsTrue(_config["SMARTPORT_WHATSAPP_ENABLED"]);
        var accessToken = !string.IsNullOrWhiteSpace(_config["SMARTPORT_WHATSAPP_ACCESS_TOKEN"]);
        var phoneNumberId = !string.IsNullOrWhiteSpace(_config["SMARTPORT_WHATSAPP_PHONE_NUMBER_ID"]);
        var businessAccountId = !string.IsNullOrWhiteSpace(_config["SMARTPORT_WHATSAPP_BUSINESS_ACCOUNT_ID"]);
        var verifyToken = !string.IsNullOrWhiteSpace(_config["SMARTPORT_WHATSAPP_VERIFY_TOKEN"]);
        var publicBaseUrl = (_config["SMARTPORT_PUBLIC_BASE_URL"] ?? string.Empty).Trim().TrimEnd('/');
        var graph = string.IsNullOrWhiteSpace(_config["SMARTPORT_WHATSAPP_GRAPH_VERSION"]) ? "v22.0" : _config["SMARTPORT_WHATSAPP_GRAPH_VERSION"]!.Trim();
        var approvedDriverAvailable = BuildTrucks().Select(t => t.DriverContact).Concat(ManualContacts.Values).Any(c => c.IsLiveWhatsAppSafe);
        var checks = new List<string>
        {
            mode is WhatsAppMode.LiveTest or WhatsAppMode.Live ? $"Mode is {mode}" : "Mode is not LiveTest/Live",
            enabled ? "WhatsApp enabled" : "WhatsApp not enabled",
            accessToken ? "Access token configured" : "Access token missing",
            phoneNumberId ? "Phone number ID configured" : "Phone number ID missing",
            verifyToken ? "Webhook verify token configured" : "Webhook verify token missing",
            !string.IsNullOrWhiteSpace(publicBaseUrl) ? "Public base URL configured" : "Public base URL missing",
            approvedDriverAvailable || !string.IsNullOrWhiteSpace(_config["SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER"]) ? "Approved consented tester driver or test recipient available" : "No approved consented tester driver/test recipient with valid WhatsApp number"
        };
        var configured = accessToken && phoneNumberId;
        return new WhatsAppConnectorStatusDto
        {
            Mode = mode, Enabled = enabled, AccessTokenConfigured = accessToken, PhoneNumberIdConfigured = phoneNumberId, BusinessAccountIdConfigured = businessAccountId,
            VerifyTokenConfigured = verifyToken, PublicBaseUrlConfigured = !string.IsNullOrWhiteSpace(publicBaseUrl), GraphVersion = graph, PublicBaseUrl = publicBaseUrl,
            WebhookCallbackUrl = string.IsNullOrWhiteSpace(publicBaseUrl) ? "Set SMARTPORT_PUBLIC_BASE_URL to show callback URL" : $"{publicBaseUrl}/webhooks/whatsapp",
            ApprovedDriverAvailable = approvedDriverAvailable || !string.IsNullOrWhiteSpace(_config["SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER"]), CredentialsConfigured = configured, LiveSendingAllowed = (mode is WhatsAppMode.LiveTest or WhatsAppMode.Live) && enabled && configured && verifyToken && !string.IsNullOrWhiteSpace(publicBaseUrl) && (approvedDriverAvailable || !string.IsNullOrWhiteSpace(_config["SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER"])),
            LiveTestReadinessChecks = checks,
            SafetyMessage = mode switch { WhatsAppMode.Demo => "Demo Mode: no external WhatsApp calls; simulated messages are stored in history.", WhatsAppMode.ConnectorReady => "ConnectorReady Mode: credentials can be checked, but live sending is blocked until LiveTest/Live.", WhatsAppMode.Live => "Live Mode: external sends require credentials, webhook, and approved/consented recipients.", _ => "LiveTest Mode: sends only go to SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER or manually added, active, approved and consented tester numbers." }
        };
    }

    public async Task<FleetTruckDto?> AcknowledgeAsync(string reference, DriverAcknowledgement acknowledgement)
    {
        Acknowledgements[reference] = acknowledgement;
        var action = acknowledgement switch { DriverAcknowledgement.Holding => OperationalActionType.ConfirmHolding, DriverAcknowledgement.Proceeding => OperationalActionType.ProceedingToGate, _ => OperationalActionType.RefreshStatus };
        var eventType = acknowledgement switch { DriverAcknowledgement.Holding => DriverEventType.DriverConfirmedHolding, DriverAcknowledgement.Proceeding => DriverEventType.DriverProceedingToGate, _ => DriverEventType.DriverAcknowledgedInstruction };
        return await RecordDriverEventAsync(reference, eventType, DataProvenanceType.AndroidDriverApp, $"Driver acknowledgement: {acknowledgement}", "Driver");
    }

    public async Task<FleetTruckDto?> RecordDriverEventAsync(string reference, DriverEventType eventType, DataProvenanceType source, string? note = null, string actor = "Driver")
    {
        var truck = await GetTruckAsync(reference); if (truck == null) return null; var updated = CloneTruck(truck); var old = updated.CurrentStatus;
        var requested = EventToAction(eventType); var sm = new OperationalStateMachineService(this);
        if (!sm.CanTransition(old, requested, out var newStatus, out var message)) { updated.NotificationHistory.Add(Notification(updated, NotificationChannel.InApp, NotificationStatus.Failed, NotificationEventType.DriverConfirmation, message, source)); StateOverrides[updated.BookingReference] = updated; return updated; }
        updated.CurrentStatus = newStatus; updated.LastUpdated = DateTime.UtcNow; updated.DriverAvailabilityStatus = eventType switch { DriverEventType.DriverBreak => "Unavailable: break 15", DriverEventType.DriverLunch => "Unavailable: lunch 30", DriverEventType.DriverDelayed => "Delayed by driver", DriverEventType.DriverReady => "Available", _ => updated.DriverAvailabilityStatus };
        var opt = _optimizer.Optimize(updated); updated.CurrentInstruction = BuildInstruction(updated, opt); updated.EtaCallForwardTime = opt.EtaCallForwardTime; updated.DelayRisk = opt.DelayRisk; updated.EstimatedIdlingMinutesAvoided = opt.IdlingMinutesAvoided; updated.EstimatedCo2KgAvoided = opt.Co2KgAvoided; updated.QueueEfficiencyScore = opt.ConfidenceScore; updated.AllowedNextActions = opt.AllowedNextActions;
        var instruction = note ?? message; updated.LatestNotification = opt.NotificationMessage;
        updated.Timeline.Add(new TruckStatusTimelineDto { Timestamp = updated.LastUpdated, Status = updated.CurrentStatus, DriverEventType = eventType, Source = source, Actor = actor, Note = instruction });
        updated.AuditTrail.Add(Audit(actor, eventType.ToString(), old, updated.CurrentStatus, instruction, source));
        updated.NotificationHistory.Add(Notification(updated, source == DataProvenanceType.AndroidDriverApp ? NotificationChannel.AndroidPush : NotificationChannel.InApp, NotificationStatus.SimulatedSent, NotificationEventType.DriverConfirmation, instruction, source));
        StateOverrides[updated.BookingReference] = updated; return updated;
    }

    public async Task<FleetTruckDto?> RecordLocationCheckInAsync(string reference, decimal? latitude, decimal? longitude, string? label, DataProvenanceType source, string actor = "Driver")
    {
        var truck = await GetTruckAsync(reference); if (truck == null) return null; var updated = CloneTruck(truck); var old = updated.CurrentStatus; var loc = _eta.Estimate(updated.BookingReference, updated.AssignedGate, updated.BerthYardStagingZone, latitude, longitude, label, source);
        updated.LastLocationCheckIn = loc; updated.LastKnownLocationLabel = loc.LocationLabel; updated.EtaCallForwardTime = loc.EstimatedArrivalTime; updated.CurrentStatus = loc.DistanceToAssignedGateKm <= 0.5m ? QueueTruckStatus.ProceedToGate : loc.DistanceToStagingKm <= 0.5m ? QueueTruckStatus.AtStaging : QueueTruckStatus.LocationShared;
        var opt = _optimizer.Optimize(updated); updated.CurrentInstruction = BuildInstruction(updated, opt); updated.LastUpdated = loc.Timestamp; updated.EstimatedIdlingMinutesAvoided = opt.IdlingMinutesAvoided; updated.EstimatedCo2KgAvoided = opt.Co2KgAvoided; updated.DelayRisk = opt.DelayRisk; updated.AllowedNextActions = opt.AllowedNextActions; updated.LatestNotification = $"WhatsApp location check-in received: {loc.LocationLabel}. ETA {loc.EstimatedArrivalTime:HH:mm}.";
        updated.Timeline.Add(new TruckStatusTimelineDto { Timestamp = loc.Timestamp, Status = updated.CurrentStatus, DriverEventType = DriverEventType.WhatsAppLocationShared, Source = source, Actor = actor, Note = $"WhatsApp location check-in: {loc.LocationLabel}, {loc.DistanceToAssignedGateKm:N1} km from gate." });
        updated.AuditTrail.Add(Audit(actor, "LocationCheckIn", old, updated.CurrentStatus, updated.LatestNotification, source)); updated.NotificationHistory.Add(Notification(updated, NotificationChannel.WhatsApp, NotificationStatus.SimulatedSent, NotificationEventType.WhatsAppLocationCheckIn, updated.LatestNotification, source));
        StateOverrides[updated.BookingReference] = updated; return updated;
    }

    private static List<FleetOperatorDto> BuildOperators() => new() { new() { Id = "durban-freight", Name = "Durban Freight Logistics", ContactPerson = "Naledi Mokoena" }, new() { Id = "kzn-cold-chain", Name = "KZN Cold Chain Carriers", ContactPerson = "Asha Pillay" }, new() { Id = "bayline-container", Name = "Bayline Container Transport", ContactPerson = "Warren Jacobs" }, new() { Id = "harborlink", Name = "HarborLink Haulage", ContactPerson = "Sibusiso Dlamini" } };
    private static List<FleetTruckDto> BuildTrucks() { var now = DateTime.UtcNow; return new() { Truck("SPQ-2026-0042", "durban-freight", "Durban Freight Logistics", "KZN-482-TR", "Thabo Ndlovu", "CONT-DUR-88421", "JOB-DUR-0042", now.AddMinutes(35), 14, "Gate 3", "Staging Area A", QueueTruckStatus.Holding, now.AddMinutes(18), "Hold position for 18 minutes to avoid Gate 3 congestion.", "Gate 3 is above threshold while Berth 204 clears imports.", QueueDelayRisk.High, 31, 42.8m), Truck("SPQ-2026-0043", "kzn-cold-chain", "KZN Cold Chain Carriers", "KZN-771-RF", "Amina Khan", "REEF-2219-ZA", "JOB-REEF-0043", now.AddMinutes(20), 7, "Gate 2", "Reefer Yard R2", QueueTruckStatus.ProceedToGate, now.AddMinutes(12), "Proceed to Gate 2 within the next 12 minutes.", "Berth slot is ready and Gate 2 queue has dropped.", QueueDelayRisk.Medium, 19, 24.6m), Truck("SPQ-2026-0044", "bayline-container", "Bayline Container Transport", "ND-119-CT", "Musa Zulu", "MSKU-781244-3", "JOB-BAY-0044", now.AddMinutes(55), 22, "Gate 4", "Yard Block C", QueueTruckStatus.Delayed, now.AddMinutes(30), "Delay arrival by 30 minutes due to berth congestion.", "Late vessel move has constrained Yard Block C.", QueueDelayRisk.Critical, 45, 63.1m), Truck("SPQ-2026-0045", "harborlink", "HarborLink Haulage", "DBN-908-HL", "Priya Naidoo", "HLX-EXP-4450", "JOB-HLX-0045", now.AddMinutes(15), 3, "Gate 1", "Export Stack E1", QueueTruckStatus.AtGate, now.AddMinutes(5), "Remain in Gate 1 lane and prepare export reference.", "Gate pre-check is complete.", QueueDelayRisk.Low, 12, 15.4m), Truck("SPQ-2026-0046", "durban-freight", "Durban Freight Logistics", "KZN-652-TR", "Lerato Dube", "CONT-DUR-88422", "JOB-DUR-0046", now.AddMinutes(70), 29, "Gate 3", "Off-site Holding", QueueTruckStatus.Rescheduled, now.AddMinutes(40), "Remain off-site until called forward.", "Road congestion and terminal stack pressure make off-site holding cleanest.", QueueDelayRisk.High, 38, 51.9m), Truck("SPQ-2026-0047", "kzn-cold-chain", "KZN Cold Chain Carriers", "KZN-314-RF", "Sipho Mthembu", "REEF-9981-ZA", "JOB-REEF-0047", now.AddMinutes(42), 11, "Gate 2", "Staging Area B", QueueTruckStatus.Waiting, now.AddMinutes(24), "Move to Staging Area B to reduce idling.", "Reefer plug capacity is constrained.", QueueDelayRisk.Medium, 22, 29.7m) }; }
    private static FleetTruckDto Truck(string booking, string opId, string op, string reg, string driver, string container, string job, DateTime appt, int q, string gate, string zone, QueueTruckStatus status, DateTime eta, string instruction, string reason, QueueDelayRisk risk, int idle, decimal co2) { var updated = DateTime.UtcNow.AddMinutes(-q); var contact = new DriverContactProfileDto { DriverName = driver, WhatsAppNumber = "DEMO-ONLY-NOT-LIVE", NormalizedWhatsAppNumber = "DEMO-ONLY-NOT-LIVE", BackupContact = "Fleet dispatcher demo contact", AssignedTruckRegistration = reg, AssignedFleetOperatorId = opId, AssignedFleetOperatorName = op, IsDemoSeedRecord = true, IsActive = true, Source = DataProvenanceType.SyntheticDemoData, ValidationMessage = "Demo seed contact. Add a tester-owned number and mark Consent + Test Approved before LiveTest WhatsApp sends." }; var truck = new FleetTruckDto { Id = booking, FleetOperatorId = opId, FleetOperatorName = op, TruckRegistration = reg, DriverName = driver, DriverContactPlaceholder = "DEMO-ONLY-NOT-LIVE", DriverContact = contact, ContainerReference = container, JobReference = job, BookingReference = booking, AppointmentTime = appt, QueueNumber = q, AssignedGate = gate, BerthYardStagingZone = zone, CurrentStatus = status, EtaCallForwardTime = eta, CurrentInstruction = new QueueInstructionDto { Reference = $"QI-{Suffix(booking)}", Instruction = instruction, Reason = reason, Explanation = BuildFallbackExplanation(status, risk, idle, co2), Source = DataProvenanceType.DeterministicFallback, GeneratedAt = updated }, DelayRisk = risk, EstimatedIdlingMinutesAvoided = idle, EstimatedCo2KgAvoided = co2, LatestNotification = $"Smart Port Update: Truck {reg} should {instruction.ToLowerInvariant()} Estimated gate call-forward: {eta:HH:mm}. Ref: {booking}.", WhatsAppNotificationStatus = NotificationStatus.SimulatedSent, InAppNotificationStatus = NotificationStatus.SimulatedSent, LastUpdated = updated, Source = DataProvenanceType.SyntheticDemoData, LastKnownLocationLabel = "Demo queue feed", AllowedNextActions = OperationalStateMachineService.GetAllowedActions(status).ToList() }; truck.Timeline = new() { new() { Timestamp = appt.AddMinutes(-60), Status = QueueTruckStatus.Scheduled, Note = "Appointment confirmed by synthetic Smart Port demo feed.", Source = DataProvenanceType.SyntheticDemoData }, new() { Timestamp = updated, Status = status, Note = instruction, Source = DataProvenanceType.DeterministicFallback } }; truck.AuditTrail.Add(Audit("Smart Port", "FallbackRecommendationGenerated", null, status, reason, DataProvenanceType.DeterministicFallback)); truck.NotificationHistory = SeedNotifications(truck); return truck; }

    private static void ApplyManualContact(FleetTruckDto truck) { if (!ManualContacts.TryGetValue(truck.TruckRegistration, out var contact)) return; truck.DriverName = contact.DriverName; truck.DriverContact = contact; truck.DriverContactPlaceholder = contact.NormalizedWhatsAppNumber; }
    private static void ApplyStateOverride(FleetTruckDto truck) { if (!StateOverrides.TryGetValue(truck.BookingReference, out var u)) return; truck.CurrentStatus = u.CurrentStatus; truck.EtaCallForwardTime = u.EtaCallForwardTime; truck.CurrentInstruction = u.CurrentInstruction; truck.DelayRisk = u.DelayRisk; truck.EstimatedIdlingMinutesAvoided = u.EstimatedIdlingMinutesAvoided; truck.EstimatedCo2KgAvoided = u.EstimatedCo2KgAvoided; truck.QueueEfficiencyScore = u.QueueEfficiencyScore; truck.LatestNotification = u.LatestNotification; truck.LastUpdated = u.LastUpdated; truck.DriverAvailabilityStatus = u.DriverAvailabilityStatus; truck.LastKnownLocationLabel = u.LastKnownLocationLabel; truck.LastLocationCheckIn = u.LastLocationCheckIn; truck.AllowedNextActions = u.AllowedNextActions; truck.Timeline = u.Timeline; truck.AuditTrail = u.AuditTrail; truck.NotificationHistory = u.NotificationHistory; }
    private static bool IsTruckMatch(FleetTruckDto t, string key) => string.Equals(t.Id, key, StringComparison.OrdinalIgnoreCase) || string.Equals(t.TruckRegistration, key, StringComparison.OrdinalIgnoreCase) || string.Equals(t.BookingReference, key, StringComparison.OrdinalIgnoreCase) || string.Equals(t.JobReference, key, StringComparison.OrdinalIgnoreCase) || string.Equals(t.ContainerReference, key, StringComparison.OrdinalIgnoreCase);
    private static string Clean(string? value) => (value ?? string.Empty).Trim(); private static string Suffix(string value, int length = 4) => string.IsNullOrEmpty(value) ? "DEMO" : value.Substring(Math.Max(0, value.Length - length)); private static bool IsTrue(string? value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
    private static string NormalizeWhatsApp(string countryCode, string number, out string validation) { validation = string.Empty; if (string.IsNullOrWhiteSpace(countryCode) || !countryCode.Trim().StartsWith('+')) { validation = "WhatsApp country code is required, e.g. South Africa +27 or international +<countrycode>."; return string.Empty; } var cc = new string(countryCode.Trim().Where(c => char.IsDigit(c) || c == '+').ToArray()); var digits = new string((number ?? string.Empty).Where(char.IsDigit).ToArray()).TrimStart('0'); if (cc.Length < 2 || digits.Length < 6) { validation = "Invalid WhatsApp number format. Use +<countrycode> and the subscriber number."; return string.Empty; } return cc + digits; }
    private static DataSourceReadinessDto Source(string badge, DataProvenanceType type, string demo, string real, string data, string plugin, string readiness) => new() { Badge = badge, SourceType = type, CurrentDemoSource = demo, RealWorldSource = real, RequiredData = data, SmartPortPlugIn = plugin, PilotReadinessLevel = readiness };
    private static WhatsAppMode ParseMode(string? value) => Enum.TryParse<WhatsAppMode>(value, true, out var mode) ? mode : WhatsAppMode.Demo;
    private static string BuildFallbackExplanation(QueueTruckStatus status, QueueDelayRisk risk, int idle, decimal co2) => status switch { QueueTruckStatus.Holding or QueueTruckStatus.HoldPosition => $"Gate congestion is active, so holding prevents spillback and avoids about {idle} idling minutes ({co2:N1} kg CO2).", QueueTruckStatus.ProceedToGate => "Berth readiness and a low gate queue make this the cleanest call-forward window.", QueueTruckStatus.Delayed or QueueTruckStatus.Rescheduled => $"A disruption or berth/yard constraint is active. Rescheduling reduces gate pressure and avoids about {idle} minutes of diesel idling.", _ when risk == QueueDelayRisk.High => "High idling risk detected; Smart Port keeps the vehicle staged until capacity improves.", _ => "Queue, berth, gate and emissions signals are balanced for the current instruction." };
    private static QueueInstructionDto BuildInstruction(FleetTruckDto truck, QueueOptimizationResultDto opt) => new() { Reference = $"QI-{Suffix(truck.BookingReference)}-{DateTime.UtcNow:HHmmss}", Instruction = opt.DriverFriendlyInstruction, Reason = opt.Reason, Explanation = opt.Explanation, Source = DataProvenanceType.DeterministicFallback, GeneratedAt = DateTime.UtcNow };
    private static OperationalActionType EventToAction(DriverEventType e) => e switch { DriverEventType.DriverConfirmedHolding => OperationalActionType.ConfirmHolding, DriverEventType.DriverArrivedAtStaging => OperationalActionType.ArrivedAtStaging, DriverEventType.DriverProceedingToGate => OperationalActionType.ProceedingToGate, DriverEventType.DriverArrivedAtGate => OperationalActionType.ArrivedAtGate, DriverEventType.DriverCompletedJob => OperationalActionType.CompleteJob, DriverEventType.DriverReady => OperationalActionType.Ready, DriverEventType.DriverBreak => OperationalActionType.Break15, DriverEventType.DriverLunch => OperationalActionType.Lunch30, DriverEventType.DriverDelayed => OperationalActionType.Delayed20, DriverEventType.DriverIssueReported => OperationalActionType.ReportIssue, DriverEventType.WhatsAppLocationRequested => OperationalActionType.RequestLocation, DriverEventType.WhatsAppLocationShared => OperationalActionType.ShareLocation, _ => OperationalActionType.RefreshStatus };
    private static DriverNotificationDto Notification(FleetTruckDto truck, NotificationChannel channel, NotificationStatus status, NotificationEventType eventType, string message, DataProvenanceType source) => new() { TruckReference = truck.BookingReference, RecipientName = truck.DriverName, RecipientContact = channel == NotificationChannel.WhatsApp ? truck.DriverContact.NormalizedWhatsAppNumber : truck.DriverContact.BackupContact, FleetOperator = truck.FleetOperatorName, Channel = channel, Message = message, Status = status, EventType = eventType, RelatedInstructionReference = truck.CurrentInstruction.Reference, Source = source, Timestamp = DateTime.UtcNow };
    private static OperationalAuditEntryDto Audit(string actor, string eventType, QueueTruckStatus? oldStatus, QueueTruckStatus? newStatus, string reason, DataProvenanceType source) => new() { Actor = actor, EventType = eventType, OldStatus = oldStatus, NewStatus = newStatus, Reason = reason, Source = source, PublicSafeNote = reason };
    private static List<DriverNotificationDto> SeedNotifications(FleetTruckDto truck) => new() { Notification(truck, NotificationChannel.InApp, NotificationStatus.SimulatedSent, NotificationEventType.AiInstructionUpdated, truck.LatestNotification, DataProvenanceType.DeterministicFallback), new() { TruckReference = truck.BookingReference, RecipientName = truck.DriverName, RecipientContact = "DEMO-ONLY-NOT-LIVE", FleetOperator = truck.FleetOperatorName, Channel = NotificationChannel.WhatsApp, Message = truck.LatestNotification, Status = NotificationStatus.SimulatedSent, EventType = NotificationEventType.AiInstructionUpdated, RelatedInstructionReference = truck.CurrentInstruction.Reference, Timestamp = truck.LastUpdated.AddSeconds(15), Source = DataProvenanceType.SyntheticDemoData } };
    private static FleetTruckDto CloneTruck(FleetTruckDto t) => new() { Id = t.Id, FleetOperatorId = t.FleetOperatorId, FleetOperatorName = t.FleetOperatorName, TruckRegistration = t.TruckRegistration, DriverName = t.DriverName, VehicleType = t.VehicleType, DriverContactPlaceholder = t.DriverContactPlaceholder, DriverContact = t.DriverContact, ContainerReference = t.ContainerReference, JobReference = t.JobReference, BookingReference = t.BookingReference, AppointmentTime = t.AppointmentTime, QueueNumber = t.QueueNumber, AssignedGate = t.AssignedGate, BerthYardStagingZone = t.BerthYardStagingZone, CurrentStatus = t.CurrentStatus, EtaCallForwardTime = t.EtaCallForwardTime, CurrentInstruction = t.CurrentInstruction, DelayRisk = t.DelayRisk, EstimatedIdlingMinutesAvoided = t.EstimatedIdlingMinutesAvoided, EstimatedCo2KgAvoided = t.EstimatedCo2KgAvoided, QueueEfficiencyScore = t.QueueEfficiencyScore, LatestNotification = t.LatestNotification, WhatsAppNotificationStatus = t.WhatsAppNotificationStatus, InAppNotificationStatus = t.InAppNotificationStatus, LastUpdated = t.LastUpdated, AcknowledgementState = t.AcknowledgementState, DriverAvailabilityStatus = t.DriverAvailabilityStatus, LastKnownLocationLabel = t.LastKnownLocationLabel, LastLocationCheckIn = t.LastLocationCheckIn, Source = t.Source, AllowedNextActions = t.AllowedNextActions.ToList(), Timeline = t.Timeline.ToList(), AuditTrail = t.AuditTrail.ToList(), NotificationHistory = t.NotificationHistory.ToList() };
    private static List<GateCapacityDto> BuildGateCapacities(List<FleetTruckDto> trucks) => new() { new() { GateName = "Gate 1", CurrentQueueCount = 3, CapacityPerHour = 18, CongestionLevel = "Low", Status = CapacityStatus.Open, AssignedTrucks = trucks.Where(t => t.AssignedGate == "Gate 1").Select(t => t.TruckRegistration).ToList(), NextAvailableTime = DateTime.UtcNow.AddMinutes(5) }, new() { GateName = "Gate 2", CurrentQueueCount = 7, CapacityPerHour = 16, CongestionLevel = "Moderate", Status = CapacityStatus.Open, AssignedTrucks = trucks.Where(t => t.AssignedGate == "Gate 2").Select(t => t.TruckRegistration).ToList(), NextAvailableTime = DateTime.UtcNow.AddMinutes(12) }, new() { GateName = "Gate 3", CurrentQueueCount = 18, CapacityPerHour = 12, CongestionLevel = "High", Status = CapacityStatus.Congested, AssignedTrucks = trucks.Where(t => t.AssignedGate == "Gate 3").Select(t => t.TruckRegistration).ToList(), NextAvailableTime = DateTime.UtcNow.AddMinutes(28) } };
    private static List<StagingAreaDto> BuildStagingAreas() => new() { new() { AreaName = "Staging Area A", Capacity = 12, CurrentTrucks = 8, DistanceToAssignedGateKm = 1.2m, Status = CapacityStatus.Available }, new() { AreaName = "Staging Area B", Capacity = 10, CurrentTrucks = 9, DistanceToAssignedGateKm = 1.6m, Status = CapacityStatus.NearFull }, new() { AreaName = "Off-site Holding", Capacity = 50, CurrentTrucks = 17, DistanceToAssignedGateKm = 7.4m, Status = CapacityStatus.Available } };
    private static List<BerthReadinessStatusDto> BuildBerthReadiness(List<FleetTruckDto> trucks) => new() { new() { BerthName = "Berth 204", ReadinessStatus = "Clearing imports", CurrentOperation = "Discharge", NextAvailableTime = DateTime.UtcNow.AddMinutes(28), DelayRisk = QueueDelayRisk.High, LinkedTruckReferences = trucks.Take(3).Select(t => t.BookingReference).ToList() }, new() { BerthName = "Reefer Berth R2", ReadinessStatus = "Ready", CurrentOperation = "Reefer loading", NextAvailableTime = DateTime.UtcNow.AddMinutes(8), DelayRisk = QueueDelayRisk.Medium, LinkedTruckReferences = trucks.Where(t => t.FleetOperatorId == "kzn-cold-chain").Select(t => t.BookingReference).ToList() } };
    internal static ExecutionPlanDto BuildExecutionPlan(List<FleetTruckDto> trucks, string scenario) { var actions = trucks.Select(t => new ExecutionPlanTruckActionDto { TruckReference = t.BookingReference, TruckRegistration = t.TruckRegistration, DriverName = t.DriverName, ActionType = t.CurrentStatus switch { QueueTruckStatus.ProceedToGate => ExecutionTruckActionType.ProceedToGate, QueueTruckStatus.Delayed or QueueTruckStatus.Rescheduled => ExecutionTruckActionType.DelayArrival, QueueTruckStatus.Waiting => ExecutionTruckActionType.MoveToStaging, _ => ExecutionTruckActionType.HoldPosition }, Reason = t.CurrentInstruction.Reason, EtaCallForwardTime = t.EtaCallForwardTime, NotificationText = t.LatestNotification, AllowedNextActions = t.AllowedNextActions, IdlingMinutesAvoided = t.EstimatedIdlingMinutesAvoided, Co2KgAvoided = t.EstimatedCo2KgAvoided }).ToList(); return new ExecutionPlanDto { ScenarioName = scenario, TruckActions = actions, ExpectedIdlingMinutesAvoided = actions.Sum(a => a.IdlingMinutesAvoided), ExpectedCo2KgAvoided = actions.Sum(a => a.Co2KgAvoided), ExpectedDelayReductionMinutes = 42, ConfidenceScore = .84m, Explanation = "Deterministic execution plan balances gate pressure, berth readiness, staging capacity, driver confirmations and emissions impact.", AuditEntries = new() { Audit("Smart Port", "ExecutionPlanGenerated", null, null, "Multi-truck execution plan generated from demo control-room state.", DataProvenanceType.DeterministicFallback) } }; }
}

public class OperationalStateMachineService : IOperationalStateMachineService
{
    private readonly IFleetDriverQueueService? _queue;
    public OperationalStateMachineService() { }
    public OperationalStateMachineService(IFleetDriverQueueService queue) => _queue = queue;
    public bool CanTransition(QueueTruckStatus currentStatus, OperationalActionType requestedAction, out QueueTruckStatus newStatus, out string message)
    {
        newStatus = requestedAction switch { OperationalActionType.ShareLocation => QueueTruckStatus.LocationShared, OperationalActionType.ConfirmHolding => QueueTruckStatus.Holding, OperationalActionType.ArrivedAtStaging or OperationalActionType.MoveToStaging => QueueTruckStatus.AtStaging, OperationalActionType.ProceedingToGate or OperationalActionType.ReleaseToGate => QueueTruckStatus.ProceedToGate, OperationalActionType.ArrivedAtGate => QueueTruckStatus.AtGate, OperationalActionType.CompleteJob => QueueTruckStatus.Completed, OperationalActionType.Delayed20 => QueueTruckStatus.Delayed, OperationalActionType.Reschedule => QueueTruckStatus.Rescheduled, OperationalActionType.MarkException or OperationalActionType.ReportIssue => QueueTruckStatus.Exception, OperationalActionType.RequestLocation => QueueTruckStatus.LocationRequested, OperationalActionType.Ready => currentStatus is QueueTruckStatus.Delayed or QueueTruckStatus.Rescheduled ? QueueTruckStatus.Waiting : currentStatus, _ => currentStatus };
        var allowed = GetAllowedActions(currentStatus); var ok = allowed.Contains(requestedAction) || requestedAction is OperationalActionType.RefreshStatus or OperationalActionType.CheckEta or OperationalActionType.RequestLocation or OperationalActionType.ShareLocation or OperationalActionType.MoveToStaging or OperationalActionType.ReleaseToGate or OperationalActionType.Reschedule or OperationalActionType.ReportIssue or OperationalActionType.MarkException or OperationalActionType.Delayed20;
        message = ok ? $"Transition accepted: {currentStatus} -> {newStatus}." : $"Invalid transition: action {requestedAction} is not allowed from {currentStatus}."; return ok;
    }
    public async Task<StateTransitionResultDto> ApplyTransitionAsync(string jobReference, OperationalActionType action, string actor, DataProvenanceType source) { if (_queue == null) return new() { Success = false, Message = "Queue service unavailable." }; var truck = await _queue.GetTruckAsync(jobReference); if (truck == null) return new() { Success = false, Message = "Truck/reference not found." }; var old = truck.CurrentStatus; if (!CanTransition(old, action, out var ns, out var msg)) return new() { Success = false, Message = msg, OldStatus = old, NewStatus = old, AllowedNextActions = GetAllowedActions(old).ToList() }; var updated = await _queue.RecordDriverEventAsync(jobReference, ActionToEvent(action), source, msg, actor); return new() { Success = true, Message = msg, OldStatus = old, NewStatus = ns, Truck = updated, AllowedNextActions = GetAllowedActions(ns).ToList() }; }
    public async Task<IReadOnlyList<OperationalActionType>> GetAllowedNextActionsAsync(string jobReference) => _queue != null && (await _queue.GetTruckAsync(jobReference)) is { } t ? GetAllowedActions(t.CurrentStatus).ToList() : Array.Empty<OperationalActionType>();
    public static IEnumerable<OperationalActionType> GetAllowedActions(QueueTruckStatus status) => status switch { QueueTruckStatus.Waiting => new[] { OperationalActionType.CheckEta, OperationalActionType.ConfirmHolding, OperationalActionType.Break15, OperationalActionType.Delayed20, OperationalActionType.ShareLocation, OperationalActionType.ReportIssue }, QueueTruckStatus.Holding or QueueTruckStatus.HoldPosition => new[] { OperationalActionType.Ready, OperationalActionType.ArrivedAtStaging, OperationalActionType.Delayed20, OperationalActionType.ReportIssue }, QueueTruckStatus.AtStaging or QueueTruckStatus.AtStagingHolding => new[] { OperationalActionType.ProceedingToGate, OperationalActionType.Break15, OperationalActionType.ReportIssue }, QueueTruckStatus.ProceedToGate => new[] { OperationalActionType.ArrivedAtGate, OperationalActionType.ReportIssue }, QueueTruckStatus.AtGate => new[] { OperationalActionType.CompleteJob, OperationalActionType.ReportIssue }, QueueTruckStatus.Completed => new[] { OperationalActionType.RefreshStatus }, QueueTruckStatus.Delayed => new[] { OperationalActionType.Ready, OperationalActionType.Reschedule, OperationalActionType.ReportIssue }, QueueTruckStatus.Rescheduled => new[] { OperationalActionType.Ready, OperationalActionType.ShareLocation }, _ => new[] { OperationalActionType.CheckEta, OperationalActionType.RequestLocation, OperationalActionType.ShareLocation, OperationalActionType.ReportIssue } };
    private static DriverEventType ActionToEvent(OperationalActionType action) => action switch { OperationalActionType.ConfirmHolding => DriverEventType.DriverConfirmedHolding, OperationalActionType.ArrivedAtStaging => DriverEventType.DriverArrivedAtStaging, OperationalActionType.ProceedingToGate => DriverEventType.DriverProceedingToGate, OperationalActionType.ArrivedAtGate => DriverEventType.DriverArrivedAtGate, OperationalActionType.CompleteJob => DriverEventType.DriverCompletedJob, OperationalActionType.Break15 => DriverEventType.DriverBreak, OperationalActionType.Lunch30 => DriverEventType.DriverLunch, OperationalActionType.Delayed20 => DriverEventType.DriverDelayed, OperationalActionType.Ready => DriverEventType.DriverReady, OperationalActionType.ReportIssue or OperationalActionType.MarkException => DriverEventType.DriverIssueReported, _ => DriverEventType.DriverAcknowledgedInstruction };
}

public class QueueOptimizationService : IQueueOptimizationService
{
    public QueueOptimizationResultDto Optimize(FleetTruckDto t, FleetQueueSummaryDto? context = null)
    {
        var gateCongested = t.AssignedGate.Contains("3", StringComparison.OrdinalIgnoreCase) || t.DelayRisk is QueueDelayRisk.High or QueueDelayRisk.Critical;
        var status = t.CurrentStatus == QueueTruckStatus.LocationShared && t.LastLocationCheckIn?.DistanceToAssignedGateKm <= 1.0m ? QueueTruckStatus.ProceedToGate : gateCongested && t.CurrentStatus is not QueueTruckStatus.AtGate and not QueueTruckStatus.Completed ? QueueTruckStatus.Holding : t.CurrentStatus;
        var eta = status == QueueTruckStatus.ProceedToGate ? DateTime.UtcNow.AddMinutes(12) : status is QueueTruckStatus.Holding or QueueTruckStatus.HoldPosition ? DateTime.UtcNow.AddMinutes(18) : t.EtaCallForwardTime;
        var idle = t.EstimatedIdlingMinutesAvoided + (status is QueueTruckStatus.Holding or QueueTruckStatus.AtStaging ? 6 : 0); var co2 = t.EstimatedCo2KgAvoided + (status is QueueTruckStatus.Holding or QueueTruckStatus.AtStaging ? 8.2m : 0);
        var driver = status switch { QueueTruckStatus.ProceedToGate => $"Proceed to {t.AssignedGate} within 12 minutes. Have reference {t.BookingReference} ready.", QueueTruckStatus.AtStaging => $"Remain at {t.BerthYardStagingZone} until Smart Port calls you forward.", QueueTruckStatus.Holding or QueueTruckStatus.HoldPosition => $"Hold position for 18 minutes. Estimated gate call-forward: {eta:HH:mm}.", QueueTruckStatus.Delayed => "Delay arrival by 20 minutes and wait for updated call-forward.", QueueTruckStatus.Completed => "Job completed. No further queue action required.", _ => t.CurrentInstruction.Instruction };
        return new() { RecommendedStatus = status, QueuePositionUpdate = Math.Max(1, t.QueueNumber - 1), GateAssignmentSuggestion = t.AssignedGate, StagingInstruction = t.BerthYardStagingZone, EtaCallForwardTime = eta, IdlingMinutesAvoided = idle, Co2KgAvoided = co2, DelayRisk = gateCongested ? QueueDelayRisk.High : t.DelayRisk, Reason = gateCongested ? $"{t.AssignedGate} is congested or berth readiness is constrained." : "Gate, berth and staging signals support the current movement.", Explanation = "Deterministic queue optimization used gate pressure, berth readiness, staging capacity, location/check-in and emissions signals. Gemini may phrase this later, but is not required.", ConfidenceScore = gateCongested ? .84m : .78m, DriverFriendlyInstruction = driver, NotificationMessage = $"Smart Port Update: Truck {t.TruckRegistration}: {driver} Ref: {t.BookingReference}.", AllowedNextActions = OperationalStateMachineService.GetAllowedActions(status).ToList() };
    }
}

public class LocationEtaService : ILocationEtaService { public LocationCheckInDto Estimate(string reference, string assignedGate, string stagingArea, decimal? latitude, decimal? longitude, string? label, DataProvenanceType source) { var safeReference = reference ?? string.Empty; var distance = safeReference.EndsWith("42", StringComparison.Ordinal) ? 4.2m : safeReference.EndsWith("43", StringComparison.Ordinal) ? .8m : 1.6m; var staging = Math.Max(.3m, distance - 1.0m); var minutes = Math.Max(5, (int)(distance * 5)); return new() { Latitude = latitude ?? -29.8587m, Longitude = longitude ?? 31.0218m, LocationLabel = string.IsNullOrWhiteSpace(label) ? $"Demo WhatsApp location check-in near {stagingArea}" : label, Timestamp = DateTime.UtcNow, DistanceToAssignedGateKm = distance, DistanceToStagingKm = staging, EstimatedTravelMinutes = minutes, EtaConfidence = .82m, EstimatedArrivalTime = DateTime.UtcNow.AddMinutes(minutes), Source = source }; } }

public class ExecutionPlanService : IExecutionPlanService
{
    private readonly IFleetDriverQueueService _queue; private static readonly ConcurrentDictionary<string, ExecutionPlanDto> Plans = new(); public ExecutionPlanService(IFleetDriverQueueService queue) => _queue = queue;
    public async Task<ExecutionPlanDto> GeneratePlanAsync(string scenarioName = "Demo gate congestion execution plan", string? fleetOperatorId = null) { var plan = DemoFleetDriverQueueService.BuildExecutionPlan((await _queue.GetTrucksAsync(fleetOperatorId)).ToList(), scenarioName); Plans[plan.PlanId] = plan; return plan; }
    public Task<ExecutionPlanDto?> GetPlanAsync(string id) { Plans.TryGetValue(id, out var p); return Task.FromResult(p); }
    public Task<IReadOnlyList<ExecutionPlanDto>> GetPlansAsync() => Task.FromResult<IReadOnlyList<ExecutionPlanDto>>(Plans.Values.OrderByDescending(p => p.CreatedAt).ToList());
}

public class DriverStatusCommandService : IDriverStatusCommandService
{
    private readonly IFleetDriverQueueService _queue; public DriverStatusCommandService(IFleetDriverQueueService queue) => _queue = queue;
    public async Task<DriverCommandResultDto> HandleCommandAsync(DriverCommandRequestDto request)
    {
        var truck = await _queue.GetTruckAsync(request.Reference); if (truck == null) return new() { Success = false, ReplyMessage = "Truck/reference not found.", Source = "Fallback" };
        var cmd = (request.CommandText ?? string.Empty).Trim().ToUpperInvariant(); if (string.IsNullOrEmpty(cmd)) cmd = "STATUS";
        if (cmd is "STATUS" or "ETA" or "HOW LONG" or "WHAT NOW" or "WHERE MUST I GO" or "HELP") return new() { Success = true, Truck = truck, ReplyMessage = BuildStatusReply(truck, cmd), Source = "Fallback" };
        var eventType = cmd switch { "READY" => DriverEventType.DriverReady, "BREAK 15" => DriverEventType.DriverBreak, "LUNCH 30" => DriverEventType.DriverLunch, "DELAYED 20" => DriverEventType.DriverDelayed, "AT STAGING" or "ARRIVED_STAGING" => DriverEventType.DriverArrivedAtStaging, "HOLDING" => DriverEventType.DriverConfirmedHolding, "PROCEEDING" or "PROCEEDING_GATE" => DriverEventType.DriverProceedingToGate, "AT GATE" or "ARRIVED_GATE" => DriverEventType.DriverArrivedAtGate, "COMPLETED" => DriverEventType.DriverCompletedJob, "ISSUE" => DriverEventType.DriverIssueReported, "LOCATION_SHARED" => DriverEventType.WhatsAppLocationShared, _ => DriverEventType.DriverAcknowledgedInstruction };
        var updated = eventType == DriverEventType.WhatsAppLocationShared ? await _queue.RecordLocationCheckInAsync(truck.BookingReference, null, null, "WhatsApp command location shared", request.Source, request.Actor) : await _queue.RecordDriverEventAsync(truck.BookingReference, eventType, request.Source, $"Driver command received: {cmd}", request.Actor);
        return new() { Success = updated != null, Truck = updated, ReplyMessage = updated == null ? "Unable to update state." : BuildStatusReply(updated, cmd), Source = "Fallback" };
    }
    private static string BuildStatusReply(FleetTruckDto t, string cmd) => cmd switch { "ETA" or "HOW LONG" => $"Estimated call-forward is {t.EtaCallForwardTime:HH:mm}, about {Math.Max(1, (int)Math.Round((t.EtaCallForwardTime - DateTime.UtcNow).TotalMinutes))} minutes from now. Current instruction: {t.CurrentInstruction.Instruction}", "BREAK 15" => "Update received. You are marked unavailable for 15 minutes. Smart Port will adjust your call-forward timing and notify you when to proceed.", "LUNCH 30" => "Update received. You are marked unavailable for 30 minutes. Smart Port will adjust your call-forward timing and notify you when to proceed.", "READY" => $"Confirmed. You are available again. Current instruction: {t.CurrentInstruction.Instruction}", "ARRIVED_GATE" or "AT GATE" => $"Confirmed. Truck {t.TruckRegistration} is now marked At Gate. Fleet dashboard and queue plan updated.", "ISSUE" => "Please describe the issue. Smart Port will flag this for fleet owner/control room review.", "WHERE MUST I GO" => $"Go to {t.BerthYardStagingZone} / {t.AssignedGate} as instructed. Current status: {t.CurrentStatus}.", "HELP" => "Supported Smart Port commands: STATUS, ETA, HOW LONG, WHAT NOW, READY, BREAK 15, LUNCH 30, DELAYED 20, HOLDING, ARRIVED_STAGING, PROCEEDING_GATE, ARRIVED_GATE, COMPLETED, ISSUE.", _ => $"Truck {t.TruckRegistration}: {t.CurrentInstruction.Instruction} Estimated gate call-forward: {t.EtaCallForwardTime:HH:mm}. Queue position: {t.QueueNumber}. Ref: {t.BookingReference}." };
}

public class NotificationTemplateService : INotificationTemplateService
{
    public string BuildMessage(FleetTruckDto truck, NotificationEventType eventType, NotificationChannel channel) => eventType switch { NotificationEventType.WhatsAppLocationCheckIn => $"Smart Port Update: Please share your current WhatsApp location so we can update your gate ETA. Ref: {truck.BookingReference}.", NotificationEventType.ProceedToGate => $"Smart Port Update: Truck {truck.TruckRegistration} may proceed to {truck.AssignedGate}. Have reference {truck.BookingReference} ready.", _ => $"Smart Port Update: Truck {truck.TruckRegistration}: {truck.CurrentInstruction.Instruction} Estimated call-forward: {truck.EtaCallForwardTime:HH:mm}. Ref: {truck.BookingReference}." };
}

public class DriverNotificationService : INotificationService
{
    private readonly IFleetDriverQueueService _queue; private readonly INotificationTemplateService _templates; private readonly IInAppNotificationService _inApp; private readonly IWhatsAppNotificationSender _whatsApp; private readonly IPushNotificationSender _push; private static readonly ConcurrentDictionary<string, List<DriverNotificationDto>> History = new(StringComparer.OrdinalIgnoreCase);
    public DriverNotificationService(IFleetDriverQueueService queue, INotificationTemplateService templates, IInAppNotificationService inApp, IWhatsAppNotificationSender whatsApp, IPushNotificationSender push) { _queue = queue; _templates = templates; _inApp = inApp; _whatsApp = whatsApp; _push = push; }
    public async Task<DriverNotificationDto> SendAsync(string reference, NotificationChannel channel, NotificationEventType eventType)
    {
        var truck = await _queue.GetTruckAsync(reference) ?? throw new InvalidOperationException("Truck/reference not found.");
        var message = _templates.BuildMessage(truck, eventType, channel);
        var whatsAppResult = new WhatsAppSendResult(NotificationStatus.Failed);
        var status = channel switch { NotificationChannel.InApp => await _inApp.SendAsync(truck, message), NotificationChannel.WhatsApp when !truck.DriverContact.IsLiveWhatsAppSafe && (_queue.GetWhatsAppConnectorStatus().Mode is WhatsAppMode.LiveTest or WhatsAppMode.Live) => NotificationStatus.BlockedSafety, NotificationChannel.WhatsApp => (whatsAppResult = await _whatsApp.SendAsync(truck, message)).Status, NotificationChannel.AndroidPush => await _push.SendAsync(truck, message), _ => NotificationStatus.Failed };
        var source = channel == NotificationChannel.WhatsApp && status is NotificationStatus.LiveTestSent or NotificationStatus.Sent ? DataProvenanceType.FutureLiveConnector : DataProvenanceType.ManualOperatorInput;
        return await RecordAsync(truck.BookingReference, channel, status, eventType, status == NotificationStatus.BlockedSafety ? $"Blocked for WhatsApp safety: {message}" : message, source, "Fleet owner", whatsAppResult.ExternalMessageId);
    }
    public async Task<DriverNotificationDto?> RecordAsync(string reference, NotificationChannel channel, NotificationStatus status, NotificationEventType eventType, string message, DataProvenanceType source, string actor = "Smart Port", string externalMessageId = "")
    {
        var truck = await _queue.GetTruckAsync(reference); if (truck == null) return null;
        var notification = new DriverNotificationDto { TruckReference = truck.BookingReference, RecipientName = truck.DriverName, RecipientContact = channel == NotificationChannel.WhatsApp ? MaskContact(truck.DriverContact.NormalizedWhatsAppNumber) : truck.DriverContact.BackupContact, FleetOperator = truck.FleetOperatorName, Channel = channel, Message = message, Status = status, EventType = eventType, RelatedInstructionReference = truck.CurrentInstruction.Reference, Source = source, Timestamp = DateTime.UtcNow, ExternalMessageId = externalMessageId };
        History.AddOrUpdate(truck.BookingReference, _ => new() { notification }, (_, list) => { lock (list) list.Add(notification); return list; });
        await _queue.RecordDriverEventAsync(truck.BookingReference, eventType == NotificationEventType.WhatsAppLocationCheckIn ? DriverEventType.WhatsAppLocationRequested : DriverEventType.DriverAcknowledgedInstruction, source, $"{channel} notification recorded with status {status}.", actor);
        return notification;
    }
    public async Task<IReadOnlyList<DriverNotificationDto>> GetHistoryAsync(string reference) { var truck = await _queue.GetTruckAsync(reference); if (truck == null) return Array.Empty<DriverNotificationDto>(); var seeded = truck.NotificationHistory; if (!History.TryGetValue(truck.BookingReference, out var sent)) return seeded.OrderByDescending(n => n.Timestamp).ToList(); lock (sent) return seeded.Concat(sent).OrderByDescending(n => n.Timestamp).ToList(); }
    private static string MaskContact(string value) { if (string.IsNullOrWhiteSpace(value) || value.StartsWith("DEMO", StringComparison.OrdinalIgnoreCase)) return value; var digits = new string(value.Where(char.IsDigit).ToArray()); return digits.Length <= 4 ? "****" : $"+••••••{digits[^4..]}"; }
}

public class InAppNotificationService : IInAppNotificationService { public Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message) => Task.FromResult(NotificationStatus.SimulatedSent); }
public class SimulatedWhatsAppNotificationSender : IWhatsAppNotificationSender, IWhatsAppConnectorService
{
    private readonly IConfiguration? _config;
    public SimulatedWhatsAppNotificationSender(IConfiguration? config = null) => _config = config;
    public WhatsAppConnectorStatusDto GetStatus() => BuildStatus(_config, approvedDriverAvailable: false);
    public Task<WhatsAppSendResult> SendAsync(FleetTruckDto truck, string message) => SendAsync(truck, message, null);
    public Task<WhatsAppSendResult> SendAsync(FleetTruckDto truck, string message, string? overrideRecipient = null, CancellationToken cancellationToken = default) => Task.FromResult(new WhatsAppSendResult(NotificationStatus.SimulatedSent, ProviderStatus: "Demo simulation"));
    internal static WhatsAppConnectorStatusDto BuildStatus(IConfiguration? config, bool approvedDriverAvailable)
    {
        static bool IsTrue(string? value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        static WhatsAppMode ParseMode(string? value) => Enum.TryParse<WhatsAppMode>(value, true, out var parsed) ? parsed : WhatsAppMode.Demo;
        var mode = ParseMode(config?["SMARTPORT_WHATSAPP_MODE"] ?? config?["SMARTPORT_NOTIFICATION_MODE"]);
        var enabled = IsTrue(config?["SMARTPORT_WHATSAPP_ENABLED"]);
        var accessToken = !string.IsNullOrWhiteSpace(config?["SMARTPORT_WHATSAPP_ACCESS_TOKEN"]);
        var phoneNumberId = !string.IsNullOrWhiteSpace(config?["SMARTPORT_WHATSAPP_PHONE_NUMBER_ID"]);
        var businessAccountId = !string.IsNullOrWhiteSpace(config?["SMARTPORT_WHATSAPP_BUSINESS_ACCOUNT_ID"]);
        var verifyToken = !string.IsNullOrWhiteSpace(config?["SMARTPORT_WHATSAPP_VERIFY_TOKEN"]);
        var publicBaseUrl = (config?["SMARTPORT_PUBLIC_BASE_URL"] ?? string.Empty).Trim().TrimEnd('/');
        var graph = string.IsNullOrWhiteSpace(config?["SMARTPORT_WHATSAPP_GRAPH_VERSION"]) ? "v22.0" : config!["SMARTPORT_WHATSAPP_GRAPH_VERSION"]!.Trim();
        var configured = accessToken && phoneNumberId;
        var liveTestRecipient = !string.IsNullOrWhiteSpace(config?["SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER"]);
        var liveAllowed = enabled && configured && verifyToken && !string.IsNullOrWhiteSpace(publicBaseUrl) && (mode == WhatsAppMode.Live ? liveTestRecipient || approvedDriverAvailable : mode == WhatsAppMode.LiveTest && (liveTestRecipient || approvedDriverAvailable));
        var checks = new List<string>
        {
            $"Mode is {mode}",
            enabled ? "WhatsApp enabled" : "WhatsApp not enabled",
            accessToken ? "Access token configured" : "Access token missing",
            phoneNumberId ? "Phone number ID configured" : "Phone number ID missing",
            businessAccountId ? "Business account ID configured" : "Business account ID missing",
            verifyToken ? "Webhook verify token configured" : "Webhook verify token missing",
            !string.IsNullOrWhiteSpace(publicBaseUrl) ? "Public base URL configured" : "Public base URL missing",
            liveTestRecipient ? "Optional test recipient configured" : approvedDriverAvailable ? "Approved consented tester driver available" : "No approved/test recipient configured"
        };
        return new WhatsAppConnectorStatusDto
        {
            Mode = mode, Enabled = enabled, AccessTokenConfigured = accessToken, PhoneNumberIdConfigured = phoneNumberId, BusinessAccountIdConfigured = businessAccountId,
            VerifyTokenConfigured = verifyToken, PublicBaseUrlConfigured = !string.IsNullOrWhiteSpace(publicBaseUrl), GraphVersion = graph, PublicBaseUrl = publicBaseUrl,
            WebhookCallbackUrl = string.IsNullOrWhiteSpace(publicBaseUrl) ? "Set SMARTPORT_PUBLIC_BASE_URL to show callback URL" : $"{publicBaseUrl}/webhooks/whatsapp",
            ApprovedDriverAvailable = approvedDriverAvailable || liveTestRecipient, CredentialsConfigured = configured, LiveSendingAllowed = liveAllowed,
            LiveTestReadinessChecks = checks,
            SafetyMessage = mode switch
            {
                WhatsAppMode.Demo => "Demo Mode: no external WhatsApp calls; simulated messages are stored in history.",
                WhatsAppMode.ConnectorReady => "ConnectorReady Mode: credentials can be checked, but live sending is blocked until LiveTest/Live.",
                WhatsAppMode.Live => "Live Mode: external sends are enabled only when credentials, public webhook and approved recipients are configured.",
                _ => "LiveTest Mode: sends only go to SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER or manually added, active, approved and consented tester numbers."
            }
        };
    }
}
public class WhatsAppCloudApiNotificationSender : IWhatsAppNotificationSender, IWhatsAppConnectorService
{
    private readonly HttpClient _http; private readonly IConfiguration _config; public WhatsAppCloudApiNotificationSender(HttpClient http, IConfiguration config) { _http = http; _config = config; }
    public WhatsAppConnectorStatusDto GetStatus() => SimulatedWhatsAppNotificationSender.BuildStatus(_config, approvedDriverAvailable: true);
    public Task<WhatsAppSendResult> SendAsync(FleetTruckDto truck, string message) => SendAsync(truck, message, null);
    public async Task<WhatsAppSendResult> SendAsync(FleetTruckDto truck, string message, string? overrideRecipient = null, CancellationToken cancellationToken = default)
    {
        var status = GetStatus();
        var token = _config["SMARTPORT_WHATSAPP_ACCESS_TOKEN"]; var phoneNumberId = _config["SMARTPORT_WHATSAPP_PHONE_NUMBER_ID"]; var graph = status.GraphVersion;
        if (status.Mode is WhatsAppMode.Demo or WhatsAppMode.ConnectorReady || !status.Enabled) return new WhatsAppSendResult(NotificationStatus.ConnectorNotConfigured, ProviderStatus: "WhatsApp external send disabled by mode");
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneNumberId)) return new WhatsAppSendResult(NotificationStatus.ConnectorNotConfigured, ProviderStatus: "WhatsApp credentials missing");
        var configuredTestRecipient = NormalizeRecipient(_config["SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER"]);
        var recipient = NormalizeRecipient(overrideRecipient) ?? (!string.IsNullOrWhiteSpace(configuredTestRecipient) && status.Mode == WhatsAppMode.LiveTest ? configuredTestRecipient : NormalizeRecipient(truck.DriverContact.NormalizedWhatsAppNumber));
        if (string.IsNullOrWhiteSpace(recipient)) return new WhatsAppSendResult(NotificationStatus.BlockedSafety, SafeError: "No approved WhatsApp recipient available");
        if (status.Mode == WhatsAppMode.LiveTest && !string.Equals(recipient, configuredTestRecipient, StringComparison.Ordinal) && !truck.DriverContact.IsLiveWhatsAppSafe) return new WhatsAppSendResult(NotificationStatus.BlockedSafety, SafeError: "LiveTest sends require configured test recipient or approved consented driver");
        if (status.Mode == WhatsAppMode.Live && !truck.DriverContact.IsLiveWhatsAppSafe && string.IsNullOrWhiteSpace(overrideRecipient)) return new WhatsAppSendResult(NotificationStatus.BlockedSafety, SafeError: "Live sends require approved consented driver recipient");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/{graph}/{phoneNumberId}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(new { messaging_product = "whatsapp", to = recipient, type = "text", text = new { body = SafeMessage(message) } }), Encoding.UTF8, "application/json");
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var externalId = ExtractMetaMessageId(body);
            if (response.IsSuccessStatusCode) return new WhatsAppSendResult(status.Mode == WhatsAppMode.Live ? NotificationStatus.Sent : NotificationStatus.LiveTestSent, externalId, $"Meta accepted message ({(int)response.StatusCode})");
            return new WhatsAppSendResult(NotificationStatus.Failed, externalId, $"Meta send failed ({(int)response.StatusCode})", ExtractSafeError(body));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new WhatsAppSendResult(NotificationStatus.Failed, SafeError: "WhatsApp Cloud API request failed safely"); }
    }
    private static string? NormalizeRecipient(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var digits = new string(value.Where(char.IsDigit).ToArray()); return digits.Length >= 8 ? digits : null; }
    private static string SafeMessage(string message) => string.IsNullOrWhiteSpace(message) ? "Smart Port update: please check your driver instructions." : message.Length > 1000 ? message[..1000] : message;
    private static string ExtractSafeError(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try { using var doc = JsonDocument.Parse(json); if (doc.RootElement.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var msg)) return msg.GetString() ?? string.Empty; } catch { }
        return "Meta returned an error";
    }
    private static string ExtractMetaMessageId(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0 && messages[0].TryGetProperty("id", out var id)) return id.GetString() ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }
}
public class WhatsAppWebhookParser : IWhatsAppWebhookParser
{
    public IReadOnlyList<WhatsAppInboundMessage> Parse(JsonElement payload)
    {
        var results = new List<WhatsAppInboundMessage>();
        if (!payload.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array) return results;
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array) continue;
            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;
                var profileName = string.Empty;
                if (value.TryGetProperty("contacts", out var contacts) && contacts.ValueKind == JsonValueKind.Array && contacts.GetArrayLength() > 0)
                {
                    var contact = contacts[0];
                    if (contact.TryGetProperty("profile", out var profile) && profile.TryGetProperty("name", out var name)) profileName = name.GetString() ?? string.Empty;
                }
                if (!value.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array) continue;
                foreach (var message in messages.EnumerateArray()) results.Add(ParseMessage(message, profileName));
            }
        }
        return results;
    }
    private static WhatsAppInboundMessage ParseMessage(JsonElement message, string profileName)
    {
        var from = message.TryGetProperty("from", out var f) ? f.GetString() ?? string.Empty : string.Empty;
        var id = message.TryGetProperty("id", out var i) ? i.GetString() ?? string.Empty : string.Empty;
        var timestamp = DateTimeOffset.UtcNow;
        if (message.TryGetProperty("timestamp", out var ts) && long.TryParse(ts.GetString(), out var unix)) timestamp = DateTimeOffset.FromUnixTimeSeconds(unix);
        var type = message.TryGetProperty("type", out var t) ? t.GetString() ?? string.Empty : string.Empty;
        string text = string.Empty, mediaType = string.Empty, mediaId = string.Empty; decimal? lat = null, lng = null;
        if (message.TryGetProperty("text", out var textElement) && textElement.TryGetProperty("body", out var body)) text = body.GetString() ?? string.Empty;
        if (message.TryGetProperty("button", out var button) && button.TryGetProperty("text", out var buttonText)) text = buttonText.GetString() ?? text;
        if (message.TryGetProperty("interactive", out var interactive)) text = ExtractInteractiveText(interactive, text);
        if (message.TryGetProperty("location", out var loc))
        {
            if (loc.TryGetProperty("latitude", out var la) && la.TryGetDecimal(out var latitude)) lat = latitude;
            if (loc.TryGetProperty("longitude", out var lo) && lo.TryGetDecimal(out var longitude)) lng = longitude;
            text = loc.TryGetProperty("name", out var name) ? name.GetString() ?? "WhatsApp shared location" : "WhatsApp shared location";
        }
        foreach (var candidate in new[] { "image", "audio", "voice", "video", "document" })
        {
            if (!message.TryGetProperty(candidate, out var media)) continue;
            mediaType = candidate; mediaId = media.TryGetProperty("id", out var mid) ? mid.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(text)) text = $"Inbound WhatsApp {candidate} received for operator review.";
        }
        return new WhatsAppInboundMessage(from, id, timestamp, text, lat, lng, profileName, mediaType, mediaId, type);
    }
    private static string ExtractInteractiveText(JsonElement interactive, string fallback)
    {
        if (interactive.TryGetProperty("button_reply", out var br) && br.TryGetProperty("title", out var bt)) return bt.GetString() ?? fallback;
        if (interactive.TryGetProperty("list_reply", out var lr) && lr.TryGetProperty("title", out var lt)) return lt.GetString() ?? fallback;
        return fallback;
    }
}
public class SimulatedPushNotificationSender : IPushNotificationSender { public Task<NotificationStatus> SendAsync(FleetTruckDto truck, string message) => Task.FromResult(NotificationStatus.SimulatedSent); }
public class MobileDeviceRegistrationService : IMobileDeviceRegistrationService { private static readonly ConcurrentDictionary<string, MobileDeviceRegistrationDto> Devices = new(StringComparer.OrdinalIgnoreCase); public Task RegisterAsync(MobileDeviceRegistrationDto registration) { Devices[$"{registration.Reference}:{registration.DeviceToken}"] = registration; return Task.CompletedTask; } public Task UnregisterAsync(string reference, string deviceToken) { Devices.TryRemove($"{reference}:{deviceToken}", out _); return Task.CompletedTask; } }

public class OperationalActionService : IOperationalActionService
{
    private readonly IDriverStatusCommandService _commands;
    private readonly INotificationService _notifications;
    private readonly IFleetDriverQueueService _queue;
    public OperationalActionService(IDriverStatusCommandService commands, INotificationService notifications, IFleetDriverQueueService queue) { _commands = commands; _notifications = notifications; _queue = queue; }
    public async Task<OperationalActionResult> ExecuteAsync(OperationalActionRequest request, CancellationToken cancellationToken = default)
    {
        var truck = await _queue.GetTruckAsync(request.Reference);
        if (truck == null) return new(false, "Truck/reference not found.", request.Source.ToString(), request.Reference, DateTime.UtcNow, NextRecommendedAction: "Verify the booking reference and retry.");
        if (request.NotificationChannel.HasValue)
        {
            var notification = await _notifications.SendAsync(truck.BookingReference, request.NotificationChannel.Value, request.ActionType == OperationalActionType.RequestLocation ? NotificationEventType.WhatsAppLocationCheckIn : NotificationEventType.AiInstructionUpdated);
            return new(notification.Status is not NotificationStatus.Failed and not NotificationStatus.BlockedSafety and not NotificationStatus.ConnectorNotConfigured, $"{request.NotificationChannel} notification recorded with status {notification.Status}.", request.NotificationChannel == NotificationChannel.WhatsApp && notification.Status == NotificationStatus.Sent ? "Live" : notification.Status == NotificationStatus.LiveTestSent ? "LiveTest" : "Demo", truck.BookingReference, notification.Timestamp, notification.RelatedInstructionReference, notification.ExternalMessageId, truck.CurrentInstruction.Instruction);
        }
        var command = string.IsNullOrWhiteSpace(request.CommandText) ? ActionToCommand(request.ActionType) : request.CommandText!;
        var result = await _commands.HandleCommandAsync(new DriverCommandRequestDto { Reference = truck.BookingReference, CommandText = command, Actor = request.Actor, Source = request.Source });
        var updated = result.Truck ?? truck;
        return new(result.Success, result.ReplyMessage, request.Source.ToString(), updated.BookingReference, DateTime.UtcNow, NextRecommendedAction: updated.CurrentInstruction.Instruction);
    }
    private static string ActionToCommand(OperationalActionType action) => action switch
    {
        OperationalActionType.MoveToStaging or OperationalActionType.ArrivedAtStaging => "ARRIVED_STAGING",
        OperationalActionType.ReleaseToGate or OperationalActionType.ProceedingToGate => "PROCEEDING_GATE",
        OperationalActionType.Reschedule or OperationalActionType.Delayed20 => "DELAYED 20",
        OperationalActionType.MarkException or OperationalActionType.ReportIssue => "ISSUE",
        OperationalActionType.ConfirmHolding => "HOLDING",
        OperationalActionType.ArrivedAtGate => "ARRIVED_GATE",
        OperationalActionType.CompleteJob => "COMPLETED",
        OperationalActionType.Ready => "READY",
        _ => "STATUS"
    };
}
