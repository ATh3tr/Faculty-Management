using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/divisions")]
public sealed class DivisionsController(DivisionService divisions) : ControllerBase
{
    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("mine")]
    public async Task<IActionResult> Mine() => Ok(await divisions.GetAssignmentAsync(User.UserId()));

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("register")]
    public async Task<IActionResult> Register() => Ok(await divisions.AssignAsync(User.UserId()));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{divisionId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> Transfer(Guid divisionId, Guid studentId)
    {
        await divisions.TransferAsync(studentId, divisionId, User.UserId());
        return NoContent();
    }
}
