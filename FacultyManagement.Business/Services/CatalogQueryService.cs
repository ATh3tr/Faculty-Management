using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class CatalogQueryService(FacultyDbContext db)
{
    public async Task<IReadOnlyCollection<AcademicYearView>> AcademicYearsAsync(CancellationToken ct = default) =>
        await db.AcademicYears.AsNoTracking().OrderByDescending(x => x.StartsOn).Select(x => new AcademicYearView(
            x.Id, x.Name, x.StartsOn, x.EndsOn, x.IsCurrent,
            x.Semesters.OrderBy(s => s.Number).Select(s => new SemesterView(s.Id, s.Number, s.StartsOn, s.EndsOn, s.IsPublished)).ToArray()))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<CourseView>> CoursesAsync(int? studyYear, CancellationToken ct = default) =>
        await db.Courses.AsNoTracking().Where(x => studyYear == null || x.StudyYear == studyYear)
            .OrderBy(x => x.StudyYear).ThenBy(x => x.SemesterNumber).ThenBy(x => x.Code)
            .Select(x => new CourseView(x.Id, x.Code, x.NameArabic, x.NameEnglish, x.StudyYear, x.SemesterNumber,
                x.TheoreticalSessionsPerWeek, x.PracticalSessionsPerDivisionPerWeek, x.RequiresProjector, x.RequiresLab, x.IsActive))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<OfferingView>> OfferingsAsync(Guid academicYearId, Guid? semesterId, CancellationToken ct = default) =>
        await db.CourseOfferings.AsNoTracking().Where(x => x.AcademicYearId == academicYearId && (semesterId == null || x.SemesterId == semesterId))
            .OrderBy(x => x.Course.StudyYear).ThenBy(x => x.Course.Code)
            .Select(x => new OfferingView(x.Id, x.CourseId, x.Course.Code, x.AcademicYearId, x.SemesterId,
                x.StaffAssignments.Select(s => new StaffAssignmentView(s.StaffUserId, s.StaffUser.FullNameArabic, s.StaffUser.FullNameEnglish, s.Role)).ToArray()))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<DivisionView>> DivisionsAsync(Guid academicYearId, int? studyYear, CancellationToken ct = default) =>
        await db.Divisions.AsNoTracking().Where(x => x.AcademicYearId == academicYearId && (studyYear == null || x.StudyYear == studyYear))
            .OrderBy(x => x.StudyYear).ThenBy(x => x.Number)
            .Select(x => new DivisionView(x.Id, x.AcademicYearId, x.StudyYear, x.Number, x.Capacity, x.Memberships.Count))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<RoomView>> RoomsAsync(CancellationToken ct = default) =>
        await db.Rooms.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new RoomView(x.Id, x.Code, x.Capacity, x.IsLab, x.HasProjector, x.IsActive)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<StudentView>> StudentsAsync(int? studyYear, string? search, CancellationToken ct = default)
    {
        var query = db.StudentProfiles.AsNoTracking().Where(x => studyYear == null || x.CurrentStudyYear == studyYear);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.UniversityNumber.Contains(search) || x.User.FullNameArabic.Contains(search) || x.User.FullNameEnglish.Contains(search));
        return await query.OrderBy(x => x.UniversityNumber).Take(250)
            .Select(x => new StudentView(x.UserId, x.UniversityNumber, x.User.FullNameArabic, x.User.FullNameEnglish, x.CurrentStudyYear, x.Standing))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<StaffView>> StaffAsync(CancellationToken ct = default)
    {
        var staff = await db.StaffProfiles.AsNoTracking().Where(x => x.User.IsApproved)
            .Select(x => new { x.UserId, x.User.FullNameArabic, x.User.FullNameEnglish, x.StaffNumber }).ToListAsync(ct);
        var roles = await (from userRole in db.UserRoles
                           join role in db.Roles on userRole.RoleId equals role.Id
                           where staff.Select(x => x.UserId).Contains(userRole.UserId)
                           select new { userRole.UserId, role.Name }).ToListAsync(ct);
        return staff.Select(x => new StaffView(x.UserId, x.FullNameArabic, x.FullNameEnglish, x.StaffNumber,
            roles.Where(r => r.UserId == x.UserId).Select(r => r.Name!).ToArray())).ToArray();
    }

    public async Task<IReadOnlyCollection<AppealView>> AppealsAsync(Guid userId, bool studentOnly, bool professorOnly, CancellationToken ct = default)
    {
        var query = db.MarkAppeals.AsNoTracking().AsQueryable();
        if (studentOnly) query = query.Where(x => x.StudentUserId == userId);
        if (professorOnly) query = query.Where(x => db.StaffCourseAssignments.Any(a => a.StaffUserId == userId
            && a.Role == FacultyManagement.Data.Domain.StaffCourseRole.Professor
            && a.CourseOffering.CourseId == x.MarkAttempt.StudentCourseRecord.CourseId));
        return await query.OrderByDescending(x => x.SubmittedAtUtc).Select(x => new AppealView(
            x.Id, x.MarkAttemptId, x.StudentUserId, x.MarkAttempt.StudentCourseRecord.StudentUser.FullNameEnglish,
            x.MarkAttempt.StudentCourseRecord.Course.Code, x.Reason, x.Status, x.ProfessorComment, x.DecisionComment, x.SubmittedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<AuditView>> AuditAsync(int take = 200, CancellationToken ct = default) =>
        await db.AuditEntries.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(Math.Clamp(take, 1, 1000))
            .Select(x => new AuditView(x.Id, x.UserId, x.Action, x.EntityType, x.EntityId, x.CreatedAtUtc)).ToListAsync(ct);
}
