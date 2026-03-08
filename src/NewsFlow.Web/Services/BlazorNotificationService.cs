using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Web.Services;

public record NotificationEvent(
    string? TargetRole,
    Guid? TargetUserId,
    string Message,
    string? EntityType,
    Guid? EntityId,
    DateTime Timestamp);

public class BlazorNotificationService : INotificationService
{
    public event Action<NotificationEvent>? OnNotification;

    public Task NotifyRoleAsync(string role, string message, string? entityType = null, Guid? entityId = null)
    {
        OnNotification?.Invoke(new(role, null, message, entityType, entityId, DateTime.UtcNow));
        return Task.CompletedTask;
    }

    public Task NotifyUserAsync(Guid userId, string message, string? entityType = null, Guid? entityId = null)
    {
        OnNotification?.Invoke(new(null, userId, message, entityType, entityId, DateTime.UtcNow));
        return Task.CompletedTask;
    }

    public Task NotifyAllAsync(string message, string? entityType = null, Guid? entityId = null)
    {
        OnNotification?.Invoke(new(null, null, message, entityType, entityId, DateTime.UtcNow));
        return Task.CompletedTask;
    }
}
