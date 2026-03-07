namespace Application.Subjects.Models;

public sealed class ParticipantResponse
{
    public required Guid UserId { get; init; }
    public required string Role { get; init; }
}
