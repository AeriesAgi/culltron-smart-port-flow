using Microsoft.EntityFrameworkCore;
using SmartPort.Application.Interfaces;
using SmartPort.Domain.Entities;
using SmartPort.Domain.Enums;
using SmartPort.Infrastructure.Persistence;

namespace SmartPort.Infrastructure.Services;

// ─── Vessel Service ──────────────────────────────────────────────────────────

public class VesselService : IVesselService
{
    private readonly SmartPortDbContext _db;
    public VesselService(SmartPortDbContext db) => _db = db;

    public async Task<PagedResult<VesselListDto>> GetVesselsAsync(VesselFilterDto filter)
    {
        var query = _db.Vessels.Where(v => !v.IsDeleted);

        if (filter.Status.HasValue)
            query = query.Where(v => v.Status == filter.Status.Value);
        if (filter.VesselType.HasValue)
            query = query.Where(v => v.VesselType == filter.VesselType.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(v =>
                v.Name.ToLower().Contains(term) ||
                v.IMONumber.ToLower().Contains(term) ||
                v.ShippingLine.ToLower().Contains(term) ||
                (v.VoyageNumber != null && v.VoyageNumber.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(v => v.EstimatedTimeOfArrival)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(v => MapToListDto(v))
            .ToListAsync();

        return new PagedResult<VesselListDto> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<VesselDetailDto?> GetVesselDetailAsync(int id)
    {
        var v = await _db.Vessels
            .Include(x => x.BerthAssignments).ThenInclude(a => a.Berth)
            .Include(x => x.Documents)
            .Include(x => x.Incidents)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (v == null) return null;

        return new VesselDetailDto
        {
            Id = v.Id, Name = v.Name, IMONumber = v.IMONumber, MMSINumber = v.MMSINumber,
            CallSign = v.CallSign, ShippingLine = v.ShippingLine, VesselType = v.VesselType,
            Status = v.Status, FlagCode = v.FlagCode, FlagCountry = v.FlagCountry,
            PortOfRegistry = v.PortOfRegistry, Operator = v.Operator, Agent = v.Agent,
            GrossTonnage = v.GrossTonnage, DeadweightTonnage = v.DeadweightTonnage,
            LengthOverall = v.LengthOverall, Beam = v.Beam, MaxDraught = v.MaxDraught,
            TEUCapacity = v.TEUCapacity, YearBuilt = v.YearBuilt,
            ETA = v.EstimatedTimeOfArrival, ETD = v.EstimatedTimeOfDeparture,
            ActualTimeOfArrival = v.ActualTimeOfArrival, ActualTimeOfDeparture = v.ActualTimeOfDeparture,
            VoyageNumber = v.VoyageNumber, TurnaroundHours = v.TurnaroundHours,
            DelayMinutes = v.DelayMinutes, DelayReason = v.DelayReason,
            PreviousPort = v.PreviousPort, NextPort = v.NextPort,
            CurrentLatitude = v.CurrentLatitude, CurrentLongitude = v.CurrentLongitude,
            BerthAssignments = v.BerthAssignments.Select(a => new BerthAssignmentDto
            {
                Id = a.Id, BerthId = a.BerthId, BerthCode = a.Berth?.Code ?? "",
                VesselId = a.VesselId, VesselName = v.Name,
                PlannedArrival = a.PlannedArrival, PlannedDeparture = a.PlannedDeparture,
                ActualArrival = a.ActualArrival, ActualDeparture = a.ActualDeparture,
                OperationalStatus = a.OperationalStatus,
                PlannedDischarge = a.PlannedDischarge, PlannedLoad = a.PlannedLoad,
                ActualDischarge = a.ActualDischarge, ActualLoad = a.ActualLoad,
                DelayMinutes = a.DelayMinutes, CargoPlanApproved = a.CargoPlanApproved
            }),
            Documents = v.Documents.Select(d => new DocumentListDto
            {
                Id = d.Id, DocumentNumber = d.DocumentNumber, Title = d.Title,
                DocumentType = d.DocumentType, Status = d.Status, ComplianceStatus = d.ComplianceStatus,
                IssuingAuthority = d.IssuingAuthority, SubmittedBy = d.SubmittedBy,
                RequiredByDate = d.RequiredByDate, IsOverdue = d.IsOverdue
            }),
            Incidents = v.Incidents.Select(i => new IncidentListDto
            {
                Id = i.Id, ReferenceNumber = i.ReferenceNumber, Title = i.Title,
                Category = i.Category, Severity = i.Severity, Status = i.Status,
                OccurredAt = i.OccurredAt, IsOverdue = i.IsOverdue
            })
        };
    }

    public async Task<IEnumerable<VesselListDto>> GetVesselsInPortAsync() =>
        await _db.Vessels
            .Where(v => !v.IsDeleted && (v.Status == VesselStatus.Berthed || v.Status == VesselStatus.CargoOperations || v.Status == VesselStatus.BerthingInProgress))
            .Select(v => MapToListDto(v))
            .ToListAsync();

    public async Task<IEnumerable<VesselListDto>> GetExpectedVesselsAsync(int hours = 48) =>
        await _db.Vessels
            .Where(v => !v.IsDeleted && v.EstimatedTimeOfArrival.HasValue &&
                        v.EstimatedTimeOfArrival.Value <= DateTime.UtcNow.AddHours(hours) &&
                        v.EstimatedTimeOfArrival.Value >= DateTime.UtcNow.AddHours(-2))
            .OrderBy(v => v.EstimatedTimeOfArrival)
            .Select(v => MapToListDto(v))
            .ToListAsync();

    public async Task<int> CreateVesselAsync(CreateVesselDto dto, string userId)
    {
        var vessel = new Vessel
        {
            IMONumber = dto.IMONumber, Name = dto.Name, ShippingLine = dto.ShippingLine,
            Agent = dto.Agent, VesselType = dto.VesselType,
            LengthOverall = dto.LengthOverall, MaxDraught = dto.MaxDraught,
            GrossTonnage = dto.GrossTonnage, TEUCapacity = dto.TEUCapacity,
            VoyageNumber = dto.VoyageNumber,
            EstimatedTimeOfArrival = dto.EstimatedTimeOfArrival,
            Status = VesselStatus.Expected, CreatedBy = userId
        };
        _db.Vessels.Add(vessel);
        await _db.SaveChangesAsync();
        return vessel.Id;
    }

    public async Task UpdateVesselAsync(int id, UpdateVesselDto dto, string userId)
    {
        var vessel = await _db.Vessels.FindAsync(id) ?? throw new KeyNotFoundException();
        vessel.Name = dto.Name; vessel.ShippingLine = dto.ShippingLine;
        vessel.Agent = dto.Agent; vessel.VesselType = dto.VesselType;
        vessel.LengthOverall = dto.LengthOverall; vessel.MaxDraught = dto.MaxDraught;
        vessel.GrossTonnage = dto.GrossTonnage; vessel.TEUCapacity = dto.TEUCapacity;
        vessel.VoyageNumber = dto.VoyageNumber; vessel.Status = dto.Status;
        vessel.EstimatedTimeOfArrival = dto.EstimatedTimeOfArrival;
        vessel.EstimatedTimeOfDeparture = dto.EstimatedTimeOfDeparture;
        vessel.DelayMinutes = dto.DelayMinutes; vessel.DelayReason = dto.DelayReason;
        vessel.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateVesselStatusAsync(int id, VesselStatus status, string userId)
    {
        var vessel = await _db.Vessels.FindAsync(id) ?? throw new KeyNotFoundException();
        vessel.Status = status; vessel.UpdatedBy = userId;
        if (status == VesselStatus.Berthed || status == VesselStatus.BerthingInProgress)
            vessel.ActualTimeOfArrival ??= DateTime.UtcNow;
        if (status == VesselStatus.Departed)
            vessel.ActualTimeOfDeparture ??= DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static VesselListDto MapToListDto(Vessel v) => new()
    {
        Id = v.Id, Name = v.Name, IMONumber = v.IMONumber, ShippingLine = v.ShippingLine,
        VesselType = v.VesselType, Status = v.Status, FlagCode = v.FlagCode,
        VoyageNumber = v.VoyageNumber, ETA = v.EstimatedTimeOfArrival,
        ETD = v.EstimatedTimeOfDeparture, DelayMinutes = v.DelayMinutes,
        LengthOverall = v.LengthOverall, MaxDraught = v.MaxDraught,
        TEUCapacity = v.TEUCapacity, Agent = v.Agent
    };
}

// ─── Berth Service ───────────────────────────────────────────────────────────

public class BerthService : IBerthService
{
    private readonly SmartPortDbContext _db;
    public BerthService(SmartPortDbContext db) => _db = db;

    public async Task<IEnumerable<BerthStatusDto>> GetAllBerthsAsync()
    {
        var now = DateTime.UtcNow;
        var berths = await _db.Berths.Include(b => b.Assignments).ThenInclude(a => a.Vessel)
            .Where(b => !b.IsDeleted).ToListAsync();

        return berths.Select(b =>
        {
            var current = b.Assignments.FirstOrDefault(a =>
                a.PlannedArrival <= now && a.PlannedDeparture >= now && !a.Vessel.IsDeleted);
            return new BerthStatusDto
            {
                Id = b.Id, Code = b.Code, Name = b.Name, Terminal = b.Terminal,
                BerthType = b.BerthType, Status = b.Status, Length = b.Length, MaxDraught = b.MaxDraught,
                CurrentVesselName = current?.Vessel?.Name,
                CurrentVesselETD = current?.PlannedDeparture,
                UtilisationPercent30Day = b.UtilisationPercent30Day,
                HasCranes = b.HasCranes, CraneCount = b.CraneCount
            };
        });
    }

    public async Task<BerthDetailDto?> GetBerthDetailAsync(int id)
    {
        var now = DateTime.UtcNow;
        var b = await _db.Berths.Include(x => x.Assignments).ThenInclude(a => a.Vessel)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (b == null) return null;

        return new BerthDetailDto
        {
            Id = b.Id, Code = b.Code, Name = b.Name, Terminal = b.Terminal,
            BerthType = b.BerthType, Status = b.Status, Length = b.Length, MaxDraught = b.MaxDraught,
            HasCranes = b.HasCranes, CraneCount = b.CraneCount,
            UtilisationPercent30Day = b.UtilisationPercent30Day,
            AverageTurnaroundHours = b.AverageTurnaroundHours,
            UpcomingAssignments = b.Assignments.Where(a => a.PlannedArrival > now)
                .OrderBy(a => a.PlannedArrival).Take(5).Select(MapAssignment),
            RecentAssignments = b.Assignments.Where(a => a.PlannedDeparture < now)
                .OrderByDescending(a => a.PlannedDeparture).Take(5).Select(MapAssignment)
        };
    }

    public async Task<IEnumerable<BerthAssignmentDto>> GetBerthScheduleAsync(DateTime from, DateTime to) =>
        await _db.BerthAssignments
            .Include(a => a.Vessel).Include(a => a.Berth)
            .Where(a => a.PlannedArrival < to && a.PlannedDeparture > from)
            .OrderBy(a => a.PlannedArrival)
            .Select(a => MapAssignmentFull(a))
            .ToListAsync();

    public async Task<IEnumerable<BerthOccupancyDto>> GetBerthOccupancyAsync()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(7);
        var assignments = await GetBerthScheduleAsync(from, to);
        var berths = await _db.Berths.Where(b => !b.IsDeleted).ToListAsync();
        return berths.Select(b => new BerthOccupancyDto
        {
            BerthId = b.Id, BerthCode = b.Code,
            Assignments = assignments.Where(a => a.BerthId == b.Id)
        });
    }

    public async Task<int> CreateBerthAssignmentAsync(CreateBerthAssignmentDto dto, string userId)
    {
        var assignment = new BerthAssignment
        {
            VesselId = dto.VesselId, BerthId = dto.BerthId,
            PlannedArrival = dto.PlannedArrival, PlannedDeparture = dto.PlannedDeparture,
            PlannedDischarge = dto.PlannedDischarge, PlannedLoad = dto.PlannedLoad,
            OperationalStatus = VesselStatus.Expected, CreatedBy = userId
        };
        _db.BerthAssignments.Add(assignment);

        var berth = await _db.Berths.FindAsync(dto.BerthId);
        if (berth != null) berth.Status = BerthStatus.Reserved;

        await _db.SaveChangesAsync();
        return assignment.Id;
    }

    public async Task UpdateBerthStatusAsync(int berthId, BerthStatus status, string userId)
    {
        var berth = await _db.Berths.FindAsync(berthId) ?? throw new KeyNotFoundException();
        berth.Status = status; berth.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }

    private static BerthAssignmentDto MapAssignment(BerthAssignment a) => new()
    {
        Id = a.Id, BerthId = a.BerthId, VesselId = a.VesselId,
        VesselName = a.Vessel?.Name ?? "", ShippingLine = a.Vessel?.ShippingLine ?? "",
        PlannedArrival = a.PlannedArrival, PlannedDeparture = a.PlannedDeparture,
        ActualArrival = a.ActualArrival, ActualDeparture = a.ActualDeparture,
        OperationalStatus = a.OperationalStatus,
        PlannedDischarge = a.PlannedDischarge, PlannedLoad = a.PlannedLoad,
        ActualDischarge = a.ActualDischarge, ActualLoad = a.ActualLoad,
        DelayMinutes = a.DelayMinutes, CargoPlanApproved = a.CargoPlanApproved
    };

    private static BerthAssignmentDto MapAssignmentFull(BerthAssignment a) => new()
    {
        Id = a.Id, BerthId = a.BerthId, BerthCode = a.Berth?.Code ?? "",
        VesselId = a.VesselId, VesselName = a.Vessel?.Name ?? "",
        ShippingLine = a.Vessel?.ShippingLine ?? "",
        PlannedArrival = a.PlannedArrival, PlannedDeparture = a.PlannedDeparture,
        ActualArrival = a.ActualArrival, ActualDeparture = a.ActualDeparture,
        OperationalStatus = a.OperationalStatus,
        PlannedDischarge = a.PlannedDischarge, PlannedLoad = a.PlannedLoad,
        ActualDischarge = a.ActualDischarge, ActualLoad = a.ActualLoad,
        DelayMinutes = a.DelayMinutes, CargoPlanApproved = a.CargoPlanApproved
    };
}

// ─── Container Service ───────────────────────────────────────────────────────

public class ContainerService : IContainerService
{
    private readonly SmartPortDbContext _db;
    public ContainerService(SmartPortDbContext db) => _db = db;

    public async Task<PagedResult<ContainerListDto>> GetContainersAsync(ContainerFilterDto filter)
    {
        var query = _db.Containers.Include(c => c.Vessel).Include(c => c.YardBlock)
            .Where(c => !c.IsDeleted);

        if (filter.Status.HasValue) query = query.Where(c => c.Status == filter.Status.Value);
        if (filter.Direction.HasValue) query = query.Where(c => c.Direction == filter.Direction.Value);
        if (filter.IsHazardous.HasValue) query = query.Where(c => c.IsHazardous == filter.IsHazardous.Value);
        if (filter.IsDwellAlert.HasValue) query = query.Where(c => c.IsDwellAlertRaised == filter.IsDwellAlert.Value);
        if (filter.IsOnHold.HasValue) query = query.Where(c => c.IsOnHold == filter.IsOnHold.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var t = filter.SearchTerm.ToLower();
            query = query.Where(c => c.ContainerNumber.ToLower().Contains(t));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(c => new ContainerListDto
            {
                Id = c.Id, ContainerNumber = c.ContainerNumber, Size = c.Size,
                ContainerType = c.ContainerType, Direction = c.Direction, Status = c.Status,
                ShippingLine = c.ShippingLine, YardSlot = c.YardSlot,
                YardBlockCode = c.YardBlock != null ? c.YardBlock.BlockCode : null,
                DwellTimeHours = c.DwellTimeHours, IsDwellAlertRaised = c.IsDwellAlertRaised,
                IsHazardous = c.IsHazardous, IsReefer = c.IsReefer, IsOnHold = c.IsOnHold,
                CustomsStatus = c.CustomsStatus, VesselName = c.Vessel != null ? c.Vessel.Name : null
            }).ToListAsync();

        return new PagedResult<ContainerListDto> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<ContainerDetailDto?> GetContainerDetailAsync(int id)
    {
        var c = await _db.Containers.Include(x => x.Vessel).Include(x => x.YardBlock)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (c == null) return null;
        return MapDetail(c);
    }

    public async Task<ContainerDetailDto?> GetContainerByNumberAsync(string containerNumber)
    {
        var c = await _db.Containers.Include(x => x.Vessel).Include(x => x.YardBlock)
            .FirstOrDefaultAsync(x => x.ContainerNumber == containerNumber && !x.IsDeleted);
        if (c == null) return null;
        return MapDetail(c);
    }

    public async Task<IEnumerable<ContainerListDto>> GetDwellAlertContainersAsync() =>
        await _db.Containers.Include(c => c.YardBlock)
            .Where(c => c.IsDwellAlertRaised && !c.IsDeleted)
            .Select(c => new ContainerListDto
            {
                Id = c.Id, ContainerNumber = c.ContainerNumber, Size = c.Size, ContainerType = c.ContainerType,
                Direction = c.Direction, Status = c.Status, ShippingLine = c.ShippingLine,
                YardSlot = c.YardSlot, YardBlockCode = c.YardBlock != null ? c.YardBlock.BlockCode : null,
                DwellTimeHours = c.DwellTimeHours, IsDwellAlertRaised = c.IsDwellAlertRaised,
                IsHazardous = c.IsHazardous, IsReefer = c.IsReefer, IsOnHold = c.IsOnHold,
                CustomsStatus = c.CustomsStatus
            }).ToListAsync();

    public async Task UpdateContainerStatusAsync(int id, ContainerStatus status, string userId)
    {
        var c = await _db.Containers.FindAsync(id) ?? throw new KeyNotFoundException();
        c.Status = status; c.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }

    private static ContainerDetailDto MapDetail(Container c) => new()
    {
        Id = c.Id, ContainerNumber = c.ContainerNumber, Size = c.Size,
        ContainerType = c.ContainerType, Direction = c.Direction, Status = c.Status,
        ShippingLine = c.ShippingLine, YardSlot = c.YardSlot,
        YardBlockCode = c.YardBlock?.BlockCode, DwellTimeHours = c.DwellTimeHours,
        IsDwellAlertRaised = c.IsDwellAlertRaised, IsHazardous = c.IsHazardous,
        IsReefer = c.IsReefer, IsOnHold = c.IsOnHold, CustomsStatus = c.CustomsStatus,
        VesselName = c.Vessel?.Name, PortOfLoading = c.PortOfLoading,
        PortOfDischarge = c.PortOfDischarge, FinalDestination = c.FinalDestination,
        GrossWeightKg = c.GrossWeightKg, HazardClass = c.HazardClass,
        ReeferSetTemp = c.ReeferSetTemp, CustomsReferenceNumber = c.CustomsReferenceNumber,
        HoldReason = c.HoldReason, GateInDateTime = c.GateInDateTime,
        GateOutDateTime = c.GateOutDateTime, FreeTimeLimitHours = c.FreeTimeLimitHours
    };
}

// ─── Yard Service ────────────────────────────────────────────────────────────

public class YardService : IYardService
{
    private readonly SmartPortDbContext _db;
    public YardService(SmartPortDbContext db) => _db = db;

    public async Task<IEnumerable<YardBlockStatusDto>> GetYardBlockStatusAsync() =>
        await _db.YardBlocks.Where(y => y.IsActive)
            .Select(y => new YardBlockStatusDto
            {
                Id = y.Id, BlockCode = y.BlockCode, Terminal = y.Terminal, Zone = y.Zone,
                TotalCapacityTEU = y.TotalCapacityTEU, CurrentOccupancyTEU = y.CurrentOccupancyTEU,
                OccupancyPercent = y.TotalCapacityTEU > 0 ? Math.Round((decimal)y.CurrentOccupancyTEU / y.TotalCapacityTEU * 100, 1) : 0,
                IsReeferBlock = y.IsReeferBlock, IsHazardousBlock = y.IsHazardousBlock,
                IsNearCapacity = y.TotalCapacityTEU > 0 && (decimal)y.CurrentOccupancyTEU / y.TotalCapacityTEU >= 0.85m,
                IsCritical = y.TotalCapacityTEU > 0 && (decimal)y.CurrentOccupancyTEU / y.TotalCapacityTEU >= 0.95m
            }).ToListAsync();

    public async Task<YardBlockDetailDto?> GetYardBlockDetailAsync(int id)
    {
        var y = await _db.YardBlocks.Include(x => x.Containers)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (y == null) return null;
        return new YardBlockDetailDto
        {
            Id = y.Id, BlockCode = y.BlockCode, Terminal = y.Terminal, Zone = y.Zone,
            TotalCapacityTEU = y.TotalCapacityTEU, CurrentOccupancyTEU = y.CurrentOccupancyTEU,
            OccupancyPercent = y.OccupancyPercent, Rows = y.Rows, Bays = y.Bays, Tiers = y.Tiers,
            IsReeferBlock = y.IsReeferBlock, IsHazardousBlock = y.IsHazardousBlock,
            IsNearCapacity = y.IsNearCapacity, IsCritical = y.IsCritical,
            Containers = y.Containers.Take(50).Select(c => new ContainerListDto
            {
                Id = c.Id, ContainerNumber = c.ContainerNumber, Size = c.Size,
                ContainerType = c.ContainerType, Direction = c.Direction, Status = c.Status,
                YardSlot = c.YardSlot, DwellTimeHours = c.DwellTimeHours,
                IsDwellAlertRaised = c.IsDwellAlertRaised, IsHazardous = c.IsHazardous, IsOnHold = c.IsOnHold,
                CustomsStatus = c.CustomsStatus
            })
        };
    }
}

// ─── Gate Service ────────────────────────────────────────────────────────────

public class GateService : IGateService
{
    private readonly SmartPortDbContext _db;
    public GateService(SmartPortDbContext db) => _db = db;

    public async Task<IEnumerable<GateStatusDto>> GetGateStatusAsync() =>
        await _db.Gates.Where(g => !g.IsDeleted).Select(g => new GateStatusDto
        {
            Id = g.Id, Code = g.Code, Name = g.Name, IsOperational = g.IsOperational,
            CurrentQueueCount = g.CurrentQueueCount, AverageProcessingMinutes = g.AverageProcessingMinutes,
            IsEntryGate = g.IsEntryGate, IsExitGate = g.IsExitGate,
            LaneCount = g.LaneCount, HasOCR = g.HasOCR
        }).ToListAsync();

    public async Task<PagedResult<TruckListDto>> GetTrucksAsync(TruckFilterDto filter)
    {
        var query = _db.Trucks.Where(t => !t.IsDeleted);
        if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var t = filter.SearchTerm.ToLower();
            query = query.Where(x => x.RegistrationNumber.ToLower().Contains(t) ||
                                     x.TransporterName.ToLower().Contains(t));
        }
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(t => new TruckListDto
            {
                Id = t.Id, RegistrationNumber = t.RegistrationNumber,
                TransporterName = t.TransporterName, DriverName = t.DriverName,
                Status = t.Status, BookingReference = t.BookingReference,
                AppointmentDateTime = t.AppointmentDateTime,
                TargetContainerNumber = t.TargetContainerNumber,
                GateInTime = t.GateInTime, PortDwellMinutes = t.PortDwellMinutes
            }).ToListAsync();
        return new PagedResult<TruckListDto> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<IEnumerable<GateTransactionDto>> GetRecentTransactionsAsync(int count = 50) =>
        await _db.GateTransactions.Include(t => t.Truck).Include(t => t.Gate)
            .OrderByDescending(t => t.TransactionTime).Take(count)
            .Select(t => new GateTransactionDto
            {
                Id = t.Id, GateCode = t.Gate.Code, TruckRegistration = t.Truck.RegistrationNumber,
                TransporterName = t.Truck.TransporterName, TransactionType = t.TransactionType,
                Status = t.Status, TransactionTime = t.TransactionTime,
                ContainerNumber = t.ContainerNumber, DocumentsVerified = t.DocumentsVerified,
                ExceptionReason = t.ExceptionReason
            }).ToListAsync();
}

// ─── Incident Service ────────────────────────────────────────────────────────

public class IncidentService : IIncidentService
{
    private readonly SmartPortDbContext _db;
    public IncidentService(SmartPortDbContext db) => _db = db;

    public async Task<PagedResult<IncidentListDto>> GetIncidentsAsync(IncidentFilterDto filter)
    {
        var query = _db.Incidents.Include(i => i.Vessel).Where(i => !i.IsDeleted);
        if (filter.Status.HasValue) query = query.Where(i => i.Status == filter.Status.Value);
        if (filter.Severity.HasValue) query = query.Where(i => i.Severity == filter.Severity.Value);
        if (filter.Category.HasValue) query = query.Where(i => i.Category == filter.Category.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var t = filter.SearchTerm.ToLower();
            query = query.Where(i => i.Title.ToLower().Contains(t) || i.ReferenceNumber.ToLower().Contains(t));
        }
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(i => i.OccurredAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(i => new IncidentListDto
            {
                Id = i.Id, ReferenceNumber = i.ReferenceNumber, Title = i.Title,
                Category = i.Category, Severity = i.Severity, Status = i.Status,
                Location = i.Location, Terminal = i.Terminal, ReportedBy = i.ReportedBy,
                AssignedTo = i.AssignedTo, OccurredAt = i.OccurredAt,
                TargetResolutionTime = i.TargetResolutionTime,
                IsOverdue = i.TargetResolutionTime.HasValue && DateTime.UtcNow > i.TargetResolutionTime && i.Status != IncidentStatus.Resolved,
                VesselName = i.Vessel != null ? i.Vessel.Name : null
            }).ToListAsync();
        return new PagedResult<IncidentListDto> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<IncidentDetailDto?> GetIncidentDetailAsync(int id)
    {
        var i = await _db.Incidents.Include(x => x.Vessel).Include(x => x.Updates)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (i == null) return null;
        return new IncidentDetailDto
        {
            Id = i.Id, ReferenceNumber = i.ReferenceNumber, Title = i.Title,
            Description = i.Description, Category = i.Category, Severity = i.Severity,
            Status = i.Status, Location = i.Location, Terminal = i.Terminal,
            ReportedBy = i.ReportedBy, AcknowledgedBy = i.AcknowledgedBy,
            AssignedTo = i.AssignedTo, OccurredAt = i.OccurredAt,
            AcknowledgedAt = i.AcknowledgedAt, TargetResolutionTime = i.TargetResolutionTime,
            IsOverdue = i.IsOverdue, VesselName = i.Vessel?.Name,
            RootCause = i.RootCause, ResolutionNotes = i.ResolutionNotes,
            CorrectiveAction = i.CorrectiveAction,
            Updates = i.Updates.OrderByDescending(u => u.CreatedAt).Select(u => new IncidentUpdateDto
            {
                UpdatedBy = u.UpdatedBy, NewStatus = u.NewStatus, Note = u.Note, CreatedAt = u.CreatedAt
            })
        };
    }

    public async Task<IEnumerable<IncidentListDto>> GetOpenIncidentsAsync() =>
        (await GetIncidentsAsync(new IncidentFilterDto { Status = null, PageSize = 100 })).Items
            .Where(i => i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed);

    public async Task<int> CreateIncidentAsync(CreateIncidentDto dto, string userId)
    {
        var count = await _db.Incidents.CountAsync() + 1;
        var incident = new Incident
        {
            ReferenceNumber = $"INC-{DateTime.UtcNow.Year}-{count:D5}",
            Title = dto.Title, Description = dto.Description,
            Category = dto.Category, Severity = dto.Severity,
            VesselId = dto.VesselId, BerthId = dto.BerthId,
            Location = dto.Location, Terminal = dto.Terminal,
            AssignedTo = dto.AssignedTo, TargetResolutionTime = dto.TargetResolutionTime,
            Status = IncidentStatus.Open, ReportedBy = userId,
            OccurredAt = DateTime.UtcNow, CreatedBy = userId
        };
        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();
        return incident.Id;
    }

    public async Task AcknowledgeIncidentAsync(int id, string userId)
    {
        var i = await _db.Incidents.FindAsync(id) ?? throw new KeyNotFoundException();
        i.Status = IncidentStatus.Acknowledged;
        i.AcknowledgedBy = userId; i.AcknowledgedAt = DateTime.UtcNow;
        _db.IncidentUpdates.Add(new IncidentUpdate { IncidentId = id, UpdatedBy = userId, NewStatus = IncidentStatus.Acknowledged, Note = "Incident acknowledged.", CreatedBy = userId });
        await _db.SaveChangesAsync();
    }

    public async Task UpdateIncidentAsync(int id, UpdateIncidentDto dto, string userId)
    {
        var i = await _db.Incidents.FindAsync(id) ?? throw new KeyNotFoundException();
        i.Severity = dto.Severity; i.Status = dto.Status; i.AssignedTo = dto.AssignedTo;
        i.TargetResolutionTime = dto.TargetResolutionTime; i.UpdatedBy = userId;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            _db.IncidentUpdates.Add(new IncidentUpdate { IncidentId = id, UpdatedBy = userId, NewStatus = dto.Status, Note = dto.Notes!, CreatedBy = userId });
        await _db.SaveChangesAsync();
    }

    public async Task ResolveIncidentAsync(int id, ResolveIncidentDto dto, string userId)
    {
        var i = await _db.Incidents.FindAsync(id) ?? throw new KeyNotFoundException();
        i.Status = IncidentStatus.Resolved; i.ResolvedBy = userId;
        i.ResolvedAt = DateTime.UtcNow; i.RootCause = dto.RootCause;
        i.ResolutionNotes = dto.ResolutionNotes; i.CorrectiveAction = dto.CorrectiveAction;
        i.RequiresFollowUp = dto.RequiresFollowUp; i.UpdatedBy = userId;
        _db.IncidentUpdates.Add(new IncidentUpdate { IncidentId = id, UpdatedBy = userId, NewStatus = IncidentStatus.Resolved, Note = dto.ResolutionNotes, CreatedBy = userId });
        await _db.SaveChangesAsync();
    }
}

// ─── Alert Service ───────────────────────────────────────────────────────────

public class AlertService : IAlertService
{
    private readonly SmartPortDbContext _db;
    public AlertService(SmartPortDbContext db) => _db = db;

    public async Task<IEnumerable<AlertDto>> GetActiveAlertsAsync() =>
        await _db.Alerts.Include(a => a.RelatedVessel)
            .Where(a => a.Status == AlertStatus.Active && !a.IsDeleted)
            .OrderByDescending(a => a.Severity).ThenByDescending(a => a.CreatedAt)
            .Select(a => new AlertDto
            {
                Id = a.Id, AlertType = a.AlertType, Status = a.Status, Severity = a.Severity,
                Title = a.Title, Message = a.Message, CreatedAt = a.CreatedAt,
                IsAutoGenerated = a.IsAutoGenerated, AcknowledgedBy = a.AcknowledgedBy,
                AcknowledgedAt = a.AcknowledgedAt,
                VesselName = a.RelatedVessel != null ? a.RelatedVessel.Name : null
            }).ToListAsync();

    public async Task<int> GetActiveAlertCountAsync() =>
        await _db.Alerts.CountAsync(a => a.Status == AlertStatus.Active && !a.IsDeleted);

    public async Task AcknowledgeAlertAsync(int id, string userId)
    {
        var alert = await _db.Alerts.FindAsync(id) ?? throw new KeyNotFoundException();
        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedBy = userId; alert.AcknowledgedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ResolveAlertAsync(int id, string userId)
    {
        var alert = await _db.Alerts.FindAsync(id) ?? throw new KeyNotFoundException();
        alert.Status = AlertStatus.Resolved;
        alert.ResolvedBy = userId; alert.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

// ─── Document Service ────────────────────────────────────────────────────────

public class DocumentService : IDocumentService
{
    private readonly SmartPortDbContext _db;
    public DocumentService(SmartPortDbContext db) => _db = db;

    public async Task<PagedResult<DocumentListDto>> GetDocumentsAsync(DocumentFilterDto filter)
    {
        var query = _db.Documents.Include(d => d.Vessel).Where(d => !d.IsDeleted);
        if (filter.DocumentType.HasValue) query = query.Where(d => d.DocumentType == filter.DocumentType.Value);
        if (filter.Status.HasValue) query = query.Where(d => d.Status == filter.Status.Value);
        if (filter.VesselId.HasValue) query = query.Where(d => d.VesselId == filter.VesselId.Value);
        if (filter.IsOverdue == true) query = query.Where(d => d.RequiredByDate.HasValue && DateTime.UtcNow > d.RequiredByDate && d.Status != DocumentStatus.Approved);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(d => d.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(d => new DocumentListDto
            {
                Id = d.Id, DocumentNumber = d.DocumentNumber, Title = d.Title,
                DocumentType = d.DocumentType, Status = d.Status, ComplianceStatus = d.ComplianceStatus,
                IssuingAuthority = d.IssuingAuthority, SubmittedBy = d.SubmittedBy,
                VesselName = d.Vessel != null ? d.Vessel.Name : null,
                RequiredByDate = d.RequiredByDate, ExpiryDate = d.ExpiryDate,
                IsOverdue = d.RequiredByDate.HasValue && DateTime.UtcNow > d.RequiredByDate && d.Status != DocumentStatus.Approved,
                IsExpired = d.ExpiryDate.HasValue && DateTime.UtcNow > d.ExpiryDate
            }).ToListAsync();
        return new PagedResult<DocumentListDto> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<DocumentDetailDto?> GetDocumentDetailAsync(int id)
    {
        var d = await _db.Documents.Include(x => x.Vessel).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (d == null) return null;
        return new DocumentDetailDto
        {
            Id = d.Id, DocumentNumber = d.DocumentNumber, Title = d.Title,
            DocumentType = d.DocumentType, Status = d.Status, ComplianceStatus = d.ComplianceStatus,
            IssuingAuthority = d.IssuingAuthority, SubmittedBy = d.SubmittedBy,
            ReviewedBy = d.ReviewedBy, ApprovedBy = d.ApprovedBy, RejectionReason = d.RejectionReason,
            VesselName = d.Vessel?.Name, IssuedDate = d.IssuedDate, SubmittedDate = d.SubmittedDate,
            ReviewedDate = d.ReviewedDate, ApprovedDate = d.ApprovedDate,
            RequiredByDate = d.RequiredByDate, ExpiryDate = d.ExpiryDate,
            IsOverdue = d.IsOverdue, IsExpired = d.IsExpired, Notes = d.Notes, FileName = d.FileName
        };
    }

    public async Task<IEnumerable<DocumentListDto>> GetOverdueDocumentsAsync()
    {
        var result = await GetDocumentsAsync(new DocumentFilterDto { IsOverdue = true, PageSize = 50 });
        return result.Items;
    }

    public async Task<int> CreateDocumentAsync(CreateDocumentDto dto, string userId)
    {
        var count = await _db.Documents.CountAsync() + 1;
        var doc = new Document
        {
            DocumentNumber = $"DOC-{DateTime.UtcNow.Year}-{count:D5}",
            DocumentType = dto.DocumentType, Title = dto.Title,
            IssuingAuthority = dto.IssuingAuthority, VesselId = dto.VesselId,
            ContainerNumber = dto.ContainerNumber, RequiredByDate = dto.RequiredByDate,
            ExpiryDate = dto.ExpiryDate, Notes = dto.Notes,
            Status = DocumentStatus.Required, ComplianceStatus = ComplianceStatus.Unknown,
            SubmittedBy = userId, CreatedBy = userId
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        return doc.Id;
    }

    public async Task UpdateDocumentStatusAsync(int id, DocumentStatus status, string userId, string? notes = null)
    {
        var doc = await _db.Documents.FindAsync(id) ?? throw new KeyNotFoundException();
        doc.Status = status; doc.UpdatedBy = userId;
        switch (status)
        {
            case DocumentStatus.UnderReview: doc.ReviewedBy = userId; doc.ReviewedDate = DateTime.UtcNow; break;
            case DocumentStatus.Approved: doc.ApprovedBy = userId; doc.ApprovedDate = DateTime.UtcNow; doc.ComplianceStatus = ComplianceStatus.Compliant; break;
            case DocumentStatus.Rejected: doc.RejectionReason = notes; doc.ComplianceStatus = ComplianceStatus.NonCompliant; break;
        }
        await _db.SaveChangesAsync();
    }
}

// ─── Analytics Service ───────────────────────────────────────────────────────

public class AnalyticsService : IAnalyticsService
{
    private readonly SmartPortDbContext _db;
    public AnalyticsService(SmartPortDbContext db) => _db = db;

    public async Task<ThroughputAnalyticsDto> GetThroughputAnalyticsAsync(AnalyticsFilterDto filter)
    {
        var data = await _db.OperationalMetrics
            .Where(m => m.MetricDate >= filter.FromDate.Date && m.MetricDate <= filter.ToDate.Date &&
                        m.MetricType == "DailyThroughputTEU" && m.Terminal == filter.Terminal)
            .OrderBy(m => m.MetricDate).ToListAsync();
        return new ThroughputAnalyticsDto
        {
            TotalTEU = data.Sum(m => m.Value),
            AverageDailyTEU = data.Count > 0 ? Math.Round(data.Average(m => m.Value), 0) : 0,
            PeakDailyTEU = data.Count > 0 ? data.Max(m => m.Value) : 0,
            DailyTrend = data.Select(m => new KpiTrendDto { Date = m.MetricDate, Value = m.Value, Label = m.MetricDate.ToString("dd MMM"), MetricType = m.MetricType })
        };
    }

    public async Task<TurnaroundAnalyticsDto> GetTurnaroundAnalyticsAsync(AnalyticsFilterDto filter)
    {
        var data = await _db.OperationalMetrics
            .Where(m => m.MetricDate >= filter.FromDate.Date && m.MetricDate <= filter.ToDate.Date &&
                        m.MetricType == "AverageTurnaroundHours" && m.Terminal == filter.Terminal)
            .OrderBy(m => m.MetricDate).ToListAsync();
        var craneData = await _db.OperationalMetrics
            .Where(m => m.MetricDate >= filter.FromDate.Date && m.MetricDate <= filter.ToDate.Date &&
                        m.MetricType == "CraneProductivity" && m.Terminal == filter.Terminal)
            .ToListAsync();
        return new TurnaroundAnalyticsDto
        {
            AverageTurnaroundHours = data.Count > 0 ? Math.Round(data.Average(m => m.Value), 1) : 0,
            MedianTurnaroundHours = data.Count > 0 ? data.OrderBy(m => m.Value).ElementAt(data.Count / 2).Value : 0,
            AverageCraneProductivity = craneData.Count > 0 ? Math.Round(craneData.Average(m => m.Value), 1) : 0,
            DailyTrend = data.Select(m => new KpiTrendDto { Date = m.MetricDate, Value = m.Value, Label = m.MetricDate.ToString("dd MMM"), MetricType = m.MetricType })
        };
    }

    public async Task<BerthEfficiencyDto> GetBerthEfficiencyAsync(AnalyticsFilterDto filter)
    {
        var berths = await _db.Berths.Where(b => !b.IsDeleted).ToListAsync();
        var avg = berths.Count > 0 ? berths.Average(b => b.UtilisationPercent30Day) : 0;
        return new BerthEfficiencyDto
        {
            AverageUtilisationPercent = Math.Round((decimal)avg, 1),
            ByBerth = berths.Select(b => new BerthEfficiencyRowDto
            {
                BerthCode = b.Code,
                UtilisationPercent = b.UtilisationPercent30Day,
                AverageTurnaroundHours = b.AverageTurnaroundHours,
                VesselCallCount = b.Assignments?.Count ?? 0
            })
        };
    }

    public async Task<YardAnalyticsDto> GetYardAnalyticsAsync(AnalyticsFilterDto filter)
    {
        var data = await _db.OperationalMetrics
            .Where(m => m.MetricDate >= filter.FromDate.Date && m.MetricDate <= filter.ToDate.Date &&
                        m.MetricType == "YardDensity" && m.Terminal == filter.Terminal)
            .OrderBy(m => m.MetricDate).ToListAsync();
        return new YardAnalyticsDto
        {
            AverageYardDensity = data.Count > 0 ? Math.Round(data.Average(m => m.Value), 1) : 0,
            AverageDwellTimeHours = 68,  // Computed from container dwell data in real impl
            DailyDensityTrend = data.Select(m => new KpiTrendDto { Date = m.MetricDate, Value = m.Value, Label = m.MetricDate.ToString("dd MMM"), MetricType = m.MetricType })
        };
    }
}

// ─── Recommendation / AI Service ─────────────────────────────────────────────

public class RecommendationService : IRecommendationService
{
    private readonly SmartPortDbContext _db;
    public RecommendationService(SmartPortDbContext db) => _db = db;

    public async Task<IEnumerable<RecommendationDto>> GetActiveRecommendationsAsync() =>
        await _db.Recommendations.Include(r => r.RelatedVessel).Include(r => r.RelatedBerth)
            .Where(r => r.Status == RecommendationStatus.Pending && !r.IsDeleted)
            .OrderByDescending(r => r.Priority).ThenByDescending(r => r.CreatedAt)
            .Select(r => new RecommendationDto
            {
                Id = r.Id, Type = r.Type, Status = r.Status, Priority = r.Priority,
                Title = r.Title, Summary = r.Summary, SuggestedAction = r.SuggestedAction,
                ImpactEstimate = r.ImpactEstimate, DetailedRationale = r.DetailedRationale,
                IsAIGenerated = r.IsAIGenerated, CreatedAt = r.CreatedAt, ExpiresAt = r.ExpiresAt,
                VesselName = r.RelatedVessel != null ? r.RelatedVessel.Name : null,
                ActedOnBy = r.ActedOnBy, ActedOnNotes = r.ActedOnNotes
            }).ToListAsync();

    public async Task AcceptRecommendationAsync(int id, string userId, string? notes)
    {
        var r = await _db.Recommendations.FindAsync(id) ?? throw new KeyNotFoundException();
        r.Status = RecommendationStatus.Accepted;
        r.ActedOnBy = userId; r.ActedOnAt = DateTime.UtcNow; r.ActedOnNotes = notes;
        await _db.SaveChangesAsync();
    }

    public async Task DismissRecommendationAsync(int id, string userId, string? notes)
    {
        var r = await _db.Recommendations.FindAsync(id) ?? throw new KeyNotFoundException();
        r.Status = RecommendationStatus.Dismissed;
        r.ActedOnBy = userId; r.ActedOnAt = DateTime.UtcNow; r.ActedOnNotes = notes;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Rules engine: scans operational data and generates/refreshes recommendations.
    /// In production this would run as a background service.
    /// </summary>
    public async Task RunRecommendationEngineAsync()
    {
        var now = DateTime.UtcNow;

        // Rule 1 – Vessels waiting >2h at anchor
        var anchoredVessels = await _db.Vessels
            .Where(v => v.Status == VesselStatus.AtAnchor && v.ActualTimeOfArrival.HasValue && !v.IsDeleted)
            .ToListAsync();
        foreach (var v in anchoredVessels)
        {
            var waitHours = (decimal)(now - v.ActualTimeOfArrival!.Value).TotalHours;
            if (waitHours < 2) continue;
            var exists = await _db.Recommendations.AnyAsync(r =>
                r.RelatedVesselId == v.Id && r.Type == RecommendationType.BerthReallocation &&
                r.Status == RecommendationStatus.Pending);
            if (!exists)
            {
                _db.Recommendations.Add(new Recommendation
                {
                    Type = RecommendationType.BerthReallocation,
                    Status = RecommendationStatus.Pending,
                    Priority = waitHours > 4 ? IncidentSeverity.Critical : IncidentSeverity.High,
                    Title = $"Berth Allocation Required – {v.Name}",
                    Summary = $"{v.Name} has been at anchorage for {waitHours:F1} hours. Review berth availability.",
                    DetailedRationale = $"Vessel {v.Name} (IMO {v.IMONumber}) arrived at {v.ActualTimeOfArrival:HH:mm} and is still awaiting berth assignment.",
                    SuggestedAction = "Review berth planner and assign earliest available compatible berth.",
                    ImpactEstimate = "Reduces anchorage cost and improves port turnaround KPI.",
                    RelatedVesselId = v.Id, TriggerMetric = "AnchorageWaitHours",
                    TriggerValue = waitHours, ThresholdValue = 2, IsAIGenerated = true,
                    ExpiresAt = now.AddHours(4), CreatedBy = "System"
                });
            }
        }

        // Rule 2 – Gate queue critical
        var criticalGates = await _db.Gates.Where(g => g.CurrentQueueCount >= 10 && g.IsOperational).ToListAsync();
        foreach (var g in criticalGates)
        {
            var exists = await _db.Recommendations.AnyAsync(r =>
                r.Type == RecommendationType.TruckQueueMitigation && r.Status == RecommendationStatus.Pending);
            if (!exists)
            {
                _db.Recommendations.Add(new Recommendation
                {
                    Type = RecommendationType.TruckQueueMitigation,
                    Status = RecommendationStatus.Pending,
                    Priority = IncidentSeverity.High,
                    Title = $"{g.Name} Queue Critical: {g.CurrentQueueCount} Trucks",
                    Summary = $"Gate {g.Code} queue has reached {g.CurrentQueueCount} trucks, exceeding the threshold of 10.",
                    SuggestedAction = "Activate additional gate lanes or divert to alternative gate.",
                    ImpactEstimate = "Estimated wait time reduction from ~" + g.CurrentQueueCount * g.AverageProcessingMinutes + " to ~18 min.",
                    TriggerMetric = "GateQueueCount", TriggerValue = g.CurrentQueueCount, ThresholdValue = 10,
                    IsAIGenerated = true, ExpiresAt = now.AddHours(2), CreatedBy = "System"
                });
            }
        }

        // Rule 3 – Containers with exceeded free time
        var dwellContainers = await _db.Containers
            .Where(c => c.DwellTimeHours > c.FreeTimeLimitHours && !c.IsDwellAlertRaised && !c.IsDeleted)
            .ToListAsync();
        foreach (var c in dwellContainers)
        {
            c.IsDwellAlertRaised = true;
        }

        await _db.SaveChangesAsync();
    }
}
