using Application.Grades.Models;

namespace Application.Grades.Contract
{
    public interface ITeamMemberGradesService
    {
        Task<TeamMemberGradeAccessResult> GetMemberGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, Guid studentId);
        Task<TeamMemberGradeAccessResult> UpsertMemberGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, Guid studentId, TeamMemberGradeAdjustmentRequest request);
        Task<TeamMemberGradeAccessResult> DeleteMemberGradeAsync(Guid currentUserId, Guid teamId, Guid assignmentId, Guid studentId);
    }
}