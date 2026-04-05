using Infrastructure.Identity;

namespace Infrastructure.Persistence.Entities;

public sealed class CaptainVote
{
    public Guid Id { get; set; }
    public Guid VotingSessionId { get; set; }
    public Guid VoterId { get; set; }
    public Guid VotedForUserId { get; set; }
    public DateTimeOffset VotedAt { get; set; }

    public CaptainVotingSession VotingSession { get; set; } = null!;
    public ApplicationUser Voter { get; set; } = null!;
    public ApplicationUser VotedFor { get; set; } = null!;
}
