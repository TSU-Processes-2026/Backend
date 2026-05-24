namespace Application.Assignments.Models;

public sealed class UpsertAssignmentRequest
{
    public string? Content { get; init; }
    public string? AssignmentData { get; init; }
    public decimal? MaxPoints { get; init; }
    public bool? SelfAssessmentEnabled { get; init; }
    public DateTimeOffset? SelfAssessmentVisibilityDate { get; init; }
    public DateTimeOffset? DeadLine { get; init; }
    public IReadOnlyList<AssignmentQuestionRequest>? Questions { get; init; }
}
