namespace Application.Subjects.Models;

public sealed class AddParticipantRequest
{
    public Guid? UserId { get; init; }
    public string? Role { get; init; }
}
