# GCIP South Africa — Environmental Impact Methodology & SDG/GEF Alignment
### Culltron Smart Port Flow

> **Status of these numbers:** every figure below is **transparently modelled potential**, derived
> from documented assumptions and the same emission factors used inside the product. They are
> **not measured results.** Actual reductions can only be claimed after a supervised pilot
> compares baseline vs. optimised operations. This document exists so that an evaluator (and a
> GEF/UNIDO MRV reviewer) can reproduce and stress-test every number.

---

## 1. Parameters & sources

| Parameter | Value | Basis |
|---|---|---|
| Truck idling fuel burn | **3.0 L/hour** | In-app constant `FlowIntelligence:IdlingLitresPerHour`; within the typical 2–4 L/h range for idling heavy diesel vehicles |
| Diesel CO₂ factor | **2.68 kg CO₂ / litre** | In-app constant `Co2KgPerLitreDiesel`; standard diesel combustion factor |
| Diesel price | **R24.00 / litre** | In-app constant `DieselPricePerLitre` (indicative SA pump price; applicant to confirm current) |
| Operating days / year | **250** | Conservative working-days assumption (applicant may revise for 24/7 ports) |

**Derived unit factor:**

```
CO₂ avoided per truck-idling-hour eliminated
  = 3.0 L/h × 2.68 kg/L
  = 8.04 kg CO₂  per truck-hour

Diesel cost avoided per truck-idling-hour eliminated
  = 3.0 L/h × R24.00/L
  = R72.00 per truck-hour
```

All three constants are **single sources of truth in the codebase**
(`src/SmartPort.Web/appsettings.json` → `FlowIntelligence`), so the product's on-screen impact and
this document use identical maths.

---

## 2. Worked scenarios (modelled)

### 2a. Pilot scale — single willing fleet
Assumptions: **25 trucks**, **1 port visit/truck/day**, **22 minutes** of avoidable idling removed
per visit (via dispatch metering + outer staging + gate expediting).

```
Avoided truck-hours/day = 25 × (22 / 60)         = 9.17 truck-h/day
CO₂ avoided/day         = 9.17 × 8.04 kg          ≈ 73.7 kg/day  (0.074 tCO₂/day)
CO₂ avoided/year        = 0.074 × 250             ≈ 18.4 tCO₂e/year
Diesel avoided/year     = 9.17 × 3.0 × 250        ≈ 6,878 L/year
Diesel cost avoided/yr  = 6,878 × R24             ≈ R165,000/year
```
**Pilot band (sensitivity 15–30 min/visit): ~13–25 tCO₂e/year.**

### 2b. Single-terminal scale (modelled high-throughput)
Assumptions: **1,500 addressable truck-visits/day**, **20 minutes** avoidable idling removed/visit.

```
Avoided truck-hours/day = 1,500 × (20 / 60)       = 500 truck-h/day
CO₂ avoided/day         = 500 × 8.04 kg           = 4,020 kg/day  (4.02 tCO₂/day)
CO₂ avoided/year        = 4.02 × 250              ≈ 1,005 tCO₂e/year
Diesel avoided/year     = 500 × 3.0 × 250         = 375,000 L/year
Diesel cost avoided/yr  = 375,000 × R24           ≈ R9.0 million/year
```
**Terminal band (sensitivity): ~0.8–1.2 ktCO₂e/year per high-throughput terminal.**

### 2c. Multi-port / 5-year potential (illustrative ceiling)
If adopted across several South African terminals and their fleets over a 5-year scale-up, the
cumulative **lifetime** mitigation potential is on the order of **single-digit ktCO₂e per terminal
per year × number of terminals × years**, i.e. a **plausible multi-kilotonne cumulative** figure.
This is an **illustrative ceiling**, fully dependent on adoption, and is presented only to show
order-of-magnitude relevance for GEF climate-mitigation objectives. **Do not quote as a commitment.**

> **MRV note:** because every avoided-idling decision is quantified and written to the immutable
> decision audit trail, a pilot produces a defensible, line-by-line evidence base to replace these
> models with **measured** reductions — exactly the verification GEF/UNIDO require.

---

## 3. Co-benefits (qualitative + indicative)

- **Local air quality / health:** less idling near port precincts reduces NOₓ and particulate
  matter in often low-income adjacent communities (SDG 11, SDG 3).
- **Energy resilience:** energy-aware sequencing reduces load-shedding exposure for reefers, cranes
  and gate systems (SDG 7).
- **Economic:** lower diesel spend, reduced demurrage/delay cost, better asset utilisation.
- **Driver welfare & fairness:** shorter idling, transparent call-forward sequencing (SDG 8).

---

## 4. SDG alignment matrix

| SDG | How Smart Port contributes |
|---|---|
| **13 Climate Action** | Directly cuts transport-sector CO₂ by eliminating avoidable idling; impact is quantified and audited |
| **9 Industry, Innovation & Infrastructure** | Digital coordination layer that modernises port/fleet operations without new heavy infrastructure |
| **11 Sustainable Cities & Communities** | Reduces port-city congestion and air pollution affecting adjacent communities |
| **7 Affordable & Clean Energy** | Energy/load-shedding-aware operations reduce reactive, emissions-heavy responses |
| **8 Decent Work & Economic Growth** | Less unpaid queue time for owner-drivers and small hauliers; economic efficiency |
| **5 Gender Equality** | Gender-mainstreaming plan: diverse team, inclusive driver UX, sex-disaggregated pilot reporting |
| **3 Good Health & Well-being** | Lower local air-pollutant exposure |
| **17 Partnerships for the Goals** | Connector-ready to partner with ports, fleets, energy and public bodies under proper agreements |

---

## 5. GEF / UNIDO framing

- **GEF focal area:** Climate Change Mitigation.
- **Mechanism:** Energy efficiency in the transport/logistics sector through **digital
  coordination** — a low-capital, fast-payback lever distinct from vehicle electrification.
- **Additionality:** the reductions come from preventing *avoidable* idling that would otherwise
  occur; they are not business-as-usual and are attributable to the coordination the platform adds.
- **Scalability:** software scales across berths, fleets and ports with low marginal cost.
- **MRV:** built-in quantification + immutable audit trail = a verification-ready foundation.

---

## 6. Assumptions register (for reviewers to challenge)

1. Idling burn of 3.0 L/h is a mid-range assumption; actuals vary by engine, ambient temperature
   and reefer load. **Pilot to measure.**
2. Avoidable-idling minutes per visit (15–30 min) are modelled, not observed. **Pilot to measure.**
3. Truck-visit volumes are illustrative; real figures require terminal data under a data-sharing
   agreement. **Pilot to confirm.**
4. Diesel price and operating-day count are indicative and should be set to current verified values.
5. Rebound effects (induced demand from smoother flow) are not modelled and should be considered in
   a rigorous pilot evaluation.

> **Bottom line:** Smart Port has a **credible, transparent, and reproducible** path to verifiable
> climate impact. These models size the opportunity honestly; a GCIP-supported pilot converts them
> into measured, MRV-grade reductions.
