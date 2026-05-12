namespace SmartPort.Application.Interfaces;

public enum SmartPortDataMode
{
    SyntheticDemo,
    CsvImport,
    RestApi,
    Database,
    ManualEntry,
    Webhook
}

public abstract class SmartPortReadingBase
{
    public string ExternalId { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = "SyntheticDemo";
    public SmartPortDataMode Mode { get; set; } = SmartPortDataMode.SyntheticDemo;
    public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsSynthetic { get; set; } = true;
    public decimal ConfidenceScore { get; set; } = 0.86m;
    public string Notes { get; set; } = string.Empty;
}

public sealed class VesselEtaReading : SmartPortReadingBase
{
    public string VesselName { get; set; } = string.Empty;
    public string VoyageNumber { get; set; } = string.Empty;
    public DateTime EstimatedArrival { get; set; }
    public int DelayMinutes { get; set; }
    public string DelayReason { get; set; } = string.Empty;
}

public sealed class BerthStatusReading : SmartPortReadingBase
{
    public string BerthCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssignedVessel { get; set; } = string.Empty;
    public int OccupancyPercent { get; set; }
}

public sealed class YardCapacityReading : SmartPortReadingBase
{
    public string YardBlock { get; set; } = string.Empty;
    public decimal CapacityPercent { get; set; }
    public int ReeferSlotsAvailable { get; set; }
    public string PressureLevel { get; set; } = string.Empty;
}

public sealed class TruckQueueReading : SmartPortReadingBase
{
    public string GateName { get; set; } = string.Empty;
    public int QueueLength { get; set; }
    public int AverageWaitMinutes { get; set; }
    public int HoldOutsidePortCandidates { get; set; }
}

public sealed class ContainerFlowReading : SmartPortReadingBase
{
    public string FlowDirection { get; set; } = string.Empty;
    public int Volume { get; set; }
    public int DwellAlertCount { get; set; }
    public string CommodityGroup { get; set; } = string.Empty;
}

public sealed class DisruptionEventReading : SmartPortReadingBase
{
    public string DisruptionType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string AffectedArea { get; set; } = string.Empty;
    public DateTime ExpectedClearanceUtc { get; set; }
}

public sealed class EnergyConstraintReading : SmartPortReadingBase
{
    public string ConstraintType { get; set; } = string.Empty;
    public DateTime WindowStartUtc { get; set; }
    public DateTime WindowEndUtc { get; set; }
    public string OperationalImpact { get; set; } = string.Empty;
}

public sealed class EmissionsIdlingReading : SmartPortReadingBase
{
    public string Area { get; set; } = string.Empty;
    public int IdlingMinutes { get; set; }
    public decimal EstimatedCo2Kg { get; set; }
    public decimal EstimatedDieselLitres { get; set; }
}

public sealed class SmartPortDecisionEvent : SmartPortReadingBase
{
    public string DecisionType { get; set; } = string.Empty;
    public string DecisionSummary { get; set; } = string.Empty;
    public string RecommendedBy { get; set; } = "SmartPort Flow";
    public string ApprovalStatus { get; set; } = "Pending human review";
}

public sealed class RecommendationAuditEvent : SmartPortReadingBase
{
    public string RecommendationId { get; set; } = string.Empty;
    public string RecommendationText { get; set; } = string.Empty;
    public string AuditStatus { get; set; } = "Review required";
    public string HumanReviewer { get; set; } = "Unassigned";
}

public sealed class SmartPortConnectorSnapshot
{
    public SmartPortDataMode Mode { get; set; } = SmartPortDataMode.SyntheticDemo;
    public string ConnectorName { get; set; } = "SyntheticSmartPortConnector";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<VesselEtaReading> VesselEtas { get; set; } = Array.Empty<VesselEtaReading>();
    public IReadOnlyList<BerthStatusReading> BerthStatuses { get; set; } = Array.Empty<BerthStatusReading>();
    public IReadOnlyList<YardCapacityReading> YardCapacities { get; set; } = Array.Empty<YardCapacityReading>();
    public IReadOnlyList<TruckQueueReading> TruckQueues { get; set; } = Array.Empty<TruckQueueReading>();
    public IReadOnlyList<ContainerFlowReading> ContainerFlows { get; set; } = Array.Empty<ContainerFlowReading>();
    public IReadOnlyList<DisruptionEventReading> Disruptions { get; set; } = Array.Empty<DisruptionEventReading>();
    public IReadOnlyList<EnergyConstraintReading> EnergyConstraints { get; set; } = Array.Empty<EnergyConstraintReading>();
    public IReadOnlyList<EmissionsIdlingReading> EmissionsIdling { get; set; } = Array.Empty<EmissionsIdlingReading>();
    public IReadOnlyList<SmartPortDecisionEvent> DecisionEvents { get; set; } = Array.Empty<SmartPortDecisionEvent>();
    public IReadOnlyList<RecommendationAuditEvent> RecommendationAuditEvents { get; set; } = Array.Empty<RecommendationAuditEvent>();
}

public sealed class SmartPortConnectorStatusDto
{
    public SmartPortDataMode Mode { get; set; }
    public string ConnectorName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public string Status { get; set; } = string.Empty;
    public string DataRequired { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class SmartPortFieldMappingDto
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetDto { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public bool IsRequiredForPilot { get; set; } = true;
    public bool IsMapped { get; set; } = true;
    public string ExampleValue { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class SmartPortReadinessReportDto
{
    public SmartPortDataMode CurrentMode { get; set; } = SmartPortDataMode.SyntheticDemo;
    public int ReadinessScore { get; set; }
    public string ReadinessLabel { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<string> RequiredLivePortData { get; set; } = Array.Empty<string>();
    public IReadOnlyList<SmartPortFieldMappingDto> MappedFields { get; set; } = Array.Empty<SmartPortFieldMappingDto>();
    public IReadOnlyList<SmartPortFieldMappingDto> MissingFields { get; set; } = Array.Empty<SmartPortFieldMappingDto>();
    public IReadOnlyList<string> Risks { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> NextPilotSteps { get; set; } = Array.Empty<string>();
    public IReadOnlyList<SmartPortConnectorStatusDto> ConnectorHealth { get; set; } = Array.Empty<SmartPortConnectorStatusDto>();
    public string NoLiveIntegrationDisclaimer { get; set; } = string.Empty;
}
