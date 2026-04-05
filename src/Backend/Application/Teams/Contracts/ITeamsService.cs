using Application.Teams.Models;

namespace Application.Teams.Contracts;

public interface ITeamsService
{
    Task<TeamSettingsResult> GetSettingsAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
    Task<TeamSettingsResult> UpdateSettingsAsync(Guid currentUserId, Guid subjectId, TeamSettingsRequest request, CancellationToken cancellationToken);
    Task<TeamListResult> GetTeamsAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
    Task<UnassignedStudentsResult> GetUnassignedStudentsAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
    Task<TeamRandomPreviewResult> PreviewRandomDistributionAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
    Task<TeamValidationResult> ValidateManualDistributionAsync(Guid currentUserId, Guid subjectId, ManualTeamDistributionRequest request, CancellationToken cancellationToken);
    Task<TeamCreateResult> CreateManualDistributionAsync(Guid currentUserId, Guid subjectId, ManualTeamDistributionRequest request, CancellationToken cancellationToken);
    Task<TeamMutationResult> CreateTeamAsync(Guid currentUserId, Guid subjectId, ManualTeamRequest request, CancellationToken cancellationToken);
    Task<TeamMutationResult> UpdateTeamAsync(Guid currentUserId, Guid subjectId, Guid teamId, ManualTeamRequest request, CancellationToken cancellationToken);
    Task<TeamFinalizeResult> FinalizeAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);

    Task<DraftStartResult> StartDraftAsync(Guid currentUserId, Guid subjectId, DraftStartRequest request, CancellationToken cancellationToken);
    Task<DraftStateResult> GetDraftStateAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
    Task<DraftPickResult> DraftPickAsync(Guid currentUserId, Guid subjectId, DraftPickRequest request, CancellationToken cancellationToken);
    Task<TeamMutationResult> StudentCreateTeamAsync(Guid currentUserId, Guid subjectId, StudentCreateTeamRequest request, CancellationToken cancellationToken);
    Task<StudentJoinTeamResult> StudentJoinTeamAsync(Guid currentUserId, Guid subjectId, Guid teamId, CancellationToken cancellationToken);
    Task<StudentLeaveTeamResult> StudentLeaveTeamAsync(Guid currentUserId, Guid subjectId, Guid teamId, CancellationToken cancellationToken);
}
