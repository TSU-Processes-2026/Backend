using Application.Submissions.Models;
using Application.Teams.Models;

namespace Infrastructure.Persistence.Entities;

public sealed class SubjectTeamSettings
{
    public Guid SubjectId { get; set; }
    public TeamDistributionMode DistributionMode { get; set; }
    public int? FixedTeamsCount { get; set; }
    public int? FixedTeamSize { get; set; }
    public int? MinTeamSize { get; set; }
    public int? MaxTeamSize { get; set; }
    public bool IsFinalized { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }
    public CaptainSelectionMethod? CaptainSelectionMode { get; set; }
    public int? CaptainVotingDeadlineDays { get; set; }
    public bool RequiresCaptain { get; set; }
    public SubmissionDecisionMode? DecisionMode { get; set; }
    public int? DecisionDeadlineDays { get; set; }
    public int? RequiredDecisionVotes { get; set; }
    public bool RequiresDecision { get; set; }
    public Subject Subject { get; set; } = null!;
}
