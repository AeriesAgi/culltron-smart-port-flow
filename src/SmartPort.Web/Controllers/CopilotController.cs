using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Infrastructure.Services;

namespace SmartPort.Web.Controllers;

[Authorize]
public class CopilotController : Controller
{
    private readonly ISmartPortCopilotChatService _chat;

    public CopilotController(ISmartPortCopilotChatService chat) => _chat = chat;

    public async Task<IActionResult> Index(string? prompt = null)
    {
        var model = await _chat.BuildPageAsync(prompt);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(string prompt)
    {
        if (IsJsonRequest())
        {
            var response = string.IsNullOrWhiteSpace(prompt) ? null : await _chat.GenerateResponseAsync(prompt);
            return Json(ToJson(response));
        }

        var model = await _chat.BuildPageAsync(prompt);
        return View("Index", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AskJson(string prompt)
    {
        var response = string.IsNullOrWhiteSpace(prompt) ? null : await _chat.GenerateResponseAsync(prompt);
        return Json(ToJson(response));
    }

    private bool IsJsonRequest() =>
        string.Equals(Request.Headers["X-Requested-With"].ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
        || Request.Headers["Accept"].Any(h => h?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

    private static object ToJson(CopilotChatResponse? response)
    {
        if (response == null)
        {
            return new
            {
                messageType = "compact",
                title = "SmartPort Copilot",
                summary = "I did not receive a prompt. Try asking about truck queues, ETA tracking, gate pressure, emissions or recommendations.",
                shortAnswer = "Choose a topic chip or type a scoped smart-port operations question.",
                intent = "empty",
                urgency = "Low",
                confidence = 100,
                affectedArea = "Smart Port Copilot",
                operationalReasoning = "No prompt was provided.",
                recommendedAction = "Choose a supported topic chip or type a focused smart port operations question.",
                expectedImpact = "Keeps the demo focused and safe.",
                emissionsImpact = "Synthetic demo data is used when operational context is requested.",
                energyImpact = "No live external systems are queried.",
                dataNote = "Synthetic demo data · local deterministic response",
                actionLinks = Array.Empty<object>(),
                suggestedFollowUps = new[] { "What is the biggest risk right now?", "Which trucks should be held outside the port?" },
                topicChips = Array.Empty<object>(),
                generatedAt = DateTime.UtcNow.ToString("HH:mm:ss"),
                isOutOfScope = false,
                isSmallTalk = true,
                isVagueButRelated = false
            };
        }

        var intent = response.Intent.ToLowerInvariant();
        var isSmallTalk = response.IsSmallTalk || intent.Contains("greeting") || intent.Contains("help");
        var isOutOfScope = response.IsOutOfScope || intent.Contains("out-of-scope") || intent.Contains("safety");
        var isVague = response.IsVagueButRelated || intent.Contains("vague");

        return new
        {
            messageType = response.MessageType,
            title = response.Title,
            summary = response.Summary,
            shortAnswer = response.ShortAnswer,
            intent = response.Intent,
            urgency = response.Severity,
            confidence = response.ConfidenceScore,
            affectedArea = response.AffectedArea,
            operationalReasoning = response.OperationalReasoning,
            recommendedAction = response.RecommendedAction,
            expectedImpact = response.ExpectedImpact,
            emissionsImpact = response.EmissionsImpact,
            energyImpact = response.EnergyImpact,
            dataNote = response.DataNote,
            generatedBy = response.GeneratedBy,
            humanApprovalRequired = response.HumanApprovalRequired,
            notAutomaticallyExecuted = response.NotAutomaticallyExecuted,
            actionLinks = response.ActionCards.Select(a => new { title = a.Title, description = a.Description, url = a.Url, icon = a.Icon }),
            suggestedFollowUps = response.SuggestedFollowUps,
            topicChips = response.TopicChips.Select(c => new { label = c.Label, prompt = c.Prompt, icon = c.Icon }),
            metricBadges = response.MetricBadges.Select(b => new { label = b.Label, value = b.Value, tone = b.Tone }),
            generatedAt = response.GeneratedAt.ToString("HH:mm:ss"),
            isOutOfScope,
            isSmallTalk,
            isVagueButRelated = isVague,
            isOperational = response.IsOperational
        };
    }
}
