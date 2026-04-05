namespace Application.Teams.Models;

public sealed record CaptainVotingSessionResponse(
    Guid SessionId,
    Guid TeamId,
    DateTimeOffset StartedAt,
    DateTimeOffset DeadlineAt,
    bool IsClosed,
    DateTimeOffset? ClosedAt,
    Guid? WinnerId);
