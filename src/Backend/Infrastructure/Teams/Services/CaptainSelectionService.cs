using Application.Teams.Contracts;
using Application.Teams.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Teams.Services;

public sealed class CaptainSelectionService : ICaptainSelectionService
{
    private const string TeacherRole = "Teacher";
    private const string AdminRole = "Admin";
    private const int DefaultVotingDeadlineDays = 7;

    private readonly LmsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CaptainSelectionService> _logger;

    public CaptainSelectionService(
        LmsDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<CaptainSelectionService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<CaptainVotingInitiateResult> InitiateVotingAsync(
        Guid subjectId,
        Guid teamId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await IsTeacherOrAdminAsync(initiatorUserId, subjectId, cancellationToken))
        {
            return CaptainVotingInitiateResult.Forbidden();
        }

        var team = await _dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId && t.SubjectId == subjectId, cancellationToken);

        if (team is null)
        {
            return CaptainVotingInitiateResult.NotFound("Team not found.");
        }

        var existingSession = await _dbContext.CaptainVotingSessions
            .FirstOrDefaultAsync(s => s.TeamId == teamId && !s.IsClosed, cancellationToken);

        if (existingSession is not null)
        {
            return CaptainVotingInitiateResult.AlreadyActive(MapSession(existingSession));
        }

        var settings = await _dbContext.SubjectTeamSettings
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId, cancellationToken);

        var deadlineDays = settings?.CaptainVotingDeadlineDays ?? DefaultVotingDeadlineDays;
        var now = _timeProvider.GetUtcNow();

        var session = new CaptainVotingSession
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            StartedAt = now,
            DeadlineAt = now.AddDays(deadlineDays),
            IsClosed = false
        };

        _dbContext.CaptainVotingSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Captain voting session {SessionId} initiated for team {TeamId} by user {UserId}",
            session.Id, teamId, initiatorUserId);

        return CaptainVotingInitiateResult.Success(MapSession(session));
    }

    public async Task<CaptainVoteResult> CastVoteAsync(
        Guid teamId,
        Guid voterId,
        Guid votedForUserId,
        CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            return CaptainVoteResult.NotFound("Team not found.");
        }

        if (!IsUserTeamMember(team, voterId))
        {
            return CaptainVoteResult.Forbidden();
        }

        if (!IsUserTeamMember(team, votedForUserId))
        {
            return CaptainVoteResult.InvalidCandidate();
        }

        var session = await _dbContext.CaptainVotingSessions
            .Include(s => s.Votes)
            .FirstOrDefaultAsync(s => s.TeamId == teamId && !s.IsClosed, cancellationToken);

        if (session is null)
        {
            return CaptainVoteResult.NotFound("No active voting session for this team.");
        }

        var now = _timeProvider.GetUtcNow();
        if (session.DeadlineAt < now)
        {
            return CaptainVoteResult.SessionClosed();
        }

        if (session.Votes.Any(v => v.VoterId == voterId))
        {
            return CaptainVoteResult.AlreadyVoted();
        }

        var vote = new CaptainVote
        {
            Id = Guid.NewGuid(),
            VotingSessionId = session.Id,
            VoterId = voterId,
            VotedForUserId = votedForUserId,
            VotedAt = now
        };

        _dbContext.CaptainVotes.Add(vote);
        session.Votes.Add(vote);

        var totalMembers = team.Members.Count;
        var votesAfterThis = session.Votes.Count;

        if (votesAfterThis >= totalMembers)
        {
            var winner = CalculateWinner(session);
            await CloseSessionAndAssignCaptainAsync(team, session, winner, now, cancellationToken);

            _logger.LogInformation(
                "Captain voting session {SessionId} completed for team {TeamId}. Captain selected: {CaptainId}",
                session.Id, teamId, winner);

            return CaptainVoteResult.Success(sessionCompleted: true, selectedCaptainId: winner);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vote cast in session {SessionId} by user {VoterId} for user {VotedForUserId}",
            session.Id, voterId, votedForUserId);

        return CaptainVoteResult.Success();
    }

    public async Task<CaptainVotingStatusResult> GetVotingStatusAsync(
        Guid teamId,
        Guid requesterId,
        CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            return CaptainVotingStatusResult.NotFound("Team not found.");
        }

        if (!IsUserTeamMember(team, requesterId))
        {
            var isTeacherOrAdmin = await IsTeacherOrAdminForTeamAsync(requesterId, teamId, cancellationToken);
            if (!isTeacherOrAdmin)
            {
                return CaptainVotingStatusResult.Forbidden();
            }
        }

        var session = await _dbContext.CaptainVotingSessions
            .Include(s => s.Votes)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(s => s.TeamId == teamId, cancellationToken);

        if (session is null)
        {
            return CaptainVotingStatusResult.NoActiveSession();
        }

        var hasCurrentUserVoted = session.Votes.Any(v => v.VoterId == requesterId);

        var response = new CaptainVotingStatusResponse(
            SessionId: session.Id,
            TeamId: session.TeamId,
            StartedAt: session.StartedAt,
            DeadlineAt: session.DeadlineAt,
            IsClosed: session.IsClosed,
            ClosedAt: session.ClosedAt,
            WinnerId: session.WinnerId,
            TotalMembers: team.Members.Count,
            VotesCast: session.Votes.Count,
            HasCurrentUserVoted: hasCurrentUserVoted);

        return CaptainVotingStatusResult.Success(response);
    }

    public async Task<CaptainSelectionResult> SelectRandomCaptainAsync(
        Guid subjectId,
        Guid teamId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await IsTeacherOrAdminAsync(initiatorUserId, subjectId, cancellationToken))
        {
            return CaptainSelectionResult.Forbidden();
        }

        var team = await _dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId && t.SubjectId == subjectId, cancellationToken);

        if (team is null)
        {
            return CaptainSelectionResult.NotFound("Team not found.");
        }

        if (team.Members.Count == 0)
        {
            return CaptainSelectionResult.InvalidOperation("Cannot select captain for a team with no members.");
        }

        var settings = await _dbContext.SubjectTeamSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId, cancellationToken);

        if (settings is not null
            && settings.RequiresCaptain
            && settings.DistributionMode is TeamDistributionMode.Manual or TeamDistributionMode.Random)
        {
            return CaptainSelectionResult.InvalidOperation("Captain must be selected by voting for Manual and Random distribution modes.");
        }

        var memberIds = team.Members.Select(m => m.UserId).ToList();
        var randomIndex = Random.Shared.Next(memberIds.Count);
        var selectedCaptainId = memberIds[randomIndex];

        var now = _timeProvider.GetUtcNow();
        team.CaptainUserId = selectedCaptainId;
        team.SelectionMethod = CaptainSelectionMethod.Random;
        team.CaptainSelectedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Captain randomly selected for team {TeamId}. Captain: {CaptainId}",
            teamId, selectedCaptainId);

        return CaptainSelectionResult.Success(new CaptainInfoResponse(
            TeamId: teamId,
            CaptainUserId: selectedCaptainId,
            SelectionMethod: CaptainSelectionMethod.Random,
            SelectedAt: now));
    }

    public async Task<CaptainSelectionResult> AssignCaptainManuallyAsync(
        Guid subjectId,
        Guid teamId,
        Guid captainUserId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await IsTeacherOrAdminAsync(initiatorUserId, subjectId, cancellationToken))
        {
            return CaptainSelectionResult.Forbidden();
        }

        var team = await _dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId && t.SubjectId == subjectId, cancellationToken);

        if (team is null)
        {
            return CaptainSelectionResult.NotFound("Team not found.");
        }

        if (!IsUserTeamMember(team, captainUserId))
        {
            return CaptainSelectionResult.InvalidOperation("The specified user is not a member of this team.");
        }

        var now = _timeProvider.GetUtcNow();
        team.CaptainUserId = captainUserId;
        team.SelectionMethod = CaptainSelectionMethod.Manual;
        team.CaptainSelectedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Captain manually assigned for team {TeamId} by user {InitiatorUserId}. Captain: {CaptainId}",
            teamId, initiatorUserId, captainUserId);

        return CaptainSelectionResult.Success(new CaptainInfoResponse(
            TeamId: teamId,
            CaptainUserId: captainUserId,
            SelectionMethod: CaptainSelectionMethod.Manual,
            SelectedAt: now));
    }

    public async Task<CaptainInfoResult> GetCaptainInfoAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            return CaptainInfoResult.NotFound("Team not found.");
        }

        if (team.CaptainUserId is null || team.SelectionMethod is null || team.CaptainSelectedAt is null)
        {
            return CaptainInfoResult.NoCaptain();
        }

        return CaptainInfoResult.Success(new CaptainInfoResponse(
            TeamId: teamId,
            CaptainUserId: team.CaptainUserId.Value,
            SelectionMethod: team.SelectionMethod.Value,
            SelectedAt: team.CaptainSelectedAt.Value));
    }

    public async Task CloseExpiredVotingSessionsAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var expiredSessions = await _dbContext.CaptainVotingSessions
            .Include(s => s.Votes)
            .Include(s => s.Team)
                .ThenInclude(t => t.Members)
            .Where(s => !s.IsClosed && s.DeadlineAt < now)
            .ToListAsync(cancellationToken);

        foreach (var session in expiredSessions)
        {
            var winner = CalculateWinner(session);
            await CloseSessionAndAssignCaptainAsync(session.Team, session, winner, now, cancellationToken);

            _logger.LogInformation(
                "Expired captain voting session {SessionId} closed for team {TeamId}. Captain selected: {CaptainId}",
                session.Id, session.TeamId, winner);
        }
    }

    public async Task<bool> HasActiveVotingSessionsForSubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CaptainVotingSessions
            .AnyAsync(
                s => !s.IsClosed && s.Team.SubjectId == subjectId,
                cancellationToken);
    }

    private static Guid? CalculateWinner(CaptainVotingSession session)
    {
        var votes = session.Votes;
        if (votes.Count == 0)
        {
            return null;
        }

        var voteCounts = votes
            .GroupBy(v => v.VotedForUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var maxVotes = voteCounts.First().Count;
        var tiedCandidates = voteCounts
            .Where(x => x.Count == maxVotes)
            .Select(x => x.UserId)
            .ToList();

        if (tiedCandidates.Count == 1)
        {
            return tiedCandidates.First();
        }

        var randomIndex = Random.Shared.Next(tiedCandidates.Count);
        return tiedCandidates[randomIndex];
    }

    private async Task CloseSessionAndAssignCaptainAsync(
        Team team,
        CaptainVotingSession session,
        Guid? winnerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        session.IsClosed = true;
        session.ClosedAt = now;
        session.WinnerId = winnerId;

        if (winnerId.HasValue)
        {
            team.CaptainUserId = winnerId.Value;
            team.SelectionMethod = CaptainSelectionMethod.Voting;
            team.CaptainSelectedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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
                     && (x.Role == TeacherRole || x.Role == AdminRole),
                cancellationToken);
    }

    private async Task<bool> IsTeacherOrAdminForTeamAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await _dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            return false;
        }

        return await IsTeacherOrAdminAsync(userId, team.SubjectId, cancellationToken);
    }

    private static CaptainVotingSessionResponse MapSession(CaptainVotingSession session)
    {
        return new CaptainVotingSessionResponse(
            SessionId: session.Id,
            TeamId: session.TeamId,
            StartedAt: session.StartedAt,
            DeadlineAt: session.DeadlineAt,
            IsClosed: session.IsClosed,
            ClosedAt: session.ClosedAt,
            WinnerId: session.WinnerId);
    }
}
