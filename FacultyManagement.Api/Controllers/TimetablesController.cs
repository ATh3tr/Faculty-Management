using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/timetables")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class TimetablesController(TimetableGeneratorService generator) : ControllerBase
{
    [HttpPost("generate")]
    public Task<GeneratedTimetableResult> Generate(GenerateTimetableRequest request) =>
        generator.GenerateAsync(request, User.UserId());

    [HttpPut("{planId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid planId)
    {
        await generator.PublishPlanAsync(planId);
        return NoContent();
    }
}
