using System.Text;

namespace SmartPort.Infrastructure.Services;

public class OperationalReportService : IOperationalReportService
{
    private readonly IAiAgentService _agent;
    private readonly IAgentNarrativeService _narrative;

    private static readonly string[] ReportTypes =
    {
        "Executive Operations Brief",
        "Operator Action Plan",
        "Explain Current Recommendations",
        "Analyze Scenario with AI Agent",
        "Emissions Reduction Report",
        "Incident Response Brief",
        "Pilot Readiness Summary",
        "Daily Port Operations Report"
    };

    public OperationalReportService(IAiAgentService agent, IAgentNarrativeService narrative)
    {
        _agent = agent;
        _narrative = narrative;
    }

    public IReadOnlyList<string> GetReportTypes() => ReportTypes;

    public AgentModeStatus GetStatus() => _narrative.GetStatus();

    public async Task<OperationalReportResult> GenerateAsync(OperationalReportRequest request, CancellationToken cancellationToken = default)
    {
        var ctx = await _agent.GetContextAsync();
        var narrativeRequest = new AgentNarrativeRequest
        {
            Purpose = "structured operational report",
            ReportType = NormalizeReportType(request.ReportType),
            RequestedMode = request.Mode,
            UserPrompt = request.UserPrompt,
            CurrentPage = "Agent Reports",
            Context = ctx,
            ScenarioSummary = request.ScenarioSummary,
            DeterministicRecommendations = ctx.TopRecommendations
        };

        var narrative = await _narrative.GenerateAsync(narrativeRequest, cancellationToken);
        var sections = BuildSections(narrativeRequest.ReportType, ctx, narrative.Narrative, request.ScenarioSummary);
        var markdown = BuildMarkdown(narrativeRequest.ReportType, sections, narrative);

        return new OperationalReportResult
        {
            ReportType = narrativeRequest.ReportType,
            Title = narrativeRequest.ReportType,
            GeneratedBy = narrative.GeneratedBy,
            UsedGemini = narrative.UsedGemini,
            FallbackActive = narrative.FallbackActive,
            GeminiStatus = narrative.Status,
            GeneratedAtUtc = narrative.GeneratedAtUtc,
            InputContextSummary = narrative.InputContextSummary,
            Sections = sections,
            Markdown = markdown
        };
    }

    private static string NormalizeReportType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return ReportTypes[0];
        return ReportTypes.FirstOrDefault(r => string.Equals(r, type.Trim(), StringComparison.OrdinalIgnoreCase)) ?? type.Trim();
    }

    private static List<ReportSectionDto> BuildSections(string reportType, OperationalContext ctx, string narrative, string scenarioSummary)
    {
        var commonRisk = ctx.BerthUtilisationPct >= 85 || ctx.YardOccupancyPct >= 85 || ctx.TrucksInQueue >= 20 ? "High" : ctx.TrucksInQueue >= 10 ? "Medium" : "Low";
        var recs = ctx.TopRecommendations.Any() ? ctx.TopRecommendations : new List<string> { "Meter truck arrivals against berth and yard capacity.", "Review active disruptions before approving operational changes.", "Keep all AI-generated recommendations human-approved and audit-tracked." };

        var sections = reportType switch
        {
            var t when t.Contains("Operator Action Plan", StringComparison.OrdinalIgnoreCase) => new List<ReportSectionDto>
            {
                new() { Heading = "Immediate Actions", Tone = "warning", Bullets = recs.Take(3).Append("Confirm human approval before dispatch, gate or berth changes.").ToList(), Body = "Focus the next shift on queue containment, berth stability and safe exception handling." },
                new() { Heading = "Next 2–4 Hours", Tone = "info", Bullets = new() { "Re-check truck ETA and hold/release candidates every operating window.", "Balance yard block pressure against vessel loading priorities.", "Escalate if queue, incident or energy conditions move from medium to high risk." } },
                new() { Heading = "Responsible Roles", Tone = "teal", Bullets = new() { "Port Operations Manager: approve plan and escalation thresholds.", "Gate Supervisor: meter arrivals and manage holds.", "Yard Controller: validate capacity and dwell pressure.", "Sustainability/Analytics Lead: track idling and CO₂ impacts." } },
                new() { Heading = "Expected Impact", Tone = "success", Body = "Reduced avoidable idling, clearer operator sequencing and audit-friendly decision ownership." }
            },
            var t when t.Contains("Emissions", StringComparison.OrdinalIgnoreCase) => new List<ReportSectionDto>
            {
                new() { Heading = "Idling / Congestion Drivers", Tone = "warning", Body = $"{ctx.TrucksInQueue} queued trucks, {ctx.TotalIdlingMinutesToday:F0} idling minutes and {ctx.EstimatedCo2Today:F1} kg CO₂ are the current synthetic exposure signals." },
                new() { Heading = "CO₂ / Fuel / Cost Opportunities", Tone = "success", Bullets = new() { "Meter arrivals to flatten gate peaks.", "Hold non-critical trucks outside the port until berth/yard capacity improves.", "Prioritise release of delayed high-risk or cold-chain vehicles where modeled." } },
                new() { Heading = "Traffic Smoothing Actions", Tone = "info", Bullets = new() { "Use ETA bands for gate release windows.", "Communicate exception-only access during high pressure.", "Review disruption routes before approving rerouting." } },
                new() { Heading = "Sustainability Impact", Tone = "teal", Body = "Impact language remains indicative for demo use and would require live telematics, gate timestamps and approved emissions factors for production reporting." }
            },
            var t when t.Contains("Scenario", StringComparison.OrdinalIgnoreCase) || t.Contains("Analyze Scenario", StringComparison.OrdinalIgnoreCase) => new List<ReportSectionDto>
            {
                new() { Heading = "Scenario Summary", Tone = "info", Body = string.IsNullOrWhiteSpace(scenarioSummary) ? "Scenario analysis uses the current synthetic operating state because no custom scenario was supplied." : scenarioSummary },
                new() { Heading = "Likely Operational Consequences", Tone = commonRisk == "High" ? "warning" : "info", Bullets = new() { $"Risk level: {commonRisk}.", $"Berth pressure: {ctx.BerthUtilisationPct:F0}% utilisation.", $"Yard pressure: {ctx.YardOccupancyPct:F0}% occupancy.", $"Queue pressure: {ctx.TrucksInQueue} trucks." } },
                new() { Heading = "Recommended Mitigation", Tone = "success", Bullets = recs.Take(4).ToList() },
                new() { Heading = "Operator Decision Points", Tone = "teal", Bullets = new() { "Approve or reject arrival metering.", "Select escalation owner.", "Confirm any customer/operator communication externally before action." } }
            },
            var t when t.Contains("Incident", StringComparison.OrdinalIgnoreCase) => new List<ReportSectionDto>
            {
                new() { Heading = "Incident / Disruption Summary", Tone = "warning", Body = $"{ctx.OpenIncidents} open incidents, {ctx.ActiveDisruptions} active disruptions and {ctx.CriticalDisruptions} critical disruption signals are present." },
                new() { Heading = "Severity and Affected Areas", Tone = commonRisk == "High" ? "danger" : "warning", Bullets = new() { $"Severity: {commonRisk}", "Affected areas may include gate flow, yard capacity, berth sequencing and fleet dispatch depending on supervisor review.", $"Energy risk active: {(ctx.LoadSheddingActive ? "Yes" : "No")}." } },
                new() { Heading = "Recommended Response", Tone = "success", Bullets = recs.Take(4).Append("Escalate if safety, cold-chain, berth or security thresholds are breached.").ToList() },
                new() { Heading = "Audit / Human Approval", Tone = "teal", Body = "AI-generated incident guidance is advisory only; all operational responses require accountable human approval and logging." }
            },
            var t when t.Contains("Pilot", StringComparison.OrdinalIgnoreCase) => new List<ReportSectionDto>
            {
                new() { Heading = "What the System Can Already Do", Tone = "success", Bullets = new() { "Demonstrate synthetic port command-center KPIs.", "Generate deterministic recommendations and AI-enhanced reports.", "Model scenarios, truck queues, idling/emissions and audit-friendly actions." } },
                new() { Heading = "Data Integrations Needed", Tone = "info", Bullets = new() { "Gate/OCR/RFID events and truck GPS/telematics.", "TOS/IPMS-style berth, vessel and yard feeds when formally approved.", "Incident, energy/load-shedding and emissions-factor data." } },
                new() { Heading = "Controls / Security Needed", Tone = "warning", Bullets = new() { "Server-side API-key management only.", "Role-based access, audit logs and human approvals.", "Data minimisation, redaction and partner-approved data boundaries." } },
                new() { Heading = "Pilot Phases and Next Steps", Tone = "teal", Bullets = new() { "Phase 1: demo workflow validation.", "Phase 2: one approved data feed and baseline metrics.", "Phase 3: supervised recommendations and weekly impact review.", "Production claims require live integration validation." } }
            },
            _ => new List<ReportSectionDto>
            {
                new() { Heading = "Current Operational State", Tone = commonRisk == "High" ? "warning" : "info", Body = $"{commonRisk} pressure with {ctx.TrucksInQueue} queued trucks, {ctx.BerthUtilisationPct:F0}% berth utilisation, {ctx.YardOccupancyPct:F0}% yard occupancy, {ctx.OpenIncidents} open incidents and {ctx.ActiveDisruptions} active disruptions." },
                new() { Heading = "Congestion / Queue Summary", Tone = "info", Body = $"Truck queue estimate is {ctx.TrucksInQueue}; gate delay active: {(ctx.GateDelayActive ? "Yes" : "No")}; road congestion active: {(ctx.RoadCongestionActive ? "Yes" : "No")}." },
                new() { Heading = "Berth / Yard Pressure", Tone = "teal", Body = $"Berths occupied/available: {ctx.BerthsOccupied}/{ctx.BerthsAvailable}. Containers in yard: {ctx.ContainersInYard}; dwell alerts: {ctx.DwellAlerts}." },
                new() { Heading = "Incidents and Emissions Notes", Tone = "warning", Body = $"Open incidents: {ctx.OpenIncidents}. Active disruptions: {ctx.ActiveDisruptions}. Estimated idling: {ctx.TotalIdlingMinutesToday:F0} min / {ctx.EstimatedCo2Today:F1} kg CO₂." },
                new() { Heading = "Top Recommended Actions", Tone = "success", Bullets = recs.Take(5).ToList() },
                new() { Heading = "Risks Requiring Human Review", Tone = "danger", Bullets = new() { "Do not auto-execute operational changes.", "Validate safety, labour, customer and port-authority constraints.", "Treat all demo impact values as indicative until live data integration is approved." } }
            }
        };

        if (!string.IsNullOrWhiteSpace(narrative))
        {
            sections.Insert(0, new ReportSectionDto
            {
                Heading = narrative.Contains("Generated by Gemini", StringComparison.OrdinalIgnoreCase) ? "Gemini Enhanced Narrative" : "Agent Narrative",
                Tone = narrative.Contains("Gemini", StringComparison.OrdinalIgnoreCase) ? "teal" : "info",
                Body = narrative
            });
        }

        return sections;
    }

    private static string BuildMarkdown(string reportType, List<ReportSectionDto> sections, AgentNarrativeResult narrative)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {reportType}");
        sb.AppendLine();
        sb.AppendLine($"Generated by: {narrative.GeneratedBy}{(narrative.FallbackActive ? " (fallback active)" : string.Empty)}");
        sb.AppendLine($"Timestamp: {narrative.GeneratedAtUtc:O}");
        sb.AppendLine($"Input context: {narrative.InputContextSummary}");
        sb.AppendLine("Human approval required: Yes");
        sb.AppendLine("Not automatically executed: Yes");
        sb.AppendLine();
        foreach (var section in sections)
        {
            sb.AppendLine($"## {section.Heading}");
            if (!string.IsNullOrWhiteSpace(section.Body)) sb.AppendLine(section.Body.Trim());
            foreach (var bullet in section.Bullets) sb.AppendLine($"- {bullet}");
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }
}
