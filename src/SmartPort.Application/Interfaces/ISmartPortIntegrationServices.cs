namespace SmartPort.Application.Interfaces;

public interface ISmartPortDataConnector
{
    SmartPortDataMode Mode { get; }
    string Name { get; }
    bool IsEnabled { get; }
    Task<SmartPortConnectorSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public interface ISyntheticSmartPortConnector : ISmartPortDataConnector { }
public interface ICsvSmartPortConnector : ISmartPortDataConnector { }
public interface IRestSmartPortConnector : ISmartPortDataConnector { }
public interface IDatabaseSmartPortConnector : ISmartPortDataConnector { }
public interface IManualSmartPortConnector : ISmartPortDataConnector { }

public interface IWebhookSmartPortIngestionService
{
    Task<SmartPortConnectorStatusDto> IngestAsync(SmartPortConnectorSnapshot snapshot, CancellationToken cancellationToken = default);
}

public interface ISmartPortIntegrationHealthService
{
    Task<IReadOnlyList<SmartPortConnectorStatusDto>> GetConnectorHealthAsync(CancellationToken cancellationToken = default);
}

public interface ISmartPortFieldMappingService
{
    Task<IReadOnlyList<SmartPortFieldMappingDto>> GetSeedMappingsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SmartPortFieldMappingDto>> GetMissingPilotMappingsAsync(CancellationToken cancellationToken = default);
}

public interface ISmartPortReadinessScoringService
{
    Task<SmartPortReadinessReportDto> GetPilotReadinessReportAsync(CancellationToken cancellationToken = default);
}
