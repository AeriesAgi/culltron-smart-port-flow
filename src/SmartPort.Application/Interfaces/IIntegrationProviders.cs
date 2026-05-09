namespace SmartPort.Application.Interfaces;

public interface ITruckTelematicsProvider
{
    Task<IntegrationProviderStatus> GetStatusAsync();
}

public interface IGpsTrackingProvider
{
    Task<IntegrationProviderStatus> GetStatusAsync();
}

public interface IGateSystemProvider
{
    Task<IntegrationProviderStatus> GetStatusAsync();
}

public interface IPortOperationsProvider
{
    Task<IntegrationProviderStatus> GetStatusAsync();
}

public interface IEnergyDisruptionProvider
{
    Task<IntegrationProviderStatus> GetStatusAsync();
}

public interface IEmissionsFactorProvider
{
    Task<IntegrationProviderStatus> GetStatusAsync();
}

public interface IExternalIntegrationHealthService
{
    Task<IReadOnlyList<IntegrationProviderStatus>> GetIntegrationHealthAsync();
}

public class IntegrationProviderStatus
{
    public string Name { get; set; } = string.Empty;
    public string DemoStatus { get; set; } = "Simulated";
    public string ProductionStatus { get; set; } = "Future integration";
    public string DataNeeded { get; set; } = string.Empty;
    public string BusinessValue { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Low";
    public string Complexity { get; set; } = "Medium";
    public string Adapter { get; set; } = string.Empty;
    public List<string> Badges { get; set; } = new();
}
