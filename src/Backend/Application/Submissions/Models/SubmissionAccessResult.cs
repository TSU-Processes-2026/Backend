using System;
using Application.Subjects.Models;

namespace Application.Submissions.Models
{
    public class SubmissionAccessResult
    {
        public SubmissionAccessStatus Status { get; private set; }
        public SubmissionDto? Submission { get; private set; }

        private SubmissionAccessResult(SubmissionAccessStatus status, SubmissionDto? submission = null)
        {
            Status = status;
            Submission = submission;
        }

        public static SubmissionAccessResult Success(SubmissionDto submission)
            => new SubmissionAccessResult(SubmissionAccessStatus.Success, submission);

        public static SubmissionAccessResult NotFound()
            => new SubmissionAccessResult(SubmissionAccessStatus.NotFound);

        public static SubmissionAccessResult Forbidden()
            => new SubmissionAccessResult(SubmissionAccessStatus.Forbidden);
    }
}