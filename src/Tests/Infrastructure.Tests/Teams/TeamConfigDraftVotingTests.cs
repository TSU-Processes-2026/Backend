using Application.Teams.Contracts;
using Application.Teams.Models;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Infrastructure.Teams.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Teams;

/// <summary>
/// Tests for team configuration and draft regression fixes.
/// </summary>
public sealed class TeamConfigDraftVotingTests
{
    private readonly LmsDbContext _dbContext;
    private readonly ITeamsService _teamsService;
    private readonly Mock<TimeProvider> _timeProvider;
    private readonly DateTimeOffset _now;

    public TeamConfigDraftVotingTests()
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase($"TeamConfigDraftTests_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new LmsDbContext(options);
        _now = DateTimeOffset.UtcNow;
        _timeProvider = new Mock<TimeProvider>();
        _timeProvider.Setup(x => x.GetUtcNow()).Returns(_now);
        _teamsService = new TeamsService(_dbContext, _timeProvider.Object);
    }

    #region Issue 1: Draft state cleared on mode change

    [Fact]
    public async Task UpdateSettings_WhenChangingFromDraftToManual_ShouldDeactivateDraftState()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Draft);
        await AddStudentToSubjectAsync(subjectId, studentId);

        // Start a draft
        var startResult = await _teamsService.StartDraftAsync(
            teacherId,
            subjectId,
            new DraftStartRequest { CaptainIds = new[] { studentId } },
            CancellationToken.None);
        startResult.Status.Should().Be(DraftStartStatus.Success);

        // Verify draft is active
        var draftBefore = await _dbContext.DraftStates.SingleAsync(x => x.SubjectId == subjectId);
        draftBefore.IsActive.Should().BeTrue();

        // Act: Change mode from Draft to Manual
        var updateResult = await _teamsService.UpdateSettingsAsync(
            teacherId,
            subjectId,
            new TeamSettingsRequest { DistributionMode = TeamDistributionMode.Manual },
            CancellationToken.None);

        // Assert
        updateResult.Status.Should().Be(TeamSettingsStatus.Success);
        var draftAfter = await _dbContext.DraftStates.SingleAsync(x => x.SubjectId == subjectId);
        draftAfter.IsActive.Should().BeFalse();
        draftAfter.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task StartDraft_AfterModeChangeAndBack_ShouldSucceed()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Draft);
        await AddStudentToSubjectAsync(subjectId, studentId);

        // Start a draft
        await _teamsService.StartDraftAsync(
            teacherId,
            subjectId,
            new DraftStartRequest { CaptainIds = new[] { studentId } },
            CancellationToken.None);

        // Change mode away from Draft
        await _teamsService.UpdateSettingsAsync(
            teacherId,
            subjectId,
            new TeamSettingsRequest { DistributionMode = TeamDistributionMode.Manual },
            CancellationToken.None);

        // Change mode back to Draft
        await _teamsService.UpdateSettingsAsync(
            teacherId,
            subjectId,
            new TeamSettingsRequest { DistributionMode = TeamDistributionMode.Draft },
            CancellationToken.None);

        // Act: Start a new draft (this would fail before the fix)
        var result = await _teamsService.StartDraftAsync(
            teacherId,
            subjectId,
            new DraftStartRequest { CaptainIds = new[] { studentId } },
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(DraftStartStatus.Success);
    }

    #endregion

    #region Issue 4: Settings response includes all config parameters

    [Fact]
    public async Task GetSettings_ShouldIncludeCaptainSelectionModeAndVotingDeadline()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Manual);

        // Update settings to include captain selection options
        var settings = await _dbContext.SubjectTeamSettings.SingleAsync(x => x.SubjectId == subjectId);
        settings.CaptainSelectionMode = CaptainSelectionMethod.Voting;
        settings.CaptainVotingDeadlineDays = 5;
        settings.RequiresCaptain = true;
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _teamsService.GetSettingsAsync(teacherId, subjectId, CancellationToken.None);

        // Assert
        result.Status.Should().Be(TeamSettingsStatus.Success);
        result.Settings.Should().NotBeNull();
        result.Settings!.CaptainSelectionMode.Should().Be(CaptainSelectionMethod.Voting);
        result.Settings.CaptainVotingDeadlineDays.Should().Be(5);
        result.Settings.RequiresCaptain.Should().BeTrue();
    }

    #endregion

    #region Issue 6: Teams list includes config info for students

    [Fact]
    public async Task GetTeams_ShouldIncludeDistributionModeAndIsFinalized()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Students);
        await AddStudentToSubjectAsync(subjectId, studentId);

        // Create a team
        var team = new Team
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            CreatedAt = _now,
            Subject = await _dbContext.Subjects.FindAsync(subjectId)
        };
        _dbContext.Teams.Add(team);
        await _dbContext.SaveChangesAsync();

        // Act: Get teams as a student
        var result = await _teamsService.GetTeamsAsync(studentId, subjectId, CancellationToken.None);

        // Assert
        result.Status.Should().Be(TeamListStatus.Success);
        result.DistributionMode.Should().Be(TeamDistributionMode.Students);
        result.IsFinalized.Should().BeFalse();
    }

    [Fact]
    public async Task GetTeams_ShouldReflectFinalizedState()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Manual);
        await AddStudentToSubjectAsync(subjectId, studentId);

        // Finalize settings
        var settings = await _dbContext.SubjectTeamSettings.SingleAsync(x => x.SubjectId == subjectId);
        settings.IsFinalized = true;
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _teamsService.GetTeamsAsync(studentId, subjectId, CancellationToken.None);

        // Assert
        result.Status.Should().Be(TeamListStatus.Success);
        result.IsFinalized.Should().BeTrue();
    }

    #endregion

    #region Issue 7: Captain mapping from multiple sources

    [Fact]
    public async Task GetTeams_ShouldShowCaptainFromTeamCaptainUserId()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Manual);
        await AddStudentToSubjectAsync(subjectId, studentId);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            CreatedAt = _now,
            CaptainUserId = studentId, // Captain set via CaptainSelectionService
            SelectionMethod = CaptainSelectionMethod.Manual,
            CaptainSelectedAt = _now,
            Subject = await _dbContext.Subjects.FindAsync(subjectId)
        };
        team.Members = new List<TeamMember>
        {
            new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                UserId = studentId,
                IsCaptain = false, // Not marked as captain in member
                Team = team
            }
        };
        _dbContext.Teams.Add(team);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _teamsService.GetTeamsAsync(teacherId, subjectId, CancellationToken.None);

        // Assert
        result.Status.Should().Be(TeamListStatus.Success);
        result.Teams.Should().HaveCount(1);
        result.Teams[0].CaptainId.Should().Be(studentId);
        result.Teams[0].Members.First().IsCaptain.Should().BeTrue();
    }

    [Fact]
    public async Task GetTeams_ShouldShowCaptainFromMemberIsCaptainFlag()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var captainId = Guid.NewGuid();

        await SetupSubjectWithSettingsAsync(subjectId, teacherId, TeamDistributionMode.Draft);
        await AddStudentToSubjectAsync(subjectId, captainId);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            CreatedAt = _now,
            CaptainUserId = null, // No captain on team level (Draft mode uses member flag)
            Subject = await _dbContext.Subjects.FindAsync(subjectId)
        };
        team.Members = new List<TeamMember>
        {
            new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                UserId = captainId,
                IsCaptain = true, // Captain marked on member (Draft mode)
                Team = team
            }
        };
        _dbContext.Teams.Add(team);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _teamsService.GetTeamsAsync(teacherId, subjectId, CancellationToken.None);

        // Assert
        result.Status.Should().Be(TeamListStatus.Success);
        result.Teams.Should().HaveCount(1);
        result.Teams[0].CaptainId.Should().Be(captainId);
        result.Teams[0].Members.First().IsCaptain.Should().BeTrue();
    }

    #endregion

    #region Helpers

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

    private async Task AddStudentToSubjectAsync(Guid subjectId, Guid studentId)
    {
        var subject = await _dbContext.Subjects.FindAsync(subjectId);
        var participant = new SubjectParticipant
        {
            SubjectId = subjectId,
            UserId = studentId,
            Role = "Student",
            Subject = subject!
        };
        _dbContext.SubjectParticipants.Add(participant);
        await _dbContext.SaveChangesAsync();
    }

    #endregion
}
