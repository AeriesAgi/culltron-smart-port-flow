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
    public string DetectedIntent { get; set; } = "operational";
    public string ConversationHistory { get; set; } = string.Empty;
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
    public string Status { get; set; } = "Local fallback";
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
            : "Governance note: this report uses synthetic/demo operational summaries and baseline rules; live production claims require validated integrations.");

        var result = new AgentNarrativeResult
        {
            Title = request.ReportType,
            Narrative = narrative.ToString().Trim(),
            GeneratedBy = AgentMode.Local,
            UsedGemini = false,
            FallbackActive = request.RequestedMode != AgentMode.Local,
            Status = request.RequestedMode == AgentMode.Local ? "Local fallback" : "Local fallback",
            InputContextSummary = BuildContextSummary(ctx, request)
        };
        return Task.FromResult(result);
    }

    public AgentModeStatus GetStatus() => new()
    {
        CurrentMode = AgentMode.Local,
        GeminiConfigured = false,
        GeminiEnabled = false,
        GeminiStatus = "Local fallback active"
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
            _logger.LogWarning("Gemini narrative generation failed; falling back to local response.");
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
        var sharedContext = $$"""
Structured operational summary (sanitized synthetic/demo data):
- Truck queue / pressure: {{ctx.TrucksInQueue}} queued trucks; active trips: {{ctx.ActiveTrips}}; high-risk trips: {{ctx.HighRiskTrips}}
- Berth utilisation: {{ctx.BerthUtilisationPct:F0}}%; berths occupied/available: {{ctx.BerthsOccupied}}/{{ctx.BerthsAvailable}}; vessels delayed/at anchor: {{ctx.VesselsDelayed}}/{{ctx.VesselsAtAnchor}}
- Yard occupancy: {{ctx.YardOccupancyPct:F0}}%; containers in yard: {{ctx.ContainersInYard}}; dwell alerts: {{ctx.DwellAlerts}}
- Incidents/disruptions: {{ctx.OpenIncidents}} open incidents; {{ctx.ActiveDisruptions}} active disruptions; {{ctx.CriticalDisruptions}} critical disruptions
- Emissions/idling: {{ctx.TotalIdlingMinutesToday:F0}} idling minutes; {{ctx.EstimatedCo2Today:F1}} kg CO2 estimate
- Energy/load-shedding active: {{ctx.LoadSheddingActive}}; road congestion active: {{ctx.RoadCongestionActive}}; gate delay active: {{ctx.GateDelayActive}}
- Top disruptions: {{string.Join("; ", ctx.TopDisruptions.Take(4))}}
- Baseline recommendation summary: {{string.Join("; ", recs.Take(6))}}
- Available modules/pages: Dashboard, AI Command Centre, Copilot, Truck Tracking, Scenario Simulator, Emissions, Recommendations/Audit, Reports, Pilot Readiness, Executive Brief
- Data note: synthetic/demo data only; not live production port data
""";

        if (request.Purpose.Contains("copilot", StringComparison.OrdinalIgnoreCase))
        {
            return $$"""
You are Culltron Smart Port Copilot, an enterprise AI assistant for the Culltron Smart Port Flow demo system. You help users understand and operate the Smart Port prototype using only the supplied system context and current synthetic/demo operational data. You can be friendly and conversational for greetings or light chat, but you must stay grounded in the Smart Port system. Do not invent live port integrations, real customers, real Transnet/IPMS/Navayuga access, signed pilots, guaranteed savings, or production deployment. If asked operational questions, use the supplied context. If asked normal greetings, respond normally and offer useful Smart Port help. Recommendations are decision-support only, require human approval, and are not automatically executed.

Detected intent: {{Sanitize(request.DetectedIntent)}}
Current user message: {{Sanitize(request.UserPrompt)}}
Recent conversation (sanitized, current browser session only): {{Sanitize(request.ConversationHistory)}}
Current page/context: {{request.CurrentPage}}

{{sharedContext}}

Response requirements:
- For greeting/small talk: answer naturally in 1-3 sentences and offer Smart Port help; do not generate a risk report.
- For help/capability: explain practical Smart Port Copilot capabilities.
- For operational questions: provide grounded reasoning, recommended next step, and human-approval note.
- For report/action/demo requests: structure the answer with concise headings or bullets.
- For password/passphrase-looking text or unrelated prompts: do not process as an operations command; redirect safely.
- Return clean conversational Markdown only, no JSON.
""";
        }

        return $$"""
You are the Culltron Smart Port Flow AI Agent. You support port operations decision-making using only the supplied operational summary. You must not claim live integration unless the supplied data says so. You must not invent real customers, real port access, real IPMS/Transnet/Navayuga integration, real production deployment, real customer data, guaranteed savings, or signed pilot agreements. You may describe this as a prototype/demo/pilot-ready architecture when appropriate. Recommendations must be explainable, human-approved, and audit-friendly.

Generate a concise enterprise operations output for: {{request.ReportType}}.
Current page/context: {{request.CurrentPage}}.
User prompt: {{Sanitize(request.UserPrompt)}}.
Scenario summary if supplied: {{Sanitize(request.ScenarioSummary)}}.

{{sharedContext}}

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

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => LooksSensitiveValue(word.Trim()) ? "[redacted-sensitive-value]" : word);
        text = string.Join(" ", words);
        return text.Length > 1000 ? text[..1000] : text;
    }

    private static bool LooksSensitiveValue(string value)
    {
        if (value.Length < 6 || value.Contains(' ')) return false;
        var hasLetter = value.Any(char.IsLetter);
        var hasDigit = value.Any(char.IsDigit);
        var hasSymbol = value.Any(ch => !char.IsLetterOrDigit(ch));
        return hasLetter && hasDigit && hasSymbol;
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
