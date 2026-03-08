namespace Application.Assignments.Models;

public sealed class AssignmentQuestionResponse
{
    public required Guid Id { get; init; }
    public required string QuestionType { get; init; }
    public required string QuestionData { get; init; }
    public required IReadOnlyList<AssignmentQuestionOptionResponse> Options { get; init; }
}
