using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class PromotionService(FacultyDbContext db, ISettingsService settings)
{
    public async Task<PromotionRunResult> RunAsync(
        Guid concludedAcademicYearId, Guid? nextAcademicYearId, bool preview, Guid adminId, CancellationToken ct = default)
    {
        if (!preview && nextAcademicYearId is null)
            throw new BusinessException("The next academic year is required for a committed promotion run.");
        if (!await db.AcademicYears.AnyAsync(x => x.Id == concludedAcademicYearId, ct))
            throw new BusinessException("Concluded academic year not found.", 404);
        if (nextAcademicYearId is not null && !await db.AcademicYears.AnyAsync(x => x.Id == nextAcademicYearId, ct))
            throw new BusinessException("Next academic year not found.", 404);
        if (!preview && await db.PromotionRuns.AnyAsync(x => x.AcademicYearId == concludedAcademicYearId && !x.IsPreview, ct))
            throw new BusinessException("Promotion has already been committed for this academic year.", 409);
        if (await db.ExamPeriods.AnyAsync(x => x.AcademicYearId == concludedAcademicYearId && !x.IsClosed, ct))
            throw new BusinessException("All exam periods must be closed before promotion.", 409);
        var hasBlockingResults = await db.StudentCourseRecords.AnyAsync(record => record.MarkAttempts
            .Where(mark => mark.IsPublished).OrderByDescending(mark => mark.EnteredAtUtc).Take(1)
            .Any(mark => mark.ResultKind == ExamResultKind.NotEntered || mark.ResultKind == ExamResultKind.Withheld), ct);
        if (hasBlockingResults)
            throw new BusinessException("Not-entered or withheld results must be resolved before promotion.", 409, "unresolved_marks");

        var maxFailures = await settings.GetIntAsync(SettingKeys.MaximumFailedCoursesForPromotion, ct);
        var programYears = await settings.GetIntAsync(SettingKeys.ProgramYears, ct);
        var students = await db.StudentProfiles.Where(x => x.Standing != AcademicStanding.Graduated && x.Standing != AcademicStanding.Suspended)
            .ToListAsync(ct);
        var run = new PromotionRun { AcademicYearId = concludedAcademicYearId, ExecutedByUserId = adminId, IsPreview = preview };
        var output = new List<PromotionStudentResult>();

        foreach (var student in students)
        {
            var failures = await db.StudentCourseRecords.CountAsync(x => x.StudentUserId == student.UserId && x.Status != CourseResultStatus.Passed, ct);
            var oldYear = student.CurrentStudyYear;
            var newYear = oldYear;
            AcademicStanding standing;

            (newYear, standing) = AcademicRules.DecidePromotion(oldYear, failures, maxFailures, programYears);

            run.Results.Add(new PromotionResult
            {
                StudentUserId = student.UserId, PreviousStudyYear = oldYear, NewStudyYear = newYear,
                OutstandingFailureCount = failures, NewStanding = standing
            });
            output.Add(new PromotionStudentResult(student.UserId, oldYear, newYear, failures, standing));

            if (preview) continue;
            student.CurrentStudyYear = newYear;
            student.Standing = standing;
            if (standing == AcademicStanding.Active && newYear > oldYear)
                await AssignNewYearCoursesAsync(student.UserId, newYear, nextAcademicYearId!.Value, ct);
        }

        db.PromotionRuns.Add(run);
        if (!preview)
            db.AuditEntries.Add(new AuditEntry
            {
                UserId = adminId, Action = "PromotionCommitted", EntityType = nameof(PromotionRun), EntityId = run.Id.ToString(),
                NewValuesJson = $"{{\"academicYearId\":\"{concludedAcademicYearId}\",\"students\":{output.Count}}}"
            });
        await db.SaveChangesAsync(ct);
        return new PromotionRunResult(run.Id, preview, output);
    }

    private async Task AssignNewYearCoursesAsync(Guid studentId, int year, Guid academicYearId, CancellationToken ct)
    {
        var existingCourseIds = await db.StudentCourseRecords.Where(x => x.StudentUserId == studentId).Select(x => x.CourseId).ToListAsync(ct);
        var newCourseIds = await db.Courses.Where(x => x.IsActive && x.StudyYear == year && !existingCourseIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct);
        db.StudentCourseRecords.AddRange(newCourseIds.Select(courseId => new StudentCourseRecord
        {
            StudentUserId = studentId, CourseId = courseId, AssignedAcademicYearId = academicYearId
        }));
    }
}
