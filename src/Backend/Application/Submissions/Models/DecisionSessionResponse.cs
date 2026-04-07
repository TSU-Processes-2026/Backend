namespace Application.Submissions.Models;

public sealed record DecisionSessionResponse(
    Guid SessionId,
    Guid SubmissionId,
    SubmissionDecisionMode Mode,
    DateTimeOffset StartedAt,
    DateTimeOffset DeadlineAt,
    bool IsClosed,
    DateTimeOffset? ClosedAt,
    DecisionResult? Result);
