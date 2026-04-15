namespace Application.Grades.Models
{
    public sealed class TeamGradeDto
    {
        public Guid id { get; set; }
        public Guid teamId { get; set; }
        public Guid assignmentId { get; set; }
        public Guid submissionId { get; set; }
        public int score { get; set; }
        public bool redistributeTotalScore { get; set; }
        public int? totalScore { get; set; }
        public string verdictText { get; set; } = string.Empty;
        public DateTime verdictedAt { get; set; }
    }
}
