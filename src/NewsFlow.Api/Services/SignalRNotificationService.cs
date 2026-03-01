using Microsoft.AspNetCore.SignalR;
using NewsFlow.Api.Hubs;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Api.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationService(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyRoleAsync(string role, string message, string? entityType = null, Guid? entityId = null)
    {
        await _hub.Clients.Group(role).SendAsync("ReceiveNotification", new
        {
            message,
            entityType,
            entityId,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyUserAsync(Guid userId, string message, string? entityType = null, Guid? entityId = null)
    {
        await _hub.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
        {
            message,
            entityType,
            entityId,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyAllAsync(string message, string? entityType = null, Guid? entityId = null)
    {
        await _hub.Clients.All.SendAsync("ReceiveNotification", new
        {
            message,
            entityType,
            entityId,
            timestamp = DateTime.UtcNow
        });
    }
}
