namespace Application.Posts.Models;

public sealed class AssignmentPostQuestionResponse
{
    public required Guid Id { get; init; }
    public required string QuestionType { get; init; }
    public required string QuestionData { get; init; }
    public required IReadOnlyList<AssignmentPostQuestionOptionResponse> Options { get; init; }
}
