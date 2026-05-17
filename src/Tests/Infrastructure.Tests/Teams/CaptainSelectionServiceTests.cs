using Application.Teams.Contracts;
using Application.Teams.Models;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Infrastructure.Teams.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Teams;

public sealed class CaptainSelectionServiceTests
{
    private readonly LmsDbContext _dbContext;
    private readonly ICaptainSelectionService _sut;
    private readonly Mock<TimeProvider> _timeProvider;
    private readonly DateTimeOffset _now;

    public CaptainSelectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase($"CaptainSelectionTests_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new LmsDbContext(options);
        _now = DateTimeOffset.UtcNow;
        _timeProvider = new Mock<TimeProvider>();
        _timeProvider.Setup(x => x.GetUtcNow()).Returns(_now);
        _sut = new CaptainSelectionService(
            _dbContext,
            _timeProvider.Object,
            NullLogger<CaptainSelectionService>.Instance);
    }

    [Fact]
    public async Task AssignCaptainManually_ShouldWork_WhenModeIsManual()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Manual);
        await SetupTeamWithMemberAsync(subjectId, teamId, studentId);

        // Act
        var result = await _sut.AssignCaptainManuallyAsync(
            subjectId,
            teamId,
            studentId,
            teacherId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CaptainSelectionStatus.Success);
        result.Captain.Should().NotBeNull();
        result.Captain!.CaptainUserId.Should().Be(studentId);
        result.Captain.SelectionMethod.Should().Be(CaptainSelectionMethod.Manual);
    }

    [Fact]
    public async Task AssignCaptainManually_ShouldWork_WhenModeIsRandom()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Random);
        await SetupTeamWithMemberAsync(subjectId, teamId, studentId);

        // Act
        var result = await _sut.AssignCaptainManuallyAsync(
            subjectId,
            teamId,
            studentId,
            teacherId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CaptainSelectionStatus.Success);
        result.Captain.Should().NotBeNull();
        result.Captain!.CaptainUserId.Should().Be(studentId);
    }

    [Fact]
    public async Task AssignCaptainManually_ShouldWork_WhenModeIsStudents()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Students);
        await SetupTeamWithMemberAsync(subjectId, teamId, studentId);

        // Act
        var result = await _sut.AssignCaptainManuallyAsync(
            subjectId,
            teamId,
            studentId,
            teacherId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CaptainSelectionStatus.Success);
        result.Captain.Should().NotBeNull();
    }

    [Fact]
    public async Task SelectRandomCaptain_ShouldWork_WhenModeIsManual()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Manual);
        await SetupTeamWithMemberAsync(subjectId, teamId, studentId);

        // Act
        var result = await _sut.SelectRandomCaptainAsync(
            subjectId,
            teamId,
            teacherId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CaptainSelectionStatus.Success);
        result.Captain.Should().NotBeNull();
        result.Captain!.CaptainUserId.Should().Be(studentId);
        result.Captain.SelectionMethod.Should().Be(CaptainSelectionMethod.Random);
    }

    [Fact]
    public async Task InitiateVoting_ShouldWork_WhenModeIsManual()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Manual);
        await SetupTeamWithMemberAsync(subjectId, teamId, studentId);

        // Act
        var result = await _sut.InitiateVotingAsync(
            subjectId,
            teamId,
            teacherId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CaptainVotingInitiateStatus.Success);
        result.Session.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignCaptainManually_ShouldReturnForbidden_WhenNotTeacher()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var randomUserId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Manual);
        await SetupTeamWithMemberAsync(subjectId, teamId, studentId);

        // Act
        var result = await _sut.AssignCaptainManuallyAsync(
            subjectId,
            teamId,
            studentId,
            randomUserId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CaptainSelectionStatus.Forbidden);
    }

    private async Task SetupSubjectWithSettingsAsync(
        Guid subjectId,
        Guid teacherId,
        TeamDistributionMode mode)
    {
        var subject = new Subject
        {
            Id = subjectId,
            Title = "Test Subject",
            Description = "Test",
            GradingMode = "five_point",
            Participants = new List<SubjectParticipant>()
        };

        var participant = new SubjectParticipant
        {
            SubjectId = subjectId,
            UserId = teacherId,
            Role = "Teacher",
            Subject = subject
        };

        subject.Participants.Add(participant);

        var settings = new SubjectTeamSettings
        {
            SubjectId = subjectId,
            DistributionMode = mode,
            IsFinalized = false,
            Subject = subject
        };

        _dbContext.Subjects.Add(subject);
        _dbContext.SubjectParticipants.Add(participant);
        _dbContext.SubjectTeamSettings.Add(settings);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SetupTeamWithMemberAsync(
        Guid subjectId,
        Guid teamId,
        Guid memberId)
    {
        var subject = await _dbContext.Subjects.FindAsync(subjectId);

        var team = new Team
        {
            Id = teamId,
            SubjectId = subjectId,
            CreatedAt = _now,
            Subject = subject!
        };

        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = memberId,
            IsCaptain = false,
            Team = team
        };

        team.Members.Add(member);

        _dbContext.Teams.Add(team);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SetupTeamWithMultipleMembersAsync(
        Guid subjectId,
        Guid teamId,
        params Guid[] memberIds)
    {
        var subject = await _dbContext.Subjects.FindAsync(subjectId);

        var team = new Team
        {
            Id = teamId,
            SubjectId = subjectId,
            CreatedAt = _now,
            Subject = subject!,
            Members = new List<TeamMember>()
        };

        foreach (var memberId in memberIds)
        {
            var member = new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                UserId = memberId,
                IsCaptain = false,
                Team = team
            };
            team.Members.Add(member);
        }

        _dbContext.Teams.Add(team);
        await _dbContext.SaveChangesAsync();

        // Clear the change tracker to ensure fresh loads
        _dbContext.ChangeTracker.Clear();
    }

    [Fact]
    public async Task CastVote_ShouldIncludeNewVoteInWinnerCalculation()
    {
        // Arrange: Single member team for simplicity
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var member1 = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Manual);
        await SetupTeamWithMemberAsync(subjectId, teamId, member1);

        // Initiate voting
        var initiateResult = await _sut.InitiateVotingAsync(subjectId, teamId, teacherId, CancellationToken.None);
        initiateResult.Status.Should().Be(CaptainVotingInitiateStatus.Success);

        // Act: The single member votes for themselves
        var voteResult = await _sut.CastVoteAsync(teamId, member1, member1, CancellationToken.None);

        // Assert: Session should complete and the vote should be counted
        voteResult.Status.Should().Be(CaptainVoteStatus.Success);
        voteResult.SessionCompleted.Should().BeTrue();
        voteResult.SelectedCaptainId.Should().Be(member1);

        // Verify the winner was determined from the vote (not random/empty)
        var session = await _dbContext.CaptainVotingSessions
            .Include(s => s.Votes)
            .FirstAsync(s => s.TeamId == teamId);
        session.WinnerId.Should().Be(member1);

        // Verify vote was persisted
        var voteCount = await _dbContext.CaptainVotes.CountAsync(v => v.VotingSessionId == session.Id);
        voteCount.Should().Be(1);
    }

    [Fact]
    public async Task CastVote_AfterSessionCompletes_ShouldReturnNotFound()
    {
        // Arrange: Single member team for simplicity
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var member1 = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Manual);
        await SetupTeamWithMemberAsync(subjectId, teamId, member1);

        // Initiate voting
        await _sut.InitiateVotingAsync(subjectId, teamId, teacherId, CancellationToken.None);

        // Cast the only vote - session completes
        var vote1Result = await _sut.CastVoteAsync(teamId, member1, member1, CancellationToken.None);
        vote1Result.Status.Should().Be(CaptainVoteStatus.Success);
        vote1Result.SessionCompleted.Should().BeTrue();

        // Try to vote again after session is closed
        var vote2Result = await _sut.CastVoteAsync(teamId, member1, member1, CancellationToken.None);
        // Session is now closed, so should return NotFound (no active session)
        vote2Result.Status.Should().Be(CaptainVoteStatus.NotFound);
    }
}
