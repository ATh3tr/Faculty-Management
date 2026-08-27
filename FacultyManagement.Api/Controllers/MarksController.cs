using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/marks")]
public sealed class MarksController(MarkService marks, MarkImportParser parser) : ControllerBase
{
    [Authorize(Roles = $"{AppRoles.ExamsOfficer},{AppRoles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Enter(EnterMarkRequest request)
    {
        var attempt = await marks.EnterAsync(request, User.UserId());
        return Created($"/api/marks/{attempt.Id}", new { attempt.Id });
    }

    [Authorize(Roles = $"{AppRoles.ExamsOfficer},{AppRoles.Admin}")]
    [HttpPut("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        await marks.PublishAsync(id);
        return NoContent();
    }

    [Authorize(Roles = $"{AppRoles.ExamsOfficer},{AppRoles.Admin}")]
    [HttpPut("{id:guid}/correct")]
    public async Task<IActionResult> Correct(Guid id, CorrectMarkRequest request)
    {
        await marks.CorrectAsync(id, request, User.UserId());
        return NoContent();
    }

    [Authorize(Roles = $"{AppRoles.ExamsOfficer},{AppRoles.Admin}")]
    [HttpPost("imports")]
    public Task<ImportResult> Import(ImportMarksRequest request) => marks.ImportAsync(request, User.UserId());

    [Authorize(Roles = $"{AppRoles.ExamsOfficer},{AppRoles.Admin}")]
    [RequestSizeLimit(10_000_000)]
    [HttpPost("imports/file")]
    public async Task<ImportResult> ImportFile(Guid examPeriodId, bool publish, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var rows = await parser.ParseAsync(stream, file.FileName, HttpContext.RequestAborted);
        return await marks.ImportAsync(new ImportMarksRequest(examPeriodId, publish, rows), User.UserId());
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("mine")]
    public Task<IReadOnlyCollection<MarkView>> Mine() => marks.StudentMarksAsync(User.UserId());
}
