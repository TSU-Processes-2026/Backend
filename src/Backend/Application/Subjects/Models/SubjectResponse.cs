namespace Application.Subjects.Models;

public sealed class SubjectResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
}
