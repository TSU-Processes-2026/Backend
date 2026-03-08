namespace Application.Assignments.Models;

public sealed class AssignmentResponse
{
    public required Guid Id { get; init; }
    public required Guid SubjectId { get; init; }
    public required Guid AuthorId { get; init; }
    public required string PostType { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string AssignmentData { get; init; }
    public required IReadOnlyList<AssignmentQuestionResponse> Questions { get; init; }
}
