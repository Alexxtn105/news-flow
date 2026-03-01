using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.References.DTOs;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Application.References.Commands;

// === Source CRUD ===
public record CreateSourceCommand(CreateSourceDto Dto) : IRequest<SourceDto>;
public class CreateSourceHandler : IRequestHandler<CreateSourceCommand, SourceDto>
{
    private readonly IApplicationDbContext _db;
    public CreateSourceHandler(IApplicationDbContext db) => _db = db;
    public async Task<SourceDto> Handle(CreateSourceCommand r, CancellationToken ct)
    {
        var entity = new Source { Name = r.Dto.Name, Description = r.Dto.Description, Type = r.Dto.Type };
        _db.Sources.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new SourceDto(entity.Id, entity.Name, entity.Description, entity.Type);
    }
}

public record UpdateSourceCommand(Guid Id, UpdateSourceDto Dto) : IRequest<SourceDto>;
public class UpdateSourceHandler : IRequestHandler<UpdateSourceCommand, SourceDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateSourceHandler(IApplicationDbContext db) => _db = db;
    public async Task<SourceDto> Handle(UpdateSourceCommand r, CancellationToken ct)
    {
        var e = await _db.Sources.FindAsync([r.Id], ct) ?? throw new KeyNotFoundException("Source not found");
        if (r.Dto.Name != null) e.Name = r.Dto.Name;
        if (r.Dto.Description != null) e.Description = r.Dto.Description;
        if (r.Dto.Type != null) e.Type = r.Dto.Type;
        await _db.SaveChangesAsync(ct);
        return new SourceDto(e.Id, e.Name, e.Description, e.Type);
    }
}

public record DeleteSourceCommand(Guid Id) : IRequest;
public class DeleteSourceHandler : IRequestHandler<DeleteSourceCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteSourceHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(DeleteSourceCommand r, CancellationToken ct)
    {
        var e = await _db.Sources.FindAsync([r.Id], ct) ?? throw new KeyNotFoundException("Source not found");
        _db.Sources.Remove(e);
        await _db.SaveChangesAsync(ct);
    }
}

// === Language CRUD ===
public record CreateLanguageCommand(CreateLanguageDto Dto) : IRequest<LanguageDto>;
public class CreateLanguageHandler : IRequestHandler<CreateLanguageCommand, LanguageDto>
{
    private readonly IApplicationDbContext _db;
    public CreateLanguageHandler(IApplicationDbContext db) => _db = db;
    public async Task<LanguageDto> Handle(CreateLanguageCommand r, CancellationToken ct)
    {
        var entity = new Language { Code = r.Dto.Code, Name = r.Dto.Name };
        _db.Languages.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new LanguageDto(entity.Id, entity.Code, entity.Name);
    }
}

public record DeleteLanguageCommand(Guid Id) : IRequest;
public class DeleteLanguageHandler : IRequestHandler<DeleteLanguageCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteLanguageHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(DeleteLanguageCommand r, CancellationToken ct)
    {
        var e = await _db.Languages.FindAsync([r.Id], ct) ?? throw new KeyNotFoundException("Language not found");
        _db.Languages.Remove(e);
        await _db.SaveChangesAsync(ct);
    }
}

// === Country CRUD ===
public record CreateCountryCommand(CreateCountryDto Dto) : IRequest<CountryDto>;
public class CreateCountryHandler : IRequestHandler<CreateCountryCommand, CountryDto>
{
    private readonly IApplicationDbContext _db;
    public CreateCountryHandler(IApplicationDbContext db) => _db = db;
    public async Task<CountryDto> Handle(CreateCountryCommand r, CancellationToken ct)
    {
        var entity = new Country { Code = r.Dto.Code, Name = r.Dto.Name };
        _db.Countries.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new CountryDto(entity.Id, entity.Code, entity.Name);
    }
}

public record DeleteCountryCommand(Guid Id) : IRequest;
public class DeleteCountryHandler : IRequestHandler<DeleteCountryCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteCountryHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(DeleteCountryCommand r, CancellationToken ct)
    {
        var e = await _db.Countries.FindAsync([r.Id], ct) ?? throw new KeyNotFoundException("Country not found");
        _db.Countries.Remove(e);
        await _db.SaveChangesAsync(ct);
    }
}

// === Tag CRUD ===
public record CreateTagCommand(CreateTagDto Dto) : IRequest<TagDto>;
public class CreateTagHandler : IRequestHandler<CreateTagCommand, TagDto>
{
    private readonly IApplicationDbContext _db;
    public CreateTagHandler(IApplicationDbContext db) => _db = db;
    public async Task<TagDto> Handle(CreateTagCommand r, CancellationToken ct)
    {
        var entity = new Tag { Name = r.Dto.Name, Category = r.Dto.Category };
        _db.Tags.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new TagDto(entity.Id, entity.Name, entity.Category);
    }
}

public record UpdateTagCommand(Guid Id, UpdateTagDto Dto) : IRequest<TagDto>;
public class UpdateTagHandler : IRequestHandler<UpdateTagCommand, TagDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateTagHandler(IApplicationDbContext db) => _db = db;
    public async Task<TagDto> Handle(UpdateTagCommand r, CancellationToken ct)
    {
        var e = await _db.Tags.FindAsync([r.Id], ct) ?? throw new KeyNotFoundException("Tag not found");
        if (r.Dto.Name != null) e.Name = r.Dto.Name;
        if (r.Dto.Category != null) e.Category = r.Dto.Category;
        await _db.SaveChangesAsync(ct);
        return new TagDto(e.Id, e.Name, e.Category);
    }
}

public record DeleteTagCommand(Guid Id) : IRequest;
public class DeleteTagHandler : IRequestHandler<DeleteTagCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteTagHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(DeleteTagCommand r, CancellationToken ct)
    {
        var e = await _db.Tags.FindAsync([r.Id], ct) ?? throw new KeyNotFoundException("Tag not found");
        _db.Tags.Remove(e);
        await _db.SaveChangesAsync(ct);
    }
}
