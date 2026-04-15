namespace Application.Submissions.Models;

public sealed record DecisionSessionStatusResponse(
    Guid SessionId,
    Guid SubmissionId,
    SubmissionDecisionMode Mode,
    int RequiredDecisionsCount,
    DateTimeOffset StartedAt,
    DateTimeOffset DeadlineAt,
    bool IsClosed,
    DateTimeOffset? ClosedAt,
    DecisionResult? Result,
    int TotalTeamMembers,
    int DecisionsCast,
    int ApprovalsCount,
    int RejectionsCount,
    bool HasCurrentUserDecided);
