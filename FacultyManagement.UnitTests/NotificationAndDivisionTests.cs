using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.UnitTests;

public sealed class NotificationAndDivisionTests
{
    [Fact]
    public async Task Announcement_sender_keeps_notification_without_realtime_popup()
    {
        await using var db = NewDatabase();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        db.Users.AddRange(User(senderId, "sender@example.com"), User(recipientId, "recipient@example.com"));
        await db.SaveChangesAsync();
        var realtime = new RecordingRealtimeNotifier();
        var service = new NotificationService(db, realtime);

        await service.CreateAsync(NotificationType.Announcement, "إعلان", "Announcement", "النص", "Body", null,
            [senderId, recipientId], default, [senderId]);

        Assert.Equal(2, await db.NotificationRecipients.CountAsync());
        Assert.Equal([recipientId], realtime.NotifiedUserIds);
    }

    [Fact]
    public async Task Existing_division_assignment_is_loaded_after_returning_to_page()
    {
        await using var db = NewDatabase();
        var studentId = Guid.NewGuid();
        var year = new AcademicYear { Name = "2026/2027", StartsOn = new DateOnly(2026, 8, 1), EndsOn = new DateOnly(2027, 7, 31), IsCurrent = true };
        var division = new Division { AcademicYearId = year.Id, StudyYear = 1, Number = 3, Capacity = 30 };
        db.Users.Add(User(studentId, "student@example.com"));
        db.AcademicYears.Add(year);
        db.Divisions.Add(division);
        db.DivisionMemberships.Add(new DivisionMembership { StudentUserId = studentId, AcademicYearId = year.Id, DivisionId = division.Id });
        await db.SaveChangesAsync();

        var result = await new DivisionService(db, new FakeSettings()).GetAssignmentAsync(studentId);

        Assert.NotNull(result);
        Assert.Equal(3, result.DivisionNumber);
        Assert.Equal(1, result.MemberCount);
    }

    private static FacultyDbContext NewDatabase() => new(new DbContextOptionsBuilder<FacultyDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ApplicationUser User(Guid id, string email) => new()
    {
        Id = id, UserName = email, Email = email, FullNameArabic = "مستخدم", FullNameEnglish = "User", IsApproved = true
    };

    private sealed class RecordingRealtimeNotifier : IRealtimeNotifier
    {
        public Guid[] NotifiedUserIds { get; private set; } = [];
        public Task AvailabilityChangedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyUsersAsync(IEnumerable<Guid> userIds, NotificationView notification, CancellationToken cancellationToken = default)
        {
            NotifiedUserIds = userIds.ToArray();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSettings : ISettingsService
    {
        public Task<int> GetIntAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(30);
        public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
        public Task SetAsync(string key, string value, Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
