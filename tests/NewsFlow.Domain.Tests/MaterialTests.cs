using NewsFlow.Domain.Entities;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Domain.Tests;

public class MaterialTests
{
    private static Material CreateNewMaterial() => new()
    {
        Title = "Test Material",
        Status = MaterialStatus.New,
        OriginalLanguageId = Guid.NewGuid(),
        SourceId = Guid.NewGuid(),
        CreatedById = Guid.NewGuid()
    };

    [Fact]
    public void TakeForTranslation_FromNew_SetsInTranslation()
    {
        var material = CreateNewMaterial();
        var userId = Guid.NewGuid();

        material.TakeForTranslation(userId);

        Assert.Equal(MaterialStatus.InTranslation, material.Status);
        Assert.Equal(userId, material.AssignedToId);
        Assert.NotNull(material.AssignedAt);
    }

    [Fact]
    public void TakeForTranslation_WhenAlreadyAssigned_Throws()
    {
        var material = CreateNewMaterial();
        material.TakeForTranslation(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            material.TakeForTranslation(Guid.NewGuid()));
    }

    [Fact]
    public void TakeForTranslation_FromTranslated_Throws()
    {
        var material = CreateNewMaterial();
        material.Status = MaterialStatus.Translated;

        Assert.Throws<InvalidOperationException>(() =>
            material.TakeForTranslation(Guid.NewGuid()));
    }

    [Fact]
    public void CompleteTranslation_SetsTranslated()
    {
        var material = CreateNewMaterial();
        material.TakeForTranslation(Guid.NewGuid());

        material.CompleteTranslation("Translated text");

        Assert.Equal(MaterialStatus.Translated, material.Status);
        Assert.Equal("Translated text", material.TranslatedText);
        Assert.Null(material.AssignedToId);
    }

    [Fact]
    public void CompleteTranslation_WithEmptyText_Throws()
    {
        var material = CreateNewMaterial();
        material.TakeForTranslation(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            material.CompleteTranslation(""));
    }

    [Fact]
    public void ReleaseFromTranslation_ReturnsToNew()
    {
        var material = CreateNewMaterial();
        material.TakeForTranslation(Guid.NewGuid());

        material.ReleaseFromTranslation();

        Assert.Equal(MaterialStatus.New, material.Status);
        Assert.Null(material.AssignedToId);
    }

    [Fact]
    public void TakeForAnalysis_FromTranslated_SetsInAnalysis()
    {
        var material = CreateNewMaterial();
        material.Status = MaterialStatus.Translated;
        var userId = Guid.NewGuid();

        material.TakeForAnalysis(userId);

        Assert.Equal(MaterialStatus.InAnalysis, material.Status);
        Assert.Equal(userId, material.AssignedToId);
    }

    [Fact]
    public void CompleteAnalysis_SetsProcessed()
    {
        var material = CreateNewMaterial();
        material.Status = MaterialStatus.Translated;
        material.TakeForAnalysis(Guid.NewGuid());

        material.CompleteAnalysis();

        Assert.Equal(MaterialStatus.Processed, material.Status);
        Assert.Null(material.AssignedToId);
    }

    [Fact]
    public void Reject_SetsRejectedWithReason()
    {
        var material = CreateNewMaterial();

        material.Reject("Low quality");

        Assert.Equal(MaterialStatus.Rejected, material.Status);
        Assert.Equal("Low quality", material.RejectionReason);
    }

    [Fact]
    public void Reject_WithEmptyReason_Throws()
    {
        var material = CreateNewMaterial();

        Assert.Throws<InvalidOperationException>(() =>
            material.Reject(""));
    }

    [Fact]
    public void SaveTranslationDraft_SavesText()
    {
        var material = CreateNewMaterial();
        material.TakeForTranslation(Guid.NewGuid());

        material.SaveTranslationDraft("Draft text");

        Assert.Equal("Draft text", material.TranslatedText);
        Assert.Equal(MaterialStatus.InTranslation, material.Status);
    }
}
