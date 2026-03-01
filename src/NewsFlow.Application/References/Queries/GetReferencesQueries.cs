using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.References.DTOs;

namespace NewsFlow.Application.References.Queries;

// Sources
public record GetSourcesQuery : IRequest<IReadOnlyList<SourceDto>>;
public class GetSourcesHandler : IRequestHandler<GetSourcesQuery, IReadOnlyList<SourceDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSourcesHandler(IApplicationDbContext db) => _db = db;
    public async Task<IReadOnlyList<SourceDto>> Handle(GetSourcesQuery r, CancellationToken ct)
        => await _db.Sources.OrderBy(s => s.Name)
            .Select(s => new SourceDto(s.Id, s.Name, s.Description, s.Type))
            .ToListAsync(ct);
}

// Languages
public record GetLanguagesQuery : IRequest<IReadOnlyList<LanguageDto>>;
public class GetLanguagesHandler : IRequestHandler<GetLanguagesQuery, IReadOnlyList<LanguageDto>>
{
    private readonly IApplicationDbContext _db;
    public GetLanguagesHandler(IApplicationDbContext db) => _db = db;
    public async Task<IReadOnlyList<LanguageDto>> Handle(GetLanguagesQuery r, CancellationToken ct)
        => await _db.Languages.OrderBy(l => l.Name)
            .Select(l => new LanguageDto(l.Id, l.Code, l.Name))
            .ToListAsync(ct);
}

// Countries
public record GetCountriesQuery : IRequest<IReadOnlyList<CountryDto>>;
public class GetCountriesHandler : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCountriesHandler(IApplicationDbContext db) => _db = db;
    public async Task<IReadOnlyList<CountryDto>> Handle(GetCountriesQuery r, CancellationToken ct)
        => await _db.Countries.OrderBy(c => c.Name)
            .Select(c => new CountryDto(c.Id, c.Code, c.Name))
            .ToListAsync(ct);
}

// Tags
public record GetTagsQuery : IRequest<IReadOnlyList<TagDto>>;
public class GetTagsHandler : IRequestHandler<GetTagsQuery, IReadOnlyList<TagDto>>
{
    private readonly IApplicationDbContext _db;
    public GetTagsHandler(IApplicationDbContext db) => _db = db;
    public async Task<IReadOnlyList<TagDto>> Handle(GetTagsQuery r, CancellationToken ct)
        => await _db.Tags.OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Category))
            .ToListAsync(ct);
}

// Roles
public record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;
public class GetRolesHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IApplicationDbContext _db;
    public GetRolesHandler(IApplicationDbContext db) => _db = db;
    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery r, CancellationToken ct)
        => await _db.Roles.OrderBy(r2 => r2.Name)
            .Select(r2 => new RoleDto(r2.Id, r2.Name, r2.Description))
            .ToListAsync(ct);
}
