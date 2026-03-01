using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Application.Users.Commands;

public record DeleteUserCommand(Guid Id) : IRequest;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteUserCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("User not found");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }
}
