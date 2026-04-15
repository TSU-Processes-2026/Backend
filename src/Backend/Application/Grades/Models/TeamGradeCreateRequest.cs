namespace Application.Grades.Models
{
    public sealed class TeamGradeCreateRequest
    {
        public Guid submissionId { get; set; }
        public int score { get; set; }
        public string verdictText { get; set; } = string.Empty;
        public bool redistributeTotalScore { get; set; }
        public int? totalScore { get; set; }
    }
}
