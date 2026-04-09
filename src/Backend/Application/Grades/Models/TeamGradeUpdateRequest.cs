namespace Application.Grades.Models
{
    public sealed class TeamGradeUpdateRequest
    {
        public int score { get; set; }
        public string verdictText { get; set; } = string.Empty;
    }
}