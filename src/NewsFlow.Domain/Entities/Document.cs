using NewsFlow.Domain.Common;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Domain.Entities;

public class Document : AggregateRoot
{
    public string? RegistrationNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public Guid? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }
    public DateTime? AssignedAt { get; set; }
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public Priority Priority { get; set; } = Priority.Normal;
    public int? EvaluationScore { get; set; }
    public string? EvaluationCommentary { get; set; }

    public ICollection<DocumentMaterial> SourceMaterials { get; set; } = [];
    public ICollection<DocumentComment> Comments { get; set; } = [];
    public ICollection<DocumentVersion> Versions { get; set; } = [];

    public void TakeForReview(Guid userId)
    {
        if (Status != DocumentStatus.Draft && Status != DocumentStatus.ReturnedForRevision)
            throw new InvalidOperationException($"Cannot take for review from status {Status}");
        if (AssignedToId.HasValue) throw new InvalidOperationException("Already assigned");
        Status = DocumentStatus.InReview;
        AssignedToId = userId;
        AssignedAt = DateTime.UtcNow;
    }

    public void ApproveReview()
    {
        if (Status != DocumentStatus.InReview) throw new InvalidOperationException("Not in review");
        Status = DocumentStatus.Reviewed;
        AssignedToId = null;
        AssignedAt = null;
    }

    public void ReturnForRevision()
    {
        if (Status != DocumentStatus.InReview) throw new InvalidOperationException("Not in review");
        Status = DocumentStatus.ReturnedForRevision;
        AssignedToId = null;
        AssignedAt = null;
    }

    public void TakeForRegistration(Guid userId)
    {
        if (Status != DocumentStatus.Reviewed) throw new InvalidOperationException($"Cannot register from {Status}");
        if (AssignedToId.HasValue) throw new InvalidOperationException("Already assigned");
        Status = DocumentStatus.InRegistration;
        AssignedToId = userId;
        AssignedAt = DateTime.UtcNow;
    }

    public void CompleteRegistration(string registrationNumber)
    {
        if (Status != DocumentStatus.InRegistration) throw new InvalidOperationException("Not in registration");
        RegistrationNumber = registrationNumber;
        Status = DocumentStatus.Registered;
        AssignedToId = null;
        AssignedAt = null;
    }

    public void TakeForControl(Guid userId)
    {
        if (Status != DocumentStatus.Registered) throw new InvalidOperationException($"Cannot control from {Status}");
        if (AssignedToId.HasValue) throw new InvalidOperationException("Already assigned");
        Status = DocumentStatus.InControl;
        AssignedToId = userId;
        AssignedAt = DateTime.UtcNow;
    }

    public void ApproveControl()
    {
        if (Status != DocumentStatus.InControl) throw new InvalidOperationException("Not in control");
        Status = DocumentStatus.Controlled;
        AssignedToId = null;
        AssignedAt = null;
    }

    public void ReturnForControl()
    {
        if (Status != DocumentStatus.InControl) throw new InvalidOperationException("Not in control");
        Status = DocumentStatus.ReturnedForControl;
        AssignedToId = null;
        AssignedAt = null;
    }

    public void TakeForEvaluation(Guid userId)
    {
        if (Status != DocumentStatus.Controlled) throw new InvalidOperationException($"Cannot evaluate from {Status}");
        if (AssignedToId.HasValue) throw new InvalidOperationException("Already assigned");
        Status = DocumentStatus.InEvaluation;
        AssignedToId = userId;
        AssignedAt = DateTime.UtcNow;
    }

    public void CompleteEvaluation(int score, string? commentary)
    {
        if (Status != DocumentStatus.InEvaluation) throw new InvalidOperationException("Not in evaluation");
        if (score < 1 || score > 5) throw new InvalidOperationException("Score must be 1-5");
        EvaluationScore = score;
        EvaluationCommentary = commentary;
        Status = DocumentStatus.Evaluated;
        AssignedToId = null;
        AssignedAt = null;
    }

    public void Release()
    {
        AssignedToId = null;
        AssignedAt = null;
    }

    public bool ReleaseStaleAssignment()
    {
        if (AssignedToId is null) return false;

        var previousStatus = Status switch
        {
            DocumentStatus.InReview => DocumentStatus.Draft,
            DocumentStatus.InRegistration => DocumentStatus.Reviewed,
            DocumentStatus.InControl => DocumentStatus.Registered,
            DocumentStatus.InEvaluation => DocumentStatus.Controlled,
            _ => (DocumentStatus?)null
        };

        if (previousStatus is null) return false;

        Status = previousStatus.Value;
        AssignedToId = null;
        AssignedAt = null;
        return true;
    }

    public DocumentVersion CreateVersionSnapshot()
    {
        var version = new DocumentVersion
        {
            DocumentId = Id,
            Title = Title,
            Content = Content,
            Status = Status,
            VersionNumber = (Versions?.Count ?? 0) + 1
        };
        Versions?.Add(version);
        return version;
    }
}
