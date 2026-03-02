using NewsFlow.Domain.Common;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Domain.Entities;

public class Material : AggregateRoot
{
    public string Title { get; set; } = string.Empty;
    public string? OriginalText { get; set; }
    public string? TranslatedText { get; set; }
    public Guid OriginalLanguageId { get; set; }
    public Language OriginalLanguage { get; set; } = null!;
    public Guid SourceId { get; set; }
    public Source Source { get; set; } = null!;
    public Guid? CountryId { get; set; }
    public Country? Country { get; set; }
    public string? Location { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EventDate { get; set; }
    public MaterialStatus Status { get; set; } = MaterialStatus.New;
    public Guid? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }
    public DateTime? AssignedAt { get; set; }
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public Priority Priority { get; set; } = Priority.Normal;
    public string? RejectionReason { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];

    // === Translation ===
    public void TakeForTranslation(Guid userId)
    {
        if (Status != MaterialStatus.New && Status != MaterialStatus.ReturnedFromTranslation)
            throw new InvalidOperationException($"Cannot take for translation from status {Status}");
        if (AssignedToId.HasValue)
            throw new InvalidOperationException("Material is already assigned");
        Status = MaterialStatus.InTranslation;
        AssignedToId = userId;
        AssignedAt = DateTime.UtcNow;
    }

    public void SaveTranslationDraft(string translatedText)
    {
        if (Status != MaterialStatus.InTranslation)
            throw new InvalidOperationException("Material is not in translation");
        TranslatedText = translatedText;
    }

    public void CompleteTranslation(string translatedText)
    {
        if (Status != MaterialStatus.InTranslation)
            throw new InvalidOperationException("Material is not in translation");
        if (string.IsNullOrWhiteSpace(translatedText))
            throw new InvalidOperationException("Translated text is required");
        TranslatedText = translatedText;
        Status = MaterialStatus.Translated;
        AssignedToId = null;
        AssignedAt = null;
    }

    public void ReleaseFromTranslation()
    {
        if (Status != MaterialStatus.InTranslation)
            throw new InvalidOperationException("Material is not in translation");
        Status = MaterialStatus.New;
        AssignedToId = null;
        AssignedAt = null;
    }

    // === Analysis ===
    public void TakeForAnalysis(Guid userId)
    {
        if (Status != MaterialStatus.Translated && Status != MaterialStatus.ReturnedFromAnalysis)
            throw new InvalidOperationException($"Cannot take for analysis from status {Status}");
        if (AssignedToId.HasValue)
            throw new InvalidOperationException("Material is already assigned");
        Status = MaterialStatus.InAnalysis;
        AssignedToId = userId;
        AssignedAt = DateTime.UtcNow;
    }

    public void CompleteAnalysis()
    {
        if (Status != MaterialStatus.InAnalysis)
            throw new InvalidOperationException("Material is not in analysis");
        Status = MaterialStatus.Processed;
        AssignedToId = null;
        AssignedAt = null;
    }

    public void ReleaseFromAnalysis()
    {
        if (Status != MaterialStatus.InAnalysis)
            throw new InvalidOperationException("Material is not in analysis");
        Status = MaterialStatus.Translated;
        AssignedToId = null;
        AssignedAt = null;
    }

    // === Return to translation ===
    public void ReturnToTranslation(string? reason)
    {
        if (Status != MaterialStatus.InAnalysis)
            throw new InvalidOperationException($"Cannot return to translation from status {Status}");
        RejectionReason = reason;
        Status = MaterialStatus.ReturnedFromTranslation;
        AssignedToId = null;
        AssignedAt = null;
    }

    // === Terminal statuses from analysis ===
    public void MarkAsNotOfInterest(string? reason)
    {
        if (Status != MaterialStatus.InAnalysis)
            throw new InvalidOperationException($"Cannot mark as not of interest from status {Status}");
        RejectionReason = reason;
        Status = MaterialStatus.NotOfInterest;
        AssignedToId = null;
        AssignedAt = null;
    }

    public void MarkAsDistorted(string? reason)
    {
        if (Status != MaterialStatus.InAnalysis)
            throw new InvalidOperationException($"Cannot mark as distorted from status {Status}");
        RejectionReason = reason;
        Status = MaterialStatus.Distorted;
        AssignedToId = null;
        AssignedAt = null;
    }

    // === Rejection ===
    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Rejection reason is required");
        RejectionReason = reason;
        Status = MaterialStatus.Rejected;
        AssignedToId = null;
        AssignedAt = null;
    }
}
