using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController]
public sealed class CommunicationController(CommunicationService communication) : ControllerBase
{
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Professor},{AppRoles.ExamsOfficer},{AppRoles.Admin}")]
    [HttpPost("api/announcements")]
    public async Task<IActionResult> Publish(CreateAnnouncementRequest request)
    {
        var id = await communication.PublishAnnouncementAsync(request, User.UserId());
        return Created($"/api/announcements/{id}", new { id });
    }

    [HttpGet("api/notifications")]
    public Task<IReadOnlyCollection<NotificationView>> Notifications(string language = "ar") =>
        communication.GetNotificationsAsync(User.UserId(), language);

    [HttpPut("api/notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await communication.MarkReadAsync(id, User.UserId());
        return NoContent();
    }
}
