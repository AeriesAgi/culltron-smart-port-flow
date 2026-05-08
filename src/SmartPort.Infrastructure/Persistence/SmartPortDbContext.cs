using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartPort.Domain.Entities;
using SmartPort.Infrastructure.Persistence.EntityConfigurations;

namespace SmartPort.Infrastructure.Persistence;

/// <summary>
/// Main EF Core database context for the Culltron Smart Port Flow platform.
/// Inherits from IdentityDbContext for built-in ASP.NET Core Identity support.
/// </summary>
public class SmartPortDbContext : IdentityDbContext<ApplicationUser>
{
    public SmartPortDbContext(DbContextOptions<SmartPortDbContext> options) : base(options) { }

    // ─── Vessel & Berth ──────────────────────────────────────────────────
    public DbSet<Vessel> Vessels => Set<Vessel>();
    public DbSet<Berth> Berths => Set<Berth>();
    public DbSet<BerthAssignment> BerthAssignments => Set<BerthAssignment>();
    public DbSet<VesselScheduleVisit> VesselScheduleVisits => Set<VesselScheduleVisit>();

    // ─── Yard / Container / Cargo ────────────────────────────────────────
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<YardBlock> YardBlocks => Set<YardBlock>();
    public DbSet<CargoRecord> CargoRecords => Set<CargoRecord>();

    // ─── Gate / Truck ────────────────────────────────────────────────────
    public DbSet<Gate> Gates => Set<Gate>();
    public DbSet<Truck> Trucks => Set<Truck>();
    public DbSet<GateTransaction> GateTransactions => Set<GateTransaction>();

    // ─── Incidents / Alerts ──────────────────────────────────────────────
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentUpdate> IncidentUpdates => Set<IncidentUpdate>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    // ─── Documents ───────────────────────────────────────────────────────
    public DbSet<Document> Documents => Set<Document>();

    // ─── Analytics ───────────────────────────────────────────────────────
    public DbSet<OperationalMetric> OperationalMetrics => Set<OperationalMetric>();

    // ─── Culltron Smart Port Flow ────────────────────────────────────────
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<FleetVehicle> FleetVehicles => Set<FleetVehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DispatchTrip> DispatchTrips => Set<DispatchTrip>();
    public DbSet<DisruptionEvent> DisruptionEvents => Set<DisruptionEvent>();
    public DbSet<FlowRecommendation> FlowRecommendations => Set<FlowRecommendation>();
    public DbSet<IdlingEmissionEstimate> IdlingEmissionEstimates => Set<IdlingEmissionEstimate>();
    public DbSet<PilotMetricSnapshot> PilotMetricSnapshots => Set<PilotMetricSnapshot>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new VesselConfiguration());
        builder.ApplyConfiguration(new BerthConfiguration());
        builder.ApplyConfiguration(new BerthAssignmentConfiguration());
        builder.ApplyConfiguration(new ContainerConfiguration());
        builder.ApplyConfiguration(new YardBlockConfiguration());
        builder.ApplyConfiguration(new CargoRecordConfiguration());
        builder.ApplyConfiguration(new GateConfiguration());
        builder.ApplyConfiguration(new TruckConfiguration());
        builder.ApplyConfiguration(new GateTransactionConfiguration());
        builder.ApplyConfiguration(new IncidentConfiguration());
        builder.ApplyConfiguration(new DocumentConfiguration());
        builder.ApplyConfiguration(new AlertConfiguration());
        builder.ApplyConfiguration(new RecommendationConfiguration());
        builder.ApplyConfiguration(new OperationalMetricConfiguration());
        builder.ApplyConfiguration(new VesselScheduleVisitConfiguration());

        // ─── Culltron configurations ──────────────────────────────────────
        builder.ApplyConfiguration(new OrganisationConfiguration());
        builder.ApplyConfiguration(new FleetVehicleConfiguration());
        builder.ApplyConfiguration(new DriverConfiguration());
        builder.ApplyConfiguration(new DispatchTripConfiguration());
        builder.ApplyConfiguration(new DisruptionEventConfiguration());
        builder.ApplyConfiguration(new FlowRecommendationConfiguration());
        builder.ApplyConfiguration(new IdlingEmissionEstimateConfiguration());
        builder.ApplyConfiguration(new PilotMetricSnapshotConfiguration());

        // Rename Identity tables
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);
        foreach (var entry in entries)
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        return base.SaveChangesAsync(cancellationToken);
    }
}
