using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class SettingsService(FacultyDbContext db) : ISettingsService
{
    public async Task<int> GetIntAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await db.SystemSettings.Where(x => x.Key == key)
            .Select(x => x.Value).SingleOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException($"Missing setting '{key}'.", 500, "missing_setting");
        return int.TryParse(value, out var parsed)
            ? parsed
            : throw new BusinessException($"Setting '{key}' is not a valid integer.", 500, "invalid_setting");
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var values = await db.SystemSettings.AsNoTracking()
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        values.TryAdd(SettingKeys.FacultyNameArabic, BrandingDefaults.FacultyNameArabic);
        values.TryAdd(SettingKeys.FacultyNameEnglish, BrandingDefaults.FacultyNameEnglish);
        return values;
    }

    public async Task SetAsync(string key, string value, Guid userId, CancellationToken cancellationToken = default)
    {
        var setting = await db.SystemSettings.FindAsync([key], cancellationToken);
        if (setting is null)
            db.SystemSettings.Add(new SystemSetting { Key = key, Value = value, UpdatedByUserId = userId });
        else
        {
            setting.Value = value;
            setting.UpdatedByUserId = userId;
            setting.UpdatedAtUtc = DateTime.UtcNow;
        }
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = userId, Action = "SettingChanged", EntityType = nameof(SystemSetting), EntityId = key,
            NewValuesJson = $"{{\"value\":\"{value.Replace("\"", "\\\"")}\"}}"
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class NotificationService(FacultyDbContext db, IRealtimeNotifier realtime) : INotificationService
{
    public async Task<Notification> CreateAsync(
        NotificationType type, string titleArabic, string titleEnglish,
        string bodyArabic, string bodyEnglish, string? link,
        IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var recipients = userIds.Distinct().ToArray();
        var notification = new Notification
        {
            Type = type,
            TitleArabic = titleArabic,
            TitleEnglish = titleEnglish,
            BodyArabic = bodyArabic,
            BodyEnglish = bodyEnglish,
            Link = link,
            Recipients = recipients.Select(x => new NotificationRecipient { UserId = x }).ToList()
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        var englishView = new NotificationView(notification.Id, type, titleEnglish, bodyEnglish, link, notification.CreatedAtUtc, false);
        await realtime.NotifyUsersAsync(recipients, englishView, cancellationToken);
        return notification;
    }
}
