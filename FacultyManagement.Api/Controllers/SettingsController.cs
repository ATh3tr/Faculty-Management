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

    [AllowAnonymous]
    [HttpGet("branding")]
    public async Task<ActionResult<BrandingSettingsResponse>> GetBranding()
    {
        var values = await settings.GetAllAsync();
        return new BrandingSettingsResponse(
            ReadName(values, SettingKeys.FacultyNameArabic, BrandingDefaults.FacultyNameArabic),
            ReadName(values, SettingKeys.FacultyNameEnglish, BrandingDefaults.FacultyNameEnglish));
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Set(string key, [FromBody] string value)
    {
        if ((key is SettingKeys.FacultyNameArabic or SettingKeys.FacultyNameEnglish) && string.IsNullOrWhiteSpace(value))
            return BadRequest("Faculty name cannot be empty.");

        await settings.SetAsync(key, value.Trim(), User.UserId());
        return NoContent();
    }

    private static string ReadName(
        IReadOnlyDictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;
}

public sealed record BrandingSettingsResponse(string FacultyNameArabic, string FacultyNameEnglish);
