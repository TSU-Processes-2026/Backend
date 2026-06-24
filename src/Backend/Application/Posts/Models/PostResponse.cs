using System.Text.Json.Serialization;

namespace Application.Posts.Models;

[JsonPolymorphic]
[JsonDerivedType(typeof(AnnouncementPostResponse), "Announcement")]
[JsonDerivedType(typeof(MaterialPostResponse), "Material")]
[JsonDerivedType(typeof(AssignmentPostResponse), "Assignment")]
public abstract class PostResponse
{
    public required Guid Id { get; init; }
    public required Guid AuthorId { get; init; }
    public required string PostType { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool ReviewEnabled { get; init; }
    public string? ReviewType { get; init; }
    public string? ReviewMode { get; init; }
    public DateTimeOffset? ReviewDeadlineAt { get; init; }
    public int? ReviewTimeLimitMinutes { get; init; }
    public DateTimeOffset? CriteriaVisibilityAt { get; init; }
    public required bool TeacherCanEditPeerScores { get; init; }
    public string? TeamReviewPolicy { get; init; }
}
