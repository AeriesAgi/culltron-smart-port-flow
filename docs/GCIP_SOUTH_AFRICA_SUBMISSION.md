# GCIP South Africa — Application Narrative
### Culltron Smart Port Flow — AI for clean, congestion-free port logistics

> **Programme:** Global Cleantech Innovation Programme (GCIP) South Africa — implemented by
> UNIDO, funded through the Global Environment Facility (GEF), hosted nationally by the
> Technology Innovation Agency (TIA) under the Department of Science and Innovation (DSI).
>
> **Cleantech category:** Energy Efficiency · Clean Transport & Smart Logistics (digital enabler).
> **GEF focal area:** Climate Change Mitigation.
>
> **Honesty statement (read first):** Culltron Smart Port Flow is a **functional software
> prototype running on synthetic / demonstration data**. It does **not** claim any live
> integration, signed pilot, customer, revenue, or partnership with Transnet, any terminal
> operating system, port authority, or government body. South African macro figures cited
> (port congestion, load-shedding, diesel cost) are drawn from **publicly reported** information
> and are used only to size the problem. All environmental impact figures are **transparently
> modelled potential**, not measured results. Applicant-specific fields (company registration,
> B-BBEE, team CVs, audited financials, IP filings, letters of support) are marked
> **`[APPLICANT TO COMPLETE]`** and must be filled with verified information before submission.

---

## 0. Applicant snapshot (form fields)

| Field | Value |
|---|---|
| Innovation name | Culltron Smart Port Flow |
| One-line description | AI decision-support that links port congestion, gate, berth, emissions and disruption signals into one human-approved action that cuts truck idling, diesel burn and CO₂. |
| Cleantech category | Energy Efficiency / Clean Transport & Smart Logistics |
| Development stage | Functional prototype on synthetic data (TRL 5–6) |
| Legal entity | `[APPLICANT TO COMPLETE — registered company name, CIPC reg. no.]` |
| B-BBEE level | `[APPLICANT TO COMPLETE]` |
| SARS / tax status | `[APPLICANT TO COMPLETE]` |
| Province / base | `[APPLICANT TO COMPLETE — e.g. KwaZulu-Natal]` |
| Lead applicant & contact | `[APPLICANT TO COMPLETE]` |
| IP status | Proprietary source; no patents filed (see §7) |
| Funding ask (GCIP) | Acceleration, mentoring, market access, and grant/seed support — `[APPLICANT TO COMPLETE amount]` |

---

## 1. Executive summary

South Africa's container ports are among the most congested in the world. The bottleneck is
rarely raw capacity — it is **coordination**. The control room, the fleet owner and the driver
each see a different fragment of the picture and act on stale, partial information. The result is
thousands of trucks **idling** at gates and in queues: burning diesel, emitting CO₂ and local air
pollutants into port-adjacent communities, and racking up demurrage and lost economic output.

**Culltron Smart Port Flow** is an enterprise AI platform that turns fragmented operational
signals — vessel/berth/gate/yard/container/truck-queue/disruption/**emissions**/energy data — into
**one prioritised, quantified, human-approved execution recommendation**. Its cross-domain
reasoning engine explicitly links domains (e.g. *berth pressure + gate backlog → N trucks idling →
tonnes CO₂ + Rand demurrage risk → re-sequence these trucks, expedite this gate*), pushes the
resulting instruction to the driver's phone, and renders the fleet's live position back to the
fleet owner — closing a **Sense → Reason → Approve → Execute** loop.

The cleantech value is direct: **every minute of idling avoided is diesel not burned and CO₂ not
emitted**, with the impact made visible and auditable at every step. The platform runs fully on a
**deterministic engine with no LLM required**, so the climate logic is transparent and reproducible,
with an optional Gemini layer for narrative reasoning.

GCIP support would take the validated prototype through a **supervised port/fleet pilot** to
measured, verifiable emissions reductions and a commercial South African SaaS offering, with a
clear path to other African ports.

---

## 2. The problem (environmental + economic)

**Environmental.** Heavy diesel trucks idling in port queues consume an estimated **2–4 litres of
diesel per hour** while stationary. At a diesel combustion factor of **~2.68 kg CO₂ per litre**, an
idling truck emits roughly **~8 kg CO₂ for every idling-hour** — plus NOₓ and particulate matter
concentrated in low-income communities living next to port precincts. Multiplied across thousands
of daily truck visits at a major terminal, avoidable idling is a material, **measurable** source of
transport-sector emissions and local air-quality harm.

**Economic.** Port congestion in South Africa is **publicly documented** as a drag on the economy —
ships waiting at anchorage, trucks queueing for hours, and exporters/importers carrying demurrage
and delay costs. Diesel (≈ **R24/litre**, indicative) burned while idling is pure waste.

**Energy.** Load-shedding compounds the problem: reefer power, cranes and gate systems are exposed
to grid instability, forcing reactive, emissions-heavy operations. Decisions made without an
energy-aware view make this worse.

**Root cause = a coordination gap, not only a capacity gap.** The three actors who could prevent an
idling event — control room, fleet owner, driver — never share the same live picture. Smart Port
closes that gap.

---

## 3. The cleantech solution & how it works

Smart Port is a layered .NET platform with three operational surfaces — **control room**, **fleet
owner dashboard**, and a mobile **driver companion** — over a single reasoning core.

1. **Sense.** Connector-ready providers ingest telematics, GPS check-ins, gate, berth, yard,
   container, disruption and emissions/energy signals into one normalised operational picture.
   (In the prototype these are synthetic providers shaped like real port feeds.)
2. **Reason (the cleantech core).** A **cross-domain reasoning engine** links signals across
   domains into **one prioritised execution recommendation** with **quantified impact**: trucks
   affected, average idling minutes, **CO₂ avoided**, fuel cost and demurrage/delay risk in Rand.
   The deterministic engine produces the full quantified result with **no LLM required**; an
   optional Gemini layer adds narrative reasoning but **never changes the numbers**.
3. **Approve.** Every recommendation is **decision-support only** and passes a **human
   Approve / Modify / Reject gate**. Nothing is auto-executed.
4. **Execute.** The approved instruction reaches the driver's phone (tap-to-check-in, no background
   tracking); the driver's check-in updates the **fleet tracker** and the **immutable audit trail**.

**Why this is credible cleantech, not vapourware:**
- The emissions logic is **deterministic and transparent** — same inputs, same CO₂/diesel maths,
  auditable line by line. It does not depend on a black-box model.
- Impact is **quantified and logged** at the point of every decision, giving a built-in MRV
  (measurement, reporting & verification) foundation that GEF/UNIDO require.
- It is **non-invasive**: it improves *coordination* of existing assets rather than requiring new
  port infrastructure, so payback and emissions reductions can start in a pilot.

---

## 4. Technology readiness & development stage

- **TRL 5–6** — technology validated in a relevant/simulated environment; a complete, working
  functional prototype with all core surfaces operational on synthetic data. **Not yet field-piloted.**
- **What works today:** cross-domain reasoning engine + deterministic fallback, execution-plan
  workflow, human-approval gates, immutable decision audit trail, driver companion (Android +
  web + token-secured mobile API), fleet tracker rendered from check-in data, emissions/idling
  impact maths, role-based access control, health endpoints, Docker/PostgreSQL deployment shape.
- **What a production deployment requires** (documented honestly): data-sharing agreements, live
  connector integration (TOS/IPMS, gate/SCADA, telematics, energy/AMI), independent security
  review, and supervised operational acceptance. See `docs/PRODUCTION_HARDENING_BACKLOG.md` and the
  in-app **Pilot-to-Production** page.

---

## 5. Innovation & competitive advantage

- **Cross-domain reasoning, not single-metric dashboards.** Most port/fleet tools report one
  domain (gate queue *or* berth *or* GPS). Smart Port's differentiator is **linking** them into a
  single ranked action with quantified climate and cost impact.
- **Deterministic-first, LLM-optional.** The platform works with **no AI key configured**, so the
  emissions logic is reproducible and auditable — important for both enterprise trust and GEF MRV.
- **Climate impact as a first-class output**, embedded in every recommendation and the audit trail,
  not bolted on as an afterthought.
- **Human-in-the-loop governance** with an immutable audit trail — deployable in a risk-averse,
  unionised, safety-critical environment.
- **Low-friction adoption** — coordinates existing trucks, gates and berths; the driver side runs
  on an ordinary smartphone.

---

## 6. Market analysis

- **Primary market:** South African commercial ports and the road-freight fleets that serve them
  (KwaZulu-Natal container corridors first, given Durban's role as the busiest container port in
  sub-Saharan Africa). Buyers: terminal operators, fleet owners/hauliers, logistics partners.
- **Adjacent markets:** inland terminals/depots, mines and bulk sites with gate-queue and idling
  problems, and **other African ports** facing the same congestion + energy-instability profile.
- **Pricing model (see in-app Pricing page):** SaaS metered on three dimensions — **per berth**,
  **per fleet vehicle**, and **per port** platform fee — across Demo → Pilot → Enterprise tiers.
- **Market sizing:** `[APPLICANT TO COMPLETE with verified TAM/SAM/SOM and any letters of intent.]`

---

## 7. Intellectual property

- Proprietary source code and product design; the cross-domain reasoning method, deterministic
  impact model, and audit/approval workflow are the core IP.
- **No patents currently filed.** A defensive IP review (trade-secret vs. patent for the reasoning
  method) is a recommended GCIP-phase activity.
- No third-party IP is misrepresented; optional Gemini and WhatsApp integrations are clearly
  external services used under their own terms.
- `[APPLICANT TO COMPLETE — confirm code ownership/assignment, trademarks, and any open-source
  licence obligations.]`

---

## 8. Environmental impact & GEF / SDG alignment

Full methodology, assumptions and a worked calculation are in **`docs/GCIP_IMPACT_AND_SDG.md`**.
Headline, transparently-modelled potential (clearly **not** measured results):

- **Unit basis:** ~**8.04 kg CO₂ avoided per truck-idling-hour eliminated**
  (3.0 L/h idling × 2.68 kg CO₂/L).
- **Pilot scale** (≈25-truck fleet, ~20–25 min avoidable idling/visit): **~20–25 tCO₂e/year**.
- **Single-terminal scale** (modelled high-throughput): **~0.8–1.2 ktCO₂e/year**.
- **Co-benefits:** diesel cost savings (≈ R24/L), reduced NOₓ/PM near port communities, shorter
  driver hours, lower demurrage, and energy-aware operations that reduce load-shedding exposure.

**GEF focal area:** Climate Change Mitigation (transport-sector energy efficiency via digital
coordination). **MRV-ready** by design: every avoided-idling decision is quantified and written to
an immutable audit trail, giving a verifiable basis for reporting reductions.

**SDG alignment:** SDG 13 (Climate Action), SDG 9 (Industry, Innovation & Infrastructure),
SDG 11 (Sustainable Cities — port-city congestion & air quality), SDG 7 (Affordable & Clean
Energy — energy-aware operations), SDG 8 (Decent Work & Economic Growth), SDG 5 (Gender — see §9),
SDG 17 (Partnerships).

---

## 9. Social, economic & gender impact

- **Local air quality & health** in communities adjacent to port precincts.
- **Jobs & SMME inclusion:** smoother dispatch supports small hauliers and owner-drivers who are
  hit hardest by unpredictable, unpaid queue time.
- **Driver welfare:** less time idling in cabs; clearer, fairer call-forward sequencing.
- **Gender mainstreaming (GCIP priority):** an explicit plan to (a) build a diverse founding/pilot
  team, (b) design the driver companion for accessibility and inclusion of women in logistics, and
  (c) report sex-disaggregated participation during the pilot. `[APPLICANT TO COMPLETE specifics.]`

---

## 10. Business model & commercialisation

- **Revenue:** SaaS subscription metered per berth / per vehicle / per port (see Pricing page),
  plus pilot/onboarding and integration services.
- **Go-to-market:** land-and-expand from a single bounded pilot (one terminal + a willing fleet)
  to additional berths and fleets, then additional ports.
- **Onboarding model:** Discovery → Connect → Pilot → Scale (see in-app Pilot-to-Production page).
- **Financial projections:** `[APPLICANT TO COMPLETE — revenue model, unit economics, 3–5 yr
  forecast, funding use-of-proceeds.]`

---

## 11. Team

`[APPLICANT TO COMPLETE — founders and key team, roles, relevant logistics/climate/software
experience, advisors, and demonstrated commitment. Attach CVs. Highlight diversity and any
domain/port experience.]`

---

## 12. Milestones & use of GCIP support

| Phase | Goal | Key activities |
|---|---|---|
| 0. Now | Validated prototype | Cross-domain engine, audit, driver/fleet loop on synthetic data (done) |
| 1. GCIP acceleration | Pilot-ready | Mentoring, business model & financials, gender plan, IP review, security review scoping |
| 2. Supervised pilot | **Measured** reductions | Data-sharing agreement, connect one terminal + one fleet, run baseline-vs-current, verify tCO₂/diesel saved via the audit trail |
| 3. Productisation | Commercial SaaS | Security hardening (SSO, secrets vault, durable audit), SLAs, support model |
| 4. Scale | Multi-port | Expand berths/fleets/ports; African market entry |

GCIP support sought: **acceleration & mentoring, market access to port/fleet stakeholders,
investor readiness, and grant/seed funding** to reach a measured pilot. `[APPLICANT TO COMPLETE
specific ask.]`

---

## 13. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Data-sharing/access to live port systems | Connector-ready architecture + NDA/data-sharing pathway; pilot scoped to a willing fleet first |
| Emissions claims must be verifiable | Deterministic, audited impact maths = built-in MRV; pilot measures actuals against baseline |
| Stakeholder change-management (unions, operators) | Human-approval gates, no automation of safety-critical actions, transparent audit trail |
| LLM dependency / cost | Deterministic engine works with no LLM; Gemini is optional and quota-guarded |
| Security in a critical environment | Documented hardening roadmap; independent review before production |

---

## 14. Pre-submission checklist (applicant)

- [ ] Confirm eligibility (South African registered SMME / innovator team per the open GCIP call)
- [ ] CIPC registration, B-BBEE certificate, SARS/tax clearance — `[APPLICANT]`
- [ ] Team CVs and signed commitment — `[APPLICANT]`
- [ ] TAM/SAM/SOM market sizing and any letters of interest/support — `[APPLICANT]`
- [ ] 3–5 year financial model and funding ask — `[APPLICANT]`
- [ ] IP ownership/assignment confirmation — `[APPLICANT]`
- [ ] Gender-mainstreaming plan with measurable targets — `[APPLICANT]`
- [ ] Pitch deck (see `docs/GCIP_PITCH_DECK_OUTLINE.md`)
- [ ] Impact model reviewed and assumptions confirmed (`docs/GCIP_IMPACT_AND_SDG.md`)
- [ ] Live demo link / video and demo access credentials ready for evaluators

> **Reminder:** keep every claim honest. Present Smart Port as a **synthetic-data prototype with
> modelled impact potential**. Do not claim live port/customer/government partnerships or measured
> savings until a supervised pilot produces verified data.
