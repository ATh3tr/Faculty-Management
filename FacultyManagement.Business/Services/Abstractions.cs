using FacultyManagement.Business.Contracts;
using FacultyManagement.Data.Domain;

namespace FacultyManagement.Business.Services;

public interface IRealtimeNotifier
{
    Task AvailabilityChangedAsync(CancellationToken cancellationToken = default);
    Task NotifyUsersAsync(IEnumerable<Guid> userIds, NotificationView notification, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<Notification> CreateAsync(
        NotificationType type, string titleArabic, string titleEnglish,
        string bodyArabic, string bodyEnglish, string? link,
        IEnumerable<Guid> userIds, CancellationToken cancellationToken = default,
        IEnumerable<Guid>? realtimeExcludedUserIds = null);
}

public interface ISettingsService
{
    Task<int> GetIntAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, Guid userId, CancellationToken cancellationToken = default);
}
