using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Materials.Commands;
using NewsFlow.Application.Materials.DTOs;

namespace NewsFlow.Application.Materials.Queries;

public record GetMaterialByIdQuery(Guid Id) : IRequest<MaterialDto>;

public class GetMaterialByIdHandler : IRequestHandler<GetMaterialByIdQuery, MaterialDto>
{
    private readonly IApplicationDbContext _db;
    public GetMaterialByIdHandler(IApplicationDbContext db) => _db = db;

    public async Task<MaterialDto> Handle(GetMaterialByIdQuery r, CancellationToken ct)
    {
        var m = await _db.Materials
            .Include(m => m.OriginalLanguage)
            .Include(m => m.Source)
            .Include(m => m.Country)
            .Include(m => m.CreatedBy)
            .Include(m => m.AssignedTo)
            .Include(m => m.Attachments)
            .Include(m => m.Tags)
            .FirstOrDefaultAsync(m => m.Id == r.Id, ct)
            ?? throw new KeyNotFoundException("Material not found");

        return CreateMaterialHandler.ToDto(m);
    }
}
