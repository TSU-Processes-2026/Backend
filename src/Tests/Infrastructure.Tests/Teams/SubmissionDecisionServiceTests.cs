using Application.Submissions.Contracts;
using Application.Submissions.Models;
using Application.Teams.Models;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Infrastructure.Submissions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Teams;

public sealed class SubmissionDecisionServiceTests
{
    private readonly LmsDbContext _dbContext;
    private readonly ISubmissionDecisionService _sut;
    private readonly Mock<TimeProvider> _timeProvider;
    private readonly DateTimeOffset _now;

    public SubmissionDecisionServiceTests()
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseInMemoryDatabase($"SubmissionDecisionTests_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new LmsDbContext(options);
        _now = DateTimeOffset.UtcNow;
        _timeProvider = new Mock<TimeProvider>();
        _timeProvider.Setup(x => x.GetUtcNow()).Returns(_now);
        _sut = new SubmissionDecisionService(
            _dbContext,
            _timeProvider.Object,
            NullLogger<SubmissionDecisionService>.Instance);
    }

    [Fact]
    public async Task InitiateDecision_Voting_ShouldFail_WhenTeamHasCaptain()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var captainId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        await SetupDecisionScenarioAsync(
            subjectId,
            teamId,
            memberId,
            captainId,
            submissionId,
            SubmissionDecisionMode.Voting,
            hasCaptain: true);

        // Act
        var result = await _sut.InitiateDecisionAsync(
            submissionId,
            memberId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(DecisionSessionInitiateStatus.InvalidOperation);
        result.ErrorMessage.Should().Contain("Voting mode is only allowed when the team has no captain");
    }

    [Fact]
    public async Task InitiateDecision_Voting_ShouldSucceed_WhenTeamHasNoCaptain()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        await SetupDecisionScenarioAsync(
            subjectId,
            teamId,
            memberId,
            captainId: null,
            submissionId,
            SubmissionDecisionMode.Voting,
            hasCaptain: false);

        // Act
        var result = await _sut.InitiateDecisionAsync(
            submissionId,
            memberId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(DecisionSessionInitiateStatus.Success);
        result.Session.Should().NotBeNull();
        result.Session!.Mode.Should().Be(SubmissionDecisionMode.Voting);
    }

    [Fact]
    public async Task InitiateDecision_CaptainDecides_ShouldSucceed_WhenTeamHasCaptain()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var captainId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        await SetupDecisionScenarioAsync(
            subjectId,
            teamId,
            memberId,
            captainId,
            submissionId,
            SubmissionDecisionMode.CaptainDecides,
            hasCaptain: true);

        // Act
        var result = await _sut.InitiateDecisionAsync(
            submissionId,
            memberId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(DecisionSessionInitiateStatus.Success);
        result.Session.Should().NotBeNull();
        result.Session!.Mode.Should().Be(SubmissionDecisionMode.CaptainDecides);
    }

    [Fact]
    public async Task InitiateDecision_CaptainDecides_ShouldFail_WhenTeamHasNoCaptain()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        await SetupDecisionScenarioAsync(
            subjectId,
            teamId,
            memberId,
            captainId: null,
            submissionId,
            SubmissionDecisionMode.CaptainDecides,
            hasCaptain: false);

        // Act
        var result = await _sut.InitiateDecisionAsync(
            submissionId,
            memberId,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(DecisionSessionInitiateStatus.NoCaptain);
    }

    private async Task SetupDecisionScenarioAsync(
        Guid subjectId,
        Guid teamId,
        Guid memberId,
        Guid? captainId,
        Guid submissionId,
        SubmissionDecisionMode decisionMode,
        bool hasCaptain)
    {
        var subject = new Subject
        {
            Id = subjectId,
            Title = "Test Subject",
            Description = "Test",
            GradingMode = "five_point",
            Participants = new List<SubjectParticipant>()
        };

        var memberParticipant = new SubjectParticipant
        {
            SubjectId = subjectId,
            UserId = memberId,
            Role = "Student",
            Subject = subject
        };
        subject.Participants.Add(memberParticipant);

        _dbContext.Subjects.Add(subject);
        _dbContext.SubjectParticipants.Add(memberParticipant);

        var settings = new SubjectTeamSettings
        {
            SubjectId = subjectId,
            DistributionMode = TeamDistributionMode.Manual,
            IsFinalized = true,
            RequiresDecision = true,
            DecisionMode = decisionMode,
            DecisionDeadlineDays = 7,
            Subject = subject
        };
        _dbContext.SubjectTeamSettings.Add(settings);

        var team = new Team
        {
            Id = teamId,
            SubjectId = subjectId,
            CreatedAt = _now,
            Subject = subject,
            CaptainUserId = hasCaptain ? captainId : null
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

        if (hasCaptain && captainId.HasValue)
        {
            var captainMember = new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                UserId = captainId.Value,
                IsCaptain = true,
                Team = team
            };
            team.Members.Add(captainMember);

            var captainParticipant = new SubjectParticipant
            {
                SubjectId = subjectId,
                UserId = captainId.Value,
                Role = "Student",
                Subject = subject
            };
            subject.Participants.Add(captainParticipant);
            _dbContext.SubjectParticipants.Add(captainParticipant);
        }

        _dbContext.Teams.Add(team);

        var post = new Post
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            AuthorId = memberId,
            PostType = "Assignment",
            Content = "Test Assignment",
            CreatedAt = _now,
            Subject = subject
        };
        _dbContext.Posts.Add(post);

        var submission = new Submission
        {
            id = submissionId,
            authorId = memberId,
            assignmentId = post.Id,
            status = SubmissionStatusEnum.Draft,
            submittedAt = DateTime.UtcNow,
            post = post,
            answers = new List<AnswerItem>()
        };
        _dbContext.Submissions.Add(submission);

        await _dbContext.SaveChangesAsync();
    }
}
