using Application.Grades.Contract;
using Application.Grades.Models;
using Application.Submissions.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Grades
{
    public sealed class TeamGradesService : ITeamGradesService
    {
        private const string TeacherRole = "Teacher";
        private const string AdminRole = "Admin";

        private readonly LmsDbContext _dbContext;
        private readonly TimeProvider _timeProvider;

        public TeamGradesService(LmsDbContext dbContext, TimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _timeProvider = timeProvider;
        }

        public async Task<TeamGradeAccessResult> GetTeamGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId)
        {
            var teamGrade = await LoadTeamGradeAsync(teamId, assignmentId);
            if (teamGrade is null)
            {
                return TeamGradeAccessResult.NotFound();
            }

            if (!await CanReadTeamGradeAsync(currentUserId, teamGrade.Team))
            {
                return TeamGradeAccessResult.Forbidden();
            }

            return TeamGradeAccessResult.Success(MapTeamGrade(teamGrade));
        }

        public async Task<TeamGradeAccessResult> CreateTeamGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, TeamGradeCreateRequest request)
        {
            var team = await _dbContext.Teams
                .Include(x => x.Members)
                .FirstOrDefaultAsync(x => x.Id == teamId);

            if (team is null)
            {
                return TeamGradeAccessResult.NotFound();
            }

            if (!await IsTeacherOrAdminAsync(currentUserId, team.SubjectId))
            {
                return TeamGradeAccessResult.Forbidden();
            }

            if (!IsRedistributionRequestValid(request.redistributeTotalScore, request.totalScore))
            {
                return TeamGradeAccessResult.Forbidden();
            }

            var existingGrade = await LoadTeamGradeAsync(teamId, assignmentId);
            if (existingGrade is not null)
            {
                return TeamGradeAccessResult.Forbidden();
            }

            var submission = await _dbContext.Submissions
                .Include(x => x.post)
                .Include(x => x.grade)
                .Include(x => x.DecisionSession)
                .FirstOrDefaultAsync(x => x.id == request.submissionId);

            var settings = await _dbContext.SubjectTeamSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SubjectId == team.SubjectId);

            if (!IsSubmissionValidForTeamGrade(team, assignmentId, submission, settings))
            {
                return TeamGradeAccessResult.NotFound();
            }

            if (submission!.grade is not null)
            {
                return TeamGradeAccessResult.Forbidden();
            }

            var grade = new Grade
            {
                id = Guid.NewGuid(),
                submissionId = submission.id,
                score = request.score,
                verdictText = request.verdictText ?? string.Empty,
                verdictedAt = _timeProvider.GetUtcNow().UtcDateTime
            };

            submission.status = SubmissionStatusEnum.Graded;

            var teamGrade = new TeamGrade
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                AssignmentId = assignmentId,
                SubmissionId = submission.id,
                RedistributeTotalScore = request.redistributeTotalScore,
                TotalScore = request.redistributeTotalScore ? request.totalScore : null,
                Team = team,
                Submission = submission
            };

            _dbContext.Grades.Add(grade);
            _dbContext.TeamGrades.Add(teamGrade);
            await _dbContext.SaveChangesAsync();

            return TeamGradeAccessResult.Success(MapTeamGrade(teamGrade, grade));
        }

        public async Task<TeamGradeAccessResult> UpdateTeamGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, TeamGradeUpdateRequest request)
        {
            var teamGrade = await LoadTeamGradeAsync(teamId, assignmentId);
            if (teamGrade is null)
            {
                return TeamGradeAccessResult.NotFound();
            }

            if (!await IsTeacherOrAdminAsync(currentUserId, teamGrade.Team.SubjectId))
            {
                return TeamGradeAccessResult.Forbidden();
            }

            if (!IsRedistributionRequestValid(request.redistributeTotalScore, request.totalScore))
            {
                return TeamGradeAccessResult.Forbidden();
            }

            var grade = teamGrade.Submission.grade;
            if (grade is null)
            {
                return TeamGradeAccessResult.NotFound();
            }

            grade.score = request.score;
            grade.verdictText = request.verdictText ?? string.Empty;
            grade.verdictedAt = _timeProvider.GetUtcNow().UtcDateTime;
            teamGrade.RedistributeTotalScore = request.redistributeTotalScore;
            teamGrade.TotalScore = request.redistributeTotalScore ? request.totalScore : null;

            await _dbContext.SaveChangesAsync();

            return TeamGradeAccessResult.Success(MapTeamGrade(teamGrade, grade));
        }

        public async Task<TeamGradeAccessResult> DeleteTeamGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId)
        {
            var teamGrade = await LoadTeamGradeAsync(teamId, assignmentId);
            if (teamGrade is null)
            {
                return TeamGradeAccessResult.NotFound();
            }

            if (!await IsTeacherOrAdminAsync(currentUserId, teamGrade.Team.SubjectId))
            {
                return TeamGradeAccessResult.Forbidden();
            }

            var grade = teamGrade.Submission.grade;
            if (grade is not null)
            {
                _dbContext.Grades.Remove(grade);
                teamGrade.Submission.status = SubmissionStatusEnum.RequiresReview;
            }

            _dbContext.TeamGrades.Remove(teamGrade);
            await _dbContext.SaveChangesAsync();

            return TeamGradeAccessResult.Success(new TeamGradeDto
            {
                id = teamGrade.Id,
                teamId = teamGrade.TeamId,
                assignmentId = teamGrade.AssignmentId,
                submissionId = teamGrade.SubmissionId,
                score = grade?.score ?? 0,
                redistributeTotalScore = teamGrade.RedistributeTotalScore,
                totalScore = teamGrade.TotalScore,
                verdictText = grade?.verdictText ?? string.Empty,
                verdictedAt = grade?.verdictedAt ?? _timeProvider.GetUtcNow().UtcDateTime
            });
        }

        private async Task<TeamGrade?> LoadTeamGradeAsync(Guid teamId, Guid assignmentId)
        {
            return await _dbContext.TeamGrades
                .Include(x => x.Team)
                    .ThenInclude(x => x.Members)
                .Include(x => x.Submission)
                    .ThenInclude(x => x.post)
                .Include(x => x.Submission)
                    .ThenInclude(x => x.grade)
                .FirstOrDefaultAsync(x => x.TeamId == teamId && x.AssignmentId == assignmentId);
        }

        private async Task<bool> CanReadTeamGradeAsync(Guid currentUserId, Team team)
        {
            if (team.Members.Any(x => x.UserId == currentUserId))
            {
                return true;
            }

            return await IsTeacherOrAdminAsync(currentUserId, team.SubjectId);
        }

        private async Task<bool> IsTeacherOrAdminAsync(Guid currentUserId, Guid subjectId)
        {
            return await _dbContext.SubjectParticipants.AnyAsync(x =>
                x.SubjectId == subjectId &&
                x.UserId == currentUserId &&
                (x.Role == TeacherRole || x.Role == AdminRole));
        }

        private static bool IsSubmissionValidForTeamGrade(
            Team team,
            Guid assignmentId,
            Submission? submission,
            SubjectTeamSettings? settings)
        {
            if (submission is null)
            {
                return false;
            }

            if (submission.assignmentId != assignmentId
                || submission.post.SubjectId != team.SubjectId
                || submission.status != SubmissionStatusEnum.RequiresReview
                || !team.Members.Any(x => x.UserId == submission.authorId))
            {
                return false;
            }

            if (settings?.RequiresDecision != true)
            {
                return true;
            }

            return submission.DecisionSession is not null
                   && submission.DecisionSession.IsClosed
                   && submission.DecisionSession.Result == DecisionResult.Approved;
        }

        private static TeamGradeDto MapTeamGrade(TeamGrade teamGrade, Grade? grade = null)
        {
            var resolvedGrade = grade ?? teamGrade.Submission.grade;

            return new TeamGradeDto
            {
                id = teamGrade.Id,
                teamId = teamGrade.TeamId,
                assignmentId = teamGrade.AssignmentId,
                submissionId = teamGrade.SubmissionId,
                score = resolvedGrade?.score ?? 0,
                redistributeTotalScore = teamGrade.RedistributeTotalScore,
                totalScore = teamGrade.TotalScore,
                verdictText = resolvedGrade?.verdictText ?? string.Empty,
                verdictedAt = resolvedGrade?.verdictedAt ?? DateTime.MinValue
            };
        }

        private static bool IsRedistributionRequestValid(bool redistributeTotalScore, int? totalScore)
        {
            if (!redistributeTotalScore)
            {
                return true;
            }

            return totalScore.HasValue && totalScore.Value >= 0;
        }
    }
}
