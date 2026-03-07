namespace Application.Subjects.Models;

public sealed class CreateSubjectRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
}
