using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class AcademicService(FacultyDbContext db, UserManager<ApplicationUser> userManager)
{
    public async Task<AcademicYear> CreateAcademicYearAsync(CreateAcademicYearRequest request, CancellationToken ct = default)
    {
        if (request.FirstSemesterStartsOn < request.StartsOn || request.SecondSemesterEndsOn > request.EndsOn)
            throw new BusinessException("Semester dates must be inside the academic year.");
        if (request.FirstSemesterEndsOn >= request.SecondSemesterStartsOn)
            throw new BusinessException("The two semesters cannot overlap.");
        if (await db.AcademicYears.AnyAsync(x => x.Name == request.Name, ct))
            throw new BusinessException("Academic year name already exists.", 409);

        var year = new AcademicYear
        {
            Name = request.Name, StartsOn = request.StartsOn, EndsOn = request.EndsOn,
            Semesters =
            [
                new Semester { Number = 1, StartsOn = request.FirstSemesterStartsOn, EndsOn = request.FirstSemesterEndsOn },
                new Semester { Number = 2, StartsOn = request.SecondSemesterStartsOn, EndsOn = request.SecondSemesterEndsOn }
            ]
        };
        db.AcademicYears.Add(year);
        await db.SaveChangesAsync(ct);
        return year;
    }

    public async Task SetCurrentAcademicYearAsync(Guid id, CancellationToken ct = default)
    {
        var selected = await db.AcademicYears.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("Academic year not found.", 404);
        var current = await db.AcademicYears.Where(x => x.IsCurrent).ToListAsync(ct);
        foreach (var item in current) item.IsCurrent = false;
        selected.IsCurrent = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task AddNonTeachingDayAsync(Guid academicYearId, SetNonTeachingDayRequest request, CancellationToken ct = default)
    {
        var year = await db.AcademicYears.FindAsync([academicYearId], ct) ?? throw new BusinessException("Academic year not found.", 404);
        if (request.Date < year.StartsOn || request.Date > year.EndsOn) throw new BusinessException("Date is outside the academic year.");
        db.NonTeachingDays.Add(new NonTeachingDay { AcademicYearId = academicYearId, Date = request.Date, Reason = request.Reason.Trim() });
        await db.SaveChangesAsync(ct);
    }

    public async Task<ExamPeriod> CreateExamPeriodAsync(CreateExamPeriodRequest request, CancellationToken ct = default)
    {
        InputRules.ValidateBilingual(request.NameArabic, request.NameEnglish, "name");
        var year = await db.AcademicYears.FindAsync([request.AcademicYearId], ct) ?? throw new BusinessException("Academic year not found.", 404);
        if (request.StartsOn < year.StartsOn || request.EndsOn > year.EndsOn || request.EndsOn < request.StartsOn)
            throw new BusinessException("Exam-period dates must be valid and inside the academic year.");
        var period = new ExamPeriod
        {
            AcademicYearId = request.AcademicYearId, NameArabic = request.NameArabic.Trim(), NameEnglish = request.NameEnglish.Trim(),
            StartsOn = request.StartsOn, EndsOn = request.EndsOn, IsRetake = request.IsRetake
        };
        db.ExamPeriods.Add(period);
        await db.SaveChangesAsync(ct);
        return period;
    }

    public async Task CloseExamPeriodAsync(Guid examPeriodId, CancellationToken ct = default)
    {
        var period = await db.ExamPeriods.FindAsync([examPeriodId], ct) ?? throw new BusinessException("Exam period not found.", 404);
        period.IsClosed = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> SyncMandatoryEnrollmentsAsync(Guid academicYearId, int studyYear, CancellationToken ct = default)
    {
        if (studyYear is < 1 or > 5) throw new BusinessException("Study year must be between 1 and 5.");
        var courseIds = await db.Courses.Where(x => x.IsActive && x.StudyYear == studyYear).Select(x => x.Id).ToListAsync(ct);
        var studentIds = await db.StudentProfiles.Where(x => x.CurrentStudyYear == studyYear && x.Standing == AcademicStanding.Active)
            .Select(x => x.UserId).ToListAsync(ct);
        var existing = await db.StudentCourseRecords.Where(x => studentIds.Contains(x.StudentUserId) && courseIds.Contains(x.CourseId))
            .Select(x => new { x.StudentUserId, x.CourseId }).ToListAsync(ct);
        var keys = existing.Select(x => (x.StudentUserId, x.CourseId)).ToHashSet();
        var added = 0;
        foreach (var studentId in studentIds)
        foreach (var courseId in courseIds)
            if (keys.Add((studentId, courseId)))
            {
                db.StudentCourseRecords.Add(new StudentCourseRecord
                {
                    StudentUserId = studentId, CourseId = courseId, AssignedAcademicYearId = academicYearId
                });
                added++;
            }
        await db.SaveChangesAsync(ct);
        return added;
    }

    public async Task<Course> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        InputRules.ValidateBilingual(request.NameArabic, request.NameEnglish, "name");
        ValidateCourse(request.StudyYear, request.SemesterNumber, request.TheoreticalSessionsPerWeek, request.PracticalSessionsPerDivisionPerWeek);
        var course = new Course
        {
            Code = request.Code.Trim(), NameArabic = request.NameArabic.Trim(), NameEnglish = request.NameEnglish.Trim(),
            StudyYear = request.StudyYear, SemesterNumber = request.SemesterNumber,
            TheoreticalSessionsPerWeek = request.TheoreticalSessionsPerWeek,
            PracticalSessionsPerDivisionPerWeek = request.PracticalSessionsPerDivisionPerWeek,
            RequiresProjector = request.RequiresProjector, RequiresLab = request.RequiresLab
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);
        return course;
    }

    public async Task UpdateCourseAsync(Guid id, UpdateCourseRequest request, CancellationToken ct = default)
    {
        InputRules.ValidateBilingual(request.NameArabic, request.NameEnglish, "name");
        ValidateCourse(request.StudyYear, request.SemesterNumber, request.TheoreticalSessionsPerWeek, request.PracticalSessionsPerDivisionPerWeek);
        var course = await db.Courses.FindAsync([id], ct) ?? throw new BusinessException("Course not found.", 404);
        course.NameArabic = request.NameArabic.Trim(); course.NameEnglish = request.NameEnglish.Trim();
        course.StudyYear = request.StudyYear; course.SemesterNumber = request.SemesterNumber;
        course.TheoreticalSessionsPerWeek = request.TheoreticalSessionsPerWeek;
        course.PracticalSessionsPerDivisionPerWeek = request.PracticalSessionsPerDivisionPerWeek;
        course.RequiresProjector = request.RequiresProjector; course.RequiresLab = request.RequiresLab; course.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task<CourseOffering> CreateOfferingAsync(CreateOfferingRequest request, CancellationToken ct = default)
    {
        var course = await db.Courses.FindAsync([request.CourseId], ct) ?? throw new BusinessException("Course not found.", 404);
        var semester = await db.Semesters.FindAsync([request.SemesterId], ct) ?? throw new BusinessException("Semester not found.", 404);
        if (semester.AcademicYearId != request.AcademicYearId || semester.Number != course.SemesterNumber)
            throw new BusinessException("Offering semester does not match the course.");
        var offering = new CourseOffering { CourseId = request.CourseId, AcademicYearId = request.AcademicYearId, SemesterId = request.SemesterId };
        db.CourseOfferings.Add(offering);
        await db.SaveChangesAsync(ct);
        return offering;
    }

    public async Task AssignStaffAsync(Guid offeringId, AssignStaffRequest request, Guid adminId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(request.StaffUserId.ToString()) ?? throw new BusinessException("Staff account not found.", 404);
        var requiredRole = request.Role == StaffCourseRole.Professor ? AppRoles.Professor : AppRoles.Teacher;
        if (!user.IsApproved || !await userManager.IsInRoleAsync(user, requiredRole))
            throw new BusinessException($"Staff member must have the {requiredRole} role.");
        if (!await db.CourseOfferings.AnyAsync(x => x.Id == offeringId, ct))
            throw new BusinessException("Course offering not found.", 404);
        db.StaffCourseAssignments.Add(new StaffCourseAssignment { CourseOfferingId = offeringId, StaffUserId = request.StaffUserId, Role = request.Role });
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = adminId, Action = "StaffCourseAssigned", EntityType = nameof(CourseOffering), EntityId = offeringId.ToString(),
            NewValuesJson = $"{{\"staffUserId\":\"{request.StaffUserId}\",\"role\":\"{request.Role}\"}}"
        });
        await db.SaveChangesAsync(ct);
    }

    private static void ValidateCourse(int year, int semester, int theory, int practical)
    {
        if (year is < 1 or > 5) throw new BusinessException("Study year must be between 1 and 5.");
        if (semester is < 1 or > 2) throw new BusinessException("Semester must be 1 or 2.");
        if (theory < 0 || practical < 0 || theory + practical == 0) throw new BusinessException("At least one weekly session is required.");
    }
}
