using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Submissions.Models
{
    public class Submission
    {
        public Guid id { get; set; }
        public Guid assignmentId { get; set; }
        public Guid authorId { get; set; }
        public List<AnswerItem>? answers { get; set; }
        public SubmissionStatusEnum status { get; set; }
        public DateTime submittedAt { get; set; }
    }
}
