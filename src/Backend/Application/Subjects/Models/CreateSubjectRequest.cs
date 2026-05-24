namespace Application.Subjects.Models;

public sealed class CreateSubjectRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? GradingMode { get; init; }
    public bool SelfAssessmentEnabled { get; init; }
    public Guid? FinalGradeScaleId { get; init; }
}
