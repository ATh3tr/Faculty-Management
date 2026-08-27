using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/schedules")]
public sealed class SchedulesController(ScheduleService schedules, ScheduleQueryService queries) : ControllerBase
{
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Professor},{AppRoles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateScheduleRequest request)
    {
        var isAdmin = User.IsInRole(AppRoles.Admin);
        if (!isAdmin) request = request with { StaffUserId = User.UserId() };
        var result = await schedules.CreateAsync(request, User.UserId());
        return Created($"/api/schedules/{result.Id}", result);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        await schedules.PublishAsync(id, User.UserId());
        return NoContent();
    }

    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Professor},{AppRoles.Admin}")]
    [HttpPut("{id:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, RescheduleRequest request)
    {
        await schedules.RescheduleAsync(id, request, User.UserId(), User.IsInRole(AppRoles.Admin));
        return NoContent();
    }

    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Professor},{AppRoles.Admin}")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await schedules.CancelAsync(id, User.UserId(), User.IsInRole(AppRoles.Admin));
        return NoContent();
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(DateOnly from, DateOnly to, string language = "ar") =>
        Ok(await queries.ForUserAsync(User.UserId(), !User.IsInRole(AppRoles.Student), language, from, to));
}
