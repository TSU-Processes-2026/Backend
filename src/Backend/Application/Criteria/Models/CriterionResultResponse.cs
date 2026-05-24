namespace Application.Criteria.Models;

public sealed class CriterionResultResponse
{
    public required Guid Id { get; init; }
    public required Guid SubmissionId { get; init; }
    public required Guid CriterionId { get; init; }
    public decimal? Value { get; init; }
    public string? Comment { get; init; }
    public required string CreatedBy { get; init; }
    public required string AssessmentType { get; init; }
}
