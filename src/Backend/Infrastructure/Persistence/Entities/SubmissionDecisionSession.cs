using Application.Submissions.Models;

namespace Infrastructure.Persistence.Entities;

public sealed class SubmissionDecisionSession
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public SubmissionDecisionMode Mode { get; set; }
    public int RequiredDecisionsCount { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset DeadlineAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public bool IsClosed { get; set; }
    public DecisionResult? Result { get; set; }

    public Submission Submission { get; set; } = null!;
    public ICollection<SubmissionDecision> Decisions { get; set; } = new List<SubmissionDecision>();
}
