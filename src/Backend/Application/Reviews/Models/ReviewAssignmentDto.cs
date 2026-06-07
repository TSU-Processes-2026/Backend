namespace Application.Reviews.Models;

public sealed class ReviewAssignmentDto
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public string TaskTitle { get; init; } = string.Empty;
    public Guid SubmissionId { get; init; }
    public string ReviewTargetType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset AssignedAt { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public DateTimeOffset? OpenedAt { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
    public ReviewDto? LatestReview { get; init; }
}

public sealed class ReviewDto
{
    public Guid Id { get; init; }
    public decimal? OverallScore { get; init; }
    public string? OverallComment { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; init; }
    public bool IsFinal { get; init; }
}
