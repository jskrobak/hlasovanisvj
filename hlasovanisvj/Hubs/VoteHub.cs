using Microsoft.AspNetCore.SignalR;

namespace hlasovanisvj.Hubs;

public class VoteHub : Hub
{
    public Task JoinResolution(int resolutionId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"res:{resolutionId}");

    public Task LeaveResolution(int resolutionId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"res:{resolutionId}");
}