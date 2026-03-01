using NewsFlow.Domain.Entities;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Domain.Tests;

public class DocumentTests
{
    private static Document CreateDraft() => new()
    {
        Title = "Test Document",
        Content = "Document content",
        Status = DocumentStatus.Draft,
        CreatedById = Guid.NewGuid()
    };

    [Fact]
    public void TakeForReview_FromDraft_SetsInReview()
    {
        var doc = CreateDraft();
        var userId = Guid.NewGuid();

        doc.TakeForReview(userId);

        Assert.Equal(DocumentStatus.InReview, doc.Status);
        Assert.Equal(userId, doc.AssignedToId);
    }

    [Fact]
    public void TakeForReview_WhenAlreadyAssigned_Throws()
    {
        var doc = CreateDraft();
        doc.TakeForReview(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            doc.TakeForReview(Guid.NewGuid()));
    }

    [Fact]
    public void ApproveReview_SetsReviewed()
    {
        var doc = CreateDraft();
        doc.TakeForReview(Guid.NewGuid());

        doc.ApproveReview();

        Assert.Equal(DocumentStatus.Reviewed, doc.Status);
        Assert.Null(doc.AssignedToId);
    }

    [Fact]
    public void ReturnForRevision_SetsReturnedForRevision()
    {
        var doc = CreateDraft();
        doc.TakeForReview(Guid.NewGuid());

        doc.ReturnForRevision();

        Assert.Equal(DocumentStatus.ReturnedForRevision, doc.Status);
        Assert.Null(doc.AssignedToId);
    }

    [Fact]
    public void TakeForRegistration_FromReviewed_SetsInRegistration()
    {
        var doc = CreateDraft();
        doc.Status = DocumentStatus.Reviewed;
        var userId = Guid.NewGuid();

        doc.TakeForRegistration(userId);

        Assert.Equal(DocumentStatus.InRegistration, doc.Status);
    }

    [Fact]
    public void CompleteRegistration_AssignsNumber()
    {
        var doc = CreateDraft();
        doc.Status = DocumentStatus.Reviewed;
        doc.TakeForRegistration(Guid.NewGuid());

        doc.CompleteRegistration("DOC-2024-001");

        Assert.Equal(DocumentStatus.Registered, doc.Status);
        Assert.Equal("DOC-2024-001", doc.RegistrationNumber);
    }

    [Fact]
    public void TakeForControl_FromRegistered_SetsInControl()
    {
        var doc = CreateDraft();
        doc.Status = DocumentStatus.Registered;

        doc.TakeForControl(Guid.NewGuid());

        Assert.Equal(DocumentStatus.InControl, doc.Status);
    }

    [Fact]
    public void ApproveControl_SetsControlled()
    {
        var doc = CreateDraft();
        doc.Status = DocumentStatus.Registered;
        doc.TakeForControl(Guid.NewGuid());

        doc.ApproveControl();

        Assert.Equal(DocumentStatus.Controlled, doc.Status);
    }

    [Fact]
    public void ReturnForControl_SetsReturnedForControl()
    {
        var doc = CreateDraft();
        doc.Status = DocumentStatus.Registered;
        doc.TakeForControl(Guid.NewGuid());

        doc.ReturnForControl();

        Assert.Equal(DocumentStatus.ReturnedForControl, doc.Status);
    }

    [Fact]
    public void CompleteEvaluation_SetsEvaluated()
    {
        var doc = CreateDraft();
        doc.Status = DocumentStatus.Controlled;
        doc.TakeForEvaluation(Guid.NewGuid());

        doc.CompleteEvaluation(4, "Good work");

        Assert.Equal(DocumentStatus.Evaluated, doc.Status);
        Assert.Equal(4, doc.EvaluationScore);
        Assert.Equal("Good work", doc.EvaluationCommentary);
    }

    [Fact]
    public void CompleteEvaluation_InvalidScore_Throws()
    {
        var doc = CreateDraft();
        doc.Status = DocumentStatus.Controlled;
        doc.TakeForEvaluation(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            doc.CompleteEvaluation(6, null));
    }

    [Fact]
    public void CreateVersionSnapshot_CreatesVersion()
    {
        var doc = CreateDraft();
        doc.Versions = [];

        var version = doc.CreateVersionSnapshot();

        Assert.Equal(doc.Title, version.Title);
        Assert.Equal(doc.Content, version.Content);
        Assert.Equal(1, version.VersionNumber);
    }

    [Fact]
    public void FullDocumentLifecycle_CompletesSuccessfully()
    {
        var doc = CreateDraft();
        var reviewer = Guid.NewGuid();
        var registrar = Guid.NewGuid();
        var controller = Guid.NewGuid();
        var evaluator = Guid.NewGuid();

        // Review
        doc.TakeForReview(reviewer);
        doc.ApproveReview();
        Assert.Equal(DocumentStatus.Reviewed, doc.Status);

        // Registration
        doc.TakeForRegistration(registrar);
        doc.CompleteRegistration("REG-001");
        Assert.Equal(DocumentStatus.Registered, doc.Status);

        // Control
        doc.TakeForControl(controller);
        doc.ApproveControl();
        Assert.Equal(DocumentStatus.Controlled, doc.Status);

        // Evaluation
        doc.TakeForEvaluation(evaluator);
        doc.CompleteEvaluation(5, "Excellent");
        Assert.Equal(DocumentStatus.Evaluated, doc.Status);
    }
}
