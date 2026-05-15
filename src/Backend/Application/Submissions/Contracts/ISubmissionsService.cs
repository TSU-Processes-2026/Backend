using Application.Submissions.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Application.Submissions.Contracts
{
    public interface ISubmissionsService
    {
        Task<SubmissionAccessResult> CreateSubmission(
            Guid assignmentId,
            Guid authorId,
            SubmissionCreateRequest request);
        Task<SubmissionAccessResult> CreateSubmissionWithSelfAssessment(
            Guid taskId,
            Guid authorId,
            SubmissionWithSelfAssessmentRequest request,
            CancellationToken cancellationToken);
        Task<List<SubmissionDto>> GetSubmissions(Guid assignmentId, int limit, int offset);
        Task<List<SubmissionDto>> GetUserSubmissions(Guid assignmentId, Guid authorId, int limit, int offset);
        Task<SubmissionAccessResult> GetSubmission(Guid submissionId);
        Task<SubmissionAccessResult> PatchSubmission(Guid submissionId, SubmissionCreateRequest request);
        Task<SubmissionAccessResult> SubmitSubmission(Guid submissionId);
        Task<SubmissionAccessResult> WithdrawSubmission(Guid submissionId);
    }
}
