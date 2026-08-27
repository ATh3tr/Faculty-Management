using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/appeals")]
public sealed class AppealsController(AppealService appeals) : ControllerBase
{
    [Authorize(Roles = AppRoles.Student)]
    [HttpPost]
    public async Task<IActionResult> Submit(CreateAppealRequest request)
    {
        var appeal = await appeals.SubmitAsync(User.UserId(), request);
        return Created($"/api/appeals/{appeal.Id}", new { appeal.Id, appeal.Status });
    }

    [Authorize(Roles = AppRoles.Professor)]
    [HttpPut("{id:guid}/professor-review")]
    public async Task<IActionResult> ProfessorReview(Guid id, ProfessorReviewRequest request)
    {
        await appeals.ReviewByProfessorAsync(id, User.UserId(), request);
        return NoContent();
    }

    [Authorize(Roles = $"{AppRoles.ExamsOfficer},{AppRoles.Admin}")]
    [HttpPut("{id:guid}/decision")]
    public async Task<IActionResult> Decide(Guid id, AppealDecisionRequest request)
    {
        await appeals.DecideAsync(id, User.UserId(), request);
        return NoContent();
    }
}
