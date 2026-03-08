namespace Application.Assignments.Models;

public sealed class UpsertAssignmentRequest
{
    public string? Content { get; init; }
    public string? AssignmentData { get; init; }
    public IReadOnlyList<AssignmentQuestionRequest>? Questions { get; init; }
}
