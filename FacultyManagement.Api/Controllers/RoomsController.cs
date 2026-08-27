using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/rooms")]
public sealed class RoomsController(RoomService rooms) : ControllerBase
{
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoomRequest request)
    {
        var room = await rooms.CreateAsync(request);
        return Created($"/api/rooms/{room.Id}", new { room.Id, room.Code });
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRoomRequest request)
    {
        await rooms.UpdateAsync(id, request);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/unavailability")]
    public async Task<IActionResult> SetUnavailable(Guid id, SetRoomUnavailabilityRequest request)
    {
        await rooms.SetUnavailableAsync(id, request);
        return NoContent();
    }

    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Professor},{AppRoles.Admin}")]
    [HttpGet("available")]
    public async Task<IActionResult> Available(DateOnly date, int slotId, bool labOnly = false, bool projector = false, int minimumCapacity = 0) =>
        Ok((await rooms.AvailableAsync(date, slotId, labOnly, projector, minimumCapacity))
            .Select(x => new { x.Id, x.Code, x.Capacity, x.IsLab, x.HasProjector }));
}
