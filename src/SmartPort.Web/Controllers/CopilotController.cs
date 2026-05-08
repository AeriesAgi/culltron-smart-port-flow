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
        var model = await _chat.BuildPageAsync(prompt);
        return View("Index", model);
    }
}
