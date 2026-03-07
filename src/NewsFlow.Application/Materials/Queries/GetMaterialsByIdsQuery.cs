using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Materials.Commands;
using NewsFlow.Application.Materials.DTOs;

namespace NewsFlow.Application.Materials.Queries;

public record GetMaterialsByIdsQuery(List<Guid> Ids) : IRequest<List<MaterialDto>>;

public class GetMaterialsByIdsHandler : IRequestHandler<GetMaterialsByIdsQuery, List<MaterialDto>>
{
    private readonly IApplicationDbContext _db;
    public GetMaterialsByIdsHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<MaterialDto>> Handle(GetMaterialsByIdsQuery r, CancellationToken ct)
    {
        var materials = await _db.Materials
            .Include(m => m.OriginalLanguage)
            .Include(m => m.Source)
            .Include(m => m.Country)
            .Include(m => m.CreatedBy)
            .Include(m => m.AssignedTo)
            .Include(m => m.Attachments)
            .Include(m => m.Tags)
            .Where(m => r.Ids.Contains(m.Id))
            .ToListAsync(ct);

        return materials.Select(CreateMaterialHandler.ToDto).ToList();
    }
}
