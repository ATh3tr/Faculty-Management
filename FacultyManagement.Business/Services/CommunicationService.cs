using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class CommunicationService(FacultyDbContext db, INotificationService notifications)
{
    public async Task<Guid> PublishAnnouncementAsync(CreateAnnouncementRequest request, Guid authorId, CancellationToken ct = default)
    {
        ValidateAudience(request);
        var announcement = new Announcement
        {
            TitleArabic = request.TitleArabic.Trim(), TitleEnglish = request.TitleEnglish.Trim(),
            BodyArabic = request.BodyArabic.Trim(), BodyEnglish = request.BodyEnglish.Trim(), Audience = request.Audience,
            StudyYear = request.StudyYear, DivisionId = request.DivisionId,
            StudentUserId = request.StudentUserId, CreatedByUserId = authorId
        };
        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(ct);
        var recipients = await ResolveRecipientsAsync(request, ct);
        await notifications.CreateAsync(NotificationType.Announcement,
            announcement.TitleArabic, announcement.TitleEnglish, announcement.BodyArabic, announcement.BodyEnglish,
            $"/announcements/{announcement.Id}", recipients, ct);
        return announcement.Id;
    }

    public async Task<IReadOnlyCollection<NotificationView>> GetNotificationsAsync(Guid userId, string language, CancellationToken ct = default)
    {
        var english = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
        return await db.NotificationRecipients.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Notification.CreatedAtUtc)
            .Select(x => new NotificationView(x.NotificationId, x.Notification.Type,
                english ? x.Notification.TitleEnglish : x.Notification.TitleArabic,
                english ? x.Notification.BodyEnglish : x.Notification.BodyArabic,
                x.Notification.Link, x.Notification.CreatedAtUtc, x.ReadAtUtc != null))
            .ToListAsync(ct);
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var recipient = await db.NotificationRecipients.SingleOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId, ct)
            ?? throw new BusinessException("Notification not found.", 404);
        recipient.ReadAtUtc ??= DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static void ValidateAudience(CreateAnnouncementRequest request)
    {
        if (request.Audience == AnnouncementAudience.StudyYear && request.StudyYear is not (>= 1 and <= 5))
            throw new BusinessException("A valid study year is required.");
        if (request.Audience == AnnouncementAudience.Division && request.DivisionId is null)
            throw new BusinessException("Division is required.");
        if (request.Audience == AnnouncementAudience.Student && request.StudentUserId is null)
            throw new BusinessException("Student is required.");
    }

    private async Task<Guid[]> ResolveRecipientsAsync(CreateAnnouncementRequest request, CancellationToken ct) => request.Audience switch
    {
        AnnouncementAudience.Everyone => await db.Users.Where(x => x.IsApproved).Select(x => x.Id).ToArrayAsync(ct),
        AnnouncementAudience.StudyYear => await db.StudentProfiles.Where(x => x.CurrentStudyYear == request.StudyYear).Select(x => x.UserId).ToArrayAsync(ct),
        AnnouncementAudience.Division => await db.DivisionMemberships.Where(x => x.DivisionId == request.DivisionId).Select(x => x.StudentUserId).ToArrayAsync(ct),
        AnnouncementAudience.Student => [request.StudentUserId!.Value],
        AnnouncementAudience.Staff => await db.Users.Where(x => x.IsApproved && x.AccountKind == AccountKind.Staff).Select(x => x.Id).ToArrayAsync(ct),
        _ => []
    };
}
