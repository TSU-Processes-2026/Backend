using Application.Submissions.Models;

namespace Application.Submissions.Contracts;

public interface ISubmissionDecisionService
{
    Task<DecisionSessionInitiateResult> InitiateDecisionAsync(
        Guid submissionId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default);

    Task<DecisionSessionStatusResult> GetDecisionStatusAsync(
        Guid submissionId,
        Guid requesterId,
        CancellationToken cancellationToken = default);

    Task<DecisionVoteResult> CastDecisionVoteAsync(
        Guid submissionId,
        Guid voterId,
        DecisionType decision,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<DecisionVoteTallyResult> GetVoteTallyAsync(
        Guid submissionId,
        Guid requesterId,
        CancellationToken cancellationToken = default);

    Task<CaptainDecisionResult> CaptainApproveAsync(
        Guid submissionId,
        Guid captainUserId,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<CaptainDecisionResult> CaptainRejectAsync(
        Guid submissionId,
        Guid captainUserId,
        string? comment,
        CancellationToken cancellationToken = default);

    Task CloseExpiredDecisionSessionsAsync(CancellationToken cancellationToken = default);

    Task<bool> HasActiveDecisionSessionsForSubjectAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<int> CloseActiveSessionsForTeamWithoutCaptainAsync(Guid teamId, CancellationToken cancellationToken = default);
}
