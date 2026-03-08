namespace Application.Assignments.Models;

public sealed class AssignmentQuestionRequest
{
    public Guid? Id { get; init; }
    public string? QuestionType { get; init; }
    public string? QuestionData { get; init; }
    public IReadOnlyList<AssignmentQuestionOptionRequest>? Options { get; init; }
}
