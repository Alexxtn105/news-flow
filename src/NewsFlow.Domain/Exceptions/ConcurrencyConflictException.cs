namespace NewsFlow.Domain.Exceptions;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("Запись была изменена другим пользователем. Обновите данные и попробуйте снова.") { }

    public ConcurrencyConflictException(string entityName, Guid entityId)
        : base($"Запись {entityName} ({entityId}) была изменена другим пользователем. Обновите данные и попробуйте снова.") { }
}
