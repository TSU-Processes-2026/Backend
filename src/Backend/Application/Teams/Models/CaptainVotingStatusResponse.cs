namespace Application.Teams.Models;

public sealed record CaptainVotingStatusResponse(
    Guid SessionId,
    Guid TeamId,
    DateTimeOffset StartedAt,
    DateTimeOffset DeadlineAt,
    bool IsClosed,
    DateTimeOffset? ClosedAt,
    Guid? WinnerId,
    int TotalMembers,
    int VotesCast,
    bool HasCurrentUserVoted);
