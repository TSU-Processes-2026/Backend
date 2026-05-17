using Application.Submissions.Models;
using System.Text.Json.Serialization;

namespace Infrastructure.Persistence.Entities
{
    public class Submission
    {
        public Guid id { get; set; }

        public Guid assignmentId { get; set; }

        [JsonIgnore]
        public Post post { get; set; }

        public Guid authorId { get; set; }

        public List<AnswerItem>? answers { get; set; }

        public SubmissionStatusEnum status { get; set; }

        public DateTime submittedAt { get; set; }
        [JsonIgnore]
        public Grade grade { get; set; }
        [JsonIgnore]
        public SubmissionDecisionSession? DecisionSession { get; set; }
        [JsonIgnore]
        public TeamGrade? TeamGrade { get; set; }
        public ICollection<CriterionResult> CriterionResults { get; set; } = new List<CriterionResult>();
    }
}