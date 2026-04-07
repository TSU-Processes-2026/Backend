using Application.Submissions.Models;
using Infrastructure.Identity;

namespace Infrastructure.Persistence.Entities;

public sealed class SubmissionDecision
{
    public Guid Id { get; set; }
    public Guid DecisionSessionId { get; set; }
    public Guid DecisionMakerId { get; set; }
    public DecisionType Decision { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
    public string? Comment { get; set; }

    public SubmissionDecisionSession DecisionSession { get; set; } = null!;
    public ApplicationUser DecisionMaker { get; set; } = null!;
}
