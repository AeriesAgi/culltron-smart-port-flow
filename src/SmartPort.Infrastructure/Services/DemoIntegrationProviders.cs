using SmartPort.Application.Interfaces;

namespace SmartPort.Infrastructure.Services;

public class DemoTruckTelematicsProvider : ITruckTelematicsProvider
{
    public Task<IntegrationProviderStatus> GetStatusAsync() => Task.FromResult(DemoIntegrationFactory.Create(
        "Truck GPS/telematics", "ITruckTelematicsProvider / DemoTruckTelematicsProvider",
        "Vehicle position, depot release, driver status, ETA events", "Improves hold/release timing and reduces avoidable idling.", "Needs Partner Data", "Medium"));
}

public class DemoGpsTrackingProvider : IGpsTrackingProvider
{
    public Task<IntegrationProviderStatus> GetStatusAsync() => Task.FromResult(DemoIntegrationFactory.Create(
        "GPS tracking stream", "IGpsTrackingProvider / DemoGpsTrackingProvider",
        "Truck checkpoint/geofence events and ETA confidence", "Adds route-level visibility before trucks reach the gate.", "Needs Partner Data", "Medium"));
}

public class DemoGateSystemProvider : IGateSystemProvider
{
    public Task<IntegrationProviderStatus> GetStatusAsync() => Task.FromResult(DemoIntegrationFactory.Create(
        "Gate OCR/RFID", "IGateSystemProvider / DemoGateSystemProvider",
        "Lane status, queue length, processing time, exception events", "Targets bottlenecks and supports overflow-lane decisions.", "Low Risk", "Medium"));
}

public class DemoPortOperationsProvider : IPortOperationsProvider
{
    public Task<IntegrationProviderStatus> GetStatusAsync() => Task.FromResult(DemoIntegrationFactory.Create(
        "Port/TOS/IPMS data", "IPortOperationsProvider / DemoPortOperationsProvider",
        "Berth plan, vessel schedule, yard capacity, container status", "Connects berth, yard and gate decisions into one command view.", "Needs Partner Data", "High"));
}

public class DemoEnergyDisruptionProvider : IEnergyDisruptionProvider
{
    public Task<IntegrationProviderStatus> GetStatusAsync() => Task.FromResult(DemoIntegrationFactory.Create(
        "Load-shedding/energy data", "IEnergyDisruptionProvider / DemoEnergyDisruptionProvider",
        "Energy schedule, outage window, backup capacity and critical assets", "Improves resilience planning during power disruption windows.", "Low Risk", "Low"));
}

public class DemoEmissionsFactorProvider : IEmissionsFactorProvider
{
    public Task<IntegrationProviderStatus> GetStatusAsync() => Task.FromResult(DemoIntegrationFactory.Create(
        "Emissions factors", "IEmissionsFactorProvider / DemoEmissionsFactorProvider",
        "Diesel burn rate, CO₂ factor, vehicle class and idling assumptions", "Makes indicative clean-logistics reporting transparent and auditable.", "Low Risk", "Low"));
}

public class DemoExternalIntegrationHealthService : IExternalIntegrationHealthService
{
    private readonly ITruckTelematicsProvider _truckTelematics;
    private readonly IGpsTrackingProvider _gpsTracking;
    private readonly IGateSystemProvider _gateSystem;
    private readonly IPortOperationsProvider _portOperations;
    private readonly IEnergyDisruptionProvider _energy;
    private readonly IEmissionsFactorProvider _emissions;

    public DemoExternalIntegrationHealthService(ITruckTelematicsProvider truckTelematics, IGpsTrackingProvider gpsTracking,
        IGateSystemProvider gateSystem, IPortOperationsProvider portOperations, IEnergyDisruptionProvider energy,
        IEmissionsFactorProvider emissions)
    {
        _truckTelematics = truckTelematics;
        _gpsTracking = gpsTracking;
        _gateSystem = gateSystem;
        _portOperations = portOperations;
        _energy = energy;
        _emissions = emissions;
    }

    public async Task<IReadOnlyList<IntegrationProviderStatus>> GetIntegrationHealthAsync()
    {
        var prepared = new List<IntegrationProviderStatus>
        {
            await _portOperations.GetStatusAsync(),
            await _gateSystem.GetStatusAsync(),
            await _truckTelematics.GetStatusAsync(),
            await _gpsTracking.GetStatusAsync(),
            DemoIntegrationFactory.Create("Fleet dispatch systems", "Future adapter documented", "Dispatch plan, fleet allocation, job status and depot release", "Aligns fleet release timing with port readiness.", "Needs Partner Data", "Medium"),
            await _energy.GetStatusAsync(),
            await _emissions.GetStatusAsync(),
            DemoIntegrationFactory.Create("Incident/disruption logs", "Future adapter documented", "Incident type, severity, owner, timestamps and resolution state", "Feeds the action plan and decision audit trail.", "Low Risk", "Medium"),
            DemoIntegrationFactory.Create("Reporting/export layer", "Future adapter documented", "KPI exports, briefs, CSV/API/reporting requirements", "Supports pilot reviews, grant updates and stakeholder reporting.", "Low Risk", "Low"),
            DemoIntegrationFactory.Create("Future API gateway", "Future adapter documented", "Authentication model, endpoint catalogue and rate limits", "Creates a controlled path from demo to production integrations.", "Needs Partner Data", "High"),
            DemoIntegrationFactory.Create("Role-based operator access", "Future adapter documented", "Operator roles, permissions, approval matrix and audit requirements", "Keeps recommendations accountable across stakeholder groups.", "Low Risk", "Medium")
        };

        return prepared;
    }
}

internal static class DemoIntegrationFactory
{
    public static IntegrationProviderStatus Create(string name, string adapter, string dataNeeded, string businessValue, string riskLevel, string complexity) => new()
    {
        Name = name,
        Adapter = adapter,
        DataNeeded = dataNeeded,
        BusinessValue = businessValue,
        RiskLevel = riskLevel,
        Complexity = complexity,
        Badges = new() { "Demo Ready", "Future Integration", "High Value", "Integration Adapter Prepared", riskLevel }
    };
}
