using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartPort.Application.Interfaces;
using SmartPort.Domain.Entities;
using SmartPort.Domain.Enums;
using SmartPort.Infrastructure.Persistence;

namespace SmartPort.Infrastructure.Services;

// ─── Congestion Risk Service ──────────────────────────────────────────────────

public class CongestionRiskService : ICongestionRiskService
{
    private readonly SmartPortDbContext _db;
    private readonly FlowIntelligenceSettings _settings;

    public CongestionRiskService(SmartPortDbContext db, IOptions<FlowIntelligenceSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
    }

    public async Task<CongestionRiskResult> CalculateRiskAsync(DispatchTrip trip)
    {
        int score = 0;
        var factors = new List<string>();

        // Factor 1: Active disruptions (up to 35 pts)
        var activeDisruptions = await _db.DisruptionEvents
            .Where(d => d.IsActive)
            .ToListAsync();

        var routeDisruption = activeDisruptions.FirstOrDefault(d =>
            !string.IsNullOrEmpty(d.AffectedRoute) &&
            trip.RouteName.Contains(d.AffectedRoute, StringComparison.OrdinalIgnoreCase));

        if (routeDisruption != null)
        {
            var pts = routeDisruption.Severity switch {
                DisruptionSeverity.Critical => 35,
                DisruptionSeverity.High     => 25,
                DisruptionSeverity.Medium   => 15,
                _                          => 8
            };
            score += pts;
            factors.Add($"Active {routeDisruption.DisruptionType} disruption on {routeDisruption.AffectedRoute} ({routeDisruption.Severity} severity, +{pts} pts)");
        }
        else if (activeDisruptions.Any())
        {
            score += 8;
            factors.Add($"{activeDisruptions.Count} active disruption(s) in the network (+8 pts)");
        }

        // Factor 2: Time of day (up to 20 pts)
        var localHour = trip.PlannedDispatchTime.ToLocalTime().Hour;
        if (localHour >= 7 && localHour <= 9)
        {
            score += 20; factors.Add("Morning peak period 07:00–09:00 (+20 pts)");
        }
        else if (localHour >= 16 && localHour <= 18)
        {
            score += 18; factors.Add("Afternoon peak period 16:00–18:00 (+18 pts)");
        }
        else if (localHour >= 12 && localHour <= 14)
        {
            score += 8; factors.Add("Midday congestion period 12:00–14:00 (+8 pts)");
        }
        else if (localHour >= 22 || localHour <= 4)
        {
            score -= 5; factors.Add("Off-peak night window — lower risk (-5 pts)");
        }

        // Factor 3: Cargo urgency (up to 15 pts)
        var urgencyPts = trip.UrgencyLevel switch {
            TripUrgencyLevel.Critical => 15,
            TripUrgencyLevel.High     => 10,
            TripUrgencyLevel.Normal   => 0,
            TripUrgencyLevel.Low      => -5,
            _ => 0
        };
        if (urgencyPts != 0)
        {
            score += urgencyPts;
            factors.Add($"Cargo urgency: {trip.UrgencyLevel} ({(urgencyPts > 0 ? "+" : "")}{urgencyPts} pts)");
        }

        // Factor 4: Cold-chain power risk (up to 10 pts)
        if (trip.CargoType == FleetCargoType.ColdChain)
        {
            var loadSheddingDisruption = activeDisruptions.Any(d => d.DisruptionType == DisruptionType.LoadShedding);
            if (loadSheddingDisruption)
            {
                score += 15;
                factors.Add("Cold-chain cargo + active load-shedding risk (+15 pts)");
            }
            else
            {
                score += 5;
                factors.Add("Cold-chain cargo — temperature sensitivity (+5 pts)");
            }
        }

        // Factor 5: Missed arrival window risk (up to 10 pts)
        var timeToWindow = (trip.PlannedArrivalWindowStart - DateTime.UtcNow).TotalMinutes;
        if (timeToWindow < 30 && timeToWindow > 0)
        {
            score += 10; factors.Add($"Arrival window opens in {timeToWindow:F0} min — tight window (+10 pts)");
        }
        else if (timeToWindow <= 0)
        {
            score += 15; factors.Add("Arrival window has already started — delayed start (+15 pts)");
        }

        // Factor 6: Active trips on same route (up to 10 pts)
        var sameRouteTrips = await _db.DispatchTrips
            .CountAsync(t => t.RouteName == trip.RouteName &&
                             (t.Status == TripStatus.Dispatched || t.Status == TripStatus.Waiting) &&
                             t.Id != trip.Id);
        if (sameRouteTrips >= 3)
        {
            score += 10; factors.Add($"{sameRouteTrips} other trucks active on same route (+10 pts)");
        }
        else if (sameRouteTrips > 0)
        {
            score += 4; factors.Add($"{sameRouteTrips} other truck(s) active on same route (+4 pts)");
        }

        // Clamp
        score = Math.Max(0, Math.Min(100, score));

        var riskLevel = score switch {
            <= 30 => FlowRiskLevel.Low,
            <= 60 => FlowRiskLevel.Medium,
            <= 80 => FlowRiskLevel.High,
            _     => FlowRiskLevel.Critical
        };

        var explanation = riskLevel switch {
            FlowRiskLevel.Low      => "Congestion risk is low. Conditions are favourable for dispatch.",
            FlowRiskLevel.Medium   => "Moderate congestion risk detected. Monitor conditions before dispatching.",
            FlowRiskLevel.High     => "High congestion risk. Consider delaying dispatch or using an alternative window.",
            FlowRiskLevel.Critical => "Critical congestion risk. Dispatch is not recommended at this time.",
            _ => "Risk level could not be determined."
        };

        return new CongestionRiskResult {
            Score       = score,
            RiskLevel   = riskLevel,
            Explanation = explanation,
            Factors     = factors
        };
    }
}

// ─── Dispatch Recommendation Service ─────────────────────────────────────────

public class DispatchRecommendationService : IDispatchRecommendationService
{
    private readonly ICongestionRiskService _riskService;
    private readonly SmartPortDbContext _db;

    public DispatchRecommendationService(ICongestionRiskService riskService, SmartPortDbContext db)
    {
        _riskService = riskService;
        _db = db;
    }

    public async Task<DispatchRecommendationResult> GenerateRecommendationAsync(DispatchTrip trip)
    {
        var risk = await _riskService.CalculateRiskAsync(trip);

        var activeDisruptions = await _db.DisruptionEvents
            .Where(d => d.IsActive)
            .ToListAsync();

        var routeDisruption = activeDisruptions.FirstOrDefault(d =>
            !string.IsNullOrEmpty(d.AffectedRoute) &&
            trip.RouteName.Contains(d.AffectedRoute, StringComparison.OrdinalIgnoreCase));

        var isColdChain   = trip.CargoType == FleetCargoType.ColdChain;
        var isCriticalUrg = trip.UrgencyLevel == TripUrgencyLevel.Critical;
        var hasLoadShed   = activeDisruptions.Any(d => d.DisruptionType == DisruptionType.LoadShedding);
        var hasGateDelay  = activeDisruptions.Any(d => d.DisruptionType == DisruptionType.GateDelay);
        var minutesToWindow = (trip.PlannedArrivalWindowStart - DateTime.UtcNow).TotalMinutes;

        FlowRecommendationType recType;
        string text;
        string reason;
        string? benefit;
        FlowConfidenceLevel confidence;

        if (isColdChain && hasLoadShed && risk.RiskLevel >= FlowRiskLevel.High)
        {
            recType    = FlowRecommendationType.PrioritiseCargo;
            confidence = FlowConfidenceLevel.High;
            text       = "Prioritise cold-chain dispatch immediately. Load-shedding risk and high congestion are active simultaneously.";
            reason     = $"Cold-chain cargo with active load-shedding detected. Congestion score: {risk.Score}/100. Delayed dispatch increases temperature risk.";
            benefit    = "Reduces product loss risk. Prioritised routing reduces total port dwell by an estimated 20–35 minutes.";
        }
        else if (routeDisruption != null && routeDisruption.Severity >= DisruptionSeverity.High)
        {
            recType    = FlowRecommendationType.HoldAtDepot;
            confidence = FlowConfidenceLevel.High;
            text       = $"Hold at depot. A {routeDisruption.Severity.ToString().ToLower()}-severity {routeDisruption.DisruptionType} disruption is active on your route.";
            reason     = $"Active disruption: {routeDisruption.Title}. Affected route: {routeDisruption.AffectedRoute}. Dispatching now will likely result in significant waiting time.";
            benefit    = "Avoids unnecessary idling. Estimated fuel saving: 2–4 litres diesel per hour of avoided waiting.";
        }
        else if (risk.Score >= 81)
        {
            recType    = FlowRecommendationType.DelayDispatch;
            confidence = FlowConfidenceLevel.High;
            text       = "Delay dispatch. Congestion risk is critical. Recommend holding for at least 45–60 minutes.";
            reason     = $"Congestion risk score: {risk.Score}/100 (Critical). Factors: {string.Join(", ", risk.Factors.Take(3))}.";
            benefit    = "Dispatching in the next congestion window could reduce waiting time by 30–60 minutes and save an estimated 1.5–3 litres diesel.";
        }
        else if (risk.Score >= 61)
        {
            if (isCriticalUrg)
            {
                recType    = FlowRecommendationType.ReleaseNow;
                confidence = FlowConfidenceLevel.Medium;
                text       = "Release now despite elevated risk. Cargo urgency overrides the moderate congestion risk.";
                reason     = $"Urgent cargo ({trip.UrgencyLevel}) requires timely dispatch. Congestion score: {risk.Score}/100 (High), but delay cost exceeds congestion cost.";
                benefit    = "Meets critical delivery window. Risk is accepted and managed.";
            }
            else
            {
                recType    = FlowRecommendationType.DelayDispatch;
                confidence = FlowConfidenceLevel.Medium;
                text       = "Consider delaying dispatch by 30–45 minutes. Congestion risk is elevated on this route.";
                reason     = $"Congestion risk score: {risk.Score}/100 (High). Primary factors: {string.Join("; ", risk.Factors.Take(2))}.";
                benefit    = "A brief delay may reduce idling time by 15–30 minutes. Estimated saving: 0.75–1.5 litres diesel.";
            }
        }
        else if (hasGateDelay)
        {
            recType    = FlowRecommendationType.MonitorOnly;
            confidence = FlowConfidenceLevel.Medium;
            text       = "Proceed with caution. Gate delay reported at the terminal — allow additional buffer time.";
            reason     = "Gate delay disruption is active. Low-to-medium congestion risk, but gate processing times may be extended.";
            benefit    = "Awareness of gate delay allows dispatcher to manage client expectations proactively.";
        }
        else if (minutesToWindow < 15 && minutesToWindow > 0)
        {
            recType    = FlowRecommendationType.ReleaseNow;
            confidence = FlowConfidenceLevel.High;
            text       = "Release truck now. Arrival window opens in under 15 minutes and conditions are acceptable.";
            reason     = $"Congestion risk is {risk.RiskLevel} (score {risk.Score}/100). Arrival window: {trip.PlannedArrivalWindowStart:HH:mm}.";
            benefit    = "On-time arrival. Avoids missed window penalty and maintains dispatch reliability score.";
        }
        else
        {
            recType    = FlowRecommendationType.ReleaseNow;
            confidence = FlowConfidenceLevel.High;
            text       = "Release truck now. Congestion risk is low and conditions are favourable for dispatch.";
            reason     = $"Congestion risk score: {risk.Score}/100 ({risk.RiskLevel}). No active disruptions on route. Arrival window timing is acceptable.";
            benefit    = "Optimal dispatch window. Expected smooth corridor access and minimal gate waiting time.";
        }

        return new DispatchRecommendationResult {
            RecommendationType = recType,
            RecommendationText = text,
            Reason             = reason,
            RiskLevel          = risk.RiskLevel,
            ConfidenceLevel    = confidence,
            ExpectedBenefit    = benefit,
            CongestionScore    = risk.Score
        };
    }
}

// ─── Idling & Emission Service ────────────────────────────────────────────────

public class IdlingEmissionService : IIdlingEmissionService
{
    private readonly FlowIntelligenceSettings _settings;
    private readonly SmartPortDbContext _db;

    public IdlingEmissionService(IOptions<FlowIntelligenceSettings> settings, SmartPortDbContext db)
    {
        _settings = settings.Value;
        _db = db;
    }

    public async Task<EmissionEstimateResult> CalculateEstimateAsync(DispatchTrip trip)
    {
        // Base idling estimate from trip status and known waiting patterns
        decimal idlingMinutes = 0;
        string notes;
        bool avoidable = false;

        if (trip.GateInTime.HasValue && trip.ActualDispatchTime.HasValue)
        {
            // Actual waiting = time from dispatch to gate-in (en-route + queue)
            var enRouteToGateMinutes = (decimal)(trip.GateInTime.Value - trip.ActualDispatchTime.Value).TotalMinutes;
            var expectedTransitMinutes = 45m; // configurable default
            idlingMinutes = Math.Max(0, enRouteToGateMinutes - expectedTransitMinutes);
            notes = $"Calculated from actual dispatch ({trip.ActualDispatchTime:HH:mm}) to gate-in ({trip.GateInTime:HH:mm}). Expected transit: {expectedTransitMinutes} min.";
        }
        else
        {
            // Estimate from active disruptions and risk
            var activeDisruptions = await _db.DisruptionEvents.Where(d => d.IsActive).ToListAsync();
            var routeDisruption = activeDisruptions.FirstOrDefault(d =>
                !string.IsNullOrEmpty(d.AffectedRoute) &&
                trip.RouteName.Contains(d.AffectedRoute, StringComparison.OrdinalIgnoreCase));

            idlingMinutes = trip.UrgencyLevel switch {
                TripUrgencyLevel.Critical => 20,
                TripUrgencyLevel.High     => 30,
                TripUrgencyLevel.Normal   => 45,
                TripUrgencyLevel.Low      => 55,
                _ => 40
            };

            if (routeDisruption != null)
            {
                idlingMinutes += routeDisruption.Severity switch {
                    DisruptionSeverity.Critical => 60,
                    DisruptionSeverity.High     => 40,
                    DisruptionSeverity.Medium   => 20,
                    _ => 10
                };
                avoidable = true;
                notes = $"Estimated. Active {routeDisruption.DisruptionType} disruption adds {(routeDisruption.Severity == DisruptionSeverity.Critical ? 60 : 40)} min estimated idling. This is avoidable with optimised dispatch timing.";
            }
            else
            {
                notes = $"Estimated based on route ({trip.RouteName}) and urgency level ({trip.UrgencyLevel}). Actual will vary by conditions.";
            }
        }

        var idlingHours   = idlingMinutes / 60m;
        var dieselLitres  = idlingHours * _settings.IdlingLitresPerHour;
        var fuelCost      = dieselLitres * _settings.DieselPricePerLitre;
        var co2Kg         = dieselLitres * _settings.Co2KgPerLitreDiesel;

        return new EmissionEstimateResult {
            EstimatedIdlingMinutes = Math.Round(idlingMinutes, 1),
            EstimatedDieselLitres  = Math.Round(dieselLitres, 2),
            EstimatedFuelCost      = Math.Round(fuelCost, 2),
            EstimatedCo2Kg         = Math.Round(co2Kg, 2),
            AvoidableIdlingFlag    = avoidable,
            Notes = notes
        };
    }
}

// ─── Pilot Metrics Service ────────────────────────────────────────────────────

public class PilotMetricsService : IPilotMetricsService
{
    private readonly SmartPortDbContext _db;

    public PilotMetricsService(SmartPortDbContext db) => _db = db;

    public async Task<PilotMetricSnapshot?> GetBaselineMetricsAsync() =>
        await _db.PilotMetricSnapshots
            .Where(p => p.MetricType == PilotMetricType.Baseline)
            .OrderByDescending(p => p.SnapshotDate)
            .FirstOrDefaultAsync();

    public async Task<PilotMetricResult> GetCurrentMetricsAsync()
    {
        var trips     = await _db.DispatchTrips.Include(t => t.EmissionEstimate).ToListAsync();
        var recs      = await _db.FlowRecommendations.ToListAsync();
        var emissions = await _db.IdlingEmissionEstimates.ToListAsync();
        var disrupt   = await _db.DisruptionEvents.CountAsync(d => d.IsActive);

        var total       = trips.Count;
        var missed      = trips.Count(t => t.IsArrivalWindowMissed);
        var completed   = trips.Count(t => t.Status == TripStatus.Completed);
        var highRisk    = recs.Count(r => r.RiskLevel >= FlowRiskLevel.High);
        var reliability = completed > 0 && total > 0
            ? Math.Round((decimal)(completed - missed) / Math.Max(1, completed) * 100, 1)
            : 85m;

        var dwellTrips = trips.Where(t => t.PortDwellMinutes.HasValue).ToList();
        var avgWaiting = dwellTrips.Any()
            ? Math.Round(dwellTrips.Average(t => t.PortDwellMinutes!.Value), 1)
            : 68m;

        return new PilotMetricResult {
            AverageWaitingMinutes     = avgWaiting,
            TotalIdlingMinutes        = emissions.Sum(e => e.EstimatedIdlingMinutes),
            EstimatedDieselLitres     = emissions.Sum(e => e.EstimatedDieselLitres),
            EstimatedFuelCost         = emissions.Sum(e => e.EstimatedFuelCost),
            EstimatedCo2Kg            = emissions.Sum(e => e.EstimatedCo2Kg),
            MissedArrivalWindows      = missed,
            DispatchReliabilityPercent = reliability,
            RecommendationsGenerated  = recs.Count,
            HighRiskTrips             = highRisk,
            TotalTrips                = total,
            ActiveDisruptions         = disrupt
        };
    }

    public async Task<PilotComparisonResult> GetPilotComparisonAsync()
    {
        var baseline = await GetBaselineMetricsAsync();
        var current  = await GetCurrentMetricsAsync();

        var result = new PilotComparisonResult { Baseline = baseline, Current = current };

        if (baseline != null)
        {
            if (baseline.AverageWaitingMinutes > 0)
                result.WaitingMinutesChangePercent = Math.Round(
                    (current.AverageWaitingMinutes - baseline.AverageWaitingMinutes) / baseline.AverageWaitingMinutes * 100, 1);
            if (baseline.EstimatedFuelCost > 0)
                result.FuelCostChangePercent = Math.Round(
                    (current.EstimatedFuelCost - baseline.EstimatedFuelCost) / baseline.EstimatedFuelCost * 100, 1);
            if (baseline.EstimatedCo2Kg > 0)
                result.Co2ChangePercent = Math.Round(
                    (current.EstimatedCo2Kg - baseline.EstimatedCo2Kg) / baseline.EstimatedCo2Kg * 100, 1);
            result.ReliabilityChangePoints = Math.Round(
                current.DispatchReliabilityPercent - baseline.DispatchReliabilityPercent, 1);
        }

        result.PeriodNote = $"Pilot period data as at {DateTime.UtcNow:dd MMM yyyy HH:mm} SAST";
        return result;
    }

    public async Task GeneratePilotSnapshotAsync(string periodLabel, int? organisationId = null)
    {
        var current = await GetCurrentMetricsAsync();
        var snapshot = new PilotMetricSnapshot {
            OrganisationId                 = organisationId,
            SnapshotDate                   = DateTime.UtcNow,
            PeriodLabel                    = periodLabel,
            MetricType                     = PilotMetricType.Current,
            AverageWaitingMinutes          = current.AverageWaitingMinutes,
            TotalIdlingMinutes             = current.TotalIdlingMinutes,
            EstimatedDieselLitres          = current.EstimatedDieselLitres,
            EstimatedFuelCost              = current.EstimatedFuelCost,
            EstimatedCo2Kg                 = current.EstimatedCo2Kg,
            MissedArrivalWindows           = current.MissedArrivalWindows,
            DispatchReliabilityPercent     = current.DispatchReliabilityPercent,
            RecommendationsGenerated       = current.RecommendationsGenerated,
            HighRiskTrips                  = current.HighRiskTrips,
            CreatedBy = "System"
        };
        _db.PilotMetricSnapshots.Add(snapshot);
        await _db.SaveChangesAsync();
    }
}

// ─── Organisation Service ─────────────────────────────────────────────────────

public class OrganisationService : IOrganisationService
{
    private readonly SmartPortDbContext _db;
    public OrganisationService(SmartPortDbContext db) => _db = db;

    public async Task<IEnumerable<OrganisationListDto>> GetAllAsync()
    {
        var orgs = await _db.Organisations.Where(o => !o.IsDeleted)
            .Select(o => new OrganisationListDto {
                Id = o.Id, Name = o.Name, OrganisationType = o.OrganisationType,
                ContactPerson = o.ContactPerson, ContactEmail = o.ContactEmail,
                Province = o.Province, IsActive = o.IsActive,
                VehicleCount = o.FleetVehicles.Count(v => v.IsActive)
            }).ToListAsync();
        return orgs;
    }

    public async Task<OrganisationDetailDto?> GetByIdAsync(int id)
    {
        var o = await _db.Organisations.Include(x => x.FleetVehicles)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (o == null) return null;
        return new OrganisationDetailDto {
            Id = o.Id, Name = o.Name, OrganisationType = o.OrganisationType,
            RegistrationNumber = o.RegistrationNumber,
            ContactPerson = o.ContactPerson, ContactEmail = o.ContactEmail,
            ContactPhone = o.ContactPhone, Address = o.Address, Province = o.Province,
            IsActive = o.IsActive, CreatedAt = o.CreatedAt,
            Vehicles = o.FleetVehicles.Select(v => new FleetVehicleListDto {
                Id = v.Id, OrganisationId = v.OrganisationId, OrganisationName = o.Name,
                RegistrationNumber = v.RegistrationNumber, FleetNumber = v.FleetNumber,
                VehicleType = v.VehicleType, CargoType = v.CargoType, Status = v.Status,
                CurrentLocation = v.CurrentLocation, CapacityTons = v.CapacityTons
            })
        };
    }

    public async Task<int> CreateAsync(SaveOrganisationDto dto, string userId)
    {
        var org = new Organisation {
            Name = dto.Name, OrganisationType = dto.OrganisationType,
            RegistrationNumber = dto.RegistrationNumber, ContactPerson = dto.ContactPerson,
            ContactEmail = dto.ContactEmail, ContactPhone = dto.ContactPhone,
            Address = dto.Address, Province = dto.Province, IsActive = true, CreatedBy = userId
        };
        _db.Organisations.Add(org);
        await _db.SaveChangesAsync();
        return org.Id;
    }

    public async Task UpdateAsync(int id, SaveOrganisationDto dto, string userId)
    {
        var org = await _db.Organisations.FindAsync(id) ?? throw new KeyNotFoundException();
        org.Name = dto.Name; org.OrganisationType = dto.OrganisationType;
        org.RegistrationNumber = dto.RegistrationNumber; org.ContactPerson = dto.ContactPerson;
        org.ContactEmail = dto.ContactEmail; org.ContactPhone = dto.ContactPhone;
        org.Address = dto.Address; org.Province = dto.Province; org.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id, bool isActive, string userId)
    {
        var org = await _db.Organisations.FindAsync(id) ?? throw new KeyNotFoundException();
        org.IsActive = isActive; org.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }
}

// ─── Fleet Vehicle Service ────────────────────────────────────────────────────

public class FleetVehicleService : IFleetVehicleService
{
    private readonly SmartPortDbContext _db;
    public FleetVehicleService(SmartPortDbContext db) => _db = db;

    public async Task<PagedResult<FleetVehicleListDto>> GetVehiclesAsync(FleetVehicleFilterDto filter)
    {
        var query = _db.FleetVehicles.Include(v => v.Organisation)
            .Where(v => !v.IsDeleted && v.IsActive);
        if (filter.OrganisationId.HasValue) query = query.Where(v => v.OrganisationId == filter.OrganisationId.Value);
        if (filter.Status.HasValue) query = query.Where(v => v.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var t = filter.SearchTerm.ToLower();
            query = query.Where(v => v.RegistrationNumber.ToLower().Contains(t) ||
                                     (v.FleetNumber != null && v.FleetNumber.ToLower().Contains(t)));
        }
        var total = await query.CountAsync();
        var items = await query.OrderBy(v => v.RegistrationNumber)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(v => new FleetVehicleListDto {
                Id = v.Id, OrganisationId = v.OrganisationId,
                OrganisationName = v.Organisation.Name,
                RegistrationNumber = v.RegistrationNumber, FleetNumber = v.FleetNumber,
                VehicleType = v.VehicleType, CargoType = v.CargoType, Status = v.Status,
                CurrentLocation = v.CurrentLocation, CapacityTons = v.CapacityTons
            }).ToListAsync();
        return new PagedResult<FleetVehicleListDto> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<FleetVehicleDetailDto?> GetByIdAsync(int id)
    {
        var v = await _db.FleetVehicles.Include(x => x.Organisation)
            .Include(x => x.DispatchTrips)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (v == null) return null;
        return new FleetVehicleDetailDto {
            Id = v.Id, OrganisationId = v.OrganisationId, OrganisationName = v.Organisation.Name,
            RegistrationNumber = v.RegistrationNumber, FleetNumber = v.FleetNumber,
            VehicleType = v.VehicleType, CargoType = v.CargoType, Status = v.Status,
            CurrentLocation = v.CurrentLocation, CapacityTons = v.CapacityTons, IsActive = v.IsActive,
            CreatedAt = v.CreatedAt,
            RecentTrips = v.DispatchTrips.OrderByDescending(t => t.PlannedDispatchTime).Take(5).Select(t => new DispatchTripListDto {
                Id = t.Id, RouteName = t.RouteName, Origin = t.Origin, Destination = t.Destination,
                Status = t.Status, PlannedDispatchTime = t.PlannedDispatchTime,
                UrgencyLevel = t.UrgencyLevel, CargoType = t.CargoType,
                PlannedArrivalWindowEnd = t.PlannedArrivalWindowEnd
            })
        };
    }

    public async Task<int> CreateAsync(SaveFleetVehicleDto dto, string userId)
    {
        var v = new FleetVehicle {
            OrganisationId = dto.OrganisationId, RegistrationNumber = dto.RegistrationNumber,
            FleetNumber = dto.FleetNumber, VehicleType = dto.VehicleType, CargoType = dto.CargoType,
            CapacityTons = dto.CapacityTons, Status = dto.Status, CurrentLocation = dto.CurrentLocation,
            IsActive = true, CreatedBy = userId
        };
        _db.FleetVehicles.Add(v);
        await _db.SaveChangesAsync();
        return v.Id;
    }

    public async Task UpdateAsync(int id, SaveFleetVehicleDto dto, string userId)
    {
        var v = await _db.FleetVehicles.FindAsync(id) ?? throw new KeyNotFoundException();
        v.OrganisationId = dto.OrganisationId; v.RegistrationNumber = dto.RegistrationNumber;
        v.FleetNumber = dto.FleetNumber; v.VehicleType = dto.VehicleType; v.CargoType = dto.CargoType;
        v.CapacityTons = dto.CapacityTons; v.Status = dto.Status; v.CurrentLocation = dto.CurrentLocation;
        v.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }
}

// ─── Dispatch Trip Service ────────────────────────────────────────────────────

public class DispatchTripService : IDispatchTripService
{
    private readonly SmartPortDbContext _db;
    public DispatchTripService(SmartPortDbContext db) => _db = db;

    public async Task<PagedResult<DispatchTripListDto>> GetTripsAsync(DispatchTripFilterDto filter)
    {
        var query = _db.DispatchTrips
            .Include(t => t.Organisation).Include(t => t.FleetVehicle).Include(t => t.Driver)
            .Include(t => t.FlowRecommendations)
            .Where(t => !t.IsDeleted);
        if (filter.OrganisationId.HasValue) query = query.Where(t => t.OrganisationId == filter.OrganisationId.Value);
        if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var s = filter.SearchTerm.ToLower();
            query = query.Where(t => t.RouteName.ToLower().Contains(s) || t.FleetVehicle.RegistrationNumber.ToLower().Contains(s));
        }
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.PlannedDispatchTime)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .ToListAsync();

        var dtos = items.Select(t => {
            var latestRec = t.FlowRecommendations.OrderByDescending(r => r.GeneratedAt).FirstOrDefault();
            return new DispatchTripListDto {
                Id = t.Id, OrganisationId = t.OrganisationId, OrganisationName = t.Organisation.Name,
                VehicleRegistration = t.FleetVehicle.RegistrationNumber, DriverName = t.Driver?.FullName,
                RouteName = t.RouteName, Origin = t.Origin, Destination = t.Destination,
                CargoType = t.CargoType, UrgencyLevel = t.UrgencyLevel, Status = t.Status,
                PlannedDispatchTime = t.PlannedDispatchTime, PlannedArrivalWindowEnd = t.PlannedArrivalWindowEnd,
                ActualArrivalTime = t.ActualArrivalTime, IsArrivalWindowMissed = t.IsArrivalWindowMissed,
                LatestRiskScore = latestRec?.CongestionScore, LatestRiskLevel = latestRec?.RiskLevel
            };
        });
        return new PagedResult<DispatchTripListDto> { Items = dtos, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<DispatchTripDetailDto?> GetByIdAsync(int id)
    {
        var t = await _db.DispatchTrips
            .Include(x => x.Organisation).Include(x => x.FleetVehicle).Include(x => x.Driver)
            .Include(x => x.FlowRecommendations).ThenInclude(r => r.Organisation)
            .Include(x => x.EmissionEstimate)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t == null) return null;

        var latestRec = t.FlowRecommendations.OrderByDescending(r => r.GeneratedAt).FirstOrDefault();
        return new DispatchTripDetailDto {
            Id = t.Id, OrganisationId = t.OrganisationId, OrganisationName = t.Organisation.Name,
            VehicleRegistration = t.FleetVehicle.RegistrationNumber, DriverName = t.Driver?.FullName,
            RouteName = t.RouteName, Origin = t.Origin, Destination = t.Destination,
            CargoType = t.CargoType, UrgencyLevel = t.UrgencyLevel, Status = t.Status,
            PlannedDispatchTime = t.PlannedDispatchTime,
            PlannedArrivalWindowStart = t.PlannedArrivalWindowStart,
            PlannedArrivalWindowEnd = t.PlannedArrivalWindowEnd,
            ActualDispatchTime = t.ActualDispatchTime, ActualArrivalTime = t.ActualArrivalTime,
            GateInTime = t.GateInTime, GateOutTime = t.GateOutTime, PortDwellMinutes = t.PortDwellMinutes,
            CargoDescription = t.CargoDescription, Notes = t.Notes,
            IsArrivalWindowMissed = t.IsArrivalWindowMissed,
            LatestRiskScore = latestRec?.CongestionScore, LatestRiskLevel = latestRec?.RiskLevel,
            EmissionEstimate = t.EmissionEstimate == null ? null : new EmissionEstimateResult {
                EstimatedIdlingMinutes = t.EmissionEstimate.EstimatedIdlingMinutes,
                EstimatedDieselLitres  = t.EmissionEstimate.EstimatedDieselLitres,
                EstimatedFuelCost      = t.EmissionEstimate.EstimatedFuelCost,
                EstimatedCo2Kg         = t.EmissionEstimate.EstimatedCo2Kg,
                AvoidableIdlingFlag    = t.EmissionEstimate.AvoidableIdlingFlag,
                Notes = t.EmissionEstimate.CalculationNotes ?? ""
            },
            Recommendations = t.FlowRecommendations.OrderByDescending(r => r.GeneratedAt).Select(r => MapRec(r, t))
        };
    }

    public async Task<IEnumerable<DispatchTripListDto>> GetHighRiskTripsAsync(int topN = 10)
    {
        var result = await GetTripsAsync(new DispatchTripFilterDto { PageSize = 100 });
        return result.Items
            .Where(t => t.LatestRiskLevel >= FlowRiskLevel.High)
            .OrderByDescending(t => t.LatestRiskScore)
            .Take(topN);
    }

    public async Task<IEnumerable<DispatchTripListDto>> GetActiveTripsAsync()
    {
        var result = await GetTripsAsync(new DispatchTripFilterDto { PageSize = 200 });
        return result.Items.Where(t => t.Status == TripStatus.Dispatched ||
            t.Status == TripStatus.Waiting || t.Status == TripStatus.AtGate ||
            t.Status == TripStatus.ReadyForDispatch);
    }

    public async Task<int> CreateAsync(SaveDispatchTripDto dto, string userId)
    {
        var trip = new DispatchTrip {
            OrganisationId = dto.OrganisationId, FleetVehicleId = dto.FleetVehicleId,
            DriverId = dto.DriverId, Origin = dto.Origin, Destination = dto.Destination,
            RouteName = dto.RouteName, CargoType = dto.CargoType,
            CargoDescription = dto.CargoDescription, UrgencyLevel = dto.UrgencyLevel,
            PlannedDispatchTime = dto.PlannedDispatchTime,
            PlannedArrivalWindowStart = dto.PlannedArrivalWindowStart,
            PlannedArrivalWindowEnd = dto.PlannedArrivalWindowEnd,
            Notes = dto.Notes, Status = TripStatus.Planned, CreatedBy = userId
        };
        _db.DispatchTrips.Add(trip);
        await _db.SaveChangesAsync();
        return trip.Id;
    }

    public async Task UpdateAsync(int id, SaveDispatchTripDto dto, string userId)
    {
        var trip = await _db.DispatchTrips.FindAsync(id) ?? throw new KeyNotFoundException();
        trip.OrganisationId = dto.OrganisationId; trip.FleetVehicleId = dto.FleetVehicleId;
        trip.DriverId = dto.DriverId; trip.Origin = dto.Origin; trip.Destination = dto.Destination;
        trip.RouteName = dto.RouteName; trip.CargoType = dto.CargoType;
        trip.CargoDescription = dto.CargoDescription; trip.UrgencyLevel = dto.UrgencyLevel;
        trip.PlannedDispatchTime = dto.PlannedDispatchTime;
        trip.PlannedArrivalWindowStart = dto.PlannedArrivalWindowStart;
        trip.PlannedArrivalWindowEnd = dto.PlannedArrivalWindowEnd;
        trip.Notes = dto.Notes; trip.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, TripStatus status, string userId)
    {
        var trip = await _db.DispatchTrips.FindAsync(id) ?? throw new KeyNotFoundException();
        trip.Status = status; trip.UpdatedBy = userId;
        if (status == TripStatus.Dispatched) trip.ActualDispatchTime = DateTime.UtcNow;
        if (status == TripStatus.AtGate)     trip.GateInTime = DateTime.UtcNow;
        if (status == TripStatus.Completed)  trip.ActualArrivalTime ??= DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static FlowRecommendationDto2 MapRec(FlowRecommendation r, DispatchTrip t) => new() {
        Id = r.Id, DispatchTripId = r.DispatchTripId, OrganisationId = r.OrganisationId,
        OrganisationName = t.Organisation?.Name ?? "",
        TripRoute = t.RouteName, VehicleRegistration = t.FleetVehicle?.RegistrationNumber,
        RecommendationType = r.RecommendationType, RiskLevel = r.RiskLevel,
        ConfidenceLevel = r.ConfidenceLevel, RecommendationText = r.RecommendationText,
        Reason = r.Reason, ExpectedBenefit = r.ExpectedBenefit, CongestionScore = r.CongestionScore,
        GeneratedAt = r.GeneratedAt, AcceptedByUser = r.AcceptedByUser,
        AcceptedAt = r.AcceptedAt, UserFeedback = r.UserFeedback, IsPending = r.IsPending,
        RiskColourClass = r.RiskColourClass
    };
}

// ─── Disruption Service ───────────────────────────────────────────────────────

public class DisruptionService : IDisruptionService
{
    private readonly SmartPortDbContext _db;
    public DisruptionService(SmartPortDbContext db) => _db = db;

    public async Task<IEnumerable<DisruptionListDto>> GetActiveAsync() =>
        await _db.DisruptionEvents.Where(d => d.IsActive && !d.IsDeleted)
            .OrderByDescending(d => d.Severity).ThenByDescending(d => d.StartTime)
            .Select(d => MapDto(d)).ToListAsync();

    public async Task<IEnumerable<DisruptionListDto>> GetAllAsync(int take = 50) =>
        await _db.DisruptionEvents.Where(d => !d.IsDeleted)
            .OrderByDescending(d => d.StartTime).Take(take)
            .Select(d => MapDto(d)).ToListAsync();

    public async Task<int> GetActiveCountAsync() =>
        await _db.DisruptionEvents.CountAsync(d => d.IsActive && !d.IsDeleted);

    public async Task<int> CreateAsync(SaveDisruptionDto dto, string userId)
    {
        var e = new DisruptionEvent {
            DisruptionType = dto.DisruptionType, Severity = dto.Severity,
            Title = dto.Title, Description = dto.Description,
            AffectedLocation = dto.AffectedLocation, AffectedRoute = dto.AffectedRoute,
            StartTime = dto.StartTime, EndTime = dto.EndTime,
            IsActive = true, CreatedBy = userId, CreatedAt = DateTime.UtcNow
        };
        _db.DisruptionEvents.Add(e);
        await _db.SaveChangesAsync();
        return e.Id;
    }

    public async Task ResolveAsync(int id, string userId)
    {
        var e = await _db.DisruptionEvents.FindAsync(id) ?? throw new KeyNotFoundException();
        e.IsActive = false; e.EndTime = DateTime.UtcNow; e.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }

    private static DisruptionListDto MapDto(DisruptionEvent d) => new() {
        Id = d.Id, DisruptionType = d.DisruptionType, Severity = d.Severity,
        Title = d.Title, Description = d.Description, AffectedLocation = d.AffectedLocation,
        AffectedRoute = d.AffectedRoute, StartTime = d.StartTime, EndTime = d.EndTime,
        IsActive = d.IsActive, CreatedBy = d.CreatedBy
    };
}

// ─── Flow Intelligence Summary Service ───────────────────────────────────────

public class FlowIntelligenceService : IFlowIntelligenceService
{
    private readonly SmartPortDbContext _db;
    private readonly IPilotMetricsService _pilot;
    private readonly IDispatchTripService _trips;
    private readonly IDisruptionService _disruptions;

    public FlowIntelligenceService(SmartPortDbContext db, IPilotMetricsService pilot,
        IDispatchTripService trips, IDisruptionService disruptions)
    {
        _db = db; _pilot = pilot; _trips = trips; _disruptions = disruptions;
    }

    public async Task<FlowIntelligenceSummaryDto> GetSummaryAsync()
    {
        var current    = await _pilot.GetCurrentMetricsAsync();
        var highRisk   = await _trips.GetHighRiskTripsAsync(5);
        var active     = await _trips.GetActiveTripsAsync();
        var disrupts   = await _disruptions.GetActiveAsync();
        var latestRecs = await GetActiveRecommendationsAsync(5);
        var comparison = await _pilot.GetPilotComparisonAsync();
        var today      = DateTime.UtcNow.Date;
        var todayEmissions = await _db.IdlingEmissionEstimates
            .Where(e => e.CreatedAt >= today).ToListAsync();

        return new FlowIntelligenceSummaryDto {
            ActiveTrips                = active.Count(),
            WaitingTrucks             = active.Count(t => t.Status == TripStatus.Waiting),
            HighRiskTrips             = highRisk.Count(),
            ActiveDisruptions         = disrupts.Count(),
            PendingRecommendations    = await _db.FlowRecommendations.CountAsync(r => r.AcceptedByUser == null && !r.IsDeleted),
            RecommendationsToday      = await GetTodayRecommendationCountAsync(),
            AverageWaitingMinutes     = current.AverageWaitingMinutes,
            TotalIdlingMinutesToday   = todayEmissions.Sum(e => e.EstimatedIdlingMinutes),
            EstimatedDieselWastedToday = todayEmissions.Sum(e => e.EstimatedDieselLitres),
            EstimatedCo2Today         = todayEmissions.Sum(e => e.EstimatedCo2Kg),
            DispatchReliabilityPercent = current.DispatchReliabilityPercent,
            HighRiskTripsList         = highRisk,
            LatestRecommendations     = latestRecs,
            ActiveDisruptionsList     = disrupts,
            PilotComparison           = comparison
        };
    }

    public async Task<IEnumerable<FlowRecommendationDto2>> GetActiveRecommendationsAsync(int take = 20)
    {
        var recs = await _db.FlowRecommendations
            .Include(r => r.Organisation)
            .Include(r => r.DispatchTrip).ThenInclude(t => t!.FleetVehicle)
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.GeneratedAt).Take(take)
            .ToListAsync();

        return recs.Select(r => new FlowRecommendationDto2 {
            Id = r.Id, DispatchTripId = r.DispatchTripId, OrganisationId = r.OrganisationId,
            OrganisationName = r.Organisation?.Name ?? "",
            TripRoute = r.DispatchTrip?.RouteName,
            VehicleRegistration = r.DispatchTrip?.FleetVehicle?.RegistrationNumber,
            RecommendationType = r.RecommendationType, RiskLevel = r.RiskLevel,
            ConfidenceLevel = r.ConfidenceLevel, RecommendationText = r.RecommendationText,
            Reason = r.Reason, ExpectedBenefit = r.ExpectedBenefit, CongestionScore = r.CongestionScore,
            GeneratedAt = r.GeneratedAt, AcceptedByUser = r.AcceptedByUser,
            AcceptedAt = r.AcceptedAt, UserFeedback = r.UserFeedback,
            IsPending = r.IsPending, RiskColourClass = r.RiskColourClass
        });
    }

    public async Task<FlowRecommendationDto2?> GetRecommendationByIdAsync(int id)
    {
        var recs = await GetActiveRecommendationsAsync(1000);
        return recs.FirstOrDefault(r => r.Id == id);
    }

    public async Task<int> GetTodayRecommendationCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _db.FlowRecommendations.CountAsync(r => r.GeneratedAt >= today && !r.IsDeleted);
    }

    public async Task AcceptRecommendationAsync(int id, string feedback, string userId)
    {
        var r = await _db.FlowRecommendations.FindAsync(id) ?? throw new KeyNotFoundException();
        r.AcceptedByUser = true; r.AcceptedAt = DateTime.UtcNow;
        r.UserFeedback = feedback; r.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }

    public async Task DismissRecommendationAsync(int id, string feedback, string userId)
    {
        var r = await _db.FlowRecommendations.FindAsync(id) ?? throw new KeyNotFoundException();
        r.AcceptedByUser = false; r.AcceptedAt = DateTime.UtcNow;
        r.UserFeedback = feedback; r.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }
}

// ─── Emissions Summary Service ────────────────────────────────────────────────

public class EmissionsSummaryService : IEmissionsSummaryService
{
    private readonly SmartPortDbContext _db;
    public EmissionsSummaryService(SmartPortDbContext db) => _db = db;

    public async Task<EmissionsSummaryDto> GetSummaryAsync()
    {
        var all = await _db.IdlingEmissionEstimates.Where(e => !e.IsDeleted).ToListAsync();
        return new EmissionsSummaryDto {
            TotalIdlingMinutes   = all.Sum(e => e.EstimatedIdlingMinutes),
            TotalDieselLitres    = all.Sum(e => e.EstimatedDieselLitres),
            TotalFuelCost        = all.Sum(e => e.EstimatedFuelCost),
            TotalCo2Kg           = all.Sum(e => e.EstimatedCo2Kg),
            AvoidableIdlingCount = all.Count(e => e.AvoidableIdlingFlag),
            TotalEstimatesCount  = all.Count,
            AvoidableDieselLitres = all.Where(e => e.AvoidableIdlingFlag).Sum(e => e.EstimatedDieselLitres),
            AvoidableFuelCost    = all.Where(e => e.AvoidableIdlingFlag).Sum(e => e.EstimatedFuelCost),
            AvoidableCo2Kg       = all.Where(e => e.AvoidableIdlingFlag).Sum(e => e.EstimatedCo2Kg)
        };
    }

    public async Task<IEnumerable<EmissionTripRowDto>> GetTopIdlingTripsAsync(int topN = 20)
    {
        return await _db.IdlingEmissionEstimates
            .Include(e => e.DispatchTrip).ThenInclude(t => t.FleetVehicle)
            .Include(e => e.DispatchTrip).ThenInclude(t => t.Organisation)
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.EstimatedIdlingMinutes)
            .Take(topN)
            .Select(e => new EmissionTripRowDto {
                TripId = e.DispatchTripId,
                VehicleRegistration = e.DispatchTrip.FleetVehicle.RegistrationNumber,
                OrganisationName = e.DispatchTrip.Organisation.Name,
                RouteName = e.DispatchTrip.RouteName,
                IdlingMinutes = e.EstimatedIdlingMinutes,
                DieselLitres  = e.EstimatedDieselLitres,
                FuelCost      = e.EstimatedFuelCost,
                Co2Kg         = e.EstimatedCo2Kg,
                AvoidableFlag = e.AvoidableIdlingFlag,
                TripDate      = e.DispatchTrip.PlannedDispatchTime
            }).ToListAsync();
    }
}
