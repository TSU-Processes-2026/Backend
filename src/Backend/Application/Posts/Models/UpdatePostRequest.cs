namespace Application.Posts.Models;

public sealed class UpdatePostRequest
{
    public string? Content { get; init; }
    public bool? ReviewEnabled { get; init; }
    public string? ReviewType { get; init; }
    public string? ReviewMode { get; init; }
    public DateTimeOffset? ReviewDeadlineAt { get; init; }
    public int? ReviewTimeLimitMinutes { get; init; }
    public DateTimeOffset? CriteriaVisibilityAt { get; init; }
    public bool? TeacherCanEditPeerScores { get; init; }
    public string? TeamReviewPolicy { get; init; }
}
