namespace Application.Posts.Models;

public sealed class AssignmentPostResponse : PostResponse
{
    public required Guid SubjectId { get; init; }
    public required string AssignmentData { get; init; }
    public required IReadOnlyList<AssignmentPostQuestionResponse> Questions { get; init; }
}
