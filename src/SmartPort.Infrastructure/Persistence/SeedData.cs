using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartPort.Domain.Entities;
using SmartPort.Domain.Enums;

namespace SmartPort.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with realistic demo data for a South African port environment.
/// Based on Durban Container Terminal (DCT) operational patterns.
/// </summary>
public static class SeedData
{
    public static async Task SeedAllAsync(
        SmartPortDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);
        await SeedBerthsAsync(context);
        await SeedYardBlocksAsync(context);
        await SeedGatesAsync(context);
        await SeedVesselsAsync(context);
        await SeedContainersAsync(context);
        await SeedIncidentsAsync(context);
        await SeedAlertsAsync(context);
        await SeedDocumentsAsync(context);
        await SeedRecommendationsAsync(context);
        await SeedOperationalMetricsAsync(context);
    }

    // ─── Roles ───────────────────────────────────────────────────────────────
    public static readonly string[] Roles =
    {
        "Admin",
        "PortOperationsManager",
        "TerminalStaff",
        "LogisticsPartner",
        "Viewer"
    };

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // ─── Demo Users ───────────────────────────────────────────────────────────
    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        var users = new[]
        {
            new { Email = "admin@smartport.co.za",         First = "Sipho",    Last = "Nkosi",      Role = "Admin",                  JobTitle = "System Administrator",      Terminal = "DCT" },
            new { Email = "ops.manager@smartport.co.za",   First = "Nomvula",  Last = "Dlamini",    Role = "PortOperationsManager",  JobTitle = "Port Operations Manager",   Terminal = "DCT" },
            new { Email = "terminal1@smartport.co.za",     First = "Thabo",    Last = "Molefe",     Role = "TerminalStaff",          JobTitle = "Terminal Supervisor",       Terminal = "DCT" },
            new { Email = "logistics@freightco.co.za",     First = "Priya",    Last = "Naidoo",     Role = "LogisticsPartner",       JobTitle = "Logistics Coordinator",     Terminal = "N/A" },
            new { Email = "executive@transnet.co.za",      First = "Johan",    Last = "van der Berg", Role = "Viewer",               JobTitle = "Executive Director",        Terminal = "HQ"  },
        };

        foreach (var u in users)
        {
            if (await userManager.FindByEmailAsync(u.Email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    FirstName = u.First,
                    LastName = u.Last,
                    JobTitle = u.JobTitle,
                    Organisation = "Transnet Port Terminals",
                    Terminal = u.Terminal,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(user, "SmartPort@2025!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, u.Role);
            }
        }
    }

    // ─── Berths ───────────────────────────────────────────────────────────────
    private static async Task SeedBerthsAsync(SmartPortDbContext context)
    {
        if (await context.Berths.AnyAsync()) return;

        var berths = new List<Berth>
        {
            new() { Code = "DCT-B1",  Name = "Berth 1 – Point",          Terminal = "Durban Container Terminal", BerthType = BerthType.Container,   Status = BerthStatus.Occupied,           Length = 320, MaxDraught = 14.0m, MaxLOA = 350m, MaxTEUPerCall = 4000, HasCranes = true, CraneCount = 4, Latitude = -29.8716m, Longitude = 31.0356m, UtilisationPercent30Day = 87, AverageTurnaroundHours = 32 },
            new() { Code = "DCT-B2",  Name = "Berth 2 – Point",          Terminal = "Durban Container Terminal", BerthType = BerthType.Container,   Status = BerthStatus.Occupied,           Length = 310, MaxDraught = 13.5m, MaxLOA = 330m, MaxTEUPerCall = 3800, HasCranes = true, CraneCount = 3, Latitude = -29.8722m, Longitude = 31.0358m, UtilisationPercent30Day = 82, AverageTurnaroundHours = 29 },
            new() { Code = "DCT-B3",  Name = "Berth 3 – Point",          Terminal = "Durban Container Terminal", BerthType = BerthType.Container,   Status = BerthStatus.Available,          Length = 300, MaxDraught = 13.0m, MaxLOA = 320m, MaxTEUPerCall = 3600, HasCranes = true, CraneCount = 3, Latitude = -29.8728m, Longitude = 31.0360m, UtilisationPercent30Day = 74, AverageTurnaroundHours = 27 },
            new() { Code = "DCT-B4",  Name = "Berth 4 – Maydon",         Terminal = "Durban Container Terminal", BerthType = BerthType.Container,   Status = BerthStatus.Reserved,           Length = 330, MaxDraught = 14.5m, MaxLOA = 360m, MaxTEUPerCall = 5000, HasCranes = true, CraneCount = 4, Latitude = -29.8734m, Longitude = 31.0362m, UtilisationPercent30Day = 91, AverageTurnaroundHours = 36 },
            new() { Code = "DCT-B5",  Name = "Berth 5 – Maydon",         Terminal = "Durban Container Terminal", BerthType = BerthType.Container,   Status = BerthStatus.UnderMaintenance,   Length = 290, MaxDraught = 12.5m, MaxLOA = 300m, MaxTEUPerCall = 3200, HasCranes = true, CraneCount = 2, Latitude = -29.8740m, Longitude = 31.0364m, MaintenanceStartDate = DateTime.UtcNow.AddDays(-2), MaintenanceEndDate = DateTime.UtcNow.AddDays(3), UtilisationPercent30Day = 65, AverageTurnaroundHours = 24 },
            new() { Code = "MPT-B1",  Name = "Berth 1 – Multipurpose",   Terminal = "Durban Multipurpose Terminal", BerthType = BerthType.MultiPurpose, Status = BerthStatus.Occupied,       Length = 280, MaxDraught = 12.0m, MaxLOA = 290m, MaxTEUPerCall = 2000, HasCranes = true, CraneCount = 2, Latitude = -29.8755m, Longitude = 31.0370m, UtilisationPercent30Day = 70, AverageTurnaroundHours = 22 },
            new() { Code = "OPT-B1",  Name = "Berth 1 – Oil Pier",       Terminal = "Durban Oil Terminal",          BerthType = BerthType.BulkLiquid,  Status = BerthStatus.Occupied,         Length = 250, MaxDraught = 15.0m, MaxLOA = 260m, MaxTEUPerCall = 0,    HasCranes = false, CraneCount = 0, Latitude = -29.8800m, Longitude = 31.0380m, UtilisationPercent30Day = 78, AverageTurnaroundHours = 18 },
            new() { Code = "OPT-B2",  Name = "Berth 2 – Oil Pier",       Terminal = "Durban Oil Terminal",          BerthType = BerthType.BulkLiquid,  Status = BerthStatus.Available,        Length = 240, MaxDraught = 14.5m, MaxLOA = 250m, MaxTEUPerCall = 0,    HasCranes = false, CraneCount = 0, Latitude = -29.8806m, Longitude = 31.0382m, UtilisationPercent30Day = 68, AverageTurnaroundHours = 16 },
        };

        context.Berths.AddRange(berths);
        await context.SaveChangesAsync();
    }

    // ─── Yard Blocks ──────────────────────────────────────────────────────────
    private static async Task SeedYardBlocksAsync(SmartPortDbContext context)
    {
        if (await context.YardBlocks.AnyAsync()) return;

        var blocks = new List<YardBlock>
        {
            new() { BlockCode = "A", Terminal = "Durban Container Terminal", Zone = "Import",  Rows = 6, Bays = 40, Tiers = 4, TotalCapacityTEU = 960,  CurrentOccupancyTEU = 810, IsReeferBlock = false },
            new() { BlockCode = "B", Terminal = "Durban Container Terminal", Zone = "Import",  Rows = 6, Bays = 40, Tiers = 4, TotalCapacityTEU = 960,  CurrentOccupancyTEU = 720, IsReeferBlock = false },
            new() { BlockCode = "C", Terminal = "Durban Container Terminal", Zone = "Export",  Rows = 6, Bays = 38, Tiers = 4, TotalCapacityTEU = 912,  CurrentOccupancyTEU = 550, IsReeferBlock = false },
            new() { BlockCode = "D", Terminal = "Durban Container Terminal", Zone = "Export",  Rows = 6, Bays = 38, Tiers = 4, TotalCapacityTEU = 912,  CurrentOccupancyTEU = 610, IsReeferBlock = false },
            new() { BlockCode = "E", Terminal = "Durban Container Terminal", Zone = "Empty",   Rows = 5, Bays = 30, Tiers = 5, TotalCapacityTEU = 750,  CurrentOccupancyTEU = 320, IsReeferBlock = false },
            new() { BlockCode = "R", Terminal = "Durban Container Terminal", Zone = "Reefer",  Rows = 4, Bays = 20, Tiers = 3, TotalCapacityTEU = 240,  CurrentOccupancyTEU = 195, IsReeferBlock = true },
            new() { BlockCode = "H", Terminal = "Durban Container Terminal", Zone = "Hazmat",  Rows = 3, Bays = 15, Tiers = 2, TotalCapacityTEU = 90,   CurrentOccupancyTEU = 42,  IsHazardousBlock = true },
            new() { BlockCode = "T", Terminal = "Durban Container Terminal", Zone = "Transit", Rows = 4, Bays = 25, Tiers = 3, TotalCapacityTEU = 300,  CurrentOccupancyTEU = 180, IsReeferBlock = false },
        };

        context.YardBlocks.AddRange(blocks);
        await context.SaveChangesAsync();
    }

    // ─── Gates ────────────────────────────────────────────────────────────────
    private static async Task SeedGatesAsync(SmartPortDbContext context)
    {
        if (await context.Gates.AnyAsync()) return;

        var gates = new List<Gate>
        {
            new() { Code = "G1", Name = "Gate 1 – Main Entry",    Terminal = "DCT", IsOperational = true,  IsEntryGate = true,  IsExitGate = false, LaneCount = 4, HasWeighbridge = true,  HasOCR = true,  HasRFID = true,  CurrentQueueCount = 12, AverageProcessingMinutes = 7  },
            new() { Code = "G2", Name = "Gate 2 – Main Exit",     Terminal = "DCT", IsOperational = true,  IsEntryGate = false, IsExitGate = true,  LaneCount = 3, HasWeighbridge = false, HasOCR = true,  HasRFID = true,  CurrentQueueCount = 8,  AverageProcessingMinutes = 5  },
            new() { Code = "G3", Name = "Gate 3 – Emergency",     Terminal = "DCT", IsOperational = false, IsEntryGate = true,  IsExitGate = true,  LaneCount = 1, HasWeighbridge = false, HasOCR = false, HasRFID = false, CurrentQueueCount = 0,  AverageProcessingMinutes = 12 },
            new() { Code = "G4", Name = "Gate 4 – Export Fast",   Terminal = "DCT", IsOperational = true,  IsEntryGate = true,  IsExitGate = false, LaneCount = 2, HasWeighbridge = true,  HasOCR = true,  HasRFID = true,  CurrentQueueCount = 6,  AverageProcessingMinutes = 5  },
        };

        context.Gates.AddRange(gates);
        await context.SaveChangesAsync();
    }

    // ─── Vessels ──────────────────────────────────────────────────────────────
    private static async Task SeedVesselsAsync(SmartPortDbContext context)
    {
        if (await context.Vessels.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var vessels = new List<Vessel>
        {
            // Currently berthed
            new() {
                IMONumber = "IMO9321483", MMSINumber = "636015816", CallSign = "D5GE3", Name = "MSC SARACEN", FlagCountry = "Liberia", FlagCode = "LR",
                VesselType = VesselType.ContainerShip, ShippingLine = "MSC", Operator = "MSC Mediterranean Shipping Company", Agent = "Med-Shipping Agencies SA",
                GrossTonnage = 140000, DeadweightTonnage = 153000, LengthOverall = 366, Beam = 51.2m, MaxDraught = 15.5m, TEUCapacity = 14000, YearBuilt = 2011,
                Status = VesselStatus.CargoOperations, CurrentDraught = 13.2m,
                EstimatedTimeOfArrival = now.AddDays(-1), ActualTimeOfArrival = now.AddDays(-1).AddHours(2),
                EstimatedTimeOfDeparture = now.AddHours(6), VoyageNumber = "FE622N", PreviousPort = "Singapore", NextPort = "Cape Town",
                PortOfRegistry = "Monrovia", TurnaroundHours = 38, DelayMinutes = 90, DelayReason = "Crane breakdown on Berth 1", PilotageRequired = true, TugAssistanceRequired = true
            },
            new() {
                IMONumber = "IMO9462008", MMSINumber = "538005068", CallSign = "V7YC6", Name = "CMA CGM CALLISTO", FlagCountry = "Marshall Islands", FlagCode = "MH",
                VesselType = VesselType.ContainerShip, ShippingLine = "CMA CGM", Operator = "CMA CGM S.A.", Agent = "Rennies Ships Agency",
                GrossTonnage = 131000, DeadweightTonnage = 143000, LengthOverall = 347, Beam = 45.6m, MaxDraught = 14.5m, TEUCapacity = 11902, YearBuilt = 2011,
                Status = VesselStatus.Berthed, CurrentDraught = 12.8m,
                EstimatedTimeOfArrival = now.AddDays(-2), ActualTimeOfArrival = now.AddDays(-2).AddHours(1),
                EstimatedTimeOfDeparture = now.AddHours(18), VoyageNumber = "0AX2NE",
                PortOfRegistry = "Majuro", TurnaroundHours = 42, PilotageRequired = true, TugAssistanceRequired = false
            },
            // Arriving
            new() {
                IMONumber = "IMO9276011", MMSINumber = "305182000", CallSign = "V2BB7", Name = "MAERSK EDMONTON", FlagCountry = "Antigua & Barbuda", FlagCode = "AG",
                VesselType = VesselType.ContainerShip, ShippingLine = "Maersk Line", Operator = "A.P. Møller-Mærsk A/S", Agent = "Barloworld Logistics",
                GrossTonnage = 94724, DeadweightTonnage = 109000, LengthOverall = 332, Beam = 42.8m, MaxDraught = 14.0m, TEUCapacity = 8690, YearBuilt = 2004,
                Status = VesselStatus.Approaching, CurrentDraught = 13.1m, CurrentLatitude = -29.1m, CurrentLongitude = 31.0m, SpeedOverGround = 12.4m,
                EstimatedTimeOfArrival = now.AddHours(8), EstimatedTimeOfDeparture = now.AddHours(48),
                VoyageNumber = "120S", PreviousPort = "Mombasa", PilotageRequired = true, TugAssistanceRequired = false
            },
            new() {
                IMONumber = "IMO9484007", MMSINumber = "636019478", CallSign = "A8MZ9", Name = "EVER GREET", FlagCountry = "Liberia", FlagCode = "LR",
                VesselType = VesselType.ContainerShip, ShippingLine = "Evergreen Line", Operator = "Evergreen Marine Corp.", Agent = "Evergreen Shipping Agency SA",
                GrossTonnage = 97571, DeadweightTonnage = 104750, LengthOverall = 335, Beam = 43.2m, MaxDraught = 14.2m, TEUCapacity = 8452, YearBuilt = 2010,
                Status = VesselStatus.AtAnchor, CurrentDraught = 12.9m, CurrentLatitude = -29.5m, CurrentLongitude = 31.2m,
                EstimatedTimeOfArrival = now.AddHours(-2), ActualTimeOfArrival = now.AddHours(-2),
                EstimatedTimeOfDeparture = now.AddHours(38), VoyageNumber = "0380-024W",
                DelayMinutes = 180, DelayReason = "No berth available – waiting at anchorage", PilotageRequired = true
            },
            // Tanker
            new() {
                IMONumber = "IMO9337727", MMSINumber = "566949000", CallSign = "9V2961", Name = "FORMOSAPRODUCT PIONEER", FlagCountry = "Singapore", FlagCode = "SG",
                VesselType = VesselType.TankerProduct, ShippingLine = "Formosa Petrochemical", Operator = "Formosa Petrochemical Corp.", Agent = "GAC South Africa",
                GrossTonnage = 30058, DeadweightTonnage = 46990, LengthOverall = 182, Beam = 32.2m, MaxDraught = 12.0m, TEUCapacity = 0, YearBuilt = 2006,
                Status = VesselStatus.CargoOperations, CurrentDraught = 11.5m,
                EstimatedTimeOfArrival = now.AddDays(-1), ActualTimeOfArrival = now.AddDays(-1),
                EstimatedTimeOfDeparture = now.AddHours(12), VoyageNumber = "FP2024-118",
                PilotageRequired = true, TugAssistanceRequired = true
            },
            // Departed (historical for analytics)
            new() {
                IMONumber = "IMO9387575", MMSINumber = "477306800", CallSign = "VRJX6", Name = "COSCO SHIPPING TAURUS", FlagCountry = "Hong Kong", FlagCode = "HK",
                VesselType = VesselType.ContainerShip, ShippingLine = "COSCO Shipping", Operator = "COSCO Shipping Lines Co.", Agent = "COSCO Shipping Lines SA",
                GrossTonnage = 153115, DeadweightTonnage = 165400, LengthOverall = 400, Beam = 58.6m, MaxDraught = 16.0m, TEUCapacity = 13386, YearBuilt = 2007,
                Status = VesselStatus.Departed, CurrentDraught = 10.0m,
                EstimatedTimeOfArrival = now.AddDays(-5), ActualTimeOfArrival = now.AddDays(-5).AddHours(1),
                EstimatedTimeOfDeparture = now.AddDays(-3), ActualTimeOfDeparture = now.AddDays(-3).AddHours(2),
                VoyageNumber = "071E", TurnaroundHours = 46
            },
        };

        context.Vessels.AddRange(vessels);
        await context.SaveChangesAsync();

        // Berth Assignments
        var berthAssignments = new List<BerthAssignment>
        {
            new() { VesselId = 1, BerthId = 1, PlannedArrival = now.AddDays(-1), PlannedDeparture = now.AddHours(8),  ActualArrival = now.AddDays(-1).AddHours(2), OperationalStatus = VesselStatus.CargoOperations, PlannedDischarge = 2800, PlannedLoad = 1400, ActualDischarge = 2600, ActualLoad = 900, CargoPlanApproved = true,  DelayMinutes = 90, DelayCategory = "Equipment" },
            new() { VesselId = 2, BerthId = 2, PlannedArrival = now.AddDays(-2), PlannedDeparture = now.AddHours(18), ActualArrival = now.AddDays(-2).AddHours(1), OperationalStatus = VesselStatus.Berthed,          PlannedDischarge = 2200, PlannedLoad = 2100, ActualDischarge = 2200, ActualLoad = 1800, CargoPlanApproved = true  },
            new() { VesselId = 3, BerthId = 4, PlannedArrival = now.AddHours(8),  PlannedDeparture = now.AddHours(56), OperationalStatus = VesselStatus.Expected,         PlannedDischarge = 3200, PlannedLoad = 2800, CargoPlanApproved = false },
            new() { VesselId = 5, BerthId = 7, PlannedArrival = now.AddDays(-1), PlannedDeparture = now.AddHours(12), ActualArrival = now.AddDays(-1), OperationalStatus = VesselStatus.CargoOperations, CargoPlanApproved = true  },
        };

        context.BerthAssignments.AddRange(berthAssignments);
        await context.SaveChangesAsync();
    }

    // ─── Containers ───────────────────────────────────────────────────────────
    private static async Task SeedContainersAsync(SmartPortDbContext context)
    {
        if (await context.Containers.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var rng = new Random(42);
        var shippingLines = new[] { "MAEU", "MSCU", "CMAU", "OOLU", "EVGU", "COSU" };
        var ports = new[] { "CNSHA", "SGSIN", "MYKUL", "KEYSB", "GBFXT", "NLRTM" };
        var containers = new List<Container>();

        for (int i = 1; i <= 120; i++)
        {
            var line = shippingLines[rng.Next(shippingLines.Length)];
            var num = $"{line}{rng.Next(1000000, 9999999)}";
            var direction = (ContainerDirection)(rng.Next(3));
            var size = rng.Next(3) < 2 ? ContainerSize.FEU40 : ContainerSize.TEU20;
            var status = (ContainerStatus)(rng.Next(0, 6));
            var isReefer = rng.Next(10) == 0;
            var isHaz = rng.Next(15) == 0;

            containers.Add(new Container
            {
                ContainerNumber = num,
                Size = size,
                ContainerType = isReefer ? ContainerType.Reefer : isHaz ? ContainerType.Hazardous : ContainerType.DryGeneral,
                Direction = direction,
                Status = status,
                VesselId = (i % 5 == 0) ? null : (i % 4 + 1),
                YardBlockId = rng.Next(1, 7),
                YardSlot = $"{(char)('A' + rng.Next(6))}{rng.Next(1, 40):D2}-{rng.Next(1, 5):D2}-{rng.Next(1, 5)}",
                ShippingLine = line,
                PortOfLoading = ports[rng.Next(ports.Length)],
                PortOfDischarge = "ZADUR",
                FinalDestination = new[] { "Johannesburg", "Pretoria", "Cape Town", "Bloemfontein", "Harare" }[rng.Next(5)],
                GrossWeightKg = rng.Next(5000, 28000),
                IsHazardous = isHaz,
                IsReefer = isReefer,
                ReeferSetTemp = isReefer ? (decimal?)rng.Next(-20, 8) : null,
                CustomsStatus = (ComplianceStatus)(rng.Next(5)),
                IsOnHold = isHaz && rng.Next(3) == 0,
                GateInDateTime = status >= ContainerStatus.GateIn ? now.AddDays(-rng.Next(1, 8)) : null,
                DwellTimeHours = status == ContainerStatus.InYard ? rng.Next(12, 96) : null,
                FreeTimeLimitHours = 72,
                IsDwellAlertRaised = false,
                CreatedAt = now.AddDays(-rng.Next(1, 14))
            });
        }

        // Set dwell alerts for containers exceeding free time
        foreach (var c in containers.Where(c => c.DwellTimeHours > c.FreeTimeLimitHours))
            c.IsDwellAlertRaised = true;

        context.Containers.AddRange(containers);
        await context.SaveChangesAsync();
    }

    // ─── Incidents ────────────────────────────────────────────────────────────
    private static async Task SeedIncidentsAsync(SmartPortDbContext context)
    {
        if (await context.Incidents.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var incidents = new List<Incident>
        {
            new() { ReferenceNumber = "INC-2025-00124", Title = "Crane #3 Hydraulic Failure – Berth 1", Description = "Ship-to-shore crane No.3 at DCT Berth 1 experienced hydraulic system failure at 06:20. Crane taken out of service. Operations continuing with 3 cranes.", Category = IncidentCategory.Equipment, Severity = IncidentSeverity.High, Status = IncidentStatus.InProgress, VesselId = 1, BerthId = 1, Location = "DCT Berth 1", Terminal = "DCT", ReportedBy = "Thabo Molefe", AssignedTo = "Maintenance Team", OccurredAt = now.AddHours(-4), TargetResolutionTime = now.AddHours(4), AcknowledgedAt = now.AddHours(-3.5), AcknowledgedBy = "Nomvula Dlamini" },
            new() { ReferenceNumber = "INC-2025-00123", Title = "Reefer Container Temperature Exceedance", Description = "Container MSCU7814203 reefer alarm triggered – temperature reading 4°C above setpoint for 45 minutes. Reefer technician dispatched.", Category = IncidentCategory.Operational, Severity = IncidentSeverity.Medium, Status = IncidentStatus.InProgress, Terminal = "DCT", Location = "Block R, Row 3", ReportedBy = "System", AssignedTo = "Reefer Technician", OccurredAt = now.AddHours(-2), TargetResolutionTime = now.AddHours(2) },
            new() { ReferenceNumber = "INC-2025-00122", Title = "Truck Involved in Near-Miss at Gate 1", Description = "A loaded tri-axle truck overshot the stop line at Gate 1 Lane 3 at 14:15. No injuries. Full CCTV investigation underway. Offending driver escorted off terminal.", Category = IncidentCategory.Safety, Severity = IncidentSeverity.Medium, Status = IncidentStatus.Acknowledged, Terminal = "DCT", Location = "Gate 1 – Lane 3", ReportedBy = "Gate Operator M. Zulu", AssignedTo = "Safety Officer", OccurredAt = now.AddHours(-6), TargetResolutionTime = now.AddHours(18), AcknowledgedAt = now.AddHours(-5), AcknowledgedBy = "Nomvula Dlamini" },
            new() { ReferenceNumber = "INC-2025-00121", Title = "Hazmat Container Leak – Block H", Description = "Minor chemical leak detected from container CMAU4418299 (Class 8 Corrosives). Area cordoned off, spill kit deployed, SAPS notified per protocol.", Category = IncidentCategory.Environmental, Severity = IncidentSeverity.Critical, Status = IncidentStatus.Open, Terminal = "DCT", Location = "Block H – Row 1", ReportedBy = "Yard Tractor Operator", OccurredAt = now.AddHours(-1), TargetResolutionTime = now.AddHours(3) },
            new() { ReferenceNumber = "INC-2025-00119", Title = "IT System Connectivity Issue – TOS", Description = "Navis N4 TOS experienced intermittent connectivity to gate kiosk system between 02:00–03:30. 6 gate transactions processed manually. Issue resolved by ICT team.", Category = IncidentCategory.Cyber, Severity = IncidentSeverity.Low, Status = IncidentStatus.Resolved, Terminal = "DCT", ReportedBy = "ICT On-Call", ResolvedBy = "ICT Team", OccurredAt = now.AddDays(-1).AddHours(2), ResolvedAt = now.AddDays(-1).AddHours(4), ResolutionNotes = "Network switch rebooted – root cause under investigation" },
        };

        context.Incidents.AddRange(incidents);
        await context.SaveChangesAsync();
    }

    // ─── Alerts ───────────────────────────────────────────────────────────────
    private static async Task SeedAlertsAsync(SmartPortDbContext context)
    {
        if (await context.Alerts.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var alerts = new List<Alert>
        {
            new() { AlertType = AlertType.VesselDelay,          Status = AlertStatus.Active,       Severity = IncidentSeverity.High,     Title = "EVER GREET – Berth Delay 3h+",          Message = "Vessel EVER GREET waiting at anchorage for 3+ hours. Berth 4 currently occupied. ETA for berthing: 22:00.", RelatedVesselId = 4, IsAutoGenerated = true, CreatedAt = now.AddHours(-1) },
            new() { AlertType = AlertType.ContainerDwellExceeded, Status = AlertStatus.Active,     Severity = IncidentSeverity.Medium,   Title = "14 Containers Exceeding 72h Free Time",  Message = "14 import containers in Block A have exceeded the 72-hour free time limit. Demurrage accruing. Notification sent to agents.", IsAutoGenerated = true, CreatedAt = now.AddHours(-3) },
            new() { AlertType = AlertType.TruckQueueCritical,   Status = AlertStatus.Acknowledged, Severity = IncidentSeverity.High,     Title = "Gate 1 Queue: 12 Trucks",               Message = "Gate 1 entry queue has reached 12 vehicles. Average wait time 48 min. Consider activating Gate 4 for additional capacity.", IsAutoGenerated = true, CreatedAt = now.AddHours(-2), AcknowledgedBy = "Nomvula Dlamini", AcknowledgedAt = now.AddHours(-1) },
            new() { AlertType = AlertType.WeatherAdvisory,      Status = AlertStatus.Active,       Severity = IncidentSeverity.Low,      Title = "SAWS Advisory: Swells 3.5–4.5m Tonight", Message = "SAWS weather advisory in effect: 3.5–4.5m swells expected off Bluff 18:00–06:00. Pilotage may be restricted after 20:00.", IsAutoGenerated = true, CreatedAt = now.AddHours(-4) },
            new() { AlertType = AlertType.DocumentOverdue,      Status = AlertStatus.Active,       Severity = IncidentSeverity.Medium,   Title = "MSC SARACEN – DG Declaration Missing",  Message = "Dangerous Goods Declaration for Voyage FE622N has not been received. 6 containers with IMDG cargo on vessel. Required prior to departure.", RelatedVesselId = 1, IsAutoGenerated = true, CreatedAt = now.AddHours(-5) },
            new() { AlertType = AlertType.BerthConflict,        Status = AlertStatus.Active,       Severity = IncidentSeverity.High,     Title = "Berth 4 Double-Booking Detected",       Message = "Scheduling conflict detected: MAERSK EDMONTON and COSCO SHIPPING ORION both allocated Berth 4 for 08:00–18:00 window. Ops Manager review required.", IsAutoGenerated = true, CreatedAt = now.AddHours(-1) },
            new() { AlertType = AlertType.CustomsHold,          Status = AlertStatus.Active,       Severity = IncidentSeverity.Medium,   Title = "SARS Hold: 3 Containers Released Pending", Message = "3 containers under SARS customs hold have release authorisation pending for >24h. Agents notified. Containers blocking yard slot allocation.", IsAutoGenerated = true, CreatedAt = now.AddHours(-8) },
        };

        context.Alerts.AddRange(alerts);
        await context.SaveChangesAsync();
    }

    // ─── Documents ────────────────────────────────────────────────────────────
    private static async Task SeedDocumentsAsync(SmartPortDbContext context)
    {
        if (await context.Documents.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var docs = new List<Document>
        {
            new() { DocumentNumber = "BL-MSC-FE622N-001", DocumentType = DocumentType.BillOfLading,         Status = DocumentStatus.Approved,     ComplianceStatus = ComplianceStatus.Compliant,    Title = "MSC SARACEN – Master B/L FE622N/001", IssuingAuthority = "MSC Mediterranean", SubmittedBy = "Med-Shipping Agencies SA", VesselId = 1, IssuedDate = now.AddDays(-3), SubmittedDate = now.AddDays(-2), ApprovedDate = now.AddDays(-1) },
            new() { DocumentNumber = "MNF-FE622N-2025",   DocumentType = DocumentType.Manifest,             Status = DocumentStatus.Approved,     ComplianceStatus = ComplianceStatus.Compliant,    Title = "MSC SARACEN – Cargo Manifest FE622N",   IssuingAuthority = "MSC Mediterranean", SubmittedBy = "Med-Shipping Agencies SA", VesselId = 1, IssuedDate = now.AddDays(-2), ApprovedDate = now.AddDays(-1) },
            new() { DocumentNumber = "DGD-FE622N-2025",   DocumentType = DocumentType.DangerousGoodsDeclaration, Status = DocumentStatus.Required, ComplianceStatus = ComplianceStatus.NonCompliant, Title = "MSC SARACEN – DG Declaration FE622N",   IssuingAuthority = "Shipper", VesselId = 1, RequiredByDate = now.AddHours(-2), SubmittedBy = "" },
            new() { DocumentNumber = "VCL-FE622N-2025",   DocumentType = DocumentType.VesselClearance,      Status = DocumentStatus.UnderReview,  ComplianceStatus = ComplianceStatus.PendingReview, Title = "MSC SARACEN – Arrival Clearance",       IssuingAuthority = "SAMSA", SubmittedBy = "Med-Shipping Agencies SA", VesselId = 1, SubmittedDate = now.AddHours(-6), RequiredByDate = now.AddHours(2) },
            new() { DocumentNumber = "CSD-0AX2NE-001",    DocumentType = DocumentType.CustomsDeclaration,   Status = DocumentStatus.Approved,     ComplianceStatus = ComplianceStatus.Compliant,    Title = "CMA CGM CALLISTO – Customs Entry",      IssuingAuthority = "SARS Customs", SubmittedBy = "Rennies Ships Agency", VesselId = 2, IssuedDate = now.AddDays(-2), ApprovedDate = now.AddDays(-1) },
            new() { DocumentNumber = "PHY-0AX2NE-2025",   DocumentType = DocumentType.PhytosanitaryCertificate, Status = DocumentStatus.Submitted, ComplianceStatus = ComplianceStatus.PendingReview, Title = "CMA CGM CALLISTO – Phyto Certificate",  IssuingAuthority = "DAFF", SubmittedBy = "Consignee ABC Pty Ltd", VesselId = 2, SubmittedDate = now.AddHours(-12), RequiredByDate = now.AddHours(6) },
        };

        context.Documents.AddRange(docs);
        await context.SaveChangesAsync();
    }

    // ─── Recommendations ──────────────────────────────────────────────────────
    private static async Task SeedRecommendationsAsync(SmartPortDbContext context)
    {
        if (await context.Recommendations.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var recs = new List<Recommendation>
        {
            new() { Type = RecommendationType.BerthReallocation, Status = RecommendationStatus.Pending, Priority = IncidentSeverity.High, Title = "Reallocate EVER GREET to Berth 3", Summary = "EVER GREET has waited 3h+ at anchorage. Berth 3 is available and vessel dimensions are compatible.", DetailedRationale = "Berth 3 (DCT-B3) is currently unoccupied with 300m available alongside. EVER GREET LOA is 335m – requires waiver approval or partial berth use. Opportunity to reduce anchorage wait and improve turnaround KPI by ~4 hours.", SuggestedAction = "Issue berth order for DCT-B3 to EVER GREET. Coordinate pilot and 2 tugs for berthing at 19:30. Notify shipping agent.", ImpactEstimate = "Reduces vessel anchorage time by ~4h, improves daily berth utilisation by 12%", RelatedVesselId = 4, RelatedBerthId = 3, TriggerMetric = "AnchorageWaitHours", TriggerValue = 3.1m, ThresholdValue = 2.0m, IsAIGenerated = true, ExpiresAt = now.AddHours(4) },
            new() { Type = RecommendationType.TruckQueueMitigation, Status = RecommendationStatus.Accepted, Priority = IncidentSeverity.High, Title = "Activate Gate 4 to Reduce Queue", Summary = "Gate 1 queue of 12 trucks exceeds operational threshold. Gate 4 is operational and can immediately accept export truck entries.", DetailedRationale = "Current Gate 1 queue at 12 vehicles represents a 48-minute average wait. Gate 4 (Export Fast Lane) has 2 lanes, weighbridge, and OCR. Can process 8 trucks/hour. Activating Gate 4 will reduce wait time to ~18 minutes within 30 minutes.", SuggestedAction = "Deploy 1 additional gate operator to Gate 4. Update TOS gate routing to split export trucks to G4. Notify transporters via TOPS portal.", ImpactEstimate = "Reduces truck wait time from 48min to ~18min. Clears current queue in ~45 minutes.", TriggerMetric = "GateQueueCount", TriggerValue = 12, ThresholdValue = 8, IsAIGenerated = true, ActedOnBy = "Nomvula Dlamini", ActedOnAt = now.AddHours(-1), ActedOnNotes = "Gate 4 activated. Additional operator deployed." },
            new() { Type = RecommendationType.ContainerPrioritisation, Status = RecommendationStatus.Pending, Priority = IncidentSeverity.Medium, Title = "Prioritise 14 Dwell-Exceeded Containers", Summary = "14 import containers in Block A have exceeded 72h free time. Expedited agent notification and customs escalation recommended.", DetailedRationale = "Containers in long dwell are occupying premium yard slots and accruing port demurrage. SARS customs system shows 6 of these have released status but agent has not collected. 8 remain in customs review.", SuggestedAction = "1. Send automated demurrage notice to all 14 agents.\n2. Escalate 6 released containers to TOPS for agent alert.\n3. Escalate 8 in-customs containers to SARS e-portal.\n4. Consider moving to remote stack area if not cleared in 24h.", ImpactEstimate = "Frees ~18 TEU slots in Block A. Recovers ~R45,000 in demurrage revenue.", TriggerMetric = "DwellHours", TriggerValue = 84, ThresholdValue = 72, IsAIGenerated = true },
            new() { Type = RecommendationType.DocumentChase, Status = RecommendationStatus.Pending, Priority = IncidentSeverity.High, Title = "DG Declaration Required – MSC SARACEN Departure at Risk", Summary = "MSC SARACEN is scheduled to depart in 6 hours but DG Declaration for 6 containers is outstanding. Vessel cannot legally depart without this document.", DetailedRationale = "IMDG Code regulations require DGD submission prior to departure. Missing document for FE622N holds 6 hazardous containers. If not received within 3 hours, vessel departure will be delayed and incur R120,000+ port dues.", SuggestedAction = "1. Immediate phone contact with Med-Shipping Agencies SA (port agent).\n2. Request emergency submission via Navis N4 document portal.\n3. If not resolved in 2h, escalate to Port Authority and SAMSA.\n4. Consider offloading 6 DG containers if DGD cannot be produced.", ImpactEstimate = "Prevents 6h+ vessel delay. Avoids ~R120,000 additional port dues.", RelatedVesselId = 1, IsAIGenerated = true, ExpiresAt = now.AddHours(6) },
        };

        context.Recommendations.AddRange(recs);
        await context.SaveChangesAsync();
    }

    // ─── Operational Metrics ─────────────────────────────────────────────────
    private static async Task SeedOperationalMetricsAsync(SmartPortDbContext context)
    {
        if (await context.OperationalMetrics.AnyAsync()) return;

        var metrics = new List<OperationalMetric>();
        var rng = new Random(42);
        var terminal = "Durban Container Terminal";

        for (int daysBack = 90; daysBack >= 0; daysBack--)
        {
            var date = DateTime.UtcNow.AddDays(-daysBack).Date;
            var seasonalFactor = 1.0 + 0.15 * Math.Sin(daysBack / 30.0 * Math.PI);

            metrics.Add(new OperationalMetric { MetricDate = date, MetricType = "DailyThroughputTEU",    Terminal = terminal, Value = (decimal)(Math.Round(2800 * seasonalFactor + rng.Next(-200, 200))), Unit = "TEU" });
            metrics.Add(new OperationalMetric { MetricDate = date, MetricType = "AverageTurnaroundHours", Terminal = terminal, Value = (decimal)(Math.Round(32 + rng.NextDouble() * 8 - 4, 1)), Unit = "Hours" });
            metrics.Add(new OperationalMetric { MetricDate = date, MetricType = "BerthUtilisationPercent", Terminal = terminal, Value = (decimal)(Math.Round(78 + rng.NextDouble() * 15, 1)), Unit = "Percent" });
            metrics.Add(new OperationalMetric { MetricDate = date, MetricType = "CraneProductivity",      Terminal = terminal, Value = (decimal)(Math.Round(24 + rng.NextDouble() * 6, 1)), Unit = "MovesPerHour" });
            metrics.Add(new OperationalMetric { MetricDate = date, MetricType = "TruckTurnaround",        Terminal = terminal, Value = 45 + rng.Next(-10, 30), Unit = "Minutes" });
            metrics.Add(new OperationalMetric { MetricDate = date, MetricType = "YardDensity",            Terminal = terminal, Value = (decimal)(Math.Round(72 + rng.NextDouble() * 15, 1)), Unit = "Percent" });
            metrics.Add(new OperationalMetric { MetricDate = date, MetricType = "VesselsServed",          Terminal = terminal, Value = rng.Next(3, 7), Unit = "Count" });
            metrics.Add(new OperationalMetric { MetricDate = date, MetricType = "DelayedVessels",         Terminal = terminal, Value = rng.Next(0, 3), Unit = "Count" });
        }

        context.OperationalMetrics.AddRange(metrics);
        await context.SaveChangesAsync();
    }
}
