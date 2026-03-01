namespace NewsFlow.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyRoleAsync(string role, string message, string? entityType = null, Guid? entityId = null);
    Task NotifyUserAsync(Guid userId, string message, string? entityType = null, Guid? entityId = null);
    Task NotifyAllAsync(string message, string? entityType = null, Guid? entityId = null);
}
