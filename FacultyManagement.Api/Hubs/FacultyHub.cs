using FacultyManagement.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FacultyManagement.Api.Hubs;

[Authorize]
public sealed class FacultyHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{Context.User!.UserId()}");
        await base.OnConnectedAsync();
    }
}
