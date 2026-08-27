using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class AppealService(FacultyDbContext db, ISettingsService settings, INotificationService notifications)
{
    public async Task<MarkAppeal> SubmitAsync(Guid studentId, CreateAppealRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new BusinessException("Appeal reason is required.");
        var attempt = await db.MarkAttempts.Include(x => x.StudentCourseRecord)
            .SingleOrDefaultAsync(x => x.Id == request.MarkAttemptId, ct)
            ?? throw new BusinessException("Mark attempt not found.", 404);
        if (attempt.StudentCourseRecord.StudentUserId != studentId) throw new BusinessException("This mark does not belong to the current student.", 403);
        if (!attempt.IsPublished || attempt.PublishedAtUtc is null) throw new BusinessException("Only published marks can be appealed.", 409);
        var days = await settings.GetIntAsync(SettingKeys.AppealDeadlineDays, ct);
        if (DateTime.UtcNow > attempt.PublishedAtUtc.Value.AddDays(days))
            throw new BusinessException("The appeal deadline has passed.", 409, "appeal_deadline_passed");
        if (await db.MarkAppeals.AnyAsync(x => x.MarkAttemptId == attempt.Id && x.StudentUserId == studentId, ct))
            throw new BusinessException("An appeal already exists for this mark.", 409);

        var appeal = new MarkAppeal { MarkAttemptId = attempt.Id, StudentUserId = studentId, Reason = request.Reason.Trim() };
        db.MarkAppeals.Add(appeal);
        await db.SaveChangesAsync(ct);
        return appeal;
    }

    public async Task ReviewByProfessorAsync(Guid appealId, Guid professorId, ProfessorReviewRequest request, CancellationToken ct = default)
    {
        var appeal = await db.MarkAppeals.Include(x => x.MarkAttempt).ThenInclude(x => x.StudentCourseRecord)
            .SingleOrDefaultAsync(x => x.Id == appealId, ct) ?? throw new BusinessException("Appeal not found.", 404);
        var courseId = appeal.MarkAttempt.StudentCourseRecord.CourseId;
        var assigned = await db.StaffCourseAssignments.AnyAsync(x => x.StaffUserId == professorId
            && x.Role == StaffCourseRole.Professor && x.CourseOffering.CourseId == courseId, ct);
        if (!assigned) throw new BusinessException("Professor is not assigned to this course.", 403);
        if (appeal.Status != AppealStatus.Submitted) throw new BusinessException("Appeal is not awaiting professor review.", 409);
        appeal.ProfessorUserId = professorId;
        appeal.ProfessorComment = request.Comment.Trim();
        appeal.Status = AppealStatus.ProfessorReviewed;
        await db.SaveChangesAsync(ct);
        await NotifyStudentAsync(appeal.StudentUserId, appeal.Id, "تمت مراجعة الاعتراض من قبل الأستاذ", "Appeal reviewed by professor", ct);
    }

    public async Task DecideAsync(Guid appealId, Guid officerId, AppealDecisionRequest request, CancellationToken ct = default)
    {
        var appeal = await db.MarkAppeals.SingleOrDefaultAsync(x => x.Id == appealId, ct)
            ?? throw new BusinessException("Appeal not found.", 404);
        if (appeal.Status != AppealStatus.ProfessorReviewed)
            throw new BusinessException("Professor review is required before a decision.", 409);
        appeal.Status = request.Accept ? AppealStatus.Accepted : AppealStatus.Rejected;
        appeal.DecisionComment = request.Comment.Trim();
        appeal.DecidedByUserId = officerId;
        appeal.DecidedAtUtc = DateTime.UtcNow;
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = officerId, Action = "AppealDecision", EntityType = nameof(MarkAppeal), EntityId = appeal.Id.ToString(),
            NewValuesJson = $"{{\"status\":\"{appeal.Status}\"}}"
        });
        await db.SaveChangesAsync(ct);
        await NotifyStudentAsync(appeal.StudentUserId, appeal.Id,
            request.Accept ? "تم قبول الاعتراض" : "تم رفض الاعتراض",
            request.Accept ? "Appeal accepted" : "Appeal rejected", ct);
    }

    private Task NotifyStudentAsync(Guid studentId, Guid appealId, string ar, string en, CancellationToken ct) =>
        notifications.CreateAsync(NotificationType.AppealChanged, ar, en, ar, en, $"/appeals/{appealId}", [studentId], ct);
}
