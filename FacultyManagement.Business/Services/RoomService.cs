using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class RoomService(FacultyDbContext db)
{
    public async Task<Room> CreateAsync(CreateRoomRequest request, CancellationToken ct = default)
    {
        if (request.Capacity <= 0) throw new BusinessException("Room capacity must be positive.");
        var room = new Room { Code = request.Code.Trim(), Capacity = request.Capacity, IsLab = request.IsLab, HasProjector = request.HasProjector };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
        return room;
    }

    public async Task UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken ct = default)
    {
        if (request.Capacity <= 0) throw new BusinessException("Room capacity must be positive.");
        var room = await db.Rooms.FindAsync([id], ct) ?? throw new BusinessException("Room not found.", 404);
        room.Code = request.Code.Trim(); room.Capacity = request.Capacity; room.IsLab = request.IsLab;
        room.HasProjector = request.HasProjector; room.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetUnavailableAsync(Guid roomId, SetRoomUnavailabilityRequest request, CancellationToken ct = default)
    {
        if (request.EndsOn < request.StartsOn) throw new BusinessException("End date must not precede start date.");
        if (!await db.Rooms.AnyAsync(x => x.Id == roomId, ct)) throw new BusinessException("Room not found.", 404);
        db.RoomUnavailabilities.Add(new RoomUnavailability
        {
            RoomId = roomId, StartsOn = request.StartsOn, EndsOn = request.EndsOn, Reason = request.Reason.Trim()
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<Room>> AvailableAsync(DateOnly date, int slotId, bool labOnly, bool projector, int minimumCapacity, CancellationToken ct = default)
    {
        return await db.Rooms.AsNoTracking().Where(x => x.IsActive && (!labOnly || x.IsLab) && (!projector || x.HasProjector)
            && x.Capacity >= minimumCapacity
            && !db.RoomUnavailabilities.Any(u => u.RoomId == x.Id && u.StartsOn <= date && u.EndsOn >= date)
            && !db.ScheduleOccurrences.Any(o => o.RoomId == x.Id && o.Date == date && o.TimeSlotId == slotId && !o.IsCancelled))
            .OrderBy(x => x.Capacity).ToListAsync(ct);
    }
}
