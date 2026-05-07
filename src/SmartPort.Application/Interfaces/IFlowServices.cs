using SmartPort.Domain.Entities;
using SmartPort.Domain.Enums;

namespace SmartPort.Application.Interfaces;

// ─── Flow Intelligence Settings ───────────────────────────────────────────────

public class FlowIntelligenceSettings
{
    public decimal IdlingLitresPerHour { get; set; } = 3.0m;
    public decimal DieselPricePerLitre { get; set; } = 24.00m;
    public decimal Co2KgPerLitreDiesel { get; set; } = 2.68m;
    public int HighWaitingThresholdMinutes { get; set; } = 60;
    public int CriticalWaitingThresholdMinutes { get; set; } = 120;
    public int HighRiskThreshold { get; set; } = 61;
    public int CriticalRiskThreshold { get; set; } = 81;
}

// ─── Congestion Risk ──────────────────────────────────────────────────────────

public interface ICongestionRiskService
{
    Task<CongestionRiskResult> CalculateRiskAsync(DispatchTrip trip);
}

public class CongestionRiskResult
{
    public int Score { get; set; }           // 0–100
    public FlowRiskLevel RiskLevel { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public List<string> Factors { get; set; } = new();

    public string RiskColourClass => RiskLevel switch {
        FlowRiskLevel.Low      => "success",
        FlowRiskLevel.Medium   => "warning",
        FlowRiskLevel.High     => "high",
        FlowRiskLevel.Critical => "danger",
        _ => "muted"
    };
}

// ─── Dispatch Recommendation ──────────────────────────────────────────────────

public interface IDispatchRecommendationService
{
    Task<DispatchRecommendationResult> GenerateRecommendationAsync(DispatchTrip trip);
}

public class DispatchRecommendationResult
{
    public FlowRecommendationType RecommendationType { get; set; }
    public string RecommendationText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public FlowRiskLevel RiskLevel { get; set; }
    public FlowConfidenceLevel ConfidenceLevel { get; set; }
    public string? ExpectedBenefit { get; set; }
    public int CongestionScore { get; set; }
}

// ─── Idling & Emissions ───────────────────────────────────────────────────────

public interface IIdlingEmissionService
{
    Task<EmissionEstimateResult> CalculateEstimateAsync(DispatchTrip trip);
}

public class EmissionEstimateResult
{
    public decimal EstimatedIdlingMinutes { get; set; }
    public decimal EstimatedDieselLitres { get; set; }
    public decimal EstimatedFuelCost { get; set; }
    public decimal EstimatedCo2Kg { get; set; }
    public bool AvoidableIdlingFlag { get; set; }
    public string Notes { get; set; } = string.Empty;
}

// ─── Pilot Metrics ────────────────────────────────────────────────────────────

public interface IPilotMetricsService
{
    Task<PilotMetricSnapshot?> GetBaselineMetricsAsync();
    Task<PilotMetricResult> GetCurrentMetricsAsync();
    Task<PilotComparisonResult> GetPilotComparisonAsync();
    Task GeneratePilotSnapshotAsync(string periodLabel, int? organisationId = null);
}

public class PilotMetricResult
{
    public decimal AverageWaitingMinutes { get; set; }
    public decimal TotalIdlingMinutes { get; set; }
    public decimal EstimatedDieselLitres { get; set; }
    public decimal EstimatedFuelCost { get; set; }
    public decimal EstimatedCo2Kg { get; set; }
    public int MissedArrivalWindows { get; set; }
    public decimal DispatchReliabilityPercent { get; set; }
    public int RecommendationsGenerated { get; set; }
    public int HighRiskTrips { get; set; }
    public int TotalTrips { get; set; }
    public int ActiveDisruptions { get; set; }
}

public class PilotComparisonResult
{
    public PilotMetricSnapshot? Baseline { get; set; }
    public PilotMetricResult Current { get; set; } = new();
    public decimal? WaitingMinutesChangePercent { get; set; }
    public decimal? IdlingChangePercent { get; set; }
    public decimal? FuelCostChangePercent { get; set; }
    public decimal? Co2ChangePercent { get; set; }
    public decimal? ReliabilityChangePoints { get; set; }
    public string PeriodNote { get; set; } = string.Empty;
}

// ─── Organisation Service ─────────────────────────────────────────────────────

public interface IOrganisationService
{
    Task<IEnumerable<OrganisationListDto>> GetAllAsync();
    Task<OrganisationDetailDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(SaveOrganisationDto dto, string userId);
    Task UpdateAsync(int id, SaveOrganisationDto dto, string userId);
    Task SetActiveAsync(int id, bool isActive, string userId);
}

// ─── Fleet Vehicle Service ────────────────────────────────────────────────────

public interface IFleetVehicleService
{
    Task<PagedResult<FleetVehicleListDto>> GetVehiclesAsync(FleetVehicleFilterDto filter);
    Task<FleetVehicleDetailDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(SaveFleetVehicleDto dto, string userId);
    Task UpdateAsync(int id, SaveFleetVehicleDto dto, string userId);
}

// ─── Dispatch Trip Service ────────────────────────────────────────────────────

public interface IDispatchTripService
{
    Task<PagedResult<DispatchTripListDto>> GetTripsAsync(DispatchTripFilterDto filter);
    Task<DispatchTripDetailDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(SaveDispatchTripDto dto, string userId);
    Task UpdateAsync(int id, SaveDispatchTripDto dto, string userId);
    Task UpdateStatusAsync(int id, TripStatus status, string userId);
    Task<IEnumerable<DispatchTripListDto>> GetHighRiskTripsAsync(int topN = 10);
    Task<IEnumerable<DispatchTripListDto>> GetActiveTripsAsync();
}

// ─── Disruption Service ───────────────────────────────────────────────────────

public interface IDisruptionService
{
    Task<IEnumerable<DisruptionListDto>> GetActiveAsync();
    Task<IEnumerable<DisruptionListDto>> GetAllAsync(int take = 50);
    Task<int> CreateAsync(SaveDisruptionDto dto, string userId);
    Task ResolveAsync(int id, string userId);
    Task<int> GetActiveCountAsync();
}

// ─── Flow Intelligence Summary Service ───────────────────────────────────────

public interface IFlowIntelligenceService
{
    Task<FlowIntelligenceSummaryDto> GetSummaryAsync();
    Task<IEnumerable<FlowRecommendationDto2>> GetActiveRecommendationsAsync(int take = 20);
    Task<FlowRecommendationDto2?> GetRecommendationByIdAsync(int id);
    Task AcceptRecommendationAsync(int id, string feedback, string userId);
    Task DismissRecommendationAsync(int id, string feedback, string userId);
    Task<int> GetTodayRecommendationCountAsync();
}

// ─── Emissions Summary Service ───────────────────────────────────────────────

public interface IEmissionsSummaryService
{
    Task<EmissionsSummaryDto> GetSummaryAsync();
    Task<IEnumerable<EmissionTripRowDto>> GetTopIdlingTripsAsync(int topN = 20);
}

// ─────────────────────────────────────────────────────────────────────────────
// DTOs
// ─────────────────────────────────────────────────────────────────────────────

// ─── Organisation DTOs ────────────────────────────────────────────────────────

public class OrganisationListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public OrganisationType OrganisationType { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int VehicleCount { get; set; }
    public int ActiveTripCount { get; set; }
}

public class OrganisationDetailDto : OrganisationListDto
{
    public string? RegistrationNumber { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public IEnumerable<FleetVehicleListDto> Vehicles { get; set; } = [];
}

public class SaveOrganisationDto
{
    public string Name { get; set; } = string.Empty;
    public OrganisationType OrganisationType { get; set; }
    public string? RegistrationNumber { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string Province { get; set; } = "KwaZulu-Natal";
}

// ─── Fleet Vehicle DTOs ───────────────────────────────────────────────────────

public class FleetVehicleListDto
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? FleetNumber { get; set; }
    public FleetVehicleType VehicleType { get; set; }
    public FleetCargoType CargoType { get; set; }
    public FleetVehicleStatus Status { get; set; }
    public string? CurrentLocation { get; set; }
    public decimal? CapacityTons { get; set; }
}

public class FleetVehicleDetailDto : FleetVehicleListDto
{
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public IEnumerable<DispatchTripListDto> RecentTrips { get; set; } = [];
}

public class SaveFleetVehicleDto
{
    public int OrganisationId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? FleetNumber { get; set; }
    public FleetVehicleType VehicleType { get; set; }
    public FleetCargoType CargoType { get; set; }
    public decimal? CapacityTons { get; set; }
    public FleetVehicleStatus Status { get; set; }
    public string? CurrentLocation { get; set; }
}

public class FleetVehicleFilterDto
{
    public int? OrganisationId { get; set; }
    public FleetVehicleStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

// ─── Dispatch Trip DTOs ───────────────────────────────────────────────────────

public class DispatchTripListDto
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public string VehicleRegistration { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public FleetCargoType CargoType { get; set; }
    public TripUrgencyLevel UrgencyLevel { get; set; }
    public TripStatus Status { get; set; }
    public DateTime PlannedDispatchTime { get; set; }
    public DateTime PlannedArrivalWindowEnd { get; set; }
    public DateTime? ActualArrivalTime { get; set; }
    public bool IsArrivalWindowMissed { get; set; }
    public int? LatestRiskScore { get; set; }
    public FlowRiskLevel? LatestRiskLevel { get; set; }
}

public class DispatchTripDetailDto : DispatchTripListDto
{
    public DateTime PlannedArrivalWindowStart { get; set; }
    public DateTime? ActualDispatchTime { get; set; }
    public DateTime? GateInTime { get; set; }
    public DateTime? GateOutTime { get; set; }
    public decimal? PortDwellMinutes { get; set; }
    public string? CargoDescription { get; set; }
    public string? Notes { get; set; }
    public EmissionEstimateResult? EmissionEstimate { get; set; }
    public IEnumerable<FlowRecommendationDto2> Recommendations { get; set; } = [];
}

public class SaveDispatchTripDto
{
    public int OrganisationId { get; set; }
    public int FleetVehicleId { get; set; }
    public int? DriverId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public FleetCargoType CargoType { get; set; }
    public string? CargoDescription { get; set; }
    public TripUrgencyLevel UrgencyLevel { get; set; }
    public DateTime PlannedDispatchTime { get; set; }
    public DateTime PlannedArrivalWindowStart { get; set; }
    public DateTime PlannedArrivalWindowEnd { get; set; }
    public string? Notes { get; set; }
}

public class DispatchTripFilterDto
{
    public int? OrganisationId { get; set; }
    public TripStatus? Status { get; set; }
    public FlowRiskLevel? RiskLevel { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

// ─── Disruption DTOs ──────────────────────────────────────────────────────────

public class DisruptionListDto
{
    public int Id { get; set; }
    public DisruptionType DisruptionType { get; set; }
    public DisruptionSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AffectedLocation { get; set; }
    public string? AffectedRoute { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class SaveDisruptionDto
{
    public DisruptionType DisruptionType { get; set; }
    public DisruptionSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AffectedLocation { get; set; }
    public string? AffectedRoute { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

// ─── Flow Recommendation DTO ──────────────────────────────────────────────────

public class FlowRecommendationDto2
{
    public int Id { get; set; }
    public int? DispatchTripId { get; set; }
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public string? TripRoute { get; set; }
    public string? VehicleRegistration { get; set; }
    public FlowRecommendationType RecommendationType { get; set; }
    public FlowRiskLevel RiskLevel { get; set; }
    public FlowConfidenceLevel ConfidenceLevel { get; set; }
    public string RecommendationText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ExpectedBenefit { get; set; }
    public int? CongestionScore { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool? AcceptedByUser { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? UserFeedback { get; set; }
    public bool IsPending { get; set; }
    public string RiskColourClass { get; set; } = "muted";
}

// ─── Flow Intelligence Summary ────────────────────────────────────────────────

public class FlowIntelligenceSummaryDto
{
    public int ActiveTrips { get; set; }
    public int WaitingTrucks { get; set; }
    public int HighRiskTrips { get; set; }
    public int ActiveDisruptions { get; set; }
    public int PendingRecommendations { get; set; }
    public int RecommendationsToday { get; set; }
    public decimal AverageWaitingMinutes { get; set; }
    public decimal TotalIdlingMinutesToday { get; set; }
    public decimal EstimatedDieselWastedToday { get; set; }
    public decimal EstimatedCo2Today { get; set; }
    public decimal DispatchReliabilityPercent { get; set; }
    public IEnumerable<DispatchTripListDto> HighRiskTripsList { get; set; } = [];
    public IEnumerable<FlowRecommendationDto2> LatestRecommendations { get; set; } = [];
    public IEnumerable<DisruptionListDto> ActiveDisruptionsList { get; set; } = [];
    public PilotComparisonResult? PilotComparison { get; set; }
}

// ─── Emissions Summary ────────────────────────────────────────────────────────

public class EmissionsSummaryDto
{
    public decimal TotalIdlingMinutes { get; set; }
    public decimal TotalDieselLitres { get; set; }
    public decimal TotalFuelCost { get; set; }
    public decimal TotalCo2Kg { get; set; }
    public int AvoidableIdlingCount { get; set; }
    public int TotalEstimatesCount { get; set; }
    public decimal AvoidableDieselLitres { get; set; }
    public decimal AvoidableFuelCost { get; set; }
    public decimal AvoidableCo2Kg { get; set; }
}

public class EmissionTripRowDto
{
    public int TripId { get; set; }
    public string VehicleRegistration { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public decimal IdlingMinutes { get; set; }
    public decimal DieselLitres { get; set; }
    public decimal FuelCost { get; set; }
    public decimal Co2Kg { get; set; }
    public bool AvoidableFlag { get; set; }
    public DateTime TripDate { get; set; }
}
