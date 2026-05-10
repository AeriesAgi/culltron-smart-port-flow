# Culltron Smart Port Flow — Architecture

Culltron Smart Port Flow is an ASP.NET Core MVC / .NET 8 demo application for enterprise-style Smart Port operations. It uses PostgreSQL through Docker Compose, synthetic seeded data, a services layer for operational intelligence, optional Gemini Agent Mode, and a local offline-safe fallback.

The system is a working deployed demo/prototype and pilot-ready architecture. It is not presented as a live production deployment inside an actual port and does not claim live port/IPMS/Navayuga/Transnet integration.

## Layer Diagram

```text
┌────────────────────────────────────────────────────────────┐
│ SmartPort.Web                                              │
│ ASP.NET Core MVC · Razor Views · Controllers · DI wiring    │
└─────────────────────────────┬──────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────┐
│ Application / Service Interfaces                            │
│ DTOs · contracts · use-case boundaries                       │
└─────────────────────────────┬──────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────┐
│ Infrastructure Services                                     │
│ EF Core · seeded data · dashboard · truck tracking ·         │
│ scenario simulation · emissions · recommendations · reports  │
└───────────────┬──────────────────────────────┬──────────────┘
                │                              │
┌───────────────▼──────────────┐   ┌───────────▼──────────────┐
│ PostgreSQL                    │   │ Optional Gemini Service   │
│ Synthetic/demo operational    │   │ Gemini 2.5 Flash · Hybrid │
│ data only                     │   │ user-triggered responses  │
└───────────────┬──────────────┘   └───────────┬──────────────┘
                │                              │
                └──────────────┬───────────────┘
                               ▼
                 Local offline-safe fallback
                 human-reviewed recommendations
                 audit-friendly decision support
```

## Key Components

- **ASP.NET Core MVC:** server-rendered enterprise UI for dashboards, command centre, Copilot, reports, simulator, truck tracking, emissions, pilot readiness, and stakeholder value pages.
- **Services layer:** encapsulates operational summaries, deterministic scoring, scenario analysis, recommendation generation, report drafting, and Copilot response orchestration.
- **PostgreSQL:** stores demo entities and synthetic operational data for repeatable judging and local runs.
- **Docker Compose:** runs the web application and database for local/demo environments.
- **Gemini optional service:** enhances user-triggered Copilot/report responses when `GEMINI_API_KEY`, `Gemini__Enabled=true`, `Gemini__Mode=Hybrid`, and `Gemini__Model=gemini-2.5-flash` are configured outside Git.
- **Local fallback service:** keeps the system usable without Gemini or network-dependent AI.
- **Human approval and audit-friendly decision support:** recommendations are not automatically executed.

## Data and AI Boundary

Gemini receives only sanitized operational summaries from synthetic/demo context. It should not receive secrets, credentials, connection strings, API keys, private deployment values, customer data, or live operational data in the public demo.

The architecture intentionally supports this safety boundary:

1. Controllers receive user-triggered actions.
2. Services gather synthetic/demo operational summaries.
3. Deterministic logic produces baseline analysis and recommendations.
4. Gemini may enhance text if enabled and available.
5. Local fallback responds if Gemini is unavailable.
6. Users review recommendations before any real-world action.

## Deployment Boundary

Runtime secrets must live outside Git in server environment variables, deployment secrets, or uncommitted local files. Do not put API keys in `docker-compose.yml`, `appsettings.json`, README content, screenshots, logs, or source code.

## Production Pilot Requirements

A production pilot would require live data integration, security review, stakeholder workflow mapping, operational validation, audit requirements, and approved data-sharing arrangements. Estimated savings and clean-logistics impact should remain demo outputs until validated against pilot baselines.
