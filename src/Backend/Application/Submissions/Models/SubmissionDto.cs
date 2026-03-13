using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace Application.Submissions.Models
{
    public class SubmissionDto
    {
        public Guid id { get; set; }
        public Guid assignmentId { get; set; }
        public Guid authorId { get; set; }
        public List<AnswerItemDto>? answers { get; set; }
        public SubmissionStatusEnum status { get; set; }
        public DateTime submittedAt { get; set; }
    }
}
