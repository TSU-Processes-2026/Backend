using Application.Grades.Models;

namespace Application.Grades.Contract
{
    public interface ITeamGradesService
    {
        Task<TeamGradeAccessResult> GetTeamGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId);
        Task<TeamGradeAccessResult> CreateTeamGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, TeamGradeCreateRequest request);
        Task<TeamGradeAccessResult> UpdateTeamGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, TeamGradeUpdateRequest request);
        Task<TeamGradeAccessResult> DeleteTeamGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId);
    }
}
