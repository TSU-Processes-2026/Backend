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
}
