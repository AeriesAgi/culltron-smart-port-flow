## Hackathon Submission

- Live Demo: https://smartport.culltron.app/
- Hugging Face Space: https://huggingface.co/spaces/atrx93/culltron-smart-port-flow
- Demo Video: https://youtu.be/-R4SJraREP4
- Pitch Deck: https://canva.link/6s7n1perwny0lbs

This demo uses seeded/synthetic operational data and does not expose real port, customer, or production data.

The live application is deployed on DigitalOcean. AMD Developer Cloud credits are available through DigitalOcean for the hackathon compute pathway and future scaling of AI simulation, forecasting, optimization, and inference workloads.

# Culltron Smart Port Flow

> Premium Smart Port Agentic Operations Copilot demonstrator by Culltron.
> ASP.NET Core 8 MVC · PostgreSQL · Entity Framework Core · Docker Compose · synthetic demo data · deterministic local intelligence.

Culltron Smart Port Flow is a smart-port operations demonstrator that shows how port teams could monitor congestion, truck ETA risk, berth pressure, yard density, load-shedding disruption, incidents, recommendations and indicative emissions/idling impact from one enterprise command-centre UI.

The product is intentionally **demo-safe**: it uses synthetic operational data, local deterministic rules/scoring, explainable response cards and scoped Copilot intent routing. It does **not** require OpenAI, Gemini, Claude, Azure OpenAI, Anthropic, external map APIs, cloud secrets or internet-dependent AI.

## Product summary

Culltron Smart Port Flow supports hackathon judging, NCIC clean-logistics positioning, pilot discovery, grant/funding conversations and future product direction. It is not presented as a live integration to any real port system. Production integrations would connect to partner-approved TOS/IPMS, gate systems, GPS/telematics, berth planning, yard systems, energy/load-shedding schedules and emissions-factor data.

## Platform modules

- Landing and public product pages.
- Dashboard command centre.
- **AI Command Centre** for system-level operational state, risk scoring, monitoring lanes, confidence and action plans.
- **SmartPort Copilot Chat** for natural-language, scoped questions into the same deterministic intelligence layer.
- Truck Tracking & ETA Intelligence.
- Scenario Simulator.
- Emissions / Idling impact.
- Incidents, disruptions and load-shedding response.
- Recommendations / decision support and audit-style reports.
- **Executive Impact Centre** for indicative business, time, diesel and CO₂ opportunity.
- **Clean Logistics Impact** for NCIC-style problem/solution/outcomes framing.
- **Pilot Readiness** with 30/60/90-day roadmap, required integrations, metrics and risks.
- **Stakeholder Value** persona cards for port authority, terminal operator, fleet operator, sustainability officer, municipality/province and operations manager.
- **Executive Brief** print-friendly shift report.

## AI Command Centre vs SmartPort Copilot Chat

### AI Command Centre

Answers: **“What is happening across the port right now?”**

It is a system-level intelligence wall, not a chat page. It shows live synthetic/demo operational state, top operational risk, gate/berth/truck/yard/emissions/load-shedding monitoring lanes, confidence indicators, generated recommendations, a deterministic shift brief, operator action plan and links to decision/audit modules.

### SmartPort Copilot Chat

Answers: **“What do I want to ask the system?”**

It supports greetings, help/capabilities, truck queues, truck tracking / ETA, hold-outside-port guidance, gate bottlenecks, berth pressure, yard congestion, vessel delays, emissions / CO₂ / idling, load-shedding, scenario simulation, recommendations, audit history, executive impact, clean logistics / NCIC alignment, pilot readiness, integrations, stakeholder value, executive brief, grant/investor summary and out-of-scope refusal.

## Local deterministic intelligence

The intelligence layer is local and deterministic by default:

- `ISmartPortIntelligenceService` builds the operational snapshot.
- `SmartPortCopilotChatService` routes supported prompts to fixed, explainable response builders.
- Truck tracking, simulator, emissions and recommendations use deterministic calculations and seeded/demo data.
- No paid external AI API or internet-dependent inference is required.

## Copilot governance and scope control

Supported prompts include:

- `What is the biggest risk right now?`
- `Which trucks should be held outside the port?`
- `How much CO2 can we save?`
- `Explain NCIC alignment.`
- `What would a 90-day pilot look like?`
- `What integrations are required?`
- `Generate executive brief.`
- `Prepare a 2-minute grant summary.`

The Copilot refuses unrelated medical/legal/financial advice, politics, celebrity/news/general knowledge, offensive prompts, prompt-injection attempts, secret/config requests and database dump requests.

## Truck Tracking & ETA Intelligence

The Truck Tracking page has no external GPS or map API dependency. It displays active trucks, delayed trucks, hold-outside-port candidates, priority-release candidates, estimated queue idling, indicative CO₂ exposure, checkpoint/geofence labels, ETA, delay risk and recommended operator actions.

## Scenario Simulator

The simulator models truck arrival spikes, vessel delays, berth pressure, crane availability, yard backlog, peak gate congestion and load-shedding. Outputs include indicative risk scores, waiting-time changes, idling minutes, fuel cost and CO₂ impact.

## Emissions / idling assumptions

Emissions figures are indicative demo estimates based on idling minutes and fixed diesel/CO₂ assumptions. They are useful for comparing scenarios and explaining avoidable idling, but they are **not verified emissions outcomes**.

## Executive Impact Centre

The Executive Impact Centre shows deterministic demo KPIs for estimated idling minutes avoided, CO₂ reduction potential, diesel/fuel cost saving, gate queue reduction, berth utilisation improvement potential, truck turnaround improvement, energy exposure, operator actions, high-risk items, operational hours saved and dispatch coordination improvement.

## Clean Logistics Impact

The Clean Logistics page frames the demonstrator around congestion, idling, diesel waste, load-shedding disruption and limited decision visibility. It explains the solution, impact areas, demo-first advantage and NCIC-style pilot outcomes while keeping all claims realistic and indicative.

## Pilot Readiness

The Pilot Readiness page defines:

- 30-day pilot setup;
- 60-day integration prototype;
- 90-day operational pilot;
- required integrations;
- success metrics;
- risks and mitigations;
- integration readiness wall.

## Stakeholder Value

Stakeholder cards explain value for port authorities, terminal operators, fleet operators, sustainability officers, municipalities/provinces and operations managers.

## Executive Brief

The Executive Brief page generates a deterministic, print-friendly shift report with date/time, top risk, port flow status, truck queue status, delayed/held trucks, gate/berth/yard pressure, emissions/idling estimate, energy risk, action plan, expected impact, integration readiness note and synthetic-data disclaimer.

## Integration-ready architecture

The codebase includes demo-backed integration interfaces that are active by default:

- `ITruckTelematicsProvider` → `DemoTruckTelematicsProvider`
- `IGpsTrackingProvider` → `DemoGpsTrackingProvider`
- `IGateSystemProvider` → `DemoGateSystemProvider`
- `IPortOperationsProvider` → `DemoPortOperationsProvider`
- `IEnergyDisruptionProvider` → `DemoEnergyDisruptionProvider`
- `IEmissionsFactorProvider` → `DemoEmissionsFactorProvider`
- `IExternalIntegrationHealthService` → `DemoExternalIntegrationHealthService`

Demo mode uses synthetic data. Production integrations would connect to GPS/telematics, TOS/IPMS, gate systems, energy schedules and emissions factors only after partner approval, governance and security review. Integration adapters are prepared but disabled/demo-backed in demo mode.

## Demo vs production distinction

| Area | Demo mode | Production direction |
| --- | --- | --- |
| Data | Synthetic/demo seed data | Partner-approved operational feeds |
| AI | Local deterministic rules/scoring | May remain deterministic or add governed models later |
| GPS/maps | No external GPS/map API | Telematics/GPS provider via approved adapter |
| Port systems | No live TOS/IPMS integration | TOS/IPMS adapter after pilot agreement |
| Emissions | Indicative assumptions | Validated factors/methodology |
| Deployment | Demonstrator app | Controlled pilot / production pathway |

## Local run instructions

```bash
dotnet restore SmartPort.sln
dotnet build SmartPort.sln
docker compose up --build
```

Default application URL:

```text
http://localhost:8080
```

Reset seeded demo data:

```bash
docker compose down -v
docker compose up --build
```

## DigitalOcean deployment command

From the repository root on the droplet:

```bash
git pull && docker compose up -d --build
```

If a full reset is intentionally required for a demo environment:

```bash
docker compose down -v && docker compose up -d --build
```

## Limitations

- Synthetic/demo data only.
- No live Transnet, TOS/IPMS, GPS, telematics, gate OCR/RFID, energy schedule or external emissions-factor integration is claimed.
- No verified emissions outcome is claimed.
- No paid external AI API is required or used by default.
- The platform is a demonstrator and pilot-ready direction, not a claimed production rollout.

## Next phase roadmap

- Partner-approved pilot site and baseline metrics.
- One integration at a time: gate queue, truck dispatch/telematics, berth/vessel schedule, energy disruption, emissions factors.
- Operator feedback loop and recommendation adoption tracking.
- Export/reporting hardening for pilot review.
- Security, role model and audit workflow review.

## Gemini Agent Intelligence Mode

Culltron Smart Port Flow now includes an optional **Gemini Agent Intelligence Mode** for richer enterprise operations briefs, report generation, recommendation explanation, scenario analysis, emissions planning, incident response notes, pilot-readiness summaries, and Copilot enhancements.

Gemini is optional. The deterministic local Smart Port intelligence engine remains the default safety fallback, and the application must continue to run without any Gemini API key. Gemini enhances, explains, summarizes, and structures operator-facing text; it does not silently replace deterministic operational scoring, recommendations, audit controls, or human approvals.

### Running without Gemini

No setup is required. Leave `GEMINI_API_KEY` unset and keep the default configuration:

```json
"Gemini": {
  "Enabled": false,
  "Model": "gemini-2.5-flash",
  "TimeoutSeconds": 20,
  "MaxOutputTokens": 2048
}
```

The UI will show local fallback / not configured status, and report buttons will still generate useful deterministic Smart Port outputs.

### Running with Gemini locally

Set the key as a server-side environment variable only, then enable Gemini configuration through environment variables or local, uncommitted settings:

```bash
export GEMINI_API_KEY="your_new_key_here"
export Gemini__Enabled=true
export Gemini__Mode=Hybrid
```

Then run the app normally. Gemini calls are button-triggered only from the Copilot / Agent Reports workflow; the app does not call Gemini automatically on page load, during seed generation, or in loops.

### Docker and deployment notes

For Docker or production hosting, inject the key through the runtime environment or secret manager instead of committed files. For example, use your platform's environment-variable settings or a Docker secret to provide `GEMINI_API_KEY` to the web container at runtime. Do not place the key in `appsettings.json`, `appsettings.Development.json`, `Dockerfile`, `docker-compose.yml`, `launchSettings.json`, README examples, screenshots, logs, or source code.

### Security and data-boundary notes

Never commit Gemini keys. The application reads the key only from `GEMINI_API_KEY`, never logs it, and never displays it in UI or diagnostics. Gemini receives only sanitized operational summaries such as truck queue estimates, berth/yard pressure, active incidents, idling/emissions estimates, deterministic recommendation summaries, and the requested report type. Do not send secrets, connection strings, credentials, private configs, customer data, API tokens, or sensitive internal values.

Gemini prompts are grounded in the Culltron Smart Port Flow prototype/demo context. Responses must not invent real Transnet access, live IPMS access, Navayuga integration, production deployment at a port, real customer data, guaranteed savings, or signed pilot agreements. Use careful wording such as “prototype,” “demo,” “pilot-ready architecture,” “designed to integrate,” “can support,” and “would require live data integration for production use.”

### Demo testing

Use the visible button-triggered report actions, such as Executive Operations Brief, Scenario Analysis Report, Operator Action Plan, Emissions Reduction Report, Incident Response Report, Pilot Readiness Report, and Daily Port Operations Report. With no key configured, verify local fallback output. With a key configured and Gemini enabled, test only a few intentional calls for demo usage.
