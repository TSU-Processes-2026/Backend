namespace Application.Grades.Models
{
    public sealed class TeamMemberGradeDto
    {
        public Guid? id { get; set; }
        public Guid teamGradeId { get; set; }
        public Guid teamId { get; set; }
        public Guid assignmentId { get; set; }
        public Guid studentId { get; set; }
        public string username { get; set; } = string.Empty;
        public int baseScore { get; set; }
        public int score { get; set; }
        public bool isAdjusted { get; set; }
        public DateTime? adjustedAt { get; set; }
    }
}