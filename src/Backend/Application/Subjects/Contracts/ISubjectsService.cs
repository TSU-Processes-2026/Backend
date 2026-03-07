using Application.Subjects.Models;

namespace Application.Subjects.Contracts;

public interface ISubjectsService
{
    Task<SubjectResponse> CreateAsync(Guid currentUserId, CreateSubjectRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubjectResponse>> GetListAsync(Guid currentUserId, int limit, int offset, CancellationToken cancellationToken);
    Task<SubjectAccessResult> GetByIdAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
    Task<SubjectAccessResult> UpdateAsync(Guid currentUserId, Guid subjectId, UpdateSubjectRequest request, CancellationToken cancellationToken);
    Task<SubjectDeleteResult> DeleteAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken);
}
