using Microsoft.EntityFrameworkCore;
using SmartPort.Domain.Entities;
using SmartPort.Domain.Enums;

namespace SmartPort.Infrastructure.Persistence;

/// <summary>
/// Seeds Culltron Smart Port Flow demo data for the KwaZulu-Natal pilot environment.
/// All data is realistic but fictional and intended for controlled demonstration use.
/// </summary>
public static class CulltronSeedData
{
    public static async Task SeedAsync(SmartPortDbContext db)
    {
        await SeedOrganisationsAsync(db);
        await SeedDriversAsync(db);
        await SeedFleetVehiclesAsync(db);
        await SeedDispatchTripsAsync(db);
        await SeedDisruptionEventsAsync(db);
        await SeedFlowRecommendationsAsync(db);
        await SeedEmissionEstimatesAsync(db);
        await SeedPilotMetricsAsync(db);
    }

    // ─── Organisations ────────────────────────────────────────────────────────

    private static async Task SeedOrganisationsAsync(SmartPortDbContext db)
    {
        if (await db.Organisations.AnyAsync()) return;

        db.Organisations.AddRange(
            new Organisation {
                Name = "Bayhead Freight Coordination",
                OrganisationType = OrganisationType.LogisticsCompany,
                RegistrationNumber = "2018/123456/07",
                ContactPerson = "Thulani Mkhize",
                ContactEmail = "ops@bayheadfreight.co.za",
                ContactPhone = "+27 31 401 0011",
                Address = "12 Bayhead Road, Durban South Industrial",
                Province = "KwaZulu-Natal",
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System"
            },
            new Organisation {
                Name = "Durban South Basin Logistics",
                OrganisationType = OrganisationType.Haulier,
                RegistrationNumber = "2015/654321/07",
                ContactPerson = "Priya Naidoo",
                ContactEmail = "dispatch@dsblogistics.co.za",
                ContactPhone = "+27 31 465 7700",
                Address = "45 Settler's Way, Clairwood, Durban",
                Province = "KwaZulu-Natal",
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System"
            },
            new Organisation {
                Name = "KZN Cold Chain Movers",
                OrganisationType = OrganisationType.LogisticsCompany,
                RegistrationNumber = "2020/987654/07",
                ContactPerson = "Sipho Dlamini",
                ContactEmail = "coldchain@kzncoldchain.co.za",
                ContactPhone = "+27 31 705 8800",
                Address = "Unit 7, Pinetown Logistics Park, Pinetown",
                Province = "KwaZulu-Natal",
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System"
            },
            new Organisation {
                Name = "Richards Bay Industrial Haulage",
                OrganisationType = OrganisationType.Haulier,
                RegistrationNumber = "2012/112233/07",
                ContactPerson = "Johan van der Merwe",
                ContactEmail = "jvdm@rbih.co.za",
                ContactPhone = "+27 35 789 0022",
                Address = "33 Aluminium Road, Richards Bay Industrial Zone",
                Province = "KwaZulu-Natal",
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System"
            },
            new Organisation {
                Name = "eThekwini Corridor Monitoring Unit",
                OrganisationType = OrganisationType.Municipality,
                ContactPerson = "Nomvula Khumalo",
                ContactEmail = "corridors@ethekwini.gov.za",
                ContactPhone = "+27 31 311 0000",
                Address = "166 KE Masinga Road, Durban CBD",
                Province = "KwaZulu-Natal",
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System"
            }
        );
        await db.SaveChangesAsync();
    }

    // ─── Drivers ──────────────────────────────────────────────────────────────

    private static async Task SeedDriversAsync(SmartPortDbContext db)
    {
        if (await db.Drivers.AnyAsync()) return;

        var orgs = await db.Organisations.ToListAsync();
        int bayheadId = orgs.First(o => o.Name.Contains("Bayhead")).Id;
        int dsbId     = orgs.First(o => o.Name.Contains("Durban South")).Id;
        int kznId     = orgs.First(o => o.Name.Contains("Cold Chain")).Id;
        int rbihId    = orgs.First(o => o.Name.Contains("Richards Bay")).Id;

        db.Drivers.AddRange(
            new Driver { OrganisationId = bayheadId, FullName = "Mandla Zulu",       PhoneNumber = "+27 82 111 2233", LicenceNumber = "DRV-KZN-00141", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Driver { OrganisationId = bayheadId, FullName = "Bongani Ntuli",      PhoneNumber = "+27 82 344 5566", LicenceNumber = "DRV-KZN-00182", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Driver { OrganisationId = dsbId,     FullName = "Thabo Shabalala",    PhoneNumber = "+27 83 455 6677", LicenceNumber = "DRV-KZN-00263", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Driver { OrganisationId = dsbId,     FullName = "Lungelo Mthembu",    PhoneNumber = "+27 83 566 7788", LicenceNumber = "DRV-KZN-00284", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Driver { OrganisationId = kznId,     FullName = "Ayanda Cele",        PhoneNumber = "+27 71 677 8899", LicenceNumber = "DRV-KZN-00315", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Driver { OrganisationId = kznId,     FullName = "Nokukhanya Sithole", PhoneNumber = "+27 71 788 9900", LicenceNumber = "DRV-KZN-00346", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Driver { OrganisationId = rbihId,    FullName = "Pieter Fourie",      PhoneNumber = "+27 82 899 0011", LicenceNumber = "DRV-KZN-00417", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Driver { OrganisationId = rbihId,    FullName = "Devlin Pillay",      PhoneNumber = "+27 84 900 1122", LicenceNumber = "DRV-KZN-00438", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" }
        );
        await db.SaveChangesAsync();
    }

    // ─── Fleet Vehicles ───────────────────────────────────────────────────────

    private static async Task SeedFleetVehiclesAsync(SmartPortDbContext db)
    {
        if (await db.FleetVehicles.AnyAsync()) return;

        var orgs = await db.Organisations.ToListAsync();
        int bayheadId = orgs.First(o => o.Name.Contains("Bayhead")).Id;
        int dsbId     = orgs.First(o => o.Name.Contains("Durban South")).Id;
        int kznId     = orgs.First(o => o.Name.Contains("Cold Chain")).Id;
        int rbihId    = orgs.First(o => o.Name.Contains("Richards Bay")).Id;

        db.FleetVehicles.AddRange(
            // Bayhead Freight - 4 vehicles
            new FleetVehicle { OrganisationId = bayheadId, RegistrationNumber = "ND 114 427", FleetNumber = "BFC-001", VehicleType = FleetVehicleType.ArticulatedTruck, CargoType = FleetCargoType.Container,      CapacityTons = 30m, Status = FleetVehicleStatus.Dispatched,  CurrentLocation = "Bayhead Road Corridor",    IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = bayheadId, RegistrationNumber = "ND 287 530", FleetNumber = "BFC-002", VehicleType = FleetVehicleType.ArticulatedTruck, CargoType = FleetCargoType.Container,      CapacityTons = 30m, Status = FleetVehicleStatus.Waiting,    CurrentLocation = "DCT Gate Queue",           IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = bayheadId, RegistrationNumber = "ND 391 661", FleetNumber = "BFC-003", VehicleType = FleetVehicleType.RigidTruck,       CargoType = FleetCargoType.GeneralFreight, CapacityTons = 10m, Status = FleetVehicleStatus.Available,  CurrentLocation = "Bayhead Depot",            IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = bayheadId, RegistrationNumber = "ND 445 792", FleetNumber = "BFC-004", VehicleType = FleetVehicleType.Flatbed,           CargoType = FleetCargoType.BreakBulk,      CapacityTons = 25m, Status = FleetVehicleStatus.Completed,  CurrentLocation = "Clairwood Yard",           IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            // DSB Logistics - 4 vehicles
            new FleetVehicle { OrganisationId = dsbId,     RegistrationNumber = "ND 512 883", FleetNumber = "DSB-001", VehicleType = FleetVehicleType.Interlink,          CargoType = FleetCargoType.Container,      CapacityTons = 36m, Status = FleetVehicleStatus.AtGate,    CurrentLocation = "DCT Gate 1",               IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = dsbId,     RegistrationNumber = "ND 623 914", FleetNumber = "DSB-002", VehicleType = FleetVehicleType.ArticulatedTruck,   CargoType = FleetCargoType.Bulk,           CapacityTons = 32m, Status = FleetVehicleStatus.Dispatched, CurrentLocation = "N2 South Freight Corridor",IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = dsbId,     RegistrationNumber = "ND 734 045", FleetNumber = "DSB-003", VehicleType = FleetVehicleType.RigidTruck,         CargoType = FleetCargoType.GeneralFreight, CapacityTons = 12m, Status = FleetVehicleStatus.Available,  CurrentLocation = "Clairwood Depot",          IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = dsbId,     RegistrationNumber = "ND 845 176", FleetNumber = "DSB-004", VehicleType = FleetVehicleType.ArticulatedTruck,   CargoType = FleetCargoType.Container,      CapacityTons = 30m, Status = FleetVehicleStatus.Delayed,   CurrentLocation = "Island View / Bluff Route",IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            // KZN Cold Chain - 4 vehicles
            new FleetVehicle { OrganisationId = kznId,     RegistrationNumber = "ND 956 307", FleetNumber = "KCC-001", VehicleType = FleetVehicleType.ReeferTruck,        CargoType = FleetCargoType.ColdChain,      CapacityTons = 20m, Status = FleetVehicleStatus.Dispatched, CurrentLocation = "Island View / Bluff Route",IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = kznId,     RegistrationNumber = "ND 067 438", FleetNumber = "KCC-002", VehicleType = FleetVehicleType.ReeferTruck,        CargoType = FleetCargoType.ColdChain,      CapacityTons = 20m, Status = FleetVehicleStatus.Available,  CurrentLocation = "Pinetown Cold Store",      IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = kznId,     RegistrationNumber = "ND 178 569", FleetNumber = "KCC-003", VehicleType = FleetVehicleType.ReeferTruck,        CargoType = FleetCargoType.ColdChain,      CapacityTons = 18m, Status = FleetVehicleStatus.Waiting,   CurrentLocation = "DCT Reefer Queue",         IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = kznId,     RegistrationNumber = "ND 289 690", FleetNumber = "KCC-004", VehicleType = FleetVehicleType.ReeferTruck,        CargoType = FleetCargoType.ColdChain,      CapacityTons = 22m, Status = FleetVehicleStatus.Completed,  CurrentLocation = "Cato Ridge Cold Hub",      IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            // Richards Bay - 3 vehicles
            new FleetVehicle { OrganisationId = rbihId,    RegistrationNumber = "RB 334 721", FleetNumber = "RBI-001", VehicleType = FleetVehicleType.ArticulatedTruck,   CargoType = FleetCargoType.Bulk,           CapacityTons = 34m, Status = FleetVehicleStatus.Dispatched, CurrentLocation = "Richards Bay Industrial",  IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = rbihId,    RegistrationNumber = "RB 445 852", FleetNumber = "RBI-002", VehicleType = FleetVehicleType.Interlink,          CargoType = FleetCargoType.Bulk,           CapacityTons = 40m, Status = FleetVehicleStatus.Available,  CurrentLocation = "RB Port Access",           IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new FleetVehicle { OrganisationId = rbihId,    RegistrationNumber = "RB 556 983", FleetNumber = "RBI-003", VehicleType = FleetVehicleType.TankerTruck,        CargoType = FleetCargoType.Fuel,           CapacityTons = 28m, Status = FleetVehicleStatus.OutOfService, CurrentLocation = "RBIH Workshop",           IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System" }
        );
        await db.SaveChangesAsync();
    }

    // ─── Dispatch Trips ───────────────────────────────────────────────────────

    private static async Task SeedDispatchTripsAsync(SmartPortDbContext db)
    {
        if (await db.DispatchTrips.AnyAsync()) return;

        var orgs     = await db.Organisations.ToListAsync();
        var vehicles = await db.FleetVehicles.ToListAsync();
        var drivers  = await db.Drivers.ToListAsync();

        var bayhead = orgs.First(o => o.Name.Contains("Bayhead"));
        var dsb     = orgs.First(o => o.Name.Contains("Durban South"));
        var kzn     = orgs.First(o => o.Name.Contains("Cold Chain"));
        var rbih    = orgs.First(o => o.Name.Contains("Richards Bay"));

        var now = DateTime.UtcNow;

        var trips = new List<DispatchTrip>
        {
            // ── Bayhead trips
            new() {
                OrganisationId = bayhead.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "BFC-001").Id,
                DriverId = drivers.First(d => d.FullName == "Mandla Zulu").Id,
                Origin = "Bayhead Depot, Durban South", Destination = "Durban Container Terminal",
                RouteName = "Bayhead Road Corridor", CargoType = FleetCargoType.Container,
                CargoDescription = "Import containers — general cargo, 2 x 40ft", UrgencyLevel = TripUrgencyLevel.Normal,
                PlannedDispatchTime = now.AddHours(-2), ActualDispatchTime = now.AddHours(-1.8),
                PlannedArrivalWindowStart = now.AddHours(-1), PlannedArrivalWindowEnd = now.AddHours(0.5),
                ActualArrivalTime = now.AddMinutes(-15), GateInTime = now.AddMinutes(-10), GateOutTime = now.AddMinutes(40),
                Status = TripStatus.Completed, CreatedAt = now.AddHours(-6), CreatedBy = "System"
            },
            new() {
                OrganisationId = bayhead.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "BFC-002").Id,
                DriverId = drivers.First(d => d.FullName == "Bongani Ntuli").Id,
                Origin = "Bayhead Depot, Durban South", Destination = "Durban Container Terminal",
                RouteName = "Bayhead Road Corridor", CargoType = FleetCargoType.Container,
                CargoDescription = "Export container — automotive parts, 1 x 20ft", UrgencyLevel = TripUrgencyLevel.High,
                PlannedDispatchTime = now.AddMinutes(-45), PlannedArrivalWindowStart = now.AddMinutes(15),
                PlannedArrivalWindowEnd = now.AddMinutes(75), Status = TripStatus.Waiting,
                CreatedAt = now.AddHours(-3), CreatedBy = "System"
            },
            new() {
                OrganisationId = bayhead.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "BFC-003").Id,
                Origin = "Bayhead Depot, Durban South", Destination = "Island View Tank Farm",
                RouteName = "Island View / Bluff Route", CargoType = FleetCargoType.GeneralFreight,
                CargoDescription = "General freight — packed goods", UrgencyLevel = TripUrgencyLevel.Low,
                PlannedDispatchTime = now.AddHours(2), PlannedArrivalWindowStart = now.AddHours(3),
                PlannedArrivalWindowEnd = now.AddHours(4.5), Status = TripStatus.Planned,
                CreatedAt = now.AddHours(-1), CreatedBy = "System"
            },
            new() {
                OrganisationId = bayhead.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "BFC-004").Id,
                Origin = "Clairwood Yard", Destination = "Durban Container Terminal",
                RouteName = "Durban Container Terminal Access", CargoType = FleetCargoType.BreakBulk,
                CargoDescription = "Break-bulk machinery parts — oversized", UrgencyLevel = TripUrgencyLevel.High,
                PlannedDispatchTime = now.AddHours(-5), ActualDispatchTime = now.AddHours(-4.8),
                PlannedArrivalWindowStart = now.AddHours(-3.5), PlannedArrivalWindowEnd = now.AddHours(-2),
                ActualArrivalTime = now.AddHours(-1.5), GateInTime = now.AddHours(-1.3), GateOutTime = now.AddHours(-0.3),
                Status = TripStatus.Completed, CreatedAt = now.AddHours(-8), CreatedBy = "System"
            },
            // ── DSB Logistics trips
            new() {
                OrganisationId = dsb.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "DSB-001").Id,
                DriverId = drivers.First(d => d.FullName == "Thabo Shabalala").Id,
                Origin = "Clairwood Depot", Destination = "Durban Container Terminal",
                RouteName = "Durban Container Terminal Access", CargoType = FleetCargoType.Container,
                CargoDescription = "Import container — electronics, 1 x 40ft HC", UrgencyLevel = TripUrgencyLevel.Critical,
                PlannedDispatchTime = now.AddMinutes(-30), ActualDispatchTime = now.AddMinutes(-25),
                PlannedArrivalWindowStart = now.AddMinutes(10), PlannedArrivalWindowEnd = now.AddMinutes(50),
                GateInTime = now.AddMinutes(5), Status = TripStatus.AtGate,
                CreatedAt = now.AddHours(-2), CreatedBy = "System"
            },
            new() {
                OrganisationId = dsb.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "DSB-002").Id,
                DriverId = drivers.First(d => d.FullName == "Lungelo Mthembu").Id,
                Origin = "Clairwood Depot", Destination = "Cato Ridge Inland Depot",
                RouteName = "N2 South Freight Corridor", CargoType = FleetCargoType.Bulk,
                CargoDescription = "Agricultural bulk — grain transfer", UrgencyLevel = TripUrgencyLevel.Normal,
                PlannedDispatchTime = now.AddHours(-1), ActualDispatchTime = now.AddHours(-0.8),
                PlannedArrivalWindowStart = now.AddMinutes(30), PlannedArrivalWindowEnd = now.AddHours(2),
                Status = TripStatus.Dispatched, CreatedAt = now.AddHours(-4), CreatedBy = "System"
            },
            new() {
                OrganisationId = dsb.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "DSB-003").Id,
                Origin = "Clairwood Depot", Destination = "Bayhead Container Park",
                RouteName = "Bayhead Road Corridor", CargoType = FleetCargoType.GeneralFreight,
                CargoDescription = "Mixed general cargo, palletised", UrgencyLevel = TripUrgencyLevel.Normal,
                PlannedDispatchTime = now.AddHours(1), PlannedArrivalWindowStart = now.AddHours(2),
                PlannedArrivalWindowEnd = now.AddHours(3), Status = TripStatus.ReadyForDispatch,
                CreatedAt = now.AddHours(-1), CreatedBy = "System"
            },
            new() {
                OrganisationId = dsb.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "DSB-004").Id,
                Origin = "Island View Terminal", Destination = "Clairwood Depot",
                RouteName = "Island View / Bluff Route", CargoType = FleetCargoType.Container,
                CargoDescription = "Empty container reposition — 2 x 20ft", UrgencyLevel = TripUrgencyLevel.Low,
                PlannedDispatchTime = now.AddHours(-3), ActualDispatchTime = now.AddHours(-2.5),
                PlannedArrivalWindowStart = now.AddHours(-1.5), PlannedArrivalWindowEnd = now.AddMinutes(-30),
                ActualArrivalTime = now.AddMinutes(-10), Status = TripStatus.Delayed,
                Notes = "Delayed due to congestion on Bluff Road. Estimated 40 min late.",
                CreatedAt = now.AddHours(-5), CreatedBy = "System"
            },
            // ── KZN Cold Chain trips
            new() {
                OrganisationId = kzn.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "KCC-001").Id,
                DriverId = drivers.First(d => d.FullName == "Ayanda Cele").Id,
                Origin = "Pinetown Cold Store", Destination = "Durban Container Terminal — Reefer Zone",
                RouteName = "Island View / Bluff Route", CargoType = FleetCargoType.ColdChain,
                CargoDescription = "Cold-chain export — fresh produce, temp -2°C", UrgencyLevel = TripUrgencyLevel.Critical,
                PlannedDispatchTime = now.AddHours(-1), ActualDispatchTime = now.AddHours(-0.9),
                PlannedArrivalWindowStart = now.AddMinutes(5), PlannedArrivalWindowEnd = now.AddMinutes(45),
                Status = TripStatus.Dispatched, CreatedAt = now.AddHours(-3), CreatedBy = "System"
            },
            new() {
                OrganisationId = kzn.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "KCC-002").Id,
                DriverId = drivers.First(d => d.FullName == "Nokukhanya Sithole").Id,
                Origin = "Pinetown Cold Store", Destination = "Cato Ridge Cold Hub",
                RouteName = "Cato Ridge Inland Link", CargoType = FleetCargoType.ColdChain,
                CargoDescription = "Cold-chain domestic — dairy products, temp 4°C", UrgencyLevel = TripUrgencyLevel.High,
                PlannedDispatchTime = now.AddHours(0.5), PlannedArrivalWindowStart = now.AddHours(2),
                PlannedArrivalWindowEnd = now.AddHours(3), Status = TripStatus.RecommendedHold,
                Notes = "System recommends holding — load-shedding risk and route congestion detected.",
                CreatedAt = now.AddHours(-0.5), CreatedBy = "System"
            },
            new() {
                OrganisationId = kzn.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "KCC-003").Id,
                Origin = "DCT Reefer Zone", Destination = "Pinetown Cold Store",
                RouteName = "Durban Container Terminal Access", CargoType = FleetCargoType.ColdChain,
                CargoDescription = "Cold-chain import — frozen fish products", UrgencyLevel = TripUrgencyLevel.High,
                PlannedDispatchTime = now.AddMinutes(-60), ActualDispatchTime = now.AddMinutes(-50),
                PlannedArrivalWindowStart = now.AddMinutes(-20), PlannedArrivalWindowEnd = now.AddMinutes(20),
                Status = TripStatus.Waiting, CreatedAt = now.AddHours(-4), CreatedBy = "System"
            },
            new() {
                OrganisationId = kzn.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "KCC-004").Id,
                Origin = "Cato Ridge Cold Hub", Destination = "Pinetown Cold Store",
                RouteName = "Cato Ridge Inland Link", CargoType = FleetCargoType.ColdChain,
                CargoDescription = "Inbound cold-chain — pharmaceutical goods", UrgencyLevel = TripUrgencyLevel.Normal,
                PlannedDispatchTime = now.AddHours(-6), ActualDispatchTime = now.AddHours(-5.8),
                PlannedArrivalWindowStart = now.AddHours(-4), PlannedArrivalWindowEnd = now.AddHours(-3),
                ActualArrivalTime = now.AddHours(-3.2), GateInTime = now.AddHours(-3.0), GateOutTime = now.AddHours(-2.5),
                Status = TripStatus.Completed, CreatedAt = now.AddHours(-8), CreatedBy = "System"
            },
            // ── Richards Bay trips
            new() {
                OrganisationId = rbih.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "RBI-001").Id,
                DriverId = drivers.First(d => d.FullName == "Pieter Fourie").Id,
                Origin = "Richards Bay Industrial Zone", Destination = "Richards Bay Port Bulk Terminal",
                RouteName = "Richards Bay Industrial Corridor", CargoType = FleetCargoType.Bulk,
                CargoDescription = "Industrial bulk — aluminium pellets", UrgencyLevel = TripUrgencyLevel.Normal,
                PlannedDispatchTime = now.AddHours(-1.5), ActualDispatchTime = now.AddHours(-1.3),
                PlannedArrivalWindowStart = now.AddMinutes(-30), PlannedArrivalWindowEnd = now.AddMinutes(30),
                Status = TripStatus.Dispatched, CreatedAt = now.AddHours(-5), CreatedBy = "System"
            },
            new() {
                OrganisationId = rbih.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "RBI-002").Id,
                DriverId = drivers.First(d => d.FullName == "Devlin Pillay").Id,
                Origin = "RBIH Depot", Destination = "Richards Bay Port Bulk Terminal",
                RouteName = "Richards Bay Industrial Corridor", CargoType = FleetCargoType.Bulk,
                CargoDescription = "Coal bulk — thermal coal export", UrgencyLevel = TripUrgencyLevel.High,
                PlannedDispatchTime = now.AddHours(1), PlannedArrivalWindowStart = now.AddHours(2.5),
                PlannedArrivalWindowEnd = now.AddHours(4), Status = TripStatus.Planned,
                CreatedAt = now.AddHours(-1), CreatedBy = "System"
            },
            // ── Historical completed trips for analytics
            new() {
                OrganisationId = bayhead.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "BFC-001").Id,
                Origin = "Bayhead Depot", Destination = "DCT", RouteName = "Bayhead Road Corridor",
                CargoType = FleetCargoType.Container, UrgencyLevel = TripUrgencyLevel.Normal,
                PlannedDispatchTime = now.AddDays(-1).AddHours(8), ActualDispatchTime = now.AddDays(-1).AddHours(8.2),
                PlannedArrivalWindowStart = now.AddDays(-1).AddHours(9), PlannedArrivalWindowEnd = now.AddDays(-1).AddHours(10),
                ActualArrivalTime = now.AddDays(-1).AddHours(9.5), GateInTime = now.AddDays(-1).AddHours(9.6), GateOutTime = now.AddDays(-1).AddHours(10.8),
                Status = TripStatus.Completed, CreatedAt = now.AddDays(-1), CreatedBy = "System"
            },
            new() {
                OrganisationId = dsb.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "DSB-002").Id,
                Origin = "Clairwood Depot", Destination = "Cato Ridge", RouteName = "N2 South Freight Corridor",
                CargoType = FleetCargoType.Bulk, UrgencyLevel = TripUrgencyLevel.Low,
                PlannedDispatchTime = now.AddDays(-1).AddHours(14), ActualDispatchTime = now.AddDays(-1).AddHours(14.5),
                PlannedArrivalWindowStart = now.AddDays(-1).AddHours(16), PlannedArrivalWindowEnd = now.AddDays(-1).AddHours(17),
                ActualArrivalTime = now.AddDays(-1).AddHours(17.2),
                GateInTime = now.AddDays(-1).AddHours(17.3), GateOutTime = now.AddDays(-1).AddHours(18.1),
                Status = TripStatus.Completed, CreatedAt = now.AddDays(-1), CreatedBy = "System"
            },
            new() {
                OrganisationId = kzn.Id, FleetVehicleId = vehicles.First(v => v.FleetNumber == "KCC-001").Id,
                Origin = "Pinetown Cold Store", Destination = "DCT Reefer", RouteName = "Island View / Bluff Route",
                CargoType = FleetCargoType.ColdChain, UrgencyLevel = TripUrgencyLevel.Critical,
                PlannedDispatchTime = now.AddDays(-2).AddHours(6), ActualDispatchTime = now.AddDays(-2).AddHours(6.1),
                PlannedArrivalWindowStart = now.AddDays(-2).AddHours(7), PlannedArrivalWindowEnd = now.AddDays(-2).AddHours(8),
                ActualArrivalTime = now.AddDays(-2).AddHours(7.3), GateInTime = now.AddDays(-2).AddHours(7.4), GateOutTime = now.AddDays(-2).AddHours(8.2),
                Status = TripStatus.Completed, CreatedAt = now.AddDays(-2), CreatedBy = "System"
            }
        );

        db.DispatchTrips.AddRange(trips);
        await db.SaveChangesAsync();
    }

    // ─── Disruption Events ────────────────────────────────────────────────────

    private static async Task SeedDisruptionEventsAsync(SmartPortDbContext db)
    {
        if (await db.DisruptionEvents.AnyAsync()) return;

        var now = DateTime.UtcNow;

        db.DisruptionEvents.AddRange(
            new DisruptionEvent {
                DisruptionType = DisruptionType.LoadShedding, Severity = DisruptionSeverity.High,
                Title = "Eskom Stage 3 Load-Shedding — Cold Chain Risk",
                Description = "Eskom has scheduled Stage 3 load-shedding from 06:00–08:30 and 16:00–18:30. Refrigerated storage facilities in Pinetown and Clairwood are operating on backup power. Cold-chain dispatches should be prioritised before the afternoon window.",
                AffectedLocation = "Pinetown, Clairwood, Maydon Wharf",
                AffectedRoute = "Island View / Bluff Route",
                StartTime = now.AddHours(-1), IsActive = true, CreatedBy = "System", CreatedAt = now.AddHours(-1)
            },
            new DisruptionEvent {
                DisruptionType = DisruptionType.GateDelay, Severity = DisruptionSeverity.Medium,
                Title = "DCT Gate 1 — Processing Delays",
                Description = "Gate 1 at Durban Container Terminal is experiencing extended processing times due to a document verification backlog. Average processing time has increased from 7 min to approximately 22 min per vehicle. Gate 4 is operational as an overflow lane.",
                AffectedLocation = "Durban Container Terminal — Gate 1",
                AffectedRoute = "Durban Container Terminal Access",
                StartTime = now.AddHours(-2), IsActive = true, CreatedBy = "System", CreatedAt = now.AddHours(-2)
            },
            new DisruptionEvent {
                DisruptionType = DisruptionType.RoadCongestion, Severity = DisruptionSeverity.High,
                Title = "Bayhead Road — Heavy Congestion",
                Description = "Bayhead Road southbound is heavily congested between Edwin Swales Drive and Settlers Way following an overturned light delivery vehicle. SAPS traffic officers are on scene. Expect delays of 35–55 minutes. Consider Island View route as alternative.",
                AffectedLocation = "Bayhead Road, Durban South",
                AffectedRoute = "Bayhead Road Corridor",
                StartTime = now.AddMinutes(-90), IsActive = true, CreatedBy = "System", CreatedAt = now.AddMinutes(-90)
            },
            new DisruptionEvent {
                DisruptionType = DisruptionType.YardDelay, Severity = DisruptionSeverity.Medium,
                Title = "DCT Yard — Afternoon Peak Pressure",
                Description = "Yard utilisation at Durban Container Terminal is at 84% with elevated traffic in Blocks A and B. Truck turnaround times in the yard have increased. Dispatchers are advised to allow an additional 20–30 minutes buffer for trips arriving between 15:00–18:00.",
                AffectedLocation = "Durban Container Terminal — Import Yard",
                AffectedRoute = "Durban Container Terminal Access",
                StartTime = now.AddHours(-0.5), IsActive = true, CreatedBy = "System", CreatedAt = now.AddHours(-0.5)
            },
            new DisruptionEvent {
                DisruptionType = DisruptionType.Weather, Severity = DisruptionSeverity.Low,
                Title = "SAWS Advisory — Reduced Visibility, Bluff",
                Description = "South African Weather Service has issued a coastal visibility advisory for the Bluff and Island View area. Low-lying sea fog is reducing visibility to below 200m. This may affect crane operations at the container terminal and road safety on the Bluff approach routes.",
                AffectedLocation = "Bluff, Island View, Maydon Wharf",
                AffectedRoute = "Island View / Bluff Route",
                StartTime = now.AddHours(-3), IsActive = true, CreatedBy = "System", CreatedAt = now.AddHours(-3)
            },
            new DisruptionEvent {
                DisruptionType = DisruptionType.EquipmentFailure, Severity = DisruptionSeverity.Low,
                Title = "Cato Ridge Weighbridge — Offline",
                Description = "The weighbridge on the N3 at Cato Ridge is temporarily offline for calibration maintenance. Trucks on the Cato Ridge Inland Link route will need to use the alternative weighbridge at Lynnfield Park. Add approximately 15 minutes to journey estimates.",
                AffectedLocation = "Cato Ridge, N3 Freight Corridor",
                AffectedRoute = "Cato Ridge Inland Link",
                StartTime = now.AddHours(-4), EndTime = now.AddHours(2),
                IsActive = true, CreatedBy = "System", CreatedAt = now.AddHours(-4)
            }
        );
        await db.SaveChangesAsync();
    }

    // ─── Flow Recommendations ──────────────────────────────────────────────────

    private static async Task SeedFlowRecommendationsAsync(SmartPortDbContext db)
    {
        if (await db.FlowRecommendations.AnyAsync()) return;

        var trips = await db.DispatchTrips.Include(t => t.Organisation).ToListAsync();
        var orgs  = await db.Organisations.ToListAsync();
        var now   = DateTime.UtcNow;

        int bayheadOrgId = orgs.First(o => o.Name.Contains("Bayhead")).Id;
        int dsbOrgId     = orgs.First(o => o.Name.Contains("Durban South")).Id;
        int kznOrgId     = orgs.First(o => o.Name.Contains("Cold Chain")).Id;
        int rbihOrgId    = orgs.First(o => o.Name.Contains("Richards Bay")).Id;

        var waitingTrip  = trips.FirstOrDefault(t => t.Status == TripStatus.Waiting);
        var coldChainT   = trips.FirstOrDefault(t => t.CargoType == FleetCargoType.ColdChain && t.Status == TripStatus.Dispatched);
        var holdTrip     = trips.FirstOrDefault(t => t.Status == TripStatus.RecommendedHold);
        var readyTrip    = trips.FirstOrDefault(t => t.Status == TripStatus.ReadyForDispatch);

        var recs = new List<FlowRecommendation>();

        if (waitingTrip != null)
        {
            recs.Add(new FlowRecommendation {
                DispatchTripId = waitingTrip.Id, OrganisationId = waitingTrip.OrganisationId,
                RecommendationType = FlowRecommendationType.EscalateDisruption, RiskLevel = FlowRiskLevel.High,
                ConfidenceLevel = FlowConfidenceLevel.High, CongestionScore = 74,
                RecommendationText = "Escalate gate delay. Waiting time on Bayhead Road Corridor has exceeded 45 minutes.",
                Reason = "Active gate delay at DCT Gate 1 combined with Bayhead Road congestion has created a compound delay. Vehicle has been stationary for 45+ minutes with no forward progress.",
                ExpectedBenefit = "Escalating to terminal operations may enable priority lane access, reducing remaining wait by an estimated 20–30 minutes.",
                GeneratedAt = now.AddMinutes(-10), AcceptedByUser = null,
                CreatedAt = now.AddMinutes(-10), CreatedBy = "FlowEngine"
            });
        }

        if (coldChainT != null)
        {
            recs.Add(new FlowRecommendation {
                DispatchTripId = coldChainT.Id, OrganisationId = coldChainT.OrganisationId,
                RecommendationType = FlowRecommendationType.PrioritiseCargo, RiskLevel = FlowRiskLevel.Critical,
                ConfidenceLevel = FlowConfidenceLevel.High, CongestionScore = 85,
                RecommendationText = "Prioritise cold-chain vehicle. Load-shedding and high congestion risk are simultaneously active.",
                Reason = "Cold-chain cargo (fresh produce, -2°C) is time and temperature critical. Eskom Stage 3 load-shedding is active, combined with 84% congestion score on the Island View / Bluff Route. Delayed arrival risks product integrity.",
                ExpectedBenefit = "Priority dispatch avoids 30–45 min additional exposure. Temperature risk is mitigated. Avoids potential product loss.",
                GeneratedAt = now.AddMinutes(-5), AcceptedByUser = null,
                CreatedAt = now.AddMinutes(-5), CreatedBy = "FlowEngine"
            });
        }

        if (holdTrip != null)
        {
            recs.Add(new FlowRecommendation {
                DispatchTripId = holdTrip.Id, OrganisationId = holdTrip.OrganisationId,
                RecommendationType = FlowRecommendationType.HoldAtDepot, RiskLevel = FlowRiskLevel.High,
                ConfidenceLevel = FlowConfidenceLevel.High, CongestionScore = 71,
                RecommendationText = "Hold at depot. Congestion and load-shedding risk are elevated on the Cato Ridge Inland Link.",
                Reason = "Cold-chain cargo with active load-shedding advisory and weighbridge closure on the N3. Dispatching now will likely result in a missed arrival window and elevated idling risk.",
                ExpectedBenefit = "Holding for 45–60 minutes avoids the current peak. Estimated saving of 2.1 litres diesel and 5.6 kg CO2.",
                GeneratedAt = now.AddMinutes(-20), AcceptedByUser = null,
                CreatedAt = now.AddMinutes(-20), CreatedBy = "FlowEngine"
            });
        }

        if (readyTrip != null)
        {
            recs.Add(new FlowRecommendation {
                DispatchTripId = readyTrip.Id, OrganisationId = readyTrip.OrganisationId,
                RecommendationType = FlowRecommendationType.ReleaseNow, RiskLevel = FlowRiskLevel.Low,
                ConfidenceLevel = FlowConfidenceLevel.High, CongestionScore = 22,
                RecommendationText = "Release truck now. Congestion risk is low and the arrival window is open.",
                Reason = "Congestion score: 22/100 (Low). No active disruptions on Bayhead Road Corridor. Arrival window opens in 55 minutes — optimal timing for dispatch.",
                ExpectedBenefit = "On-time arrival. Maintains dispatch reliability score. Minimal gate waiting expected.",
                GeneratedAt = now.AddMinutes(-15), AcceptedByUser = true, AcceptedAt = now.AddMinutes(-12),
                UserFeedback = "Dispatched as recommended.", CreatedAt = now.AddMinutes(-15), CreatedBy = "FlowEngine"
            });
        }

        // General recommendations without trip links
        recs.Add(new FlowRecommendation {
            DispatchTripId = null, OrganisationId = bayheadOrgId,
            RecommendationType = FlowRecommendationType.MonitorOnly, RiskLevel = FlowRiskLevel.Medium,
            ConfidenceLevel = FlowConfidenceLevel.Medium, CongestionScore = 48,
            RecommendationText = "Monitor Bayhead Road conditions. Congestion is moderate but stable.",
            Reason = "Bayhead Road congestion reported. Current score: 48/100. No new vehicles should be dispatched on this corridor until the overturned vehicle is cleared.",
            ExpectedBenefit = "Prevents additional vehicles joining the congestion queue. Reduces aggregate fleet idling time.",
            GeneratedAt = now.AddMinutes(-35), AcceptedByUser = null,
            CreatedAt = now.AddMinutes(-35), CreatedBy = "FlowEngine"
        });

        recs.Add(new FlowRecommendation {
            DispatchTripId = null, OrganisationId = kznOrgId,
            RecommendationType = FlowRecommendationType.RerouteIfPossible, RiskLevel = FlowRiskLevel.Medium,
            ConfidenceLevel = FlowConfidenceLevel.Medium, CongestionScore = 55,
            RecommendationText = "Consider alternative route for cold-chain outbound. Island View fog advisory may impact Bluff approach.",
            Reason = "SAWS visibility advisory on Island View / Bluff Route. Reduced visibility affects road safety and may cause crane operations to slow at the terminal.",
            ExpectedBenefit = "Alternative via South Coast Road reduces exposure to visibility-related delays by an estimated 20 minutes.",
            GeneratedAt = now.AddHours(-1), AcceptedByUser = false, AcceptedAt = now.AddHours(-0.8),
            UserFeedback = "Dismissed — driver is experienced on the Bluff route.",
            CreatedAt = now.AddHours(-1), CreatedBy = "FlowEngine"
        });

        db.FlowRecommendations.AddRange(recs);
        await db.SaveChangesAsync();
    }

    // ─── Idling Emission Estimates ────────────────────────────────────────────

    private static async Task SeedEmissionEstimatesAsync(SmartPortDbContext db)
    {
        if (await db.IdlingEmissionEstimates.AnyAsync()) return;

        var completedTrips = await db.DispatchTrips
            .Where(t => t.Status == TripStatus.Completed || t.Status == TripStatus.Waiting ||
                        t.Status == TripStatus.Delayed    || t.Status == TripStatus.Dispatched)
            .ToListAsync();

        var rng = new Random(42);
        var estimates = new List<IdlingEmissionEstimate>();

        foreach (var trip in completedTrips)
        {
            var idlingMin = trip.CargoType == FleetCargoType.ColdChain
                ? (decimal)(rng.Next(30, 80))
                : trip.Status == TripStatus.Waiting || trip.Status == TripStatus.Delayed
                    ? (decimal)(rng.Next(40, 100))
                    : (decimal)(rng.Next(15, 60));

            var dieselL  = Math.Round(idlingMin / 60m * 3.0m, 2);
            var cost     = Math.Round(dieselL * 24.00m, 2);
            var co2      = Math.Round(dieselL * 2.68m, 2);
            var avoidable = idlingMin > 50 || trip.Status == TripStatus.Delayed;

            estimates.Add(new IdlingEmissionEstimate {
                DispatchTripId         = trip.Id,
                EstimatedIdlingMinutes = idlingMin,
                EstimatedDieselLitres  = dieselL,
                EstimatedFuelCost      = cost,
                EstimatedCo2Kg         = co2,
                AvoidableIdlingFlag    = avoidable,
                CalculationNotes       = $"Seeded estimate for {trip.RouteName}. Based on route profile and trip status.",
                CreatedAt = trip.CreatedAt, CreatedBy = "System"
            });
        }

        db.IdlingEmissionEstimates.AddRange(estimates);
        await db.SaveChangesAsync();
    }

    // ─── Pilot Metric Snapshots ───────────────────────────────────────────────

    private static async Task SeedPilotMetricsAsync(SmartPortDbContext db)
    {
        if (await db.PilotMetricSnapshots.AnyAsync()) return;

        var now  = DateTime.UtcNow;
        var orgs = await db.Organisations.ToListAsync();
        int bayheadId = orgs.First(o => o.Name.Contains("Bayhead")).Id;

        db.PilotMetricSnapshots.AddRange(
            // ── Platform-wide baseline (pre-Culltron)
            new PilotMetricSnapshot {
                OrganisationId = null, SnapshotDate = now.AddDays(-60), PeriodLabel = "Baseline — May 2025 (Pre-Platform)",
                MetricType = PilotMetricType.Baseline,
                AverageWaitingMinutes = 112m, TotalIdlingMinutes = 4820m,
                EstimatedDieselLitres = 241.0m, EstimatedFuelCost = 5784.0m, EstimatedCo2Kg = 645.9m,
                MissedArrivalWindows = 18, DispatchReliabilityPercent = 67.4m,
                RecommendationsGenerated = 0, HighRiskTrips = 0,
                Notes = "Pre-platform baseline captured from manual dispatch logs. Average based on 58 trips in May 2025.",
                CreatedAt = now.AddDays(-60), CreatedBy = "System"
            },
            // ── Platform-wide current snapshot (pilot month 1)
            new PilotMetricSnapshot {
                OrganisationId = null, SnapshotDate = now.AddDays(-30), PeriodLabel = "Pilot Month 1 — June 2025",
                MetricType = PilotMetricType.Current,
                AverageWaitingMinutes = 88m, TotalIdlingMinutes = 3640m,
                EstimatedDieselLitres = 182.0m, EstimatedFuelCost = 4368.0m, EstimatedCo2Kg = 487.8m,
                MissedArrivalWindows = 11, DispatchReliabilityPercent = 76.2m,
                RecommendationsGenerated = 42, HighRiskTrips = 14,
                Notes = "End-of-month snapshot for pilot month 1. Recommendations accepted: 29/42 (69%).",
                CreatedAt = now.AddDays(-30), CreatedBy = "System"
            },
            // ── Target
            new PilotMetricSnapshot {
                OrganisationId = null, SnapshotDate = now.AddDays(-60), PeriodLabel = "Target — Pilot End (Month 3)",
                MetricType = PilotMetricType.Target,
                AverageWaitingMinutes = 65m, TotalIdlingMinutes = 2400m,
                EstimatedDieselLitres = 120.0m, EstimatedFuelCost = 2880.0m, EstimatedCo2Kg = 321.6m,
                MissedArrivalWindows = 5, DispatchReliabilityPercent = 88.0m,
                RecommendationsGenerated = 80, HighRiskTrips = 8,
                Notes = "Pilot target KPIs agreed with Bayhead Freight Coordination and DSB Logistics for Month 3 of the pilot.",
                CreatedAt = now.AddDays(-60), CreatedBy = "System"
            },
            // ── Org-level baseline (Bayhead only)
            new PilotMetricSnapshot {
                OrganisationId = bayheadId, SnapshotDate = now.AddDays(-60), PeriodLabel = "Bayhead — Baseline May 2025",
                MetricType = PilotMetricType.Baseline,
                AverageWaitingMinutes = 118m, TotalIdlingMinutes = 1640m,
                EstimatedDieselLitres = 82.0m, EstimatedFuelCost = 1968.0m, EstimatedCo2Kg = 219.8m,
                MissedArrivalWindows = 7, DispatchReliabilityPercent = 64.0m,
                RecommendationsGenerated = 0, HighRiskTrips = 0,
                Notes = "Bayhead Freight Coordination baseline from manual dispatch records, May 2025.",
                CreatedAt = now.AddDays(-60), CreatedBy = "System"
            },
            new PilotMetricSnapshot {
                OrganisationId = bayheadId, SnapshotDate = now.AddDays(-30), PeriodLabel = "Bayhead — Pilot Month 1 June 2025",
                MetricType = PilotMetricType.Current,
                AverageWaitingMinutes = 82m, TotalIdlingMinutes = 1180m,
                EstimatedDieselLitres = 59.0m, EstimatedFuelCost = 1416.0m, EstimatedCo2Kg = 158.1m,
                MissedArrivalWindows = 3, DispatchReliabilityPercent = 79.5m,
                RecommendationsGenerated = 18, HighRiskTrips = 5,
                Notes = "Bayhead Month 1 pilot result. Dispatch reliability improved +15.5 points from baseline.",
                CreatedAt = now.AddDays(-30), CreatedBy = "System"
            },
            new PilotMetricSnapshot {
                OrganisationId = bayheadId, SnapshotDate = now, PeriodLabel = "Bayhead — Pilot Month 2 Snapshot",
                MetricType = PilotMetricType.Current,
                AverageWaitingMinutes = 74m, TotalIdlingMinutes = 980m,
                EstimatedDieselLitres = 49.0m, EstimatedFuelCost = 1176.0m, EstimatedCo2Kg = 131.3m,
                MissedArrivalWindows = 2, DispatchReliabilityPercent = 83.1m,
                RecommendationsGenerated = 24, HighRiskTrips = 4,
                Notes = "Mid-pilot Month 2 snapshot. Positive trend — on track to meet target by Month 3.",
                CreatedAt = now, CreatedBy = "System"
            }
        );

        await db.SaveChangesAsync();
    }
}
