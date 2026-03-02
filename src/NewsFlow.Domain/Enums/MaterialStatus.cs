namespace NewsFlow.Domain.Enums;

public enum MaterialStatus
{
    New = 0,
    InTranslation = 1,
    ReturnedFromTranslation = 2,
    Translated = 3,
    InAnalysis = 4,
    ReturnedFromAnalysis = 5,
    Processed = 6,
    Rejected = 7,
    NotOfInterest = 8,
    Distorted = 9
}
