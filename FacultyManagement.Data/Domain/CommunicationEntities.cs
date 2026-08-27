namespace FacultyManagement.Data.Domain;

public sealed class Announcement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TitleArabic { get; set; } = string.Empty;
    public string TitleEnglish { get; set; } = string.Empty;
    public string BodyArabic { get; set; } = string.Empty;
    public string BodyEnglish { get; set; } = string.Empty;
    public AnnouncementAudience Audience { get; set; }
    public int? StudyYear { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? StudentUserId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NotificationType Type { get; set; }
    public string TitleArabic { get; set; } = string.Empty;
    public string TitleEnglish { get; set; } = string.Empty;
    public string BodyArabic { get; set; } = string.Empty;
    public string BodyEnglish { get; set; } = string.Empty;
    public string? Link { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<NotificationRecipient> Recipients { get; set; } = [];
}

public sealed class NotificationRecipient
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public Notification Notification { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

public sealed class SystemSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}

public sealed class AuditEntry
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
