using NewsFlow.Domain.Exceptions;

namespace NewsFlow.Domain.Tests;

public class ConcurrencyConflictExceptionTests
{
    [Fact]
    public void DefaultConstructor_HasDefaultMessage()
    {
        var ex = new ConcurrencyConflictException();

        Assert.Contains("изменена другим пользователем", ex.Message);
    }

    [Fact]
    public void Constructor_WithEntityInfo_IncludesEntityNameAndId()
    {
        var entityId = Guid.NewGuid();

        var ex = new ConcurrencyConflictException("Material", entityId);

        Assert.Contains("Material", ex.Message);
        Assert.Contains(entityId.ToString(), ex.Message);
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("original error");

        var ex = new ConcurrencyConflictException("Document", Guid.NewGuid(), inner);

        Assert.Same(inner, ex.InnerException);
    }
}
