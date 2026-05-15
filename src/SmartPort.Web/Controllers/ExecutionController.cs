using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.Interfaces;
using SmartPort.Shared.Constants;

namespace SmartPort.Web.Controllers;

[Authorize(Policy = Policies.CanAccessFleet)]
public class ExecutionController : Controller
{
    private readonly IExecutionPlanService _plans;
    public ExecutionController(IExecutionPlanService plans) => _plans = plans;

    [HttpGet("/execution")]
    [HttpGet("/execution/plans")]
    public async Task<IActionResult> Plans() => View("~/Views/Fleet/ExecutionPlans.cshtml", await _plans.GetPlansAsync());

    [HttpPost("/execution/plans/generate")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Generate()
    {
        var plan = await _plans.GeneratePlanAsync();
        return Redirect($"/execution/plans/{plan.PlanId}");
    }

    [HttpGet("/execution/plans/{id}")]
    public async Task<IActionResult> Detail(string id) => View("~/Views/Fleet/ExecutionPlanDetail.cshtml", await _plans.GetPlanAsync(id) ?? await _plans.GeneratePlanAsync());
}
