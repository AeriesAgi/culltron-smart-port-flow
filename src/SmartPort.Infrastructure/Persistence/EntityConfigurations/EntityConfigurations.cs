using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartPort.Domain.Entities;
using SmartPort.Domain.Enums;

namespace SmartPort.Infrastructure.Persistence.EntityConfigurations;

public class VesselConfiguration : IEntityTypeConfiguration<Vessel>
{
    public void Configure(EntityTypeBuilder<Vessel> builder)
    {
        builder.ToTable("Vessels");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.IMONumber).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Name).HasMaxLength(100).IsRequired();
        builder.Property(v => v.CallSign).HasMaxLength(20);
        builder.Property(v => v.FlagCode).HasMaxLength(3);
        builder.Property(v => v.Operator).HasMaxLength(100);
        builder.Property(v => v.ShippingLine).HasMaxLength(100);
        builder.Property(v => v.Agent).HasMaxLength(100);
        builder.Property(v => v.GrossTonnage).HasPrecision(12, 2);
        builder.Property(v => v.DeadweightTonnage).HasPrecision(12, 2);
        builder.Property(v => v.LengthOverall).HasPrecision(8, 2);
        builder.Property(v => v.Beam).HasPrecision(8, 2);
        builder.Property(v => v.MaxDraught).HasPrecision(6, 2);
        builder.Property(v => v.CurrentDraught).HasPrecision(6, 2);
        builder.Property(v => v.CurrentLatitude).HasPrecision(10, 7);
        builder.Property(v => v.CurrentLongitude).HasPrecision(10, 7);
        builder.HasIndex(v => v.IMONumber).IsUnique();
        builder.HasIndex(v => v.MMSINumber);
        builder.HasIndex(v => v.Status);

        builder.HasMany(v => v.BerthAssignments).WithOne(a => a.Vessel).HasForeignKey(a => a.VesselId);
        builder.HasMany(v => v.Containers).WithOne(c => c.Vessel).HasForeignKey(c => c.VesselId);
        builder.HasMany(v => v.CargoRecords).WithOne(c => c.Vessel).HasForeignKey(c => c.VesselId);
        builder.HasMany(v => v.Documents).WithOne(d => d.Vessel).HasForeignKey(d => d.VesselId);
        builder.HasMany(v => v.Incidents).WithOne(i => i.Vessel).HasForeignKey(i => i.VesselId);
        builder.HasMany(v => v.ScheduleVisits).WithOne(s => s.Vessel).HasForeignKey(s => s.VesselId);
    }
}

public class BerthConfiguration : IEntityTypeConfiguration<Berth>
{
    public void Configure(EntityTypeBuilder<Berth> builder)
    {
        builder.ToTable("Berths");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Code).HasMaxLength(20).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Terminal).HasMaxLength(100);
        builder.Property(b => b.Length).HasPrecision(8, 2);
        builder.Property(b => b.MaxDraught).HasPrecision(6, 2);
        builder.Property(b => b.MaxLOA).HasPrecision(8, 2);
        builder.Property(b => b.Latitude).HasPrecision(10, 7);
        builder.Property(b => b.Longitude).HasPrecision(10, 7);
        builder.HasIndex(b => b.Code).IsUnique();

        builder.HasMany(b => b.Assignments).WithOne(a => a.Berth).HasForeignKey(a => a.BerthId);
    }
}

public class BerthAssignmentConfiguration : IEntityTypeConfiguration<BerthAssignment>
{
    public void Configure(EntityTypeBuilder<BerthAssignment> builder)
    {
        builder.ToTable("BerthAssignments");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.BerthId, a.PlannedArrival });
        builder.HasIndex(a => a.VesselId);
    }
}

public class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("Containers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ContainerNumber).HasMaxLength(11).IsRequired(); // ISO 6346
        builder.Property(c => c.ShippingLine).HasMaxLength(100);
        builder.Property(c => c.CustomsReferenceNumber).HasMaxLength(50);
        builder.Property(c => c.GrossWeightKg).HasPrecision(10, 2);
        builder.HasIndex(c => c.ContainerNumber);
        builder.HasIndex(c => c.Status);
    }
}

public class YardBlockConfiguration : IEntityTypeConfiguration<YardBlock>
{
    public void Configure(EntityTypeBuilder<YardBlock> builder)
    {
        builder.ToTable("YardBlocks");
        builder.HasKey(y => y.Id);
        builder.Property(y => y.BlockCode).HasMaxLength(20).IsRequired();
        builder.Property(y => y.Terminal).HasMaxLength(100);
        builder.HasIndex(y => new { y.Terminal, y.BlockCode }).IsUnique();
    }
}

public class CargoRecordConfiguration : IEntityTypeConfiguration<CargoRecord>
{
    public void Configure(EntityTypeBuilder<CargoRecord> builder)
    {
        builder.ToTable("CargoRecords");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.BillOfLadingNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Consignee).HasMaxLength(200);
        builder.Property(c => c.Shipper).HasMaxLength(200);
        builder.Property(c => c.GrossWeightKg).HasPrecision(12, 2);
        builder.Property(c => c.VolumeM3).HasPrecision(10, 3);
        builder.HasIndex(c => c.BillOfLadingNumber);
    }
}

public class GateConfiguration : IEntityTypeConfiguration<Gate>
{
    public void Configure(EntityTypeBuilder<Gate> builder)
    {
        builder.ToTable("Gates");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Code).HasMaxLength(20).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(100);
        builder.HasIndex(g => g.Code).IsUnique();
    }
}

public class TruckConfiguration : IEntityTypeConfiguration<Truck>
{
    public void Configure(EntityTypeBuilder<Truck> builder)
    {
        builder.ToTable("Trucks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.RegistrationNumber).HasMaxLength(20).IsRequired();
        builder.Property(t => t.TransporterName).HasMaxLength(150);
        builder.Property(t => t.DriverName).HasMaxLength(100);
        builder.HasIndex(t => t.RegistrationNumber);
    }
}

public class GateTransactionConfiguration : IEntityTypeConfiguration<GateTransaction>
{
    public void Configure(EntityTypeBuilder<GateTransaction> builder)
    {
        builder.ToTable("GateTransactions");
        builder.HasKey(g => g.Id);
        builder.HasIndex(g => g.TransactionTime);
        builder.HasIndex(g => g.GateId);
    }
}

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ReferenceNumber).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Title).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(2000);
        builder.Property(i => i.ReportedBy).HasMaxLength(100);
        builder.HasIndex(i => i.ReferenceNumber).IsUnique();
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.Severity);
    }
}

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DocumentNumber).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.HasIndex(d => d.DocumentNumber);
        builder.HasIndex(d => d.Status);
    }
}

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Message).HasMaxLength(1000);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.CreatedAt);
    }
}

public class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Summary).HasMaxLength(500);
        builder.Property(r => r.TriggerValue).HasPrecision(10, 2);
        builder.Property(r => r.ThresholdValue).HasPrecision(10, 2);
    }
}

public class OperationalMetricConfiguration : IEntityTypeConfiguration<OperationalMetric>
{
    public void Configure(EntityTypeBuilder<OperationalMetric> builder)
    {
        builder.ToTable("OperationalMetrics");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MetricType).HasMaxLength(100);
        builder.Property(m => m.Terminal).HasMaxLength(100);
        builder.Property(m => m.Value).HasPrecision(14, 4);
        builder.HasIndex(m => new { m.MetricDate, m.MetricType, m.Terminal });
    }
}

public class VesselScheduleVisitConfiguration : IEntityTypeConfiguration<VesselScheduleVisit>
{
    public void Configure(EntityTypeBuilder<VesselScheduleVisit> builder)
    {
        builder.ToTable("VesselScheduleVisits");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.ServiceName).HasMaxLength(100);
        builder.Property(v => v.VoyageNumber).HasMaxLength(30);
        builder.HasIndex(v => new { v.VesselId, v.EstimatedArrival });
    }
}

// ─── Culltron Smart Port Flow Entity Configurations ──────────────────────────

public class OrganisationConfiguration : IEntityTypeConfiguration<Organisation>
{
    public void Configure(EntityTypeBuilder<Organisation> builder)
    {
        builder.ToTable("Organisations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.ContactPerson).HasMaxLength(100);
        builder.Property(o => o.ContactEmail).HasMaxLength(200);
        builder.Property(o => o.Province).HasMaxLength(100);
        builder.HasIndex(o => o.Name);
        builder.HasMany(o => o.FleetVehicles).WithOne(v => v.Organisation).HasForeignKey(v => v.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Drivers).WithOne(d => d.Organisation).HasForeignKey(d => d.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.DispatchTrips).WithOne(t => t.Organisation).HasForeignKey(t => t.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FleetVehicleConfiguration : IEntityTypeConfiguration<FleetVehicle>
{
    public void Configure(EntityTypeBuilder<FleetVehicle> builder)
    {
        builder.ToTable("FleetVehicles");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.RegistrationNumber).HasMaxLength(20).IsRequired();
        builder.Property(v => v.FleetNumber).HasMaxLength(30);
        builder.Property(v => v.CurrentLocation).HasMaxLength(200);
        builder.Property(v => v.CapacityTons).HasPrecision(8, 2);
        builder.HasIndex(v => v.RegistrationNumber);
        builder.HasIndex(v => v.Status);
    }
}

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FullName).HasMaxLength(100).IsRequired();
        builder.Property(d => d.PhoneNumber).HasMaxLength(20);
        builder.Property(d => d.LicenceNumber).HasMaxLength(30);
    }
}

public class DispatchTripConfiguration : IEntityTypeConfiguration<DispatchTrip>
{
    public void Configure(EntityTypeBuilder<DispatchTrip> builder)
    {
        builder.ToTable("DispatchTrips");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Origin).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Destination).HasMaxLength(200).IsRequired();
        builder.Property(t => t.RouteName).HasMaxLength(200);
        builder.Property(t => t.CargoDescription).HasMaxLength(500);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.PlannedDispatchTime);
        builder.HasOne(t => t.FleetVehicle).WithMany(v => v.DispatchTrips).HasForeignKey(t => t.FleetVehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Driver).WithMany(d => d.DispatchTrips).HasForeignKey(t => t.DriverId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.EmissionEstimate).WithOne(e => e.DispatchTrip).HasForeignKey<IdlingEmissionEstimate>(e => e.DispatchTripId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DisruptionEventConfiguration : IEntityTypeConfiguration<DisruptionEvent>
{
    public void Configure(EntityTypeBuilder<DisruptionEvent> builder)
    {
        builder.ToTable("DisruptionEvents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(2000);
        builder.Property(d => d.AffectedLocation).HasMaxLength(200);
        builder.Property(d => d.AffectedRoute).HasMaxLength(200);
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.HasIndex(d => d.IsActive);
        builder.HasIndex(d => d.Severity);
    }
}

public class FlowRecommendationConfiguration : IEntityTypeConfiguration<FlowRecommendation>
{
    public void Configure(EntityTypeBuilder<FlowRecommendation> builder)
    {
        builder.ToTable("FlowRecommendations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RecommendationText).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.Property(r => r.ExpectedBenefit).HasMaxLength(500);
        builder.Property(r => r.UserFeedback).HasMaxLength(1000);
        builder.HasIndex(r => r.GeneratedAt);
        builder.HasIndex(r => r.RiskLevel);
        builder.HasOne(r => r.DispatchTrip).WithMany(t => t.FlowRecommendations).HasForeignKey(r => r.DispatchTripId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(r => r.Organisation).WithMany(o => o.FlowRecommendations).HasForeignKey(r => r.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IdlingEmissionEstimateConfiguration : IEntityTypeConfiguration<IdlingEmissionEstimate>
{
    public void Configure(EntityTypeBuilder<IdlingEmissionEstimate> builder)
    {
        builder.ToTable("IdlingEmissionEstimates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EstimatedIdlingMinutes).HasPrecision(10, 2);
        builder.Property(e => e.EstimatedDieselLitres).HasPrecision(10, 3);
        builder.Property(e => e.EstimatedFuelCost).HasPrecision(12, 2);
        builder.Property(e => e.EstimatedCo2Kg).HasPrecision(10, 3);
        builder.Property(e => e.CalculationNotes).HasMaxLength(500);
    }
}

public class PilotMetricSnapshotConfiguration : IEntityTypeConfiguration<PilotMetricSnapshot>
{
    public void Configure(EntityTypeBuilder<PilotMetricSnapshot> builder)
    {
        builder.ToTable("PilotMetricSnapshots");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PeriodLabel).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.AverageWaitingMinutes).HasPrecision(10, 2);
        builder.Property(p => p.TotalIdlingMinutes).HasPrecision(12, 2);
        builder.Property(p => p.EstimatedDieselLitres).HasPrecision(12, 3);
        builder.Property(p => p.EstimatedFuelCost).HasPrecision(14, 2);
        builder.Property(p => p.EstimatedCo2Kg).HasPrecision(12, 3);
        builder.Property(p => p.DispatchReliabilityPercent).HasPrecision(6, 2);
        builder.HasIndex(p => new { p.SnapshotDate, p.MetricType });
        builder.HasOne(p => p.Organisation).WithMany(o => o.PilotMetrics).HasForeignKey(p => p.OrganisationId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}
