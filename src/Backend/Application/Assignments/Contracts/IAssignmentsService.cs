using Application.Assignments.Models;

namespace Application.Assignments.Contracts;

public interface IAssignmentsService
{
    Task<AssignmentListResult> GetSubjectAssignmentsAsync(Guid currentUserId, Guid subjectId, int limit, int offset, CancellationToken cancellationToken);
    Task<AssignmentAccessResult> GetByIdAsync(Guid currentUserId, Guid assignmentId, CancellationToken cancellationToken);
    Task<AssignmentUpdateResult> CreateAsync(Guid currentUserId, Guid subjectId, UpsertAssignmentRequest request, CancellationToken cancellationToken);
    Task<AssignmentUpdateResult> UpdateAsync(Guid currentUserId, Guid assignmentId, UpsertAssignmentRequest request, CancellationToken cancellationToken);
    Task<AssignmentDeleteResult> DeleteAsync(Guid currentUserId, Guid assignmentId, CancellationToken cancellationToken);
}
