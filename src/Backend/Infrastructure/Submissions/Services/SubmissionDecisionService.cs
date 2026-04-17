using Application.Submissions.Contracts;
using Application.Submissions.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Submissions.Services;

public sealed class SubmissionDecisionService : ISubmissionDecisionService
{
    private const int DefaultDecisionDeadlineDays = 7;

    private readonly LmsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SubmissionDecisionService> _logger;

    public SubmissionDecisionService(
        LmsDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<SubmissionDecisionService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<DecisionSessionInitiateResult> InitiateDecisionAsync(
        Guid submissionId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.Submissions
            .Include(s => s.post)
            .Include(s => s.DecisionSession)
            .FirstOrDefaultAsync(s => s.id == submissionId, cancellationToken);

        if (submission is null)
        {
            return DecisionSessionInitiateResult.NotFound("Submission not found.");
        }

        if (submission.status == SubmissionStatusEnum.Graded)
        {
            return DecisionSessionInitiateResult.InvalidOperation("Decision cannot be initiated for graded submissions.");
        }

        var subjectId = submission.post.SubjectId;

        var team = await GetTeamForUserAndSubjectAsync(initiatorUserId, subjectId, cancellationToken);
        if (team is null)
        {
            return DecisionSessionInitiateResult.Forbidden();
        }

        if (!IsUserTeamMember(team, initiatorUserId))
        {
            return DecisionSessionInitiateResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId, cancellationToken);

        if (settings is null || !settings.RequiresDecision)
        {
            return DecisionSessionInitiateResult.InvalidOperation("Decision is not required for submissions in this subject.");
        }

        var decisionMode = settings.DecisionMode ?? SubmissionDecisionMode.Voting;

        // Fallback to team voting if captain mode is configured but captain is not assigned yet.
        if (decisionMode == SubmissionDecisionMode.CaptainDecides && team.CaptainUserId is null)
        {
            decisionMode = SubmissionDecisionMode.Voting;
        }

        if (submission.DecisionSession is not null && !submission.DecisionSession.IsClosed)
        {
            return DecisionSessionInitiateResult.AlreadyActive(MapSession(submission.DecisionSession));
        }

        if (await HasApprovedDecisionForTeamAssignmentAsync(team.Id, submission.assignmentId, submissionId, cancellationToken))
        {
            return DecisionSessionInitiateResult.InvalidOperation("Another submission of the team is already selected as the final decision.");
        }

        var totalMembers = team.Members.Count;
        if (totalMembers <= 0)
        {
            return DecisionSessionInitiateResult.InvalidOperation("Team has no members.");
        }

        var requiredDecisionsCount = settings.RequiredDecisionVotes ?? (decisionMode == SubmissionDecisionMode.CaptainDecides ? 1 : totalMembers);
        if (requiredDecisionsCount < 1 || requiredDecisionsCount > totalMembers)
        {
            return DecisionSessionInitiateResult.InvalidOperation("Required decisions count must be between 1 and team member count.");
        }

        if (decisionMode == SubmissionDecisionMode.CaptainDecides && requiredDecisionsCount != 1)
        {
            return DecisionSessionInitiateResult.InvalidOperation("Required decisions count must be 1 for captain decision mode.");
        }

        var deadlineDays = settings.DecisionDeadlineDays ?? DefaultDecisionDeadlineDays;
        var now = _timeProvider.GetUtcNow();

        var session = new SubmissionDecisionSession
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            Mode = decisionMode,
            RequiredDecisionsCount = requiredDecisionsCount,
            StartedAt = now,
            DeadlineAt = now.AddDays(deadlineDays),
            IsClosed = false
        };

        _dbContext.SubmissionDecisionSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Decision session {SessionId} initiated for submission {SubmissionId} by user {UserId} with mode {Mode}",
            session.Id, submissionId, initiatorUserId, decisionMode);

        return DecisionSessionInitiateResult.Success(MapSession(session));
    }

    public async Task<DecisionSessionStatusResult> GetDecisionStatusAsync(
        Guid submissionId,
        Guid requesterId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.Submissions
            .Include(s => s.post)
            .Include(s => s.DecisionSession)
                .ThenInclude(ds => ds!.Decisions)
            .FirstOrDefaultAsync(s => s.id == submissionId, cancellationToken);

        if (submission is null)
        {
            return DecisionSessionStatusResult.NotFound("Submission not found.");
        }

        var subjectId = submission.post.SubjectId;

        var team = await GetTeamForUserAndSubjectAsync(requesterId, subjectId, cancellationToken);
        if (team is null || !IsUserTeamMember(team, requesterId))
        {
            var isTeacherOrAdmin = await IsTeacherOrAdminAsync(requesterId, subjectId, cancellationToken);
            if (!isTeacherOrAdmin)
            {
                return DecisionSessionStatusResult.Forbidden();
            }
        }

        var session = submission.DecisionSession;
        if (session is null)
        {
            return DecisionSessionStatusResult.NoActiveSession();
        }

        var hasCurrentUserDecided = session.Decisions.Any(d => d.DecisionMakerId == requesterId);
        var approvalsCount = session.Decisions.Count(d => d.Decision == DecisionType.Approve);
        var rejectionsCount = session.Decisions.Count(d => d.Decision == DecisionType.Reject);

        var teamMembersCount = team?.Members.Count ?? 0;

        var response = new DecisionSessionStatusResponse(
            SessionId: session.Id,
            SubmissionId: session.SubmissionId,
            Mode: session.Mode,
            RequiredDecisionsCount: session.RequiredDecisionsCount,
            StartedAt: session.StartedAt,
            DeadlineAt: session.DeadlineAt,
            IsClosed: session.IsClosed,
            ClosedAt: session.ClosedAt,
            Result: session.Result,
            TotalTeamMembers: teamMembersCount,
            DecisionsCast: session.Decisions.Count,
            ApprovalsCount: approvalsCount,
            RejectionsCount: rejectionsCount,
            HasCurrentUserDecided: hasCurrentUserDecided);

        return DecisionSessionStatusResult.Success(response);
    }

    public async Task<DecisionVoteResult> CastDecisionVoteAsync(
        Guid submissionId,
        Guid voterId,
        DecisionType decision,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.Submissions
            .Include(s => s.post)
            .Include(s => s.DecisionSession)
                .ThenInclude(ds => ds!.Decisions)
            .FirstOrDefaultAsync(s => s.id == submissionId, cancellationToken);

        if (submission is null)
        {
            return DecisionVoteResult.NotFound("Submission not found.");
        }

        var subjectId = submission.post.SubjectId;

        var team = await GetTeamForUserAndSubjectAsync(voterId, subjectId, cancellationToken);
        if (team is null || !IsUserTeamMember(team, voterId))
        {
            return DecisionVoteResult.Forbidden();
        }

        var session = submission.DecisionSession;
        if (session is null || session.IsClosed)
        {
            return DecisionVoteResult.NotFound("No active decision session for this submission.");
        }

        if (session.Mode != SubmissionDecisionMode.Voting)
        {
            return DecisionVoteResult.WrongDecisionMode();
        }

        var now = _timeProvider.GetUtcNow();
        if (session.DeadlineAt < now)
        {
            return DecisionVoteResult.SessionClosed();
        }

        if (session.Decisions.Any(d => d.DecisionMakerId == voterId))
        {
            return DecisionVoteResult.AlreadyVoted();
        }

        var newDecision = new SubmissionDecision
        {
            Id = Guid.NewGuid(),
            DecisionSessionId = session.Id,
            DecisionMakerId = voterId,
            Decision = decision,
            DecidedAt = now,
            Comment = comment
        };

        _dbContext.SubmissionDecisions.Add(newDecision);

        var approvalsAfterThis = session.Decisions.Count(d => d.Decision == DecisionType.Approve)
                                 + (decision == DecisionType.Approve ? 1 : 0);
        var rejectionsAfterThis = session.Decisions.Count(d => d.Decision == DecisionType.Reject)
                                  + (decision == DecisionType.Reject ? 1 : 0);

        var decisionsAfterThis = session.Decisions.Count + 1;
        var requiredDecisionsReached = decisionsAfterThis >= session.RequiredDecisionsCount;

        if (requiredDecisionsReached)
        {
            var finalResult = ResolveVotingResult(approvalsAfterThis, rejectionsAfterThis);
            session.IsClosed = true;
            session.ClosedAt = now;
            session.Result = finalResult;

            if (finalResult == DecisionResult.Approved)
            {
                submission.status = SubmissionStatusEnum.RequiresReview;
            }

            await CloseSiblingDecisionSessionsAsync(
                team.Id,
                submission.assignmentId,
                submissionId,
                finalResult == DecisionResult.Approved ? DecisionResult.Rejected : DecisionResult.Expired,
                now,
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Decision session {SessionId} completed for submission {SubmissionId}. Result: {Result}",
                session.Id, submissionId, finalResult);

            return DecisionVoteResult.Success(sessionCompleted: true, finalResult: finalResult);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Decision vote cast in session {SessionId} by user {VoterId}. Decision: {Decision}",
            session.Id, voterId, decision);

        return DecisionVoteResult.Success();
    }

    public async Task<DecisionVoteTallyResult> GetVoteTallyAsync(
        Guid submissionId,
        Guid requesterId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.Submissions
            .Include(s => s.post)
            .Include(s => s.DecisionSession)
                .ThenInclude(ds => ds!.Decisions)
            .FirstOrDefaultAsync(s => s.id == submissionId, cancellationToken);

        if (submission is null)
        {
            return DecisionVoteTallyResult.NotFound("Submission not found.");
        }

        var subjectId = submission.post.SubjectId;

        var team = await GetTeamForUserAndSubjectAsync(requesterId, subjectId, cancellationToken);
        if (team is null || !IsUserTeamMember(team, requesterId))
        {
            var isTeacherOrAdmin = await IsTeacherOrAdminAsync(requesterId, subjectId, cancellationToken);
            if (!isTeacherOrAdmin)
            {
                return DecisionVoteTallyResult.Forbidden();
            }
        }

        var session = submission.DecisionSession;
        if (session is null)
        {
            return DecisionVoteTallyResult.NoActiveSession();
        }

        var approvalsCount = session.Decisions.Count(d => d.Decision == DecisionType.Approve);
        var rejectionsCount = session.Decisions.Count(d => d.Decision == DecisionType.Reject);
        var totalMembers = team?.Members.Count ?? 0;
        var requiredDecisionsReached = session.Decisions.Count >= session.RequiredDecisionsCount;

        var response = new DecisionVoteTallyResponse(
            SessionId: session.Id,
            SubmissionId: session.SubmissionId,
            TotalTeamMembers: totalMembers,
            RequiredDecisionsCount: session.RequiredDecisionsCount,
            TotalDecisions: session.Decisions.Count,
            ApprovalsCount: approvalsCount,
            RejectionsCount: rejectionsCount,
            RequiredDecisionsReached: requiredDecisionsReached,
            IsClosed: session.IsClosed,
            Result: session.Result);

        return DecisionVoteTallyResult.Success(response);
    }

    public async Task<CaptainDecisionResult> CaptainApproveAsync(
        Guid submissionId,
        Guid captainUserId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        return await CaptainDecideAsync(submissionId, captainUserId, DecisionType.Approve, comment, cancellationToken);
    }

    public async Task<CaptainDecisionResult> CaptainRejectAsync(
        Guid submissionId,
        Guid captainUserId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        return await CaptainDecideAsync(submissionId, captainUserId, DecisionType.Reject, comment, cancellationToken);
    }

    public async Task CloseExpiredDecisionSessionsAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var expiredSessions = await _dbContext.SubmissionDecisionSessions
            .Where(s => !s.IsClosed && s.DeadlineAt < now)
            .ToListAsync(cancellationToken);

        foreach (var session in expiredSessions)
        {
            if (session.Mode == SubmissionDecisionMode.Voting)
            {
                await _dbContext.Entry(session)
                    .Reference(s => s.Submission)
                    .LoadAsync(cancellationToken);

                await _dbContext.Entry(session)
                    .Collection(s => s.Decisions)
                    .LoadAsync(cancellationToken);

                var approvalsCount = session.Decisions.Count(d => d.Decision == DecisionType.Approve);
                var rejectionsCount = session.Decisions.Count(d => d.Decision == DecisionType.Reject);

                if (approvalsCount + rejectionsCount > 0)
                {
                    var finalResult = ResolveVotingResult(approvalsCount, rejectionsCount);
                    session.IsClosed = true;
                    session.ClosedAt = now;
                    session.Result = finalResult;

                    if (finalResult == DecisionResult.Approved)
                    {
                        session.Submission.status = SubmissionStatusEnum.RequiresReview;
                    }

                    _logger.LogInformation(
                        "Expired decision session {SessionId} closed for submission {SubmissionId}. Result: {Result}",
                        session.Id, session.SubmissionId, finalResult);

                    continue;
                }
            }

            session.IsClosed = true;
            session.ClosedAt = now;
            session.Result = DecisionResult.Expired;

            _logger.LogInformation(
                "Expired decision session {SessionId} closed for submission {SubmissionId}. Result: Expired",
                session.Id, session.SubmissionId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<CaptainDecisionResult> CaptainDecideAsync(
        Guid submissionId,
        Guid captainUserId,
        DecisionType decision,
        string? comment,
        CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .Include(s => s.post)
            .Include(s => s.DecisionSession)
                .ThenInclude(ds => ds!.Decisions)
            .FirstOrDefaultAsync(s => s.id == submissionId, cancellationToken);

        if (submission is null)
        {
            return CaptainDecisionResult.NotFound("Submission not found.");
        }

        var subjectId = submission.post.SubjectId;

        var team = await GetTeamForUserAndSubjectAsync(captainUserId, subjectId, cancellationToken);
        if (team is null)
        {
            return CaptainDecisionResult.Forbidden();
        }

        if (team.CaptainUserId != captainUserId)
        {
            return CaptainDecisionResult.NotCaptain();
        }

        var session = submission.DecisionSession;
        if (session is null || session.IsClosed)
        {
            return CaptainDecisionResult.NotFound("No active decision session for this submission.");
        }

        if (session.Mode != SubmissionDecisionMode.CaptainDecides)
        {
            return CaptainDecisionResult.WrongDecisionMode();
        }

        var now = _timeProvider.GetUtcNow();
        if (session.DeadlineAt < now)
        {
            return CaptainDecisionResult.SessionClosed();
        }

        var newDecision = new SubmissionDecision
        {
            Id = Guid.NewGuid(),
            DecisionSessionId = session.Id,
            DecisionMakerId = captainUserId,
            Decision = decision,
            DecidedAt = now,
            Comment = comment
        };

        _dbContext.SubmissionDecisions.Add(newDecision);

        session.IsClosed = true;
        session.ClosedAt = now;
        session.Result = decision == DecisionType.Approve ? DecisionResult.Approved : DecisionResult.Rejected;

        if (decision == DecisionType.Approve)
        {
            submission.status = SubmissionStatusEnum.RequiresReview;
        }

        await CloseSiblingDecisionSessionsAsync(
            team.Id,
            submission.assignmentId,
            submissionId,
            session.Result == DecisionResult.Approved ? DecisionResult.Rejected : DecisionResult.Expired,
            now,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Captain decision made in session {SessionId} by captain {CaptainUserId}. Decision: {Decision}, Result: {Result}",
            session.Id, captainUserId, decision, session.Result);

        return CaptainDecisionResult.Success(MapSession(session));
    }

    private async Task<Team?> GetTeamForUserAndSubjectAsync(
        Guid userId,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Teams
            .Include(t => t.Members)
            .Where(t => t.SubjectId == subjectId && t.Members.Any(m => m.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsUserTeamMember(Team team, Guid userId)
    {
        return team.Members.Any(m => m.UserId == userId);
    }

    private async Task<bool> IsTeacherOrAdminAsync(
        Guid userId,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(
                x => x.SubjectId == subjectId
                     && x.UserId == userId
                     && (x.Role == "Teacher" || x.Role == "Admin"),
                cancellationToken);
    }

    public async Task<bool> HasActiveDecisionSessionsForSubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        var teamIds = await _dbContext.Teams
            .Where(t => t.SubjectId == subjectId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (teamIds.Count == 0)
        {
            return false;
        }

        return await _dbContext.SubmissionDecisionSessions
            .AnyAsync(
                s => !s.IsClosed
                     && _dbContext.Submissions
                         .Where(sub => sub.id == s.SubmissionId)
                         .Any(sub => _dbContext.Teams
                             .Where(t => teamIds.Contains(t.Id))
                             .Any(t => t.Members.Any(m => m.UserId == sub.post.AuthorId))),
                cancellationToken);
    }

    public async Task<int> CloseActiveSessionsForTeamWithoutCaptainAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            return 0;
        }

        var memberIds = team.Members.Select(m => m.UserId).ToList();
        if (memberIds.Count == 0)
        {
            return 0;
        }

        var activeSessions = await _dbContext.SubmissionDecisionSessions
            .Include(s => s.Submission)
                .ThenInclude(sub => sub.post)
            .Where(s => !s.IsClosed
                        && s.Mode == SubmissionDecisionMode.CaptainDecides
                        && memberIds.Contains(s.Submission.post.AuthorId))
            .ToListAsync(cancellationToken);

        if (activeSessions.Count == 0)
        {
            return 0;
        }

        var now = _timeProvider.GetUtcNow();
        foreach (var session in activeSessions)
        {
            session.IsClosed = true;
            session.ClosedAt = now;
            session.Result = DecisionResult.Expired;

            _logger.LogInformation(
                "Decision session {SessionId} closed due to captain removal from team {TeamId}. Result: Expired",
                session.Id, teamId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return activeSessions.Count;
    }

    private static DecisionSessionResponse MapSession(SubmissionDecisionSession session)
    {
        return new DecisionSessionResponse(
            SessionId: session.Id,
            SubmissionId: session.SubmissionId,
            Mode: session.Mode,
            RequiredDecisionsCount: session.RequiredDecisionsCount,
            StartedAt: session.StartedAt,
            DeadlineAt: session.DeadlineAt,
            IsClosed: session.IsClosed,
            ClosedAt: session.ClosedAt,
            Result: session.Result);
    }

    private async Task<bool> HasApprovedDecisionForTeamAssignmentAsync(
        Guid teamId,
        Guid assignmentId,
        Guid excludedSubmissionId,
        CancellationToken cancellationToken)
    {
        var teamMemberIds = await _dbContext.TeamMembers
            .Where(x => x.TeamId == teamId)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        if (teamMemberIds.Count == 0)
        {
            return false;
        }

        return await _dbContext.SubmissionDecisionSessions
            .Include(x => x.Submission)
            .AnyAsync(
                x => x.SubmissionId != excludedSubmissionId
                    && x.IsClosed
                    && x.Result == DecisionResult.Approved
                    && x.Submission.assignmentId == assignmentId
                    && teamMemberIds.Contains(x.Submission.authorId),
                cancellationToken);
    }

    private async Task CloseSiblingDecisionSessionsAsync(
        Guid teamId,
        Guid assignmentId,
        Guid selectedSubmissionId,
        DecisionResult siblingResult,
        DateTimeOffset closedAt,
        CancellationToken cancellationToken)
    {
        var teamMemberIds = await _dbContext.TeamMembers
            .Where(x => x.TeamId == teamId)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        if (teamMemberIds.Count == 0)
        {
            return;
        }

        var siblingSessions = await _dbContext.SubmissionDecisionSessions
            .Include(x => x.Submission)
            .Where(x =>
                !x.IsClosed &&
                x.SubmissionId != selectedSubmissionId &&
                x.Submission.assignmentId == assignmentId &&
                teamMemberIds.Contains(x.Submission.authorId))
            .ToListAsync(cancellationToken);

        foreach (var siblingSession in siblingSessions)
        {
            siblingSession.IsClosed = true;
            siblingSession.ClosedAt = closedAt;
            siblingSession.Result = siblingResult;
        }
    }

    private static DecisionResult ResolveVotingResult(int approvalsCount, int rejectionsCount)
    {
        if (approvalsCount > rejectionsCount)
        {
            return DecisionResult.Approved;
        }

        if (rejectionsCount > approvalsCount)
        {
            return DecisionResult.Rejected;
        }

        return Random.Shared.Next(2) == 0 ? DecisionResult.Approved : DecisionResult.Rejected;
    }
}
