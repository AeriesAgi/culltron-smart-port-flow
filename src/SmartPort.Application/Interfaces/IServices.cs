using SmartPort.Domain.Entities;
using SmartPort.Domain.Enums;

namespace SmartPort.Application.Interfaces;

// ─── Dashboard ───────────────────────────────────────────────────────────────

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<IEnumerable<KpiTrendDto>> GetKpiTrendsAsync(int days = 30);
}

// ─── Vessel ──────────────────────────────────────────────────────────────────

public interface IVesselService
{
    Task<PagedResult<VesselListDto>> GetVesselsAsync(VesselFilterDto filter);
    Task<VesselDetailDto?> GetVesselDetailAsync(int id);
    Task<IEnumerable<VesselListDto>> GetVesselsInPortAsync();
    Task<IEnumerable<VesselListDto>> GetExpectedVesselsAsync(int hours = 48);
    Task<int> CreateVesselAsync(CreateVesselDto dto, string userId);
    Task UpdateVesselAsync(int id, UpdateVesselDto dto, string userId);
    Task UpdateVesselStatusAsync(int id, VesselStatus status, string userId);
}

// ─── Berth ───────────────────────────────────────────────────────────────────

public interface IBerthService
{
    Task<IEnumerable<BerthStatusDto>> GetAllBerthsAsync();
    Task<BerthDetailDto?> GetBerthDetailAsync(int id);
    Task<IEnumerable<BerthAssignmentDto>> GetBerthScheduleAsync(DateTime from, DateTime to);
    Task<int> CreateBerthAssignmentAsync(CreateBerthAssignmentDto dto, string userId);
    Task UpdateBerthStatusAsync(int berthId, BerthStatus status, string userId);
    Task<IEnumerable<BerthOccupancyDto>> GetBerthOccupancyAsync();
}

// ─── Container ───────────────────────────────────────────────────────────────

public interface IContainerService
{
    Task<PagedResult<ContainerListDto>> GetContainersAsync(ContainerFilterDto filter);
    Task<ContainerDetailDto?> GetContainerDetailAsync(int id);
    Task<ContainerDetailDto?> GetContainerByNumberAsync(string containerNumber);
    Task<IEnumerable<ContainerListDto>> GetDwellAlertContainersAsync();
    Task UpdateContainerStatusAsync(int id, ContainerStatus status, string userId);
}

// ─── Yard ────────────────────────────────────────────────────────────────────

public interface IYardService
{
    Task<IEnumerable<YardBlockStatusDto>> GetYardBlockStatusAsync();
    Task<YardBlockDetailDto?> GetYardBlockDetailAsync(int id);
}

// ─── Gate / Truck ────────────────────────────────────────────────────────────

public interface IGateService
{
    Task<IEnumerable<GateStatusDto>> GetGateStatusAsync();
    Task<PagedResult<TruckListDto>> GetTrucksAsync(TruckFilterDto filter);
    Task<IEnumerable<GateTransactionDto>> GetRecentTransactionsAsync(int count = 50);
}

// ─── Incident ────────────────────────────────────────────────────────────────

public interface IIncidentService
{
    Task<PagedResult<IncidentListDto>> GetIncidentsAsync(IncidentFilterDto filter);
    Task<IncidentDetailDto?> GetIncidentDetailAsync(int id);
    Task<int> CreateIncidentAsync(CreateIncidentDto dto, string userId);
    Task AcknowledgeIncidentAsync(int id, string userId);
    Task UpdateIncidentAsync(int id, UpdateIncidentDto dto, string userId);
    Task ResolveIncidentAsync(int id, ResolveIncidentDto dto, string userId);
    Task<IEnumerable<IncidentListDto>> GetOpenIncidentsAsync();
}

// ─── Alert ───────────────────────────────────────────────────────────────────

public interface IAlertService
{
    Task<IEnumerable<AlertDto>> GetActiveAlertsAsync();
    Task AcknowledgeAlertAsync(int id, string userId);
    Task ResolveAlertAsync(int id, string userId);
    Task<int> GetActiveAlertCountAsync();
}

// ─── Document ────────────────────────────────────────────────────────────────

public interface IDocumentService
{
    Task<PagedResult<DocumentListDto>> GetDocumentsAsync(DocumentFilterDto filter);
    Task<DocumentDetailDto?> GetDocumentDetailAsync(int id);
    Task<int> CreateDocumentAsync(CreateDocumentDto dto, string userId);
    Task UpdateDocumentStatusAsync(int id, DocumentStatus status, string userId, string? notes = null);
    Task<IEnumerable<DocumentListDto>> GetOverdueDocumentsAsync();
}

// ─── Analytics ───────────────────────────────────────────────────────────────

public interface IAnalyticsService
{
    Task<ThroughputAnalyticsDto> GetThroughputAnalyticsAsync(AnalyticsFilterDto filter);
    Task<TurnaroundAnalyticsDto> GetTurnaroundAnalyticsAsync(AnalyticsFilterDto filter);
    Task<BerthEfficiencyDto> GetBerthEfficiencyAsync(AnalyticsFilterDto filter);
    Task<YardAnalyticsDto> GetYardAnalyticsAsync(AnalyticsFilterDto filter);
}

// ─── Recommendation / AI ─────────────────────────────────────────────────────

public interface IRecommendationService
{
    Task<IEnumerable<RecommendationDto>> GetActiveRecommendationsAsync();
    Task AcceptRecommendationAsync(int id, string userId, string? notes);
    Task DismissRecommendationAsync(int id, string userId, string? notes);
    Task RunRecommendationEngineAsync();  // trigger rules engine
}
