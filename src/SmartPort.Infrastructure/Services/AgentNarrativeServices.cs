using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartPort.Infrastructure.Services;

public enum AgentMode
{
    Local,
    Gemini,
    Hybrid
}

public class GeminiSettings
{
    public bool Enabled { get; set; } = false;
    public string Model { get; set; } = "gemini-2.5-flash";
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxOutputTokens { get; set; } = 2048;
    public AgentMode Mode { get; set; } = AgentMode.Hybrid;
}

public class AgentNarrativeRequest
{
    public string Purpose { get; set; } = "operations brief";
    public string UserPrompt { get; set; } = string.Empty;
    public string CurrentPage { get; set; } = "Agent Reports";
    public string ReportType { get; set; } = "Executive Operations Brief";
    public AgentMode RequestedMode { get; set; } = AgentMode.Hybrid;
    public OperationalContext Context { get; set; } = new();
    public IReadOnlyList<string> DeterministicRecommendations { get; set; } = Array.Empty<string>();
    public string ScenarioSummary { get; set; } = string.Empty;
}

public class AgentNarrativeResult
{
    public string Title { get; set; } = string.Empty;
    public string Narrative { get; set; } = string.Empty;
    public AgentMode GeneratedBy { get; set; } = AgentMode.Local;
    public bool UsedGemini { get; set; }
    public bool FallbackActive { get; set; }
    public string Status { get; set; } = "Local deterministic";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string InputContextSummary { get; set; } = string.Empty;
    public string SafetyNote { get; set; } = "Human approval required. Not automatically executed.";
}

public class OperationalReportRequest
{
    public string ReportType { get; set; } = "Executive Operations Brief";
    public AgentMode Mode { get; set; } = AgentMode.Hybrid;
    public string ScenarioSummary { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
}

public class OperationalReportResult
{
    public string ReportType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public AgentMode GeneratedBy { get; set; } = AgentMode.Local;
    public bool UsedGemini { get; set; }
    public bool FallbackActive { get; set; }
    public string GeminiStatus { get; set; } = "Not configured";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string InputContextSummary { get; set; } = string.Empty;
    public string SafetyNote { get; set; } = "Operational recommendations remain human-approved and audit-tracked. This report is not automatically executed.";
    public List<ReportSectionDto> Sections { get; set; } = new();
    public string Markdown { get; set; } = string.Empty;
}

public class ReportSectionDto
{
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Tone { get; set; } = "info";
    public List<string> Bullets { get; set; } = new();
}

public class AgentModeStatus
{
    public AgentMode CurrentMode { get; set; }
    public string CurrentModeLabel => CurrentMode switch
    {
        AgentMode.Gemini => "Gemini Intelligence Mode",
        AgentMode.Hybrid => "Hybrid Agent Mode",
        _ => "Local Intelligence Mode"
    };
    public bool GeminiConfigured { get; set; }
    public bool GeminiEnabled { get; set; }
    public string GeminiStatus { get; set; } = "Not configured";
    public DateTime? LastGeneratedBriefUtc { get; set; }
}

public interface IAgentNarrativeService
{
    Task<AgentNarrativeResult> GenerateAsync(AgentNarrativeRequest request, CancellationToken cancellationToken = default);
    AgentModeStatus GetStatus();
}

public interface IOperationalReportService
{
    Task<OperationalReportResult> GenerateAsync(OperationalReportRequest request, CancellationToken cancellationToken = default);
    AgentModeStatus GetStatus();
    IReadOnlyList<string> GetReportTypes();
}

public class LocalAgentNarrativeService : IAgentNarrativeService
{
    public Task<AgentNarrativeResult> GenerateAsync(AgentNarrativeRequest request, CancellationToken cancellationToken = default)
    {
        var ctx = request.Context;
        var riskScore = Score(ctx);
        var severity = Severity(riskScore);
        var recs = request.DeterministicRecommendations.Any() ? request.DeterministicRecommendations : ctx.TopRecommendations;
        var narrative = new StringBuilder();
        narrative.AppendLine($"## {request.ReportType}");
        narrative.AppendLine($"Current state: {severity} operational pressure with {ctx.TrucksInQueue} queued trucks, {ctx.BerthUtilisationPct:F0}% berth utilisation, {ctx.YardOccupancyPct:F0}% yard occupancy, {ctx.OpenIncidents} open incidents and {ctx.ActiveDisruptions} active disruptions.");
        narrative.AppendLine();
        narrative.AppendLine($"Congestion and emissions: estimated idling exposure is {ctx.TotalIdlingMinutesToday:F0} minutes / {ctx.EstimatedCo2Today:F1} kg CO₂ today. Gate pressure is {(ctx.GateDelayActive ? "active" : "within planned limits")} and road congestion is {(ctx.RoadCongestionActive ? "active" : "not currently dominant")}.");
        narrative.AppendLine();
        narrative.AppendLine("Recommended operator focus:");
        foreach (var rec in recs.Take(5)) narrative.AppendLine($"- {rec}");
        narrative.AppendLine("- Keep recommendations human-approved, logged and reversible; do not auto-execute berth, gate or fleet changes.");
        narrative.AppendLine();
        narrative.AppendLine(request.ReportType.Contains("Pilot", StringComparison.OrdinalIgnoreCase)
            ? "Pilot note: this is a prototype/demo and pilot-ready architecture. Production use would require approved live data integrations, security controls and partner validation."
            : "Governance note: this report uses synthetic/demo operational summaries and deterministic rules; live production claims require validated integrations.");

        var result = new AgentNarrativeResult
        {
            Title = request.ReportType,
            Narrative = narrative.ToString().Trim(),
            GeneratedBy = AgentMode.Local,
            UsedGemini = false,
            FallbackActive = request.RequestedMode != AgentMode.Local,
            Status = request.RequestedMode == AgentMode.Local ? "Local deterministic" : "Local deterministic fallback",
            InputContextSummary = BuildContextSummary(ctx, request)
        };
        return Task.FromResult(result);
    }

    public AgentModeStatus GetStatus() => new()
    {
        CurrentMode = AgentMode.Local,
        GeminiConfigured = false,
        GeminiEnabled = false,
        GeminiStatus = "Local deterministic fallback active"
    };

    public static string BuildContextSummary(OperationalContext ctx, AgentNarrativeRequest request) =>
        $"{request.ReportType} · trucks={ctx.TrucksInQueue}, berth={ctx.BerthUtilisationPct:F0}%, yard={ctx.YardOccupancyPct:F0}%, incidents={ctx.OpenIncidents}, disruptions={ctx.ActiveDisruptions}, idling={ctx.TotalIdlingMinutesToday:F0} min, CO2={ctx.EstimatedCo2Today:F1} kg";

    private static int Score(OperationalContext ctx)
    {
        var score = 20 + Math.Min(25, ctx.TrucksInQueue * 2);
        score += ctx.BerthUtilisationPct > 80 ? 20 : ctx.BerthUtilisationPct > 60 ? 10 : 0;
        score += ctx.YardOccupancyPct > 85 ? 20 : ctx.YardOccupancyPct > 70 ? 10 : 0;
        score += ctx.LoadSheddingActive ? 18 : 0;
        score += Math.Min(15, ctx.OpenIncidents * 2 + ctx.CriticalDisruptions * 5);
        return Math.Min(100, score);
    }

    private static string Severity(int score) => score switch
    {
        >= 85 => "Critical",
        >= 65 => "High",
        >= 40 => "Medium",
        _ => "Low"
    };
}

public class GeminiAgentNarrativeService : IAgentNarrativeService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiAgentNarrativeService> _logger;

    public GeminiAgentNarrativeService(HttpClient httpClient, IOptions<GeminiSettings> settings, ILogger<GeminiAgentNarrativeService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AgentNarrativeResult> GenerateAsync(AgentNarrativeRequest request, CancellationToken cancellationToken = default)
    {
        var key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(key))
        {
            return new AgentNarrativeResult
            {
                Title = request.ReportType,
                GeneratedBy = AgentMode.Local,
                FallbackActive = true,
                Status = string.IsNullOrWhiteSpace(key) ? "Gemini not configured" : "Gemini disabled",
                InputContextSummary = LocalAgentNarrativeService.BuildContextSummary(request.Context, request)
            };
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_settings.TimeoutSeconds, 3, 60)));

        try
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_settings.Model)}:generateContent?key={Uri.EscapeDataString(key)}";
            var body = new
            {
                contents = new[] { new { role = "user", parts = new[] { new { text = BuildPrompt(request) } } } },
                generationConfig = new { maxOutputTokens = _settings.MaxOutputTokens, temperature = 0.35, topP = 0.9 }
            };

            using var response = await _httpClient.PostAsJsonAsync(endpoint, body, JsonOptions, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return Failure(request, response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden ? "Gemini authentication failed" : response.StatusCode == (HttpStatusCode)429 ? "Gemini rate limit reached" : $"Gemini unavailable ({(int)response.StatusCode})");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token);
            var text = ExtractText(doc);
            if (string.IsNullOrWhiteSpace(text)) return Failure(request, "Gemini returned an empty response");

            return new AgentNarrativeResult
            {
                Title = request.ReportType,
                Narrative = text.Trim(),
                GeneratedBy = AgentMode.Gemini,
                UsedGemini = true,
                FallbackActive = false,
                Status = "Gemini available",
                InputContextSummary = LocalAgentNarrativeService.BuildContextSummary(request.Context, request)
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(request, "Gemini timed out");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning("Gemini narrative generation failed; falling back to local deterministic response.");
            return Failure(request, "Gemini network or response error");
        }
    }

    public AgentModeStatus GetStatus()
    {
        var configured = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMINI_API_KEY"));
        return new AgentModeStatus
        {
            CurrentMode = _settings.Mode,
            GeminiConfigured = configured,
            GeminiEnabled = _settings.Enabled,
            GeminiStatus = _settings.Enabled && configured ? "Available" : configured ? "Configured but disabled" : "Not configured"
        };
    }

    private AgentNarrativeResult Failure(AgentNarrativeRequest request, string status) => new()
    {
        Title = request.ReportType,
        GeneratedBy = AgentMode.Local,
        FallbackActive = true,
        Status = status,
        InputContextSummary = LocalAgentNarrativeService.BuildContextSummary(request.Context, request)
    };

    private static string ExtractText(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return string.Empty;
        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text)) sb.AppendLine(text.GetString());
        }
        return sb.ToString();
    }

    private static string BuildPrompt(AgentNarrativeRequest request)
    {
        var ctx = request.Context;
        var recs = request.DeterministicRecommendations.Any() ? request.DeterministicRecommendations : ctx.TopRecommendations;
        return $$"""
You are the Culltron Smart Port Flow AI Agent. You support port operations decision-making using only the supplied operational summary. You must not claim live integration unless the supplied data says so. You must not invent real customers, real port access, real IPMS/Transnet/Navayuga integration, real production deployment, real customer data, guaranteed savings, or signed pilot agreements. You may describe this as a prototype/demo/pilot-ready architecture when appropriate. Recommendations must be explainable, human-approved, and audit-friendly.

Generate a concise enterprise operations output for: {{request.ReportType}}.
Current page/context: {{request.CurrentPage}}.
User prompt: {{Sanitize(request.UserPrompt)}}.
Scenario summary if supplied: {{Sanitize(request.ScenarioSummary)}}.

Structured operational summary (sanitized):
- Vessels in port: {{ctx.VesselsInPort}}; delayed: {{ctx.VesselsDelayed}}; at anchor: {{ctx.VesselsAtAnchor}}
- Berth utilisation: {{ctx.BerthUtilisationPct:F0}}%; berths occupied/available: {{ctx.BerthsOccupied}}/{{ctx.BerthsAvailable}}
- Yard occupancy: {{ctx.YardOccupancyPct:F0}}%; containers in yard: {{ctx.ContainersInYard}}; dwell alerts: {{ctx.DwellAlerts}}
- Truck queue estimate: {{ctx.TrucksInQueue}}; active trips: {{ctx.ActiveTrips}}; high-risk trips: {{ctx.HighRiskTrips}}
- Open incidents: {{ctx.OpenIncidents}}; active disruptions: {{ctx.ActiveDisruptions}}; critical disruptions: {{ctx.CriticalDisruptions}}
- Idling estimate: {{ctx.TotalIdlingMinutesToday:F0}} minutes; emissions estimate: {{ctx.EstimatedCo2Today:F1}} kg CO2
- Energy/load-shedding active: {{ctx.LoadSheddingActive}}; road congestion active: {{ctx.RoadCongestionActive}}; gate delay active: {{ctx.GateDelayActive}}
- Top disruptions: {{string.Join("; ", ctx.TopDisruptions.Take(4))}}
- Deterministic recommendation summary: {{string.Join("; ", recs.Take(6))}}

Formatting requirements:
- Return clean Markdown only, no JSON.
- Use professional headings and bullet points.
- Include audit note: Generated by Gemini enhancement; human approval required; not automatically executed.
- Use careful language: prototype, demo, pilot-ready architecture, designed to integrate, can support, would require live data integration for production use.
""";
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Not supplied";
        var text = value.Replace("`", "'").Replace("<", "(").Replace(">", ")");
        foreach (var marker in new[] { "api key", "password", "connection string", "token", "secret" })
        {
            text = text.Replace(marker, "[redacted-sensitive-term]", StringComparison.OrdinalIgnoreCase);
        }
        return text.Length > 1000 ? text[..1000] : text;
    }
}

public class HybridAgentNarrativeService : IAgentNarrativeService
{
    private readonly LocalAgentNarrativeService _local;
    private readonly GeminiAgentNarrativeService _gemini;
    private DateTime? _lastGeneratedBriefUtc;

    public HybridAgentNarrativeService(LocalAgentNarrativeService local, GeminiAgentNarrativeService gemini)
    {
        _local = local;
        _gemini = gemini;
    }

    public async Task<AgentNarrativeResult> GenerateAsync(AgentNarrativeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RequestedMode == AgentMode.Local)
        {
            var localOnly = await _local.GenerateAsync(request, cancellationToken);
            _lastGeneratedBriefUtc = localOnly.GeneratedAtUtc;
            return localOnly;
        }

        var gemini = await _gemini.GenerateAsync(request, cancellationToken);
        if (gemini.UsedGemini)
        {
            _lastGeneratedBriefUtc = gemini.GeneratedAtUtc;
            return gemini;
        }

        var fallback = await _local.GenerateAsync(request, cancellationToken);
        fallback.FallbackActive = true;
        fallback.Status = gemini.Status;
        fallback.InputContextSummary = gemini.InputContextSummary;
        _lastGeneratedBriefUtc = fallback.GeneratedAtUtc;
        return fallback;
    }

    public AgentModeStatus GetStatus()
    {
        var geminiStatus = _gemini.GetStatus();
        return new AgentModeStatus
        {
            CurrentMode = geminiStatus.CurrentMode,
            GeminiConfigured = geminiStatus.GeminiConfigured,
            GeminiEnabled = geminiStatus.GeminiEnabled,
            GeminiStatus = geminiStatus.GeminiStatus,
            LastGeneratedBriefUtc = _lastGeneratedBriefUtc
        };
    }
}
