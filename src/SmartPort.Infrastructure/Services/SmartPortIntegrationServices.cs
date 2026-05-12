using SmartPort.Application.Interfaces;

namespace SmartPort.Infrastructure.Services;

public sealed class SmartPortIntegrationSettings
{
    public SmartPortDataMode Mode { get; set; } = SmartPortDataMode.SyntheticDemo;
    public bool PilotApproved { get; set; }
    public bool CsvImportEnabled { get; set; }
    public bool RestApiEnabled { get; set; }
    public bool DatabaseEnabled { get; set; }
    public bool ManualEntryEnabled { get; set; }
    public bool WebhookEnabled { get; set; }

    public bool IsEnabled(SmartPortDataMode mode) => mode switch
    {
        SmartPortDataMode.SyntheticDemo => true,
        SmartPortDataMode.CsvImport => PilotApproved && CsvImportEnabled,
        SmartPortDataMode.RestApi => PilotApproved && RestApiEnabled,
        SmartPortDataMode.Database => PilotApproved && DatabaseEnabled,
        SmartPortDataMode.ManualEntry => PilotApproved && ManualEntryEnabled,
        SmartPortDataMode.Webhook => PilotApproved && WebhookEnabled,
        _ => false
    };

}

public abstract class SmartPortDataConnectorBase : ISmartPortDataConnector
{
    protected SmartPortDataConnectorBase(SmartPortIntegrationSettings settings)
    {
        Settings = settings;
    }

    protected SmartPortIntegrationSettings Settings { get; }
    public abstract SmartPortDataMode Mode { get; }
    public abstract string Name { get; }
    public virtual bool IsEnabled => Settings.IsEnabled(Mode);

    public virtual Task<SmartPortConnectorSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = new SmartPortConnectorSnapshot
        {
            Mode = Mode,
            ConnectorName = Name,
            GeneratedAtUtc = DateTime.UtcNow
        };

        return Task.FromResult(snapshot);
    }
}

public sealed class SyntheticSmartPortConnector : SmartPortDataConnectorBase, ISyntheticSmartPortConnector
{
    public SyntheticSmartPortConnector(SmartPortIntegrationSettings settings) : base(settings) { }
    public override SmartPortDataMode Mode => SmartPortDataMode.SyntheticDemo;
    public override string Name => nameof(SyntheticSmartPortConnector);
    public override bool IsEnabled => true;

    public override Task<SmartPortConnectorSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var snapshot = new SmartPortConnectorSnapshot
        {
            Mode = Mode,
            ConnectorName = Name,
            GeneratedAtUtc = now,
            VesselEtas = new[]
            {
                new VesselEtaReading { ExternalId = "VES-DEMO-001", VesselName = "Demo Horizon", VoyageNumber = "DH-042", EstimatedArrival = now.AddHours(5), DelayMinutes = 35, DelayReason = "Berth window pressure", Notes = "Synthetic vessel ETA for pilot readiness demo." },
                new VesselEtaReading { ExternalId = "VES-DEMO-002", VesselName = "Demo Meridian", VoyageNumber = "DM-118", EstimatedArrival = now.AddHours(11), DelayMinutes = 0, DelayReason = "On schedule", Notes = "Synthetic ETA; not connected to AIS/IPMS/TOS." }
            },
            BerthStatuses = new[]
            {
                new BerthStatusReading { ExternalId = "BERTH-1", BerthCode = "B1", Status = "Occupied", AssignedVessel = "Demo Horizon", OccupancyPercent = 92, Notes = "Synthetic berth pressure." },
                new BerthStatusReading { ExternalId = "BERTH-2", BerthCode = "B2", Status = "Available in 3h", AssignedVessel = "Demo Meridian", OccupancyPercent = 68, Notes = "Synthetic berth status." }
            },
            YardCapacities = new[]
            {
                new YardCapacityReading { ExternalId = "YARD-A", YardBlock = "A", CapacityPercent = 84, ReeferSlotsAvailable = 14, PressureLevel = "High", Notes = "Synthetic yard block capacity." },
                new YardCapacityReading { ExternalId = "YARD-C", YardBlock = "C", CapacityPercent = 63, ReeferSlotsAvailable = 31, PressureLevel = "Moderate", Notes = "Synthetic yard block capacity." }
            },
            TruckQueues = new[]
            {
                new TruckQueueReading { ExternalId = "GATE-N", GateName = "North Gate", QueueLength = 47, AverageWaitMinutes = 58, HoldOutsidePortCandidates = 9, Notes = "Synthetic queue above demo threshold." },
                new TruckQueueReading { ExternalId = "GATE-S", GateName = "South Gate", QueueLength = 21, AverageWaitMinutes = 26, HoldOutsidePortCandidates = 2, Notes = "Synthetic queue within manageable range." }
            },
            ContainerFlows = new[]
            {
                new ContainerFlowReading { ExternalId = "FLOW-IMPORT", FlowDirection = "Import", Volume = 1280, DwellAlertCount = 34, CommodityGroup = "Mixed containers", Notes = "Synthetic daily volume." },
                new ContainerFlowReading { ExternalId = "FLOW-EXPORT", FlowDirection = "Export", Volume = 940, DwellAlertCount = 11, CommodityGroup = "Reefer and dry", Notes = "Synthetic daily volume." }
            },
            Disruptions = new[]
            {
                new DisruptionEventReading { ExternalId = "DISR-ENERGY", DisruptionType = "Energy constraint", Severity = "Medium", AffectedArea = "Reefer yard / gate processing", ExpectedClearanceUtc = now.AddHours(4), Notes = "Synthetic disruption used for scenario planning." }
            },
            EnergyConstraints = new[]
            {
                new EnergyConstraintReading { ExternalId = "ENERGY-PEAK", ConstraintType = "Load-shed risk window", WindowStartUtc = now.AddHours(2), WindowEndUtc = now.AddHours(5), OperationalImpact = "Peak reefer demand overlaps gate surge", Notes = "Synthetic energy constraint." }
            },
            EmissionsIdling = new[]
            {
                new EmissionsIdlingReading { ExternalId = "IDLE-GATES", Area = "Gate staging", IdlingMinutes = 1860, EstimatedCo2Kg = 248.4m, EstimatedDieselLitres = 93.0m, Notes = "Indicative synthetic idling estimate." }
            },
            DecisionEvents = new[]
            {
                new SmartPortDecisionEvent { ExternalId = "DEC-001", DecisionType = "Gate smoothing", DecisionSummary = "Hold selected trucks outside the port until berth-yard pressure eases.", ApprovalStatus = "Pending human review", Notes = "No automatic execution." }
            },
            RecommendationAuditEvents = new[]
            {
                new RecommendationAuditEvent { ExternalId = "AUD-001", RecommendationId = "REC-DEMO-001", RecommendationText = "Truck queue is above threshold. Prioritise gate smoothing and berth-yard coordination.", AuditStatus = "Review required", HumanReviewer = "Pilot operator", Notes = "Synthetic audit event." }
            }
        };

        return Task.FromResult(snapshot);
    }
}

public sealed class CsvSmartPortConnector : SmartPortDataConnectorBase, ICsvSmartPortConnector
{
    public CsvSmartPortConnector(SmartPortIntegrationSettings settings) : base(settings) { }
    public override SmartPortDataMode Mode => SmartPortDataMode.CsvImport;
    public override string Name => nameof(CsvSmartPortConnector);
}

public sealed class RestSmartPortConnector : SmartPortDataConnectorBase, IRestSmartPortConnector
{
    public RestSmartPortConnector(SmartPortIntegrationSettings settings) : base(settings) { }
    public override SmartPortDataMode Mode => SmartPortDataMode.RestApi;
    public override string Name => nameof(RestSmartPortConnector);
}

public sealed class DatabaseSmartPortConnector : SmartPortDataConnectorBase, IDatabaseSmartPortConnector
{
    public DatabaseSmartPortConnector(SmartPortIntegrationSettings settings) : base(settings) { }
    public override SmartPortDataMode Mode => SmartPortDataMode.Database;
    public override string Name => nameof(DatabaseSmartPortConnector);
}

public sealed class ManualSmartPortConnector : SmartPortDataConnectorBase, IManualSmartPortConnector
{
    public ManualSmartPortConnector(SmartPortIntegrationSettings settings) : base(settings) { }
    public override SmartPortDataMode Mode => SmartPortDataMode.ManualEntry;
    public override string Name => nameof(ManualSmartPortConnector);
}

public sealed class WebhookSmartPortIngestionService : IWebhookSmartPortIngestionService
{
    private readonly SmartPortIntegrationSettings _settings;

    public WebhookSmartPortIngestionService(SmartPortIntegrationSettings settings)
    {
        _settings = settings;
    }

    public Task<SmartPortConnectorStatusDto> IngestAsync(SmartPortConnectorSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var enabled = _settings.IsEnabled(SmartPortDataMode.Webhook);
        return Task.FromResult(new SmartPortConnectorStatusDto
        {
            Mode = SmartPortDataMode.Webhook,
            ConnectorName = nameof(WebhookSmartPortIngestionService),
            IsEnabled = enabled,
            IsDefault = false,
            Status = enabled ? "Ready for approved pilot webhook payloads" : "Disabled until pilot approval and webhook configuration exist",
            DataRequired = "Signed webhook schema, source IP policy, authentication approach and sample payloads",
            Notes = enabled
                ? $"Last demo payload contained {snapshot.TruckQueues.Count} truck queue readings."
                : "No webhook endpoint is active for live third-party data in this demo."
        });
    }
}

public sealed class SmartPortIntegrationHealthService : ISmartPortIntegrationHealthService
{
    private readonly IEnumerable<ISmartPortDataConnector> _connectors;
    private readonly IWebhookSmartPortIngestionService _webhook;

    public SmartPortIntegrationHealthService(IEnumerable<ISmartPortDataConnector> connectors, IWebhookSmartPortIngestionService webhook)
    {
        _connectors = connectors;
        _webhook = webhook;
    }

    public async Task<IReadOnlyList<SmartPortConnectorStatusDto>> GetConnectorHealthAsync(CancellationToken cancellationToken = default)
    {
        var statuses = _connectors
            .OrderBy(c => c.Mode == SmartPortDataMode.SyntheticDemo ? 0 : (int)c.Mode + 1)
            .Select(c => new SmartPortConnectorStatusDto
            {
                Mode = c.Mode,
                ConnectorName = c.Name,
                IsDefault = c.Mode == SmartPortDataMode.SyntheticDemo,
                IsEnabled = c.IsEnabled,
                RequiresApproval = c.Mode != SmartPortDataMode.SyntheticDemo,
                Status = c.IsEnabled
                    ? (c.Mode == SmartPortDataMode.SyntheticDemo ? "Active default demo connector" : "Enabled for approved pilot configuration")
                    : "Disabled until real endpoint/credentials and pilot approvals exist",
                DataRequired = GetDataRequired(c.Mode),
                Notes = c.Mode == SmartPortDataMode.SyntheticDemo
                    ? "SyntheticDemo remains the safe default and requires no API keys or internet access."
                    : "Prepared connector architecture only; no live third-party system access is claimed."
            })
            .ToList();

        var syntheticSnapshot = await _connectors.First(c => c.Mode == SmartPortDataMode.SyntheticDemo).GetSnapshotAsync(cancellationToken);
        statuses.Add(await _webhook.IngestAsync(syntheticSnapshot, cancellationToken));
        return statuses;
    }

    private static string GetDataRequired(SmartPortDataMode mode) => mode switch
    {
        SmartPortDataMode.SyntheticDemo => "Seeded demo readings for vessel ETA, berth, yard, truck queues, containers, disruptions, energy and emissions.",
        SmartPortDataMode.CsvImport => "Approved CSV exports for vessel, berth, yard, truck queue, container, disruption, energy and emissions fields.",
        SmartPortDataMode.RestApi => "Approved REST API base URL, schema, authentication method, rate limits and sample payloads.",
        SmartPortDataMode.Database => "Approved read-only database replica/view, schema documentation and least-privilege credentials.",
        SmartPortDataMode.ManualEntry => "Approved operator workflow, required fields, reviewer role and audit policy.",
        SmartPortDataMode.Webhook => "Approved webhook schema, signing/authentication method, retry policy and sample payloads.",
        _ => "Pilot data specification required."
    };
}

public sealed class SmartPortFieldMappingService : ISmartPortFieldMappingService
{
    public Task<IReadOnlyList<SmartPortFieldMappingDto>> GetSeedMappingsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SmartPortFieldMappingDto> mappings = new[]
        {
            Mapping("vessel_eta", nameof(VesselEtaReading), nameof(VesselEtaReading.EstimatedArrival), "2026-05-12T14:30:00Z", "Maps ETA export fields to pilot vessel arrival planning."),
            Mapping("berth_status", nameof(BerthStatusReading), nameof(BerthStatusReading.Status), "Occupied", "Tracks berth availability and berth pressure."),
            Mapping("truck_queue_length", nameof(TruckQueueReading), nameof(TruckQueueReading.QueueLength), "47", "Supports gate smoothing and hold-outside-port recommendations."),
            Mapping("yard_capacity_percent", nameof(YardCapacityReading), nameof(YardCapacityReading.CapacityPercent), "84", "Supports yard congestion and discharge planning."),
            Mapping("container_volume", nameof(ContainerFlowReading), nameof(ContainerFlowReading.Volume), "1280", "Supports import/export flow and dwell-risk monitoring."),
            Mapping("disruption_type", nameof(DisruptionEventReading), nameof(DisruptionEventReading.DisruptionType), "Energy constraint", "Supports incident/disruption scenario planning."),
            Mapping("energy_constraint", nameof(EnergyConstraintReading), nameof(EnergyConstraintReading.ConstraintType), "Load-shed risk window", "Supports peak energy and reefer-demand planning."),
            Mapping("idling_minutes", nameof(EmissionsIdlingReading), nameof(EmissionsIdlingReading.IdlingMinutes), "1860", "Supports indicative clean-logistics and emissions reporting.")
        };

        return Task.FromResult(mappings);
    }

    public Task<IReadOnlyList<SmartPortFieldMappingDto>> GetMissingPilotMappingsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SmartPortFieldMappingDto> missing = new[]
        {
            new SmartPortFieldMappingDto { SourceField = "external_event_id", TargetDto = nameof(SmartPortDecisionEvent), TargetField = nameof(SmartPortDecisionEvent.ExternalId), IsMapped = false, ExampleValue = "partner-event-123", Notes = "Needed for end-to-end traceability when partner sample exports are available." },
            new SmartPortFieldMappingDto { SourceField = "reviewer_user_id", TargetDto = nameof(RecommendationAuditEvent), TargetField = nameof(RecommendationAuditEvent.HumanReviewer), IsMapped = false, ExampleValue = "operator@example.com", Notes = "Needed to bind recommendation approvals to pilot user roles." }
        };

        return Task.FromResult(missing);
    }

    private static SmartPortFieldMappingDto Mapping(string sourceField, string targetDto, string targetField, string exampleValue, string notes) => new()
    {
        SourceField = sourceField,
        TargetDto = targetDto,
        TargetField = targetField,
        ExampleValue = exampleValue,
        Notes = notes
    };
}

public sealed class SmartPortReadinessScoringService : ISmartPortReadinessScoringService
{
    private readonly ISmartPortIntegrationHealthService _health;
    private readonly ISmartPortFieldMappingService _mapping;

    public SmartPortReadinessScoringService(ISmartPortIntegrationHealthService health, ISmartPortFieldMappingService mapping)
    {
        _health = health;
        _mapping = mapping;
    }

    public async Task<SmartPortReadinessReportDto> GetPilotReadinessReportAsync(CancellationToken cancellationToken = default)
    {
        var health = await _health.GetConnectorHealthAsync(cancellationToken);
        var mapped = await _mapping.GetSeedMappingsAsync(cancellationToken);
        var missing = await _mapping.GetMissingPilotMappingsAsync(cancellationToken);
        var enabledLiveConnectors = health.Count(h => h.Mode != SmartPortDataMode.SyntheticDemo && h.IsEnabled);
        var mappingScore = (int)Math.Round(mapped.Count * 100m / (mapped.Count + missing.Count));
        var score = Math.Clamp(mappingScore - 4 + enabledLiveConnectors, 0, 100);

        return new SmartPortReadinessReportDto
        {
            CurrentMode = SmartPortDataMode.SyntheticDemo,
            ReadinessScore = score,
            ReadinessLabel = score >= 80 ? "Pilot mapping mostly ready" : "Demo ready; sample partner data needed",
            GeneratedAtUtc = DateTime.UtcNow,
            ConnectorHealth = health,
            MappedFields = mapped,
            MissingFields = missing,
            RequiredLivePortData = new[]
            {
                "Vessel ETA and voyage updates",
                "Berth occupancy/status and assignment windows",
                "Yard capacity by block and reefer availability",
                "Truck queue length, gate processing time and hold/release status",
                "Container volume, direction, dwell alerts and exception flags",
                "Incidents/disruptions with severity, area and clearance estimates",
                "Energy/load-shedding or constraint windows",
                "Idling minutes, fuel/emissions assumptions and audit references"
            },
            Risks = new[]
            {
                "Pilot data quality and field naming may differ from the synthetic demo schema.",
                "Live connectors must remain disabled until endpoint access, credentials and pilot approvals exist.",
                "Operational users need clear approval rights before recommendations influence real workflows.",
                "Savings and emissions outcomes remain estimates until measured against a pilot baseline."
            },
            Recommendations = new[]
            {
                "Synthetic demo mode is active. Connect CSV/API export from operational systems before sandbox testing.",
                "Truck queue is above threshold. Prioritise gate smoothing and berth-yard coordination.",
                "Energy constraint overlaps peak reefer demand. Prepare load-shift scenario.",
                "Field mappings are mostly complete. Next step: request sample export from pilot partner."
            },
            NextPilotSteps = new[]
            {
                "Confirm pilot sponsor, data owner and operator reviewer roles.",
                "Request sample CSV/API/database extracts for the mapped fields.",
                "Validate field mappings in a sandbox with synthetic fallback still enabled.",
                "Run controlled pilot sessions with human approval and audit review.",
                "Compare queue, turnaround, idling and emissions estimates against agreed baselines."
            },
            NoLiveIntegrationDisclaimer = "This prototype currently runs on synthetic/demo data. The codebase includes an integration-ready architecture for onboarding real port operations data such as CSV exports, REST APIs, databases, manual entries, and webhooks during a controlled pilot. No live third-party port system access is claimed in this demo."
        };
    }
}
