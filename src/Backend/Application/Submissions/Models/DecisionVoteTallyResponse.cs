namespace Application.Submissions.Models;

public sealed record DecisionVoteTallyResponse(
    Guid SessionId,
    Guid SubmissionId,
    int TotalTeamMembers,
    int RequiredDecisionsCount,
    int TotalDecisions,
    int ApprovalsCount,
    int RejectionsCount,
    bool RequiredDecisionsReached,
    bool IsClosed,
    DecisionResult? Result);
