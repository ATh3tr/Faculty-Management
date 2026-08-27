using System.Data;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class ScheduleService(FacultyDbContext db, INotificationService notifications, IRealtimeNotifier realtime)
{
    private static readonly DayOfWeek[] TeachingDays =
        [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday];

    public async Task<ScheduleResult> CreateAsync(CreateScheduleRequest request, Guid creatorId, CancellationToken ct = default)
    {
        var series = await BuildAndValidateSeriesAsync(request, creatorId, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await AcquireScheduleLockAsync(ct);
        db.ScheduleSeries.Add(series);
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = creatorId, Action = "ScheduleCreated", EntityType = nameof(ScheduleSeries), EntityId = series.Id.ToString(),
            NewValuesJson = $"{{\"roomId\":\"{series.RoomId}\",\"slotId\":{series.TimeSlotId},\"status\":\"{series.Status}\"}}"
        });
        if (series.Status == ScheduleStatus.Published)
        {
            var dates = await ExpandDatesAsync(series.DayOfWeek, series.StartsOn, series.EndsOn, series.IsRecurring, ct);
            await EnsureNoConflictsAsync(series, dates, null, ct);
            foreach (var date in dates)
                series.Occurrences.Add(NewOccurrence(series, date));
        }
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessException("The room or staff member was booked by another request.", 409, "schedule_conflict");
        }

        if (series.Status == ScheduleStatus.Published)
        {
            await NotifyAudienceAsync(series, NotificationType.ScheduleCreated, "تمت جدولة محاضرة", "Lecture scheduled", ct);
            await realtime.AvailabilityChangedAsync(ct);
        }
        return new ScheduleResult(series.Id, series.Status, series.Occurrences.Count);
    }

    public async Task PublishAsync(Guid seriesId, Guid actorId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await AcquireScheduleLockAsync(ct);
        var series = await db.ScheduleSeries.Include(x => x.CourseOffering).ThenInclude(x => x!.Course)
            .Include(x => x.Division).SingleOrDefaultAsync(x => x.Id == seriesId, ct)
            ?? throw new BusinessException("Schedule series not found.", 404);
        if (series.Status == ScheduleStatus.Published) return;
        if (series.Status == ScheduleStatus.Cancelled) throw new BusinessException("A cancelled series cannot be published.", 409);
        var dates = await ExpandDatesAsync(series.DayOfWeek, series.StartsOn, series.EndsOn, series.IsRecurring, ct);
        await EnsureNoConflictsAsync(series, dates, null, ct);
        series.Status = ScheduleStatus.Published;
        foreach (var date in dates) series.Occurrences.Add(NewOccurrence(series, date));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await NotifyAudienceAsync(series, NotificationType.TimetablePublished, "تم نشر الجدول", "Timetable published", ct);
        await realtime.AvailabilityChangedAsync(ct);
    }

    public async Task RescheduleAsync(Guid seriesId, RescheduleRequest request, Guid actorId, bool isAdmin, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await AcquireScheduleLockAsync(ct);
        var series = await db.ScheduleSeries.Include(x => x.Occurrences).Include(x => x.CourseOffering).ThenInclude(x => x!.Course)
            .Include(x => x.Division).SingleOrDefaultAsync(x => x.Id == seriesId, ct)
            ?? throw new BusinessException("Schedule series not found.", 404);
        if (!isAdmin && series.StaffUserId != actorId) throw new BusinessException("Only the owner or Admin can reschedule this series.", 403);
        if (series.Status == ScheduleStatus.Cancelled) throw new BusinessException("Cancelled series cannot be rescheduled.", 409);
        var oldValues = $"{{\"roomId\":\"{series.RoomId}\",\"slotId\":{series.TimeSlotId},\"day\":\"{series.DayOfWeek}\"}}";
        series.RoomId = request.RoomId; series.TimeSlotId = request.TimeSlotId; series.DayOfWeek = request.DayOfWeek;
        series.StartsOn = request.StartsOn; series.EndsOn = request.EndsOn;
        await ValidateRoomAndEquipmentAsync(series, ct);
        var dates = await ExpandDatesAsync(series.DayOfWeek, series.StartsOn, series.EndsOn, series.IsRecurring, ct);
        await EnsureNoConflictsAsync(series, dates, series.Id, ct);
        db.ScheduleOccurrences.RemoveRange(series.Occurrences);
        series.Occurrences.Clear();
        foreach (var date in dates) series.Occurrences.Add(NewOccurrence(series, date));
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = actorId, Action = "ScheduleRescheduled", EntityType = nameof(ScheduleSeries), EntityId = series.Id.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = $"{{\"roomId\":\"{series.RoomId}\",\"slotId\":{series.TimeSlotId},\"day\":\"{series.DayOfWeek}\"}}"
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await NotifyAudienceAsync(series, NotificationType.ScheduleChanged, "تم تعديل موعد المحاضرة", "Lecture rescheduled", ct);
        await realtime.AvailabilityChangedAsync(ct);
    }

    public async Task CancelAsync(Guid seriesId, Guid actorId, bool isAdmin, CancellationToken ct = default)
    {
        var series = await db.ScheduleSeries.Include(x => x.Occurrences).Include(x => x.CourseOffering).ThenInclude(x => x!.Course)
            .Include(x => x.Division).SingleOrDefaultAsync(x => x.Id == seriesId, ct)
            ?? throw new BusinessException("Schedule series not found.", 404);
        if (!isAdmin && series.StaffUserId != actorId) throw new BusinessException("Only the owner or Admin can cancel this series.", 403);
        if (series.Status == ScheduleStatus.Cancelled) return;
        series.Status = ScheduleStatus.Cancelled;
        foreach (var occurrence in series.Occurrences.Where(x => !x.IsCancelled))
        {
            occurrence.IsCancelled = true;
            occurrence.CancelledAtUtc = DateTime.UtcNow;
        }
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = actorId, Action = "ScheduleCancelled", EntityType = nameof(ScheduleSeries), EntityId = series.Id.ToString()
        });
        await db.SaveChangesAsync(ct);
        await NotifyAudienceAsync(series, NotificationType.ScheduleCancelled, "تم إلغاء المحاضرة", "Lecture cancelled", ct);
        await realtime.AvailabilityChangedAsync(ct);
    }

    private async Task<ScheduleSeries> BuildAndValidateSeriesAsync(CreateScheduleRequest request, Guid creatorId, CancellationToken ct)
    {
        if (!TeachingDays.Contains(request.DayOfWeek)) throw new BusinessException("Teaching is only scheduled Sunday through Thursday.");
        if (request.EndsOn < request.StartsOn) throw new BusinessException("End date must not precede start date.");
        if (!await db.TimeSlots.AnyAsync(x => x.Id == request.TimeSlotId, ct)) throw new BusinessException("Time slot not found.", 404);
        var series = new ScheduleSeries
        {
            ActivityType = request.ActivityType, TitleArabic = request.TitleArabic.Trim(), TitleEnglish = request.TitleEnglish.Trim(),
            CourseOfferingId = request.CourseOfferingId, DivisionId = request.DivisionId,
            AudienceStudyYear = request.AudienceStudyYear, RoomId = request.RoomId,
            StaffUserId = request.StaffUserId, TimeSlotId = request.TimeSlotId, DayOfWeek = request.DayOfWeek,
            StartsOn = request.StartsOn, EndsOn = request.EndsOn, IsRecurring = request.IsRecurring,
            Status = request.Status, CreatedByUserId = creatorId, Source = ScheduleSource.Manual
        };

        if (request.ActivityType is ActivityType.TheoreticalLecture or ActivityType.PracticalLecture)
        {
            if (request.CourseOfferingId is null) throw new BusinessException("Course offering is required for lectures.");
            var offering = await db.CourseOfferings.Include(x => x.Course).SingleOrDefaultAsync(x => x.Id == request.CourseOfferingId, ct)
                ?? throw new BusinessException("Course offering not found.", 404);
            series.CourseOffering = offering;
            var role = request.ActivityType == ActivityType.TheoreticalLecture ? StaffCourseRole.Professor : StaffCourseRole.Teacher;
            if (!await db.StaffCourseAssignments.AnyAsync(x => x.CourseOfferingId == offering.Id && x.StaffUserId == request.StaffUserId && x.Role == role, ct))
                throw new BusinessException("Staff member is not assigned to this course and lecture type.", 403);
            if (request.ActivityType == ActivityType.TheoreticalLecture)
            {
                series.AudienceStudyYear = offering.Course.StudyYear;
                series.DivisionId = null;
            }
            else
            {
                if (request.DivisionId is null) throw new BusinessException("Division is required for practical lectures.");
                series.Division = await db.Divisions.Include(x => x.Memberships).SingleOrDefaultAsync(x => x.Id == request.DivisionId, ct)
                    ?? throw new BusinessException("Division not found.", 404);
                if (series.Division.StudyYear != offering.Course.StudyYear)
                    throw new BusinessException("Division study year must match the course.");
                series.AudienceStudyYear = series.Division.StudyYear;
            }
        }
        await ValidateRoomAndEquipmentAsync(series, ct);
        return series;
    }

    private async Task ValidateRoomAndEquipmentAsync(ScheduleSeries series, CancellationToken ct)
    {
        var room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == series.RoomId && x.IsActive, ct)
            ?? throw new BusinessException("Active room not found.", 404);
        if (series.ActivityType == ActivityType.PracticalLecture)
        {
            if (!room.IsLab) throw new BusinessException("Practical lectures require a laboratory.");
            var size = series.Division?.Memberships.Count ?? await db.DivisionMemberships.CountAsync(x => x.DivisionId == series.DivisionId, ct);
            if (room.Capacity < size) throw new BusinessException("Laboratory capacity is smaller than the division.", 409);
        }
        if (series.CourseOffering?.Course.RequiresProjector == true && !room.HasProjector)
            throw new BusinessException("This course requires a projector.");
        if (series.CourseOffering?.Course.RequiresLab == true && !room.IsLab)
            throw new BusinessException("This course requires a laboratory.");
    }

    private async Task<IReadOnlyCollection<DateOnly>> ExpandDatesAsync(DayOfWeek day, DateOnly start, DateOnly end, bool recurring, CancellationToken ct)
    {
        var first = start;
        while (first.DayOfWeek != day && first <= end) first = first.AddDays(1);
        if (first > end) throw new BusinessException("The selected day does not occur within the date range.");
        var holidays = await db.NonTeachingDays.Where(x => x.Date >= start && x.Date <= end).Select(x => x.Date).ToListAsync(ct);
        if (!recurring) return holidays.Contains(first) ? throw new BusinessException("The selected date is a non-teaching day.") : [first];
        var dates = new List<DateOnly>();
        for (var date = first; date <= end; date = date.AddDays(7))
            if (!holidays.Contains(date)) dates.Add(date);
        if (dates.Count == 0) throw new BusinessException("The recurring series has no teaching dates.");
        return dates;
    }

    private async Task EnsureNoConflictsAsync(ScheduleSeries series, IReadOnlyCollection<DateOnly> dates, Guid? excludedSeriesId, CancellationToken ct)
    {
        var unavailableRanges = await db.RoomUnavailabilities.Where(x => x.RoomId == series.RoomId).AsNoTracking().ToListAsync(ct);
        var roomUnavailable = unavailableRanges.Any(x => dates.Any(date => x.StartsOn <= date && x.EndsOn >= date));
        if (roomUnavailable) throw new BusinessException("Room is unavailable during part of the requested period.", 409);

        var conflicts = await db.ScheduleOccurrences.Where(x => dates.Contains(x.Date) && x.TimeSlotId == series.TimeSlotId && !x.IsCancelled
            && x.ScheduleSeriesId != excludedSeriesId).Include(x => x.ScheduleSeries).ToListAsync(ct);
        if (conflicts.Any(x => x.RoomId == series.RoomId)) throw new BusinessException("Room conflict detected.", 409, "room_conflict");
        if (conflicts.Any(x => x.StaffUserId == series.StaffUserId)) throw new BusinessException("Staff conflict detected.", 409, "staff_conflict");

        if (series.ActivityType == ActivityType.TheoreticalLecture && conflicts.Any(x =>
                x.ScheduleSeries.AudienceStudyYear == series.AudienceStudyYear &&
                x.ScheduleSeries.ActivityType is ActivityType.TheoreticalLecture or ActivityType.PracticalLecture))
            throw new BusinessException("Study-year timetable conflict detected.", 409, "audience_conflict");
        if (series.ActivityType == ActivityType.PracticalLecture && conflicts.Any(x =>
                x.ScheduleSeries.ActivityType == ActivityType.TheoreticalLecture && x.ScheduleSeries.AudienceStudyYear == series.AudienceStudyYear
                || x.ScheduleSeries.ActivityType == ActivityType.PracticalLecture && x.ScheduleSeries.DivisionId == series.DivisionId))
            throw new BusinessException("Division timetable conflict detected.", 409, "audience_conflict");
    }

    private static ScheduleOccurrence NewOccurrence(ScheduleSeries series, DateOnly date) => new()
    {
        ScheduleSeriesId = series.Id, RoomId = series.RoomId, StaffUserId = series.StaffUserId,
        TimeSlotId = series.TimeSlotId, Date = date
    };

    private async Task NotifyAudienceAsync(ScheduleSeries series, NotificationType type, string titleAr, string titleEn, CancellationToken ct)
    {
        Guid[] userIds;
        if (series.ActivityType == ActivityType.TheoreticalLecture && series.CourseOfferingId is not null)
        {
            var courseId = series.CourseOffering?.CourseId ?? await db.CourseOfferings.Where(x => x.Id == series.CourseOfferingId).Select(x => x.CourseId).SingleAsync(ct);
            userIds = await db.StudentCourseRecords.Where(x => x.CourseId == courseId && x.Status != CourseResultStatus.Passed)
                .Select(x => x.StudentUserId).Distinct().ToArrayAsync(ct);
        }
        else if (series.ActivityType == ActivityType.PracticalLecture && series.DivisionId is not null)
            userIds = await db.DivisionMemberships.Where(x => x.DivisionId == series.DivisionId).Select(x => x.StudentUserId).ToArrayAsync(ct);
        else return;

        await notifications.CreateAsync(type, titleAr, titleEn,
            $"{series.TitleArabic} - {series.DayOfWeek} - الفترة {series.TimeSlotId}",
            $"{series.TitleEnglish} - {series.DayOfWeek} - period {series.TimeSlotId}",
            $"/schedule/{series.Id}", userIds, ct);
    }

    private async Task AcquireScheduleLockAsync(CancellationToken ct) => await db.Database.ExecuteSqlRawAsync(
        "DECLARE @result int; EXEC @result = sp_getapplock @Resource = N'FacultyManagement.Schedule', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Could not acquire schedule lock.', 1;", ct);
}
