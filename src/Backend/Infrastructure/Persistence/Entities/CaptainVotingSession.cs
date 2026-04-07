using Infrastructure.Identity;

namespace Infrastructure.Persistence.Entities;

public sealed class CaptainVotingSession
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset DeadlineAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public bool IsClosed { get; set; }
    public Guid? WinnerId { get; set; }

    public Team Team { get; set; } = null!;
    public ApplicationUser? Winner { get; set; }
    public ICollection<CaptainVote> Votes { get; set; } = new List<CaptainVote>();
}
