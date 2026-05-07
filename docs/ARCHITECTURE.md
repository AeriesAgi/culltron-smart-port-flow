# SmartPort — Architecture Document

## Layer Diagram

```
┌──────────────────────────────────────────────────┐
│             SmartPort.Web (ASP.NET Core MVC)      │
│   Controllers · Razor Views · CSS/JS · DI wiring  │
└────────────────────┬─────────────────────────────┘
                     │ depends on
┌────────────────────▼─────────────────────────────┐
│           SmartPort.Application                   │
│    Service Interfaces · DTOs · No EF references   │
└──────────┬──────────────────────────┬────────────┘
           │ implemented by           │ references
┌──────────▼──────────┐   ┌──────────▼────────────┐
│ SmartPort.           │   │ SmartPort.Domain       │
│ Infrastructure       │   │ Entities · Enums       │
│ EF Core · Services   │   │ No dependencies        │
│ Seed Data            │   └───────────────────────┘
└─────────────────────┘
           │ cross-cutting
┌──────────▼──────────┐
│  SmartPort.Shared    │
│  Constants · Roles   │
└─────────────────────┘
```

## Key Decisions

### ASP.NET Core MVC (not SPA)
Server-side Razor rendering is appropriate for this enterprise context:
- Better SEO for the public site
- Simpler auth cookie handling
- No separate build pipeline
- Maintainable by .NET teams without React expertise

### PostgreSQL + EF Core
- PostgreSQL reliability and JSON capability suit future AIS data
- EF Core provides type-safe queries and clean migration workflow
- Npgsql driver gives excellent .NET integration

### ASP.NET Core Identity
- Industry-standard auth with PBKDF2 password hashing
- Built-in account lockout, token providers
- Role and policy-based authorization on all sensitive routes

### Clean Architecture
- Domain layer: pure C# entities and enums, zero dependencies
- Application layer: interfaces and DTOs only — no EF references
- Infrastructure: implements application interfaces using EF Core
- Web: MVC entry point, wires everything via DI in Program.cs

### Rules-Based AI Layer
The recommendation engine is a plain C# service (`RecommendationService`) that:
- Reads operational data from the database
- Applies threshold rules (anchorage wait, queue depth, dwell time)
- Writes `Recommendation` entities
- Is invoked on demand or via a background timer

This design means the AI layer can be replaced or augmented with ML.NET, Azure AI, or external APIs without changing the service interface.

## Domain Model Summary

```
Vessel ──────────────── BerthAssignment ──── Berth
  │                                           
  ├── Container[]  ──── YardBlock
  ├── CargoRecord[]
  ├── Document[]
  ├── Incident[]
  └── VesselScheduleVisit[]

Gate ── GateTransaction ── Truck

Incident ── IncidentUpdate[]

Alert
Recommendation

OperationalMetric  (time-series KPIs)

ApplicationUser (ASP.NET Core Identity)
```

## Database Tables

All tables are in the default `public` schema.

| Table | Purpose |
|---|---|
| Users | ASP.NET Core Identity users (extended) |
| Roles | Identity roles |
| UserRoles | Many-to-many user/role mapping |
| Vessels | Vessel registry and live status |
| Berths | Physical berth definitions |
| BerthAssignments | Vessel-to-berth allocation per call |
| VesselScheduleVisits | Shipping line schedule entries |
| Containers | Container tracking records |
| YardBlocks | Yard storage zone definitions |
| CargoRecords | Manifest-level cargo records |
| Gates | Gate definitions |
| Trucks | Truck/driver registry |
| GateTransactions | Individual gate entry/exit events |
| Incidents | Operational incident records |
| IncidentUpdates | Audit trail for incidents |
| Alerts | System and manual alerts |
| Recommendations | AI recommendation records |
| Documents | Document and compliance records |
| OperationalMetrics | Time-series KPI data |
