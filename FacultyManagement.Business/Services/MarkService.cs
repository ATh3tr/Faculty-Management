using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class MarkService(FacultyDbContext db, INotificationService notifications)
{
    public async Task<MarkAttempt> EnterAsync(EnterMarkRequest request, Guid officerId, CancellationToken ct = default)
    {
        ValidateResult(request.ResultKind, request.Mark);
        var record = await db.StudentCourseRecords.SingleOrDefaultAsync(x => x.Id == request.StudentCourseRecordId, ct)
            ?? throw new BusinessException("Student course record not found.", 404);
        if (record.Status == CourseResultStatus.Passed)
            throw new BusinessException("A passed course cannot be attempted again.", 409, "course_already_passed");
        var period = await db.ExamPeriods.SingleOrDefaultAsync(x => x.Id == request.ExamPeriodId, ct)
            ?? throw new BusinessException("Exam period not found.", 404);
        if (period.IsClosed) throw new BusinessException("The exam period is closed.", 409);

        var attempt = new MarkAttempt
        {
            StudentCourseRecordId = record.Id, ExamPeriodId = period.Id,
            ResultKind = request.ResultKind, Mark = request.Mark, EnteredByUserId = officerId,
            IsPublished = request.Publish, PublishedAtUtc = request.Publish ? DateTime.UtcNow : null
        };
        db.MarkAttempts.Add(attempt);
        if (request.Publish) ApplyCourseStatus(record, request.ResultKind, request.Mark);
        await db.SaveChangesAsync(ct);
        if (request.Publish) await NotifyMarkAsync(record.StudentUserId, attempt, false, ct);
        return attempt;
    }

    public async Task PublishAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await db.MarkAttempts.Include(x => x.StudentCourseRecord)
            .SingleOrDefaultAsync(x => x.Id == attemptId, ct) ?? throw new BusinessException("Mark attempt not found.", 404);
        if (attempt.IsPublished) return;
        attempt.IsPublished = true;
        attempt.PublishedAtUtc = DateTime.UtcNow;
        await RecalculateCourseStatusAsync(attempt.StudentCourseRecord, attempt.Id, attempt.ResultKind, attempt.Mark, ct);
        await db.SaveChangesAsync(ct);
        await NotifyMarkAsync(attempt.StudentCourseRecord.StudentUserId, attempt, false, ct);
    }

    public async Task CorrectAsync(Guid attemptId, CorrectMarkRequest request, Guid officerId, CancellationToken ct = default)
    {
        ValidateResult(request.ResultKind, request.Mark);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new BusinessException("A correction reason is required.");
        var attempt = await db.MarkAttempts.Include(x => x.StudentCourseRecord)
            .SingleOrDefaultAsync(x => x.Id == attemptId, ct) ?? throw new BusinessException("Mark attempt not found.", 404);
        var correction = new MarkCorrection
        {
            MarkAttemptId = attempt.Id, OldResultKind = attempt.ResultKind, OldMark = attempt.Mark,
            NewResultKind = request.ResultKind, NewMark = request.Mark,
            Reason = request.Reason.Trim(), CorrectedByUserId = officerId
        };
        db.MarkCorrections.Add(correction);
        attempt.ResultKind = request.ResultKind;
        attempt.Mark = request.Mark;
        if (attempt.IsPublished)
            await RecalculateCourseStatusAsync(attempt.StudentCourseRecord, attempt.Id, request.ResultKind, request.Mark, ct);
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = officerId, Action = "MarkCorrection", EntityType = nameof(MarkAttempt), EntityId = attempt.Id.ToString(),
            OldValuesJson = $"{{\"kind\":\"{correction.OldResultKind}\",\"mark\":{correction.OldMark?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}}}",
            NewValuesJson = $"{{\"kind\":\"{correction.NewResultKind}\",\"mark\":{correction.NewMark?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}}}"
        });
        await db.SaveChangesAsync(ct);
        if (attempt.IsPublished) await NotifyMarkAsync(attempt.StudentCourseRecord.StudentUserId, attempt, true, ct);
    }

    public async Task<ImportResult> ImportAsync(ImportMarksRequest request, Guid officerId, CancellationToken ct = default)
    {
        var errors = new List<ImportError>();
        var prepared = new List<(StudentCourseRecord Record, ImportMarkRow Row)>();
        var period = await db.ExamPeriods.SingleOrDefaultAsync(x => x.Id == request.ExamPeriodId, ct);
        if (period is null) throw new BusinessException("Exam period not found.", 404);
        if (period.IsClosed) throw new BusinessException("The exam period is closed.", 409);

        var rowNumber = 1;
        foreach (var row in request.Rows)
        {
            try
            {
                ValidateResult(row.ResultKind, row.Mark);
                var record = await db.StudentCourseRecords.Include(x => x.StudentUser).Include(x => x.Course)
                    .SingleOrDefaultAsync(x => x.StudentUser.StudentProfile!.UniversityNumber == row.UniversityNumber && x.Course.Code == row.CourseCode, ct);
                if (record is null) throw new BusinessException("Student/course record was not found.");
                if (record.Status == CourseResultStatus.Passed) throw new BusinessException("Course is already passed.");
                if (prepared.Any(x => x.Record.Id == record.Id)) throw new BusinessException("Duplicate student/course row in the import.");
                prepared.Add((record, row));
            }
            catch (BusinessException ex) { errors.Add(new ImportError(rowNumber, ex.Message)); }
            rowNumber++;
        }

        if (errors.Count > 0) return new ImportResult(0, errors);
        foreach (var item in prepared)
        {
            var attempt = new MarkAttempt
            {
                StudentCourseRecordId = item.Record.Id, ExamPeriodId = request.ExamPeriodId,
                ResultKind = item.Row.ResultKind, Mark = item.Row.Mark, EnteredByUserId = officerId,
                IsPublished = request.Publish, PublishedAtUtc = request.Publish ? DateTime.UtcNow : null
            };
            db.MarkAttempts.Add(attempt);
            if (request.Publish) ApplyCourseStatus(item.Record, item.Row.ResultKind, item.Row.Mark);
        }
        await db.SaveChangesAsync(ct);

        if (request.Publish)
        {
            foreach (var studentId in prepared.Select(x => x.Record.StudentUserId).Distinct())
                await notifications.CreateAsync(NotificationType.MarkPublished, "تم نشر علامة", "Mark published",
                    "تم نشر علامة امتحانية جديدة.", "A new exam mark has been published.", "/marks", [studentId], ct);
        }
        return new ImportResult(prepared.Count, []);
    }

    public async Task<IReadOnlyCollection<MarkView>> StudentMarksAsync(Guid studentId, CancellationToken ct = default)
    {
        var rows = await db.StudentCourseRecords.Where(x => x.StudentUserId == studentId)
            .SelectMany(x => x.MarkAttempts.Where(m => m.IsPublished).OrderByDescending(m => m.EnteredAtUtc).Take(1),
                (record, mark) => new MarkView(mark.Id, record.CourseId, record.Course.Code, mark.ResultKind, mark.Mark, mark.IsPublished, mark.EnteredAtUtc))
            .AsNoTracking().ToListAsync(ct);
        return rows;
    }

    private static void ValidateResult(ExamResultKind kind, decimal? mark)
    {
        if (kind == ExamResultKind.Numeric && (mark is null or < 0 or > 100))
            throw new BusinessException("Numeric marks must be between 0 and 100.");
        if (kind != ExamResultKind.Numeric && mark is not null)
            throw new BusinessException("Non-numeric results cannot include a numeric mark.");
    }

    private static void ApplyCourseStatus(StudentCourseRecord record, ExamResultKind kind, decimal? mark)
    {
        record.Status = AcademicRules.ResultAfterAttempt(kind, mark);
        if (record.Status == CourseResultStatus.Passed)
        {
            record.PassedAtUtc = DateTime.UtcNow;
        }
    }

    private async Task RecalculateCourseStatusAsync(StudentCourseRecord record, Guid currentAttemptId, ExamResultKind currentKind, decimal? currentMark, CancellationToken ct)
    {
        var otherPassExists = await db.MarkAttempts.AnyAsync(x => x.StudentCourseRecordId == record.Id && x.Id != currentAttemptId
            && x.ResultKind == ExamResultKind.Numeric && x.Mark >= 60, ct);
        if (otherPassExists || currentKind == ExamResultKind.Numeric && currentMark >= 60)
        {
            record.Status = CourseResultStatus.Passed;
            record.PassedAtUtc ??= DateTime.UtcNow;
        }
        else
        {
            record.Status = currentKind is ExamResultKind.Numeric or ExamResultKind.Absent ? CourseResultStatus.Failed : CourseResultStatus.InProgress;
            record.PassedAtUtc = null;
        }
    }

    private Task NotifyMarkAsync(Guid studentId, MarkAttempt attempt, bool corrected, CancellationToken ct) =>
        notifications.CreateAsync(corrected ? NotificationType.MarkCorrected : NotificationType.MarkPublished,
            corrected ? "تم تصحيح العلامة" : "تم نشر العلامة", corrected ? "Mark corrected" : "Mark published",
            corrected ? "تم تصحيح علامتك الامتحانية." : "تم نشر علامة امتحانية جديدة.",
            corrected ? "Your exam mark was corrected." : "A new exam mark was published.",
            "/marks", [studentId], ct);
}
