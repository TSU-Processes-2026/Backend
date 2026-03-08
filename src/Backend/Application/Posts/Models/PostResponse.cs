using System.Text.Json.Serialization;

namespace Application.Posts.Models;

[JsonPolymorphic]
[JsonDerivedType(typeof(AnnouncementPostResponse))]
[JsonDerivedType(typeof(MaterialPostResponse))]
[JsonDerivedType(typeof(AssignmentPostResponse))]
public abstract class PostResponse
{
    public required Guid Id { get; init; }
    public required Guid AuthorId { get; init; }
    public required string PostType { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
