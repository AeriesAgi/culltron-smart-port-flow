using Microsoft.EntityFrameworkCore;
using SmartPort.Domain.Enums;
using SmartPort.Infrastructure.Persistence;

namespace SmartPort.Infrastructure.Services;

public interface ITruckTrackingService
{
    Task<TruckTrackingDashboardDto> GetDashboardAsync();
}

public class TruckTrackingDashboardDto
{
    public int ActiveTrucks { get; set; }
    public int QueueRiskCount { get; set; }
    public int DelayedCount { get; set; }
    public int HoldOutsidePortCount { get; set; }
    public int PriorityReleaseCount { get; set; }
    public decimal AverageEtaMinutes { get; set; }
    public decimal TotalIdlingMinutes { get; set; }
    public decimal EstimatedCo2Kg { get; set; }
    public int GatePressureScore { get; set; }
    public int AiConfidenceScore { get; set; } = 92;
    public List<TruckEtaDto> Trucks { get; set; } = new();
}

public class TruckEtaDto
{
    public string FleetIdentifier { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string RouteCorridor { get; set; } = string.Empty;
    public string CurrentCheckpoint { get; set; } = string.Empty;
    public int EtaMinutesToGate { get; set; }
    public string QueueStatus { get; set; } = string.Empty;
    public decimal IdlingMinutes { get; set; }
    public decimal EstimatedCo2Kg { get; set; }
    public int DelayRiskScore { get; set; }
    public string Status { get; set; } = "On Time";
    public string StatusTone { get; set; } = "success";
    public string RecommendedAction { get; set; } = string.Empty;
    public List<string> Timeline { get; set; } = new();
}

public class TruckTrackingService : ITruckTrackingService
{
    private readonly SmartPortDbContext _db;

    public TruckTrackingService(SmartPortDbContext db) => _db = db;

    public async Task<TruckTrackingDashboardDto> GetDashboardAsync()
    {
        var trips = await _db.DispatchTrips
            .Include(t => t.Organisation)
            .Include(t => t.FleetVehicle)
            .Where(t => !t.IsDeleted && t.Status != TripStatus.Completed && t.Status != TripStatus.Cancelled)
            .OrderBy(t => t.PlannedDispatchTime)
            .Take(8)
            .ToListAsync();

        var gateQueue = await _db.Gates.Where(g => !g.IsDeleted).SumAsync(g => g.CurrentQueueCount);
        var baseEta = Math.Clamp(gateQueue * 3 + 12, 18, 85);
        var trucks = trips.Select((trip, index) => MapTrip(trip, index, baseEta)).ToList();

        if (trucks.Count == 0)
        {
            trucks = BuildFallbackTrucks(baseEta);
        }

        return new TruckTrackingDashboardDto
        {
            ActiveTrucks = trucks.Count,
            QueueRiskCount = trucks.Count(t => t.Status is "Queue Risk" or "Delayed"),
            DelayedCount = trucks.Count(t => t.Status == "Delayed"),
            HoldOutsidePortCount = trucks.Count(t => t.Status == "Hold Outside Port"),
            PriorityReleaseCount = trucks.Count(t => t.Status == "Priority Release"),
            AverageEtaMinutes = Math.Round((decimal)trucks.Average(t => t.EtaMinutesToGate), 0),
            TotalIdlingMinutes = Math.Round(trucks.Sum(t => t.IdlingMinutes), 0),
            EstimatedCo2Kg = Math.Round(trucks.Sum(t => t.EstimatedCo2Kg), 1),
            GatePressureScore = Math.Min(100, gateQueue * 4 + trucks.Count(t => t.DelayRiskScore > 70) * 8 + 22),
            Trucks = trucks
        };
    }

    private static TruckEtaDto MapTrip(Domain.Entities.DispatchTrip trip, int index, int baseEta)
    {
        var minutesLate = (DateTime.UtcNow - trip.PlannedDispatchTime).TotalMinutes;
        var eta = Math.Clamp(baseEta + index * 7 + (trip.UrgencyLevel == TripUrgencyLevel.Critical ? -10 : 0), 8, 120);
        var idling = Math.Max(0, eta - 18 + index * 2);
        var risk = Math.Clamp((int)(eta * 0.7m + idling * 0.9m + (minutesLate > 0 ? 18 : 0)), 10, 98);
        var status = ResolveStatus(trip, risk, eta, index);
        var checkpoint = Checkpoint(index, status);

        return new TruckEtaDto
        {
            FleetIdentifier = trip.FleetVehicle?.RegistrationNumber ?? $"TRK-{trip.Id:000}",
            OrganisationName = trip.Organisation?.Name ?? "Demo logistics partner",
            RouteCorridor = string.IsNullOrWhiteSpace(trip.RouteName) ? $"{trip.Origin} → {trip.Destination}" : trip.RouteName,
            CurrentCheckpoint = checkpoint,
            EtaMinutesToGate = eta,
            QueueStatus = risk >= 78 ? "Gate queue saturated" : risk >= 58 ? "Queue building" : "Flowing",
            IdlingMinutes = Math.Round(idling, 0),
            EstimatedCo2Kg = Math.Round(idling / 60m * 3.0m * 2.68m, 1),
            DelayRiskScore = risk,
            Status = status,
            StatusTone = Tone(status),
            RecommendedAction = Action(status),
            Timeline = Timeline(checkpoint)
        };
    }

    private static string ResolveStatus(Domain.Entities.DispatchTrip trip, int risk, int eta, int index)
    {
        if (trip.UrgencyLevel == TripUrgencyLevel.Critical && risk < 86) return "Priority Release";
        if (risk >= 86) return "Hold Outside Port";
        if (risk >= 72) return "Delayed";
        if (risk >= 55 || eta > 45) return "Queue Risk";
        return index % 5 == 0 ? "Priority Release" : "On Time";
    }

    private static string Tone(string status) => status switch
    {
        "On Time" => "success",
        "Queue Risk" => "warning",
        "Delayed" => "danger",
        "Hold Outside Port" => "high",
        "Priority Release" => "info",
        _ => "muted"
    };

    private static string Action(string status) => status switch
    {
        "Priority Release" => "Pre-clear documents and route to priority gate lane.",
        "Hold Outside Port" => "Hold at staging checkpoint until gate queue drops below threshold.",
        "Delayed" => "Notify dispatcher, shift slot by 20 minutes and keep driver outside port perimeter.",
        "Queue Risk" => "Meter arrival, confirm gate slot and avoid joining the live queue too early.",
        _ => "Maintain ETA and monitor gate pressure."
    };

    private static string Checkpoint(int index, string status) => status switch
    {
        "Hold Outside Port" => "Outer staging zone",
        "Delayed" => index % 2 == 0 ? "N2 corridor checkpoint" : "Bayhead approach",
        "Priority Release" => "Pre-cleared priority lane",
        "Queue Risk" => "Gate approach geofence",
        _ => index % 2 == 0 ? "Depot departure confirmed" : "M7 corridor geofence"
    };

    private static List<string> Timeline(string checkpoint) => new()
    {
        "Depot release",
        checkpoint,
        "Gate pre-clearance",
        "Terminal handoff"
    };

    private static List<TruckEtaDto> BuildFallbackTrucks(int baseEta) => Enumerable.Range(1, 5)
        .Select(i =>
        {
            var eta = baseEta + i * 6;
            var risk = Math.Min(95, eta + i * 8);
            var status = risk > 82 ? "Hold Outside Port" : risk > 65 ? "Queue Risk" : i == 1 ? "Priority Release" : "On Time";
            var checkpoint = Checkpoint(i, status);
            var idling = Math.Max(0, eta - 20);
            return new TruckEtaDto
            {
                FleetIdentifier = $"CSPF-{100 + i}",
                OrganisationName = "Culltron Demo Fleet",
                RouteCorridor = i % 2 == 0 ? "N2 North → Durban Container Terminal" : "M7 West → Bayhead Gate",
                CurrentCheckpoint = checkpoint,
                EtaMinutesToGate = eta,
                QueueStatus = risk > 75 ? "Gate queue saturated" : "Queue building",
                IdlingMinutes = idling,
                EstimatedCo2Kg = Math.Round(idling / 60m * 3.0m * 2.68m, 1),
                DelayRiskScore = risk,
                Status = status,
                StatusTone = Tone(status),
                RecommendedAction = Action(status),
                Timeline = Timeline(checkpoint)
            };
        }).ToList();
}
