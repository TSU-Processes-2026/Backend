namespace Application.Teams.Models;

public sealed class DraftStartRequest
{
    public IReadOnlyList<Guid> CaptainIds { get; init; } = Array.Empty<Guid>();
}
