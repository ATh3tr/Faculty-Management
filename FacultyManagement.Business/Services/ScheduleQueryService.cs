using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class ScheduleQueryService(FacultyDbContext db)
{
    public async Task<IReadOnlyCollection<ScheduleView>> ForUserAsync(Guid userId, bool isStaff, string language, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from || to.DayNumber - from.DayNumber > 180) throw new BusinessException("Schedule range must be between 0 and 180 days.");
        var query = db.ScheduleOccurrences.AsNoTracking().Where(x => x.Date >= from && x.Date <= to && !x.IsCancelled
            && x.ScheduleSeries.Status == ScheduleStatus.Published);
        if (isStaff)
            query = query.Where(x => x.StaffUserId == userId);
        else
        {
            var profile = await db.StudentProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
                ?? throw new BusinessException("Student profile not found.", 404);
            var currentYearId = await db.AcademicYears.Where(x => x.IsCurrent).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
            var divisionId = currentYearId is null ? null : await db.DivisionMemberships
                .Where(x => x.StudentUserId == userId && x.AcademicYearId == currentYearId).Select(x => (Guid?)x.DivisionId).SingleOrDefaultAsync(ct);
            var failedCourseIds = await db.StudentCourseRecords.Where(x => x.StudentUserId == userId && x.Status != CourseResultStatus.Passed)
                .Select(x => x.CourseId).ToListAsync(ct);
            query = query.Where(x =>
                x.ScheduleSeries.ActivityType == ActivityType.PracticalLecture && x.ScheduleSeries.DivisionId == divisionId
                || x.ScheduleSeries.ActivityType == ActivityType.TheoreticalLecture &&
                   (x.ScheduleSeries.AudienceStudyYear == profile.CurrentStudyYear ||
                    x.ScheduleSeries.CourseOfferingId != null && failedCourseIds.Contains(x.ScheduleSeries.CourseOffering!.CourseId)));
        }
        var english = language == "en";
        return await query.OrderBy(x => x.Date).ThenBy(x => x.TimeSlotId)
            .Select(x => new ScheduleView(x.ScheduleSeriesId, x.ScheduleSeries.ActivityType,
                english ? x.ScheduleSeries.TitleEnglish : x.ScheduleSeries.TitleArabic,
                x.ScheduleSeries.Room.Code, x.Date, x.ScheduleSeries.TimeSlot.StartsAt,
                x.ScheduleSeries.TimeSlot.EndsAt, x.IsCancelled)).ToListAsync(ct);
    }
}
