using Application.Subjects.Models;

namespace Application.Subjects.Contracts;

public interface ISubjectsService
{
    Task<SubjectResponse> CreateAsync(Guid currentUserId, CreateSubjectRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubjectResponse>> GetListAsync(Guid currentUserId, int limit, int offset, CancellationToken cancellationToken);
    Task<SubjectAccessResult> GetByIdAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
    Task<SubjectAccessResult> UpdateAsync(Guid currentUserId, Guid subjectId, UpdateSubjectRequest request, CancellationToken cancellationToken);
    Task<SubjectDeleteResult> DeleteAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
    Task<JoinSubjectResult> JoinAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
    Task<ParticipantMutationResult> AddParticipantAsync(Guid currentUserId, Guid subjectId, AddParticipantRequest request, CancellationToken cancellationToken);
    Task<ParticipantsListResult> GetParticipantsAsync(Guid currentUserId, Guid subjectId, int limit, int offset, CancellationToken cancellationToken);
    Task<ParticipantMutationResult> UpdateParticipantRoleAsync(Guid currentUserId, Guid subjectId, Guid targetUserId, UpdateParticipantRoleRequest request, CancellationToken cancellationToken);
    Task<ParticipantDeleteResult> DeleteParticipantAsync(Guid currentUserId, Guid subjectId, Guid targetUserId, CancellationToken cancellationToken);
}
