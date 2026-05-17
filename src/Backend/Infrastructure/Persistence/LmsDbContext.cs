using Infrastructure.Identity;
using Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class LmsDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public LmsDbContext(DbContextOptions<LmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<SubjectParticipant> SubjectParticipants => Set<SubjectParticipant>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<AssignmentQuestion> AssignmentQuestions => Set<AssignmentQuestion>();
    public DbSet<AssignmentQuestionOption> AssignmentQuestionOptions => Set<AssignmentQuestionOption>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<AnswerItem> AnswerItems => Set<AnswerItem>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<TeamGrade> TeamGrades => Set<TeamGrade>();
    public DbSet<TeamMemberGradeAdjustment> TeamMemberGradeAdjustments => Set<TeamMemberGradeAdjustment>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<SubjectTeamSettings> SubjectTeamSettings => Set<SubjectTeamSettings>();
    public DbSet<CaptainVotingSession> CaptainVotingSessions => Set<CaptainVotingSession>();
    public DbSet<CaptainVote> CaptainVotes => Set<CaptainVote>();
    public DbSet<SubmissionDecisionSession> SubmissionDecisionSessions => Set<SubmissionDecisionSession>();
    public DbSet<SubmissionDecision> SubmissionDecisions => Set<SubmissionDecision>();
    public DbSet<DraftState> DraftStates => Set<DraftState>();
    public DbSet<Criterion> Criteria => Set<Criterion>();
    public DbSet<CriterionResult> CriterionResults => Set<CriterionResult>();
    public DbSet<GradeScale> GradeScales => Set<GradeScale>();
    public DbSet<CourseGrade> CourseGrades => Set<CourseGrade>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(LmsDbContext).Assembly);
    }
}
