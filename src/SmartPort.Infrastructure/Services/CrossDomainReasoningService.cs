namespace SmartPort.Infrastructure.Services;

/// <summary>
/// One prioritised, quantified execution recommendation produced by LINKING signals
/// across domains (congestion + gate backlog + berth + emissions + disruptions) rather
/// than answering each domain in isolation. The deterministic engine and the optional
/// Gemini enhancement both produce this exact shape, so the platform never degrades when
/// no LLM is configured.
/// </summary>
public sealed class CrossDomainRecommendation
{
    public string Id { get; set; } = "REC-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>The one-line quantified statement, e.g.
    /// "Berth B3 congestion + Gate GP-2 backlog = 14 trucks idling avg 38 min → +2.1t CO₂, ~R47k demurrage risk."</summary>
    public string Headline { get; set; } = string.Empty;

    public string PrimaryDriverA { get; set; } = string.Empty; // e.g. "Berth B3 congestion"
    public string PrimaryDriverB { get; set; } = string.Empty; // e.g. "Gate GP-2 backlog"
    public List<string> LinkedSignals { get; set; } = new();   // the cross-domain factors that combined

    // Quantified impact
    public int TrucksAffected { get; set; }
    public int AvgIdlingMinutes { get; set; }
    public decimal Co2Kg { get; set; }
    public decimal Co2Tonnes { get; set; }
    public decimal DemurrageRiskRand { get; set; }
    public decimal FuelCostRand { get; set; }

    // Recommendation
    public string RecommendedAction { get; set; } = string.Empty;
    public List<string> ActionSteps { get; set; } = new();

    public string Severity { get; set; } = "Medium"; // Low / Medium / High / Critical
    public int Confidence { get; set; } = 88;

    // Provenance & governance
    public string Source { get; set; } = "Deterministic fallback engine";
    public bool UsedGemini { get; set; }
    public string? GeminiNarrative { get; set; }
    public string ApprovalState { get; set; } = "Pending"; // Pending until a human Approves / Modifies / Rejects
    public bool HumanApprovalRequired { get; set; } = true;
    public bool NotAutomaticallyExecuted { get; set; } = true;
    public string DataNote { get; set; } =
        "Synthetic demo data · quantified impact is computed deterministically · human approval required · not automatically executed.";

    /// <summary>Readable CO₂ figure that shows tonnes only when meaningful, otherwise kg.</summary>
    public string Co2Display => Co2Kg >= 1000m ? $"{Co2Tonnes:0.0}t CO₂" : $"{Co2Kg:0} kg CO₂";
}

public interface ICrossDomainReasoningService
{
    /// <summary>Page-load safe: deterministic only, never calls an LLM.</summary>
    Task<CrossDomainRecommendation> GetDeterministicAsync();

    /// <summary>Action-triggered: deterministic numbers, plus optional Gemini narrative when configured.
    /// Records the generated recommendation to the decision audit trail.</summary>
    Task<CrossDomainRecommendation> GenerateAsync(string actor, bool allowGemini, CancellationToken ct = default);
}

public sealed class CrossDomainReasoningService : ICrossDomainReasoningService
{
    // Indicative synthetic cost factors (kept consistent with FlowIntelligence defaults).
    private const decimal IdlingLitresPerHour = 3.0m;
    private const decimal DieselPricePerLitre = 24.0m;
    private const decimal Co2KgPerLitreDiesel = 2.68m;
    // Indicative demurrage / appointment-miss risk exposure per truck-hour of avoidable idling.
    private const decimal DemurrageRandPerTruckHour = 850m;

    private readonly ISmartPortIntelligenceService _intelligence;
    private readonly ITruckTrackingService _tracking;
    private readonly IAiAgentService _agent;
    private readonly IAgentNarrativeService _narrative;
    private readonly IDecisionAuditService _audit;

    public CrossDomainReasoningService(
        ISmartPortIntelligenceService intelligence,
        ITruckTrackingService tracking,
        IAiAgentService agent,
        IAgentNarrativeService narrative,
        IDecisionAuditService audit)
    {
        _intelligence = intelligence;
        _tracking = tracking;
        _agent = agent;
        _narrative = narrative;
        _audit = audit;
    }

    public async Task<CrossDomainRecommendation> GetDeterministicAsync()
    {
        var snapshot = await _intelligence.GetSnapshotAsync();
        var trucks = await _tracking.GetDashboardAsync();
        return BuildDeterministic(snapshot, trucks);
    }

    public async Task<CrossDomainRecommendation> GenerateAsync(string actor, bool allowGemini, CancellationToken ct = default)
    {
        var snapshot = await _intelligence.GetSnapshotAsync();
        var trucks = await _tracking.GetDashboardAsync();
        var rec = BuildDeterministic(snapshot, trucks);

        if (allowGemini)
        {
            try
            {
                var status = _narrative.GetStatus();
                if (status.GeminiEnabled && status.GeminiConfigured)
                {
                    var ctx = await _agent.GetContextAsync();
                    var enhanced = await _narrative.GenerateAsync(new AgentNarrativeRequest
                    {
                        Purpose = "cross-domain execution recommendation",
                        ReportType = "Cross-Domain Execution Recommendation",
                        DetectedIntent = "cross-domain-reasoning",
                        UserPrompt =
                            "Explain, in 2-4 sentences, why these linked signals combine into one prioritised action. " +
                            "Do NOT change the numbers; reinforce the deterministic recommendation below. Deterministic recommendation: " +
                            rec.Headline + " Recommended action: " + rec.RecommendedAction,
                        CurrentPage = "Cross-Domain Reasoning",
                        RequestedMode = AgentMode.Hybrid,
                        TaskCategory = GeminiTaskCategory.Premium,
                        ActionType = "cross-domain-reasoning",
                        Context = ctx,
                        DeterministicRecommendations = rec.ActionSteps
                    }, ct);

                    if (enhanced.UsedGemini && !string.IsNullOrWhiteSpace(enhanced.Narrative))
                    {
                        rec.UsedGemini = true;
                        rec.GeminiNarrative = enhanced.Narrative.Trim();
                        rec.Source = "Gemini enhancement + deterministic impact engine";
                    }
                }
            }
            catch
            {
                // Never let an LLM problem break the recommendation — deterministic result already stands.
                rec.UsedGemini = false;
            }
        }

        _audit.Record(
            actor: actor,
            category: "Cross-Domain Reasoning",
            action: "Recommendation generated",
            subject: rec.Id,
            reasoning: $"{rec.PrimaryDriverA} + {rec.PrimaryDriverB}. " + string.Join("; ", rec.LinkedSignals),
            source: rec.Source,
            approvalState: rec.ApprovalState,
            impact: ImpactSummary(rec));

        return rec;
    }

    public static string ImpactSummary(CrossDomainRecommendation rec)
        => $"{rec.TrucksAffected} trucks · {rec.AvgIdlingMinutes} min avg idling · {rec.Co2Display} · ~R{rec.DemurrageRiskRand:#,0} demurrage risk";

    // ── Deterministic core ──────────────────────────────────────────────────────
    private CrossDomainRecommendation BuildDeterministic(SmartPortOperationsSnapshot s, TruckTrackingDashboardDto t)
    {
        // 1) Identify the two strongest linked drivers across domains.
        var berthLabel = s.VesselsAtAnchor > 0 || s.BerthUtilisation >= 80
            ? $"Berth pressure ({s.BerthUtilisation:0}% utilised, {s.VesselsAtAnchor} at anchor)"
            : $"Berth utilisation {s.BerthUtilisation:0}%";
        var gateLabel = $"{s.TopGateBottleneck} ({s.TrucksInQueue} trucks queued)";

        // 2) Quantify trucks affected — prefer concrete hold/delay/queue figures.
        var trucksAffected = Math.Max(
            t.HoldOutsidePortCount + t.DelayedCount,
            Math.Max(s.TrucksInQueue, t.QueueRiskCount));
        if (trucksAffected <= 0) trucksAffected = Math.Max(1, t.ActiveTrucks);

        // 3) Idling and emissions from the shared snapshot/tracking figures.
        var totalIdling = Math.Max(s.IdlingMinutes, t.TotalIdlingMinutes);
        var avgIdling = (int)Math.Round(totalIdling / Math.Max(1, trucksAffected), MidpointRounding.AwayFromZero);
        if (avgIdling <= 0) avgIdling = (int)Math.Round(totalIdling > 0 ? totalIdling : 18m);

        var co2Kg = Math.Round(Math.Max(s.EmissionsIdlingEstimateKg, t.EstimatedCo2Kg), 1);

        // 4) Cost exposure (indicative).
        var idlingTruckHours = trucksAffected * (avgIdling / 60m);
        var fuelCost = Math.Round(idlingTruckHours * IdlingLitresPerHour * DieselPricePerLitre, 0);
        var demurrage = Math.Round(idlingTruckHours * DemurrageRandPerTruckHour, 0);

        // 5) Recommended action — re-sequence a slice of trucks + expedite the gate.
        var resequence = Math.Max(1, (int)Math.Ceiling(trucksAffected / 2.0));
        var holdCount = Math.Max(0, t.HoldOutsidePortCount);
        var priorityCount = Math.Max(0, t.PriorityReleaseCount);

        var rec = new CrossDomainRecommendation
        {
            PrimaryDriverA = berthLabel,
            PrimaryDriverB = gateLabel,
            TrucksAffected = trucksAffected,
            AvgIdlingMinutes = avgIdling,
            Co2Kg = co2Kg,
            Co2Tonnes = Math.Round(co2Kg / 1000m, 2),
            FuelCostRand = fuelCost,
            DemurrageRiskRand = demurrage,
            Severity = NormaliseSeverity(s.TopRiskSeverity),
            Confidence = Math.Clamp(Math.Min(s.Confidence, t.AiConfidenceScore), 50, 97),
            Source = "Deterministic fallback engine",
            RecommendedAction =
                $"Re-sequence {resequence} high-risk trucks to outer staging and expedite {ShortGate(s.TopGateBottleneck)} before the gate backlog cascades into berth and yard pressure."
        };

        rec.LinkedSignals.Add($"Gate: {s.TrucksInQueue} trucks queued · gate pressure {t.GatePressureScore}/100");
        rec.LinkedSignals.Add($"Berth: {s.BerthUtilisation:0}% utilised · {s.VesselsAtAnchor} vessel(s) at anchor");
        rec.LinkedSignals.Add($"Yard: {s.YardOccupancy:0}% occupancy");
        rec.LinkedSignals.Add($"Emissions: {totalIdling:0} idling min today · {rec.Co2Display}");
        rec.LinkedSignals.Add($"Disruptions: {s.ActiveDisruptions} active · energy: {s.LoadSheddingEnergyRisk}");

        rec.ActionSteps.Add(holdCount > 0
            ? $"Hold {holdCount} truck(s) at outer staging until {ShortGate(s.TopGateBottleneck)} backlog clears."
            : $"Stage {resequence} truck(s) outside the port until the gate backlog clears.");
        rec.ActionSteps.Add(priorityCount > 0
            ? $"Priority-release {priorityCount} appointment-critical truck(s) through {ShortGate(s.TopGateBottleneck)}."
            : $"Expedite {ShortGate(s.TopGateBottleneck)} — open an additional lane or add a clerk.");
        if (s.VesselsAtAnchor > 0 || s.BerthUtilisation >= 85)
            rec.ActionSteps.Add("Sequence yard moves to clear berth-side congestion before the next vessel window.");
        if (s.LoadSheddingEnergyRisk.Contains("Active", StringComparison.OrdinalIgnoreCase))
            rec.ActionSteps.Add("Apply the load-shedding playbook: protect reefer power and re-time energy-sensitive moves.");
        rec.ActionSteps.Add("On approval, notify affected fleet owners and drivers via the approved channel — no automatic send.");

        rec.Headline =
            $"{TitleDriver(berthLabel)} + {TitleDriver(gateLabel)} = {trucksAffected} trucks idling avg {avgIdling} min → " +
            $"{rec.Co2Display}, ~R{demurrage:#,0} demurrage risk. " +
            $"Recommended: re-sequence {resequence} trucks to outer staging, expedite {ShortGate(s.TopGateBottleneck)}.";

        return rec;
    }

    private static string TitleDriver(string label)
    {
        var idx = label.IndexOf(" (", StringComparison.Ordinal);
        return idx > 0 ? label[..idx] : label;
    }

    private static string ShortGate(string bottleneck)
    {
        if (string.IsNullOrWhiteSpace(bottleneck)) return "the primary gate";
        return bottleneck.Length > 42 ? bottleneck[..42].Trim() : bottleneck;
    }

    private static string NormaliseSeverity(string severity)
    {
        var s = (severity ?? string.Empty).Trim().ToLowerInvariant();
        return s switch
        {
            "critical" => "Critical",
            "high" => "High",
            "low" => "Low",
            _ => "Medium"
        };
    }
}
