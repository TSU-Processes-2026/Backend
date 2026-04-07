using Application.Teams.Models;

namespace Application.Teams.Contracts;

public interface ICaptainSelectionService
{
    Task<CaptainVotingInitiateResult> InitiateVotingAsync(
        Guid subjectId,
        Guid teamId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default);

    Task<CaptainVoteResult> CastVoteAsync(
        Guid teamId,
        Guid voterId,
        Guid votedForUserId,
        CancellationToken cancellationToken = default);

    Task<CaptainVotingStatusResult> GetVotingStatusAsync(
        Guid teamId,
        Guid requesterId,
        CancellationToken cancellationToken = default);

    Task<CaptainSelectionResult> SelectRandomCaptainAsync(
        Guid subjectId,
        Guid teamId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default);

    Task<CaptainSelectionResult> AssignCaptainManuallyAsync(
        Guid subjectId,
        Guid teamId,
        Guid captainUserId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default);

    Task<CaptainInfoResult> GetCaptainInfoAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task CloseExpiredVotingSessionsAsync(CancellationToken cancellationToken = default);

    Task<bool> HasActiveVotingSessionsForSubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken = default);
}
