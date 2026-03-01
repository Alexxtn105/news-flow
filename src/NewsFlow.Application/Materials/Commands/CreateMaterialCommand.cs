using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Materials.DTOs;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Application.Materials.Commands;

public record CreateMaterialCommand(CreateMaterialDto Dto) : IRequest<MaterialDto>;

public class CreateMaterialHandler : IRequestHandler<CreateMaterialCommand, MaterialDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateMaterialHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MaterialDto> Handle(CreateMaterialCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var tags = dto.TagIds != null
            ? await _db.Tags.Where(t => dto.TagIds.Contains(t.Id)).ToListAsync(ct)
            : new List<Tag>();

        var material = new Material
        {
            Title = dto.Title,
            OriginalText = dto.OriginalText,
            OriginalLanguageId = dto.OriginalLanguageId,
            SourceId = dto.SourceId,
            CountryId = dto.CountryId,
            Location = dto.Location,
            EventDate = dto.EventDate,
            Priority = dto.Priority,
            CreatedById = userId,
            Tags = tags
        };

        _db.Materials.Add(material);
        await _db.SaveChangesAsync(ct);

        // Reload with navigation properties
        var loaded = await _db.Materials
            .Include(m => m.OriginalLanguage)
            .Include(m => m.Source)
            .Include(m => m.Country)
            .Include(m => m.CreatedBy)
            .Include(m => m.AssignedTo)
            .Include(m => m.Attachments)
            .Include(m => m.Tags)
            .FirstAsync(m => m.Id == material.Id, ct);

        return ToDto(loaded);
    }

    internal static MaterialDto ToDto(Material m) => new(
        m.Id, m.Title, m.OriginalText, m.TranslatedText,
        m.OriginalLanguage?.Name ?? "", m.Source?.Name ?? "",
        m.Country?.Name, m.Location, m.ReceivedAt, m.EventDate,
        m.Status, m.Priority, m.AssignedTo?.FullName, m.CreatedAt,
        m.CreatedBy?.FullName ?? "",
        m.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName, a.ContentType, a.Size)).ToList(),
        m.Tags.Select(t => t.Name).ToList());
}
