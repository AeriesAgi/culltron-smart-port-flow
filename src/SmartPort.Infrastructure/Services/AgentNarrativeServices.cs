using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
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
    public bool Enabled { get; set; } = true;
    public string Model { get; set; } = "gemini-2.5-flash";
    public string PrimaryModel { get; set; } = "gemini-2.5-flash";
    public string PremiumModel { get; set; } = "gemini-2.5-flash";
    public string RoutineModel { get; set; } = "gemini-3.1-flash-lite";
    public string FallbackModels { get; set; } = "gemini-3.1-flash-lite,gemini-2.5-flash-lite,gemini-2.0-flash-lite,gemini-2.0-flash";
    public string ExperimentalFallbackModels { get; set; } = string.Empty;
    public bool AllowExperimentalModels { get; set; } = false;
    public int MaxCallsPerSession { get; set; } = 20;
    public int ManualTestCooldownSeconds { get; set; } = 60;
    public int QuotaCooldownMinutes { get; set; } = 30;
    public bool AutoRunOnAgentPage { get; set; } = false;
    public bool AutoRunOnDemoTour { get; set; } = false;
    public int AutoRunCooldownMinutes { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxOutputTokens { get; set; } = 2048;
    public AgentMode Mode { get; set; } = AgentMode.Hybrid;
}

public enum GeminiTaskCategory
{
    Routine,
    Premium
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
    public GeminiTaskCategory TaskCategory { get; set; } = GeminiTaskCategory.Routine;
    public string ActionType { get; set; } = "operations-brief";
}

public class AgentNarrativeResult
{
    public string Title { get; set; } = string.Empty;
    public string Narrative { get; set; } = string.Empty;
    public AgentMode GeneratedBy { get; set; } = AgentMode.Local;
    public bool UsedGemini { get; set; }
    public bool FallbackActive { get; set; }
    public string Status { get; set; } = "Deterministic fallback";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string InputContextSummary { get; set; } = string.Empty;
    public string SafetyNote { get; set; } = "Human approval required. Not automatically executed.";
    public string ModelAttempted { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = "Deterministic fallback";
    public string SourceLabel { get; set; } = "Deterministic fallback";
    public string FallbackReason { get; set; } = string.Empty;
    public int? LatencyMs { get; set; }
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
    public string PrimaryModel { get; set; } = "gemini-2.5-flash";
    public string PremiumModel { get; set; } = "gemini-2.5-flash";
    public string RoutineModel { get; set; } = "gemini-3.1-flash-lite";
    public string FallbackModels { get; set; } = string.Empty;
    public int CallsSinceStart { get; set; }
    public IReadOnlyDictionary<string, int> CallsByActionType { get; set; } = new Dictionary<string, int>();
    public DateTime? LastCallUtc { get; set; }
    public string LastActionType { get; set; } = string.Empty;
    public string LastRouteSource { get; set; } = string.Empty;
    public string LastModelAttempted { get; set; } = string.Empty;
    public string LastModelUsed { get; set; } = string.Empty;
    public string LastResult { get; set; } = string.Empty;
    public int? LastLatencyMs { get; set; }
    public bool QuotaLimited { get; set; }
    public bool FallbackActive { get; set; }
    public bool AutoRunOnAgentPage { get; set; }
    public bool AutoRunOnDemoTour { get; set; }
    public int AutoRunCooldownMinutes { get; set; }
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
            Status = request.RequestedMode == AgentMode.Local ? "Deterministic fallback" : "Deterministic fallback",
            InputContextSummary = BuildContextSummary(ctx, request),
            ModelAttempted = "Deterministic fallback",
            ModelUsed = "Deterministic fallback",
            SourceLabel = "Deterministic fallback"
        };
        return Task.FromResult(result);
    }

    public AgentModeStatus GetStatus() => new()
    {
        CurrentMode = AgentMode.Local,
        GeminiConfigured = false,
        GeminiEnabled = false,
        GeminiStatus = "Deterministic fallback active"
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
    private static readonly object DiagnosticsLock = new();
    private static readonly Dictionary<string, DateTime> QuotaLimitedModels = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> UnsupportedModels = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> CallsByActionType = new(StringComparer.OrdinalIgnoreCase);
    private static int CallsSinceStart;
    private static DateTime? LastCallUtc;
    private static string LastActionType = string.Empty;
    private static string LastRouteSource = string.Empty;
    private static string LastModelAttempted = string.Empty;
    private static string LastModelUsed = string.Empty;
    private static string LastResult = string.Empty;
    private static int? LastLatencyMs;
    private static bool LastQuotaLimited;
    private static bool LastFallbackActive;

    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAgentNarrativeService> _logger;

    public GeminiAgentNarrativeService(HttpClient httpClient, IOptions<GeminiSettings> settings, IConfiguration configuration, ILogger<GeminiAgentNarrativeService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AgentNarrativeResult> GenerateAsync(AgentNarrativeRequest request, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        var key = GetGeminiApiKey();
        var enabled = IsGeminiEnabled();
        var chain = BuildModelChain(request).ToList();
        var attempted = string.Join(" → ", chain);
        if (!enabled || string.IsNullOrWhiteSpace(key))
        {
            var status = string.IsNullOrWhiteSpace(key) ? "Gemini not configured; deterministic fallback used" : "Gemini disabled; deterministic fallback used";
            RecordDiagnostics(request, attempted, "Deterministic fallback", status, (int)(DateTime.UtcNow - started).TotalMilliseconds, quotaLimited: false, fallbackActive: true);
            return Failure(request, status, attempted, null, started);
        }

        if (CallsSinceStart >= GetInt("Gemini:MaxCallsPerSession", "Gemini__MaxCallsPerSession", _settings.MaxCallsPerSession))
        {
            RecordDiagnostics(request, attempted, "Deterministic fallback", "Gemini max calls per session reached", (int)(DateTime.UtcNow - started).TotalMilliseconds, quotaLimited: false, fallbackActive: true);
            return Failure(request, "Gemini max calls per session reached", attempted, null, started);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_settings.TimeoutSeconds, 3, 60)));

        string? lastFailure = null;
        foreach (var model in chain)
        {
            if (string.IsNullOrWhiteSpace(model) || UnsupportedModels.Contains(model)) continue;
            if (IsModelQuotaCooling(model)) { lastFailure = $"{model} quota cooldown active"; continue; }

            try
            {
                _logger.LogInformation("Gemini call requested. action={ActionType} route={RouteSource} category={Category} model={Model}", Safe(request.ActionType), Safe(request.CurrentPage), request.TaskCategory, model);
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(key)}";
                var body = new
                {
                    contents = new[] { new { role = "user", parts = new[] { new { text = BuildPrompt(request) } } } },
                    generationConfig = new { maxOutputTokens = _settings.MaxOutputTokens, temperature = request.TaskCategory == GeminiTaskCategory.Premium ? 0.32 : 0.25, topP = 0.9 }
                };

                using var response = await _httpClient.PostAsJsonAsync(endpoint, body, JsonOptions, timeoutCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    lastFailure = ClassifyFailure(response.StatusCode, model);
                    if (response.StatusCode == (HttpStatusCode)429)
                    {
                        MarkQuotaLimited(model);
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        UnsupportedModels.Add(model);
                    }
                    _logger.LogWarning("Gemini model skipped. action={ActionType} route={RouteSource} model={Model} reason={Reason}", Safe(request.ActionType), Safe(request.CurrentPage), model, lastFailure);
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token);
                var text = ExtractText(doc);
                if (string.IsNullOrWhiteSpace(text))
                {
                    lastFailure = $"{model} returned an empty response";
                    continue;
                }

                var latency = (int)Math.Max(1, (DateTime.UtcNow - started).TotalMilliseconds);
                var sourceLabel = LabelForModel(request, model);
                RecordDiagnostics(request, attempted, model, "Gemini available", latency, quotaLimited: false, fallbackActive: sourceLabel.Contains("fallback", StringComparison.OrdinalIgnoreCase));
                return new AgentNarrativeResult
                {
                    Title = request.ReportType,
                    Narrative = text.Trim(),
                    GeneratedBy = AgentMode.Gemini,
                    UsedGemini = true,
                    FallbackActive = sourceLabel.Contains("fallback", StringComparison.OrdinalIgnoreCase),
                    Status = "Gemini available",
                    InputContextSummary = LocalAgentNarrativeService.BuildContextSummary(request.Context, request),
                    ModelAttempted = attempted,
                    ModelUsed = model,
                    SourceLabel = sourceLabel,
                    LatencyMs = latency
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastFailure = $"{model} timed out";
                break;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                lastFailure = $"{model} network or response error";
                _logger.LogWarning("Gemini model failed; trying configured fallback. action={ActionType} route={RouteSource} model={Model}", Safe(request.ActionType), Safe(request.CurrentPage), model);
            }
        }

        var failure = string.IsNullOrWhiteSpace(lastFailure) ? "No supported Gemini text model available" : lastFailure;
        var totalLatency = (int)Math.Max(1, (DateTime.UtcNow - started).TotalMilliseconds);
        RecordDiagnostics(request, attempted, "Deterministic fallback", failure, totalLatency, LastQuotaLimited, fallbackActive: true);
        return Failure(request, failure, attempted, null, started);
    }

    public AgentModeStatus GetStatus()
    {
        var configured = !string.IsNullOrWhiteSpace(GetGeminiApiKey());
        var enabled = IsGeminiEnabled();
        lock (DiagnosticsLock)
        {
            return new AgentModeStatus
            {
                CurrentMode = GetGeminiMode(),
                GeminiConfigured = configured,
                GeminiEnabled = enabled,
                GeminiStatus = enabled && configured ? "Configured; on-demand Gemini active" : configured ? "Configured but disabled" : "Not configured; deterministic fallback ready",
                PrimaryModel = GetString("Gemini:PrimaryModel", "GEMINI_MODEL", GetString("Gemini:Model", "GEMINI_MODEL", _settings.PrimaryModel)),
                PremiumModel = GetString("Gemini:PremiumModel", null, _settings.PremiumModel),
                RoutineModel = GetString("Gemini:RoutineModel", null, _settings.RoutineModel),
                FallbackModels = string.Join(", ", BuildFallbackModels()),
                CallsSinceStart = CallsSinceStart,
                CallsByActionType = new Dictionary<string, int>(CallsByActionType),
                LastCallUtc = LastCallUtc,
                LastActionType = LastActionType,
                LastRouteSource = LastRouteSource,
                LastModelAttempted = LastModelAttempted,
                LastModelUsed = LastModelUsed,
                LastResult = LastResult,
                LastLatencyMs = LastLatencyMs,
                QuotaLimited = LastQuotaLimited || QuotaLimitedModels.Values.Any(until => until > DateTime.UtcNow),
                FallbackActive = LastFallbackActive,
                AutoRunOnAgentPage = GetBool("Gemini:AutoRunOnAgentPage", "Gemini__AutoRunOnAgentPage", _settings.AutoRunOnAgentPage),
                AutoRunOnDemoTour = GetBool("Gemini:AutoRunOnDemoTour", "Gemini__AutoRunOnDemoTour", _settings.AutoRunOnDemoTour),
                AutoRunCooldownMinutes = GetInt("Gemini:AutoRunCooldownMinutes", "Gemini__AutoRunCooldownMinutes", _settings.AutoRunCooldownMinutes)
            };
        }
    }

    private string? GetGeminiApiKey() => _configuration["GEMINI_API_KEY"] ?? _configuration["Gemini:ApiKey"];
    private bool IsGeminiEnabled() => GetBool("Gemini:Enabled", "GEMINI_ENABLED", _settings.Enabled);
    private AgentMode GetGeminiMode()
    {
        var configured = !string.IsNullOrWhiteSpace(_configuration["GEMINI_MODE"]) ? _configuration["GEMINI_MODE"] : _configuration["Gemini:Mode"];
        return Enum.TryParse<AgentMode>(configured, true, out var mode) ? mode : _settings.Mode;
    }

    private IEnumerable<string> BuildModelChain(AgentNarrativeRequest request)
    {
        var first = request.TaskCategory == GeminiTaskCategory.Premium
            ? GetString("Gemini:PremiumModel", null, _settings.PremiumModel)
            : GetString("Gemini:RoutineModel", null, _settings.RoutineModel);
        yield return first;
        foreach (var model in BuildFallbackModels()) yield return model;
    }

    private IEnumerable<string> BuildFallbackModels()
    {
        var fallback = GetString("Gemini:FallbackModels", null, _settings.FallbackModels);
        var models = SplitCsv(fallback).ToList();
        if (GetBool("Gemini:AllowExperimentalModels", null, _settings.AllowExperimentalModels)) models.AddRange(SplitCsv(GetString("Gemini:ExperimentalFallbackModels", null, _settings.ExperimentalFallbackModels)));
        return models.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private bool IsModelQuotaCooling(string model)
    {
        lock (DiagnosticsLock)
        {
            return QuotaLimitedModels.TryGetValue(model, out var until) && until > DateTime.UtcNow;
        }
    }

    private void MarkQuotaLimited(string model)
    {
        lock (DiagnosticsLock)
        {
            QuotaLimitedModels[model] = DateTime.UtcNow.AddMinutes(GetInt("Gemini:QuotaCooldownMinutes", "Gemini__QuotaCooldownMinutes", _settings.QuotaCooldownMinutes));
            LastQuotaLimited = true;
        }
    }

    private void RecordDiagnostics(AgentNarrativeRequest request, string modelAttempted, string model, string result, int latencyMs, bool quotaLimited, bool fallbackActive)
    {
        lock (DiagnosticsLock)
        {
            CallsSinceStart++;
            var action = string.IsNullOrWhiteSpace(request.ActionType) ? request.ReportType : request.ActionType;
            CallsByActionType[action] = CallsByActionType.GetValueOrDefault(action) + 1;
            LastCallUtc = DateTime.UtcNow;
            LastActionType = action;
            LastRouteSource = request.CurrentPage;
            LastModelAttempted = modelAttempted;
            LastModelUsed = model;
            LastResult = result;
            LastLatencyMs = latencyMs;
            LastQuotaLimited = quotaLimited || result.Contains("quota", StringComparison.OrdinalIgnoreCase) || QuotaLimitedModels.Values.Any(until => until > DateTime.UtcNow);
            LastFallbackActive = fallbackActive;
        }
    }

    private AgentNarrativeResult Failure(AgentNarrativeRequest request, string status, string attempted, string? modelUsed, DateTime started) => new()
    {
        Title = request.ReportType,
        GeneratedBy = AgentMode.Local,
        FallbackActive = true,
        Status = status,
        InputContextSummary = LocalAgentNarrativeService.BuildContextSummary(request.Context, request),
        ModelAttempted = attempted,
        ModelUsed = modelUsed ?? "Deterministic fallback",
        SourceLabel = "Deterministic fallback",
        FallbackReason = status,
        LatencyMs = (int)Math.Max(1, (DateTime.UtcNow - started).TotalMilliseconds)
    };

    private static string LabelForModel(AgentNarrativeRequest request, string model)
    {
        var first = request.TaskCategory == GeminiTaskCategory.Premium ? "premium" : "routine";
        if ((request.TaskCategory == GeminiTaskCategory.Premium && model.Contains("2.5-flash", StringComparison.OrdinalIgnoreCase) && !model.Contains("lite", StringComparison.OrdinalIgnoreCase)) ||
            (request.TaskCategory == GeminiTaskCategory.Routine && model.Contains("3.1-flash-lite", StringComparison.OrdinalIgnoreCase)))
        {
            return $"Gemini {first}: {model}";
        }
        return $"Gemini fallback: {model}";
    }

    private static string ClassifyFailure(HttpStatusCode code, string model) =>
        code == HttpStatusCode.Unauthorized || code == HttpStatusCode.Forbidden ? "Gemini authentication failed" :
        code == (HttpStatusCode)429 ? $"{model} quota-limited; fallback active" :
        code == HttpStatusCode.NotFound || code == HttpStatusCode.BadRequest ? $"{model} unsupported or unavailable" :
        $"{model} unavailable ({(int)code})";

    private string GetString(string key, string? envKey, string fallback)
    {
        var envValue = envKey is null ? null : _configuration[envKey];
        var configured = !string.IsNullOrWhiteSpace(envValue) ? envValue : _configuration[key];
        return string.IsNullOrWhiteSpace(configured) ? fallback : configured;
    }
    private bool GetBool(string key, string? envKey, bool fallback)
    {
        var envValue = envKey is null ? null : _configuration[envKey];
        var configured = !string.IsNullOrWhiteSpace(envValue) ? envValue : _configuration[key];
        return bool.TryParse(configured, out var value) ? value : fallback;
    }
    private int GetInt(string key, string? envKey, int fallback)
    {
        var envValue = envKey is null ? null : _configuration[envKey];
        var configured = !string.IsNullOrWhiteSpace(envValue) ? envValue : _configuration[key];
        return int.TryParse(configured, out var value) ? value : fallback;
    }
    private static IEnumerable<string> SplitCsv(string value) => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(v => !string.IsNullOrWhiteSpace(v));
    private static string Safe(string value) => string.IsNullOrWhiteSpace(value) ? "n/a" : value.Replace('\n', ' ').Replace('\r', ' ');

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
- Prefer structured JSON with fields: summary, detectedBottleneck, recommendedActions, fleetOwnerInstructions, driverInstructions, riskFlags, approvalRequired, emissionsImpact, confidence, caveats. If JSON is not practical, return clean Markdown.
- Use professional headings and bullet points when returning Markdown.
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
        geminiStatus.LastGeneratedBriefUtc = _lastGeneratedBriefUtc;
        return geminiStatus;
    }
}
