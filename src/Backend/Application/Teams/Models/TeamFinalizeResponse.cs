namespace Application.Teams.Models;

public sealed class TeamFinalizeResponse
{
    public bool IsFinalized { get; init; }
    public DateTimeOffset? FinalizedAt { get; init; }
}
