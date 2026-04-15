using Application.Grades.Contract;
using Application.Grades.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Grades
{
    public sealed class TeamMemberGradesService : ITeamMemberGradesService
    {
        private const string TeacherRole = "Teacher";
        private const string AdminRole = "Admin";

        private readonly LmsDbContext _dbContext;
        private readonly TimeProvider _timeProvider;

        public TeamMemberGradesService(LmsDbContext dbContext, TimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _timeProvider = timeProvider;
        }

        public async Task<TeamMemberGradeAccessResult> GetMemberGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, Guid studentId)
        {
            var teamGrade = await LoadTeamGradeAsync(teamId, assignmentId);
            if (teamGrade is null)
            {
                return TeamMemberGradeAccessResult.NotFound();
            }

            if (!teamGrade.Team.Members.Any(x => x.UserId == studentId))
            {
                return TeamMemberGradeAccessResult.NotFound();
            }

            if (!await CanReadMemberGradeAsync(currentUserId, teamGrade.Team.SubjectId, studentId))
            {
                return TeamMemberGradeAccessResult.Forbidden();
            }

            return TeamMemberGradeAccessResult.Success(await MapMemberGradeAsync(teamGrade, studentId));
        }

        public async Task<TeamMemberGradeAccessResult> UpsertMemberGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, Guid studentId, TeamMemberGradeAdjustmentRequest request)
        {
            var teamGrade = await LoadTeamGradeAsync(teamId, assignmentId);
            if (teamGrade is null)
            {
                return TeamMemberGradeAccessResult.NotFound();
            }

            if (!await IsTeacherOrAdminAsync(currentUserId, teamGrade.Team.SubjectId))
            {
                return TeamMemberGradeAccessResult.Forbidden();
            }

            if (!teamGrade.Team.Members.Any(x => x.UserId == studentId))
            {
                return TeamMemberGradeAccessResult.NotFound();
            }

            var adjustment = await _dbContext.TeamMemberGradeAdjustments
                .FirstOrDefaultAsync(x => x.TeamGradeId == teamGrade.Id && x.StudentId == studentId);

            if (adjustment is null)
            {
                adjustment = new TeamMemberGradeAdjustment
                {
                    Id = Guid.NewGuid(),
                    TeamGradeId = teamGrade.Id,
                    StudentId = studentId
                };

                _dbContext.TeamMemberGradeAdjustments.Add(adjustment);
            }

            adjustment.Score = request.score;
            adjustment.AdjustedAt = _timeProvider.GetUtcNow().UtcDateTime;

            await _dbContext.SaveChangesAsync();

            return TeamMemberGradeAccessResult.Success(await MapMemberGradeAsync(teamGrade, studentId, adjustment));
        }

        public async Task<TeamMemberGradeAccessResult> DeleteMemberGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, Guid studentId)
        {
            var teamGrade = await LoadTeamGradeAsync(teamId, assignmentId);
            if (teamGrade is null)
            {
                return TeamMemberGradeAccessResult.NotFound();
            }

            if (!await IsTeacherOrAdminAsync(currentUserId, teamGrade.Team.SubjectId))
            {
                return TeamMemberGradeAccessResult.Forbidden();
            }

            if (!teamGrade.Team.Members.Any(x => x.UserId == studentId))
            {
                return TeamMemberGradeAccessResult.NotFound();
            }

            var adjustment = await _dbContext.TeamMemberGradeAdjustments
                .FirstOrDefaultAsync(x => x.TeamGradeId == teamGrade.Id && x.StudentId == studentId);

            if (adjustment is null)
            {
                return TeamMemberGradeAccessResult.NotFound();
            }

            _dbContext.TeamMemberGradeAdjustments.Remove(adjustment);
            await _dbContext.SaveChangesAsync();

            return TeamMemberGradeAccessResult.Success(await MapMemberGradeAsync(teamGrade, studentId));
        }

        private async Task<TeamGrade?> LoadTeamGradeAsync(Guid teamId, Guid assignmentId)
        {
            return await _dbContext.TeamGrades
                .Include(x => x.Team)
                    .ThenInclude(x => x.Members)
                .Include(x => x.Submission)
                    .ThenInclude(x => x.grade)
                .Include(x => x.MemberGradeAdjustments)
                .FirstOrDefaultAsync(x => x.TeamId == teamId && x.AssignmentId == assignmentId);
        }

        private async Task<bool> CanReadMemberGradeAsync(Guid currentUserId, Guid subjectId, Guid studentId)
        {
            if (currentUserId == studentId)
            {
                return true;
            }

            return await IsTeacherOrAdminAsync(currentUserId, subjectId);
        }

        private async Task<bool> IsTeacherOrAdminAsync(Guid currentUserId, Guid subjectId)
        {
            return await _dbContext.SubjectParticipants.AnyAsync(x =>
                x.SubjectId == subjectId &&
                x.UserId == currentUserId &&
                (x.Role == TeacherRole || x.Role == AdminRole));
        }

        private async Task<TeamMemberGradeDto> MapMemberGradeAsync(
            TeamGrade teamGrade,
            Guid studentId,
            TeamMemberGradeAdjustment? adjustment = null)
        {
            var resolvedAdjustment = adjustment
                                     ?? teamGrade.MemberGradeAdjustments.FirstOrDefault(x => x.StudentId == studentId);

            var username = await _dbContext.Users
                .Where(x => x.Id == studentId)
                .Select(x => x.UserName)
                .FirstOrDefaultAsync()
                ?? string.Empty;

            var baseScore = ResolveBaseScore(teamGrade, studentId);

            return new TeamMemberGradeDto
            {
                id = resolvedAdjustment?.Id,
                teamGradeId = teamGrade.Id,
                teamId = teamGrade.TeamId,
                assignmentId = teamGrade.AssignmentId,
                studentId = studentId,
                username = username,
                baseScore = baseScore,
                score = resolvedAdjustment?.Score ?? baseScore,
                isAdjusted = resolvedAdjustment is not null,
                adjustedAt = resolvedAdjustment?.AdjustedAt
            };
        }

        private static int ResolveBaseScore(TeamGrade teamGrade, Guid studentId)
        {
            var defaultScore = teamGrade.Submission.grade?.score ?? 0;

            if (!teamGrade.RedistributeTotalScore || !teamGrade.TotalScore.HasValue)
            {
                return defaultScore;
            }

            var orderedMembers = teamGrade.Team.Members
                .Select(x => x.UserId)
                .OrderBy(x => x)
                .ToList();

            if (orderedMembers.Count == 0)
            {
                return defaultScore;
            }

            var memberIndex = orderedMembers.FindIndex(x => x == studentId);
            if (memberIndex < 0)
            {
                return defaultScore;
            }

            var totalScore = teamGrade.TotalScore.Value;
            var baseScore = totalScore / orderedMembers.Count;
            var remainder = totalScore % orderedMembers.Count;

            return memberIndex < remainder ? baseScore + 1 : baseScore;
        }
    }
}
