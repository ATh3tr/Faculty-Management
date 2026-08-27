using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/settings")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class SettingsController(ISettingsService settings) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyDictionary<string, string>> Get() => settings.GetAllAsync();

    [HttpPut("{key}")]
    public async Task<IActionResult> Set(string key, [FromBody] string value)
    {
        await settings.SetAsync(key, value, User.UserId());
        return NoContent();
    }
}
