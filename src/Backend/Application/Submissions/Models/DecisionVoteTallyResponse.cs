namespace Application.Submissions.Models;

public sealed record DecisionVoteTallyResponse(
    Guid SessionId,
    Guid SubmissionId,
    int TotalTeamMembers,
    int TotalDecisions,
    int ApprovalsCount,
    int RejectionsCount,
    bool MajorityReached,
    bool IsClosed,
    DecisionResult? Result);
