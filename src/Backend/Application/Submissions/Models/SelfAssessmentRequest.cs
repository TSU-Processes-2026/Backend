namespace Application.Submissions.Models;

public sealed class SelfAssessmentRequest
{
    public Guid CriterionId { get; init; }
    public decimal Value { get; init; }
    public string? Comment { get; init; }
}
