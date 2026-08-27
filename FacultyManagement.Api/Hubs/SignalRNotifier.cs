using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using Microsoft.AspNetCore.SignalR;

namespace FacultyManagement.Api.Hubs;

public sealed class SignalRNotifier(IHubContext<FacultyHub> hub) : IRealtimeNotifier
{
    public Task AvailabilityChangedAsync(CancellationToken cancellationToken = default) =>
        hub.Clients.All.SendAsync("availabilityChanged", cancellationToken);

    public Task NotifyUsersAsync(IEnumerable<Guid> userIds, NotificationView notification, CancellationToken cancellationToken = default) =>
        hub.Clients.Groups(userIds.Distinct().Select(x => $"user:{x}").ToArray()).SendAsync("notificationReceived", notification, cancellationToken);
}
