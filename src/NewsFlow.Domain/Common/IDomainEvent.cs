using MediatR;

namespace NewsFlow.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn => DateTime.UtcNow;
}
