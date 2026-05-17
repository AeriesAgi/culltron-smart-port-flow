using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPort.Application.DTOs;
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
    public async Task<IActionResult> Plans()
    {
        var plans = await _plans.GetPlansAsync();
        if (!plans.Any()) await _plans.GeneratePlanAsync();
        return View("~/Views/Fleet/ExecutionPlans.cshtml", await _plans.GetPlansAsync());
    }

    [HttpPost("/execution/plans/generate")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Generate()
    {
        var plan = await _plans.GeneratePlanAsync("Gate congestion recovery and emissions reduction plan");
        TempData["Success"] = "Execution plan generated from current queue, staging, driver check-in and emissions signals.";
        return Redirect($"/execution/plans/{plan.PlanId}");
    }

    [HttpGet("/execution/plans/{id}")]
    public async Task<IActionResult> Detail(string id) => View("~/Views/Fleet/ExecutionPlanDetail.cshtml", await _plans.GetPlanAsync(id) ?? await _plans.GeneratePlanAsync());

    [HttpPost("/execution/plans/{id}/approve")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Approve(string id) => await Update(id, ExecutionPlanStatus.Approved, "Plan approved by control room. Fleet owners can dispatch staged truck instructions.");

    [HttpPost("/execution/plans/{id}/dispatch")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Dispatch(string id) => await Update(id, ExecutionPlanStatus.Dispatching, "Plan dispatched to fleet owners and Driver Companion channels. Awaiting check-ins and driver confirmations.");

    [HttpPost("/execution/plans/{id}/complete")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Complete(string id) => await Update(id, ExecutionPlanStatus.Completed, "Execution plan completed. Delay/idling impact captured for audit and judge demo.");

    [HttpPost("/execution/plans/{id}/exception")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exception(string id) => await Update(id, ExecutionPlanStatus.Exception, "Exception flagged. Control room review required before further release.");

    [HttpPost("/execution/plans/{id}/truck-action")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TruckAction(string id, string truckReference, ExecutionTruckActionType actionType)
    {
        var note = actionType switch
        {
            ExecutionTruckActionType.MoveToStaging => "Move selected truck to staging and keep driver app open for next instruction.",
            ExecutionTruckActionType.ProceedToGate => "Release selected truck to assigned gate based on queue/staging readiness.",
            ExecutionTruckActionType.Reschedule => "Reschedule selected truck by 20 minutes and protect gate capacity.",
            ExecutionTruckActionType.MarkException => "Mark selected truck as exception and escalate to fleet owner.",
            ExecutionTruckActionType.RequestLocation => "Request fresh Driver Companion app check-in from selected truck.",
            _ => "Hold selected truck pending control-room confirmation."
        };
        var plan = await _plans.RecordTruckActionAsync(id, truckReference, actionType, "Control room", note);
        TempData[plan == null ? "Warning" : "Success"] = plan == null ? "Execution plan not found." : $"{truckReference}: {note}";
        return Redirect($"/execution/plans/{id}");
    }

    private async Task<IActionResult> Update(string id, ExecutionPlanStatus status, string note)
    {
        var plan = await _plans.UpdateStatusAsync(id, status, "Control room", note);
        TempData[plan == null ? "Warning" : "Success"] = plan == null ? "Execution plan not found." : note;
        return Redirect($"/execution/plans/{id}");
    }
}
