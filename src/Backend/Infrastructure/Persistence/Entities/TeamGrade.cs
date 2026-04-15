namespace Infrastructure.Persistence.Entities
{
    public sealed class TeamGrade
    {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid AssignmentId { get; set; }
        public Guid SubmissionId { get; set; }
        public bool RedistributeTotalScore { get; set; }
        public int? TotalScore { get; set; }

        public Team Team { get; set; } = null!;
        public Submission Submission { get; set; } = null!;
        public ICollection<TeamMemberGradeAdjustment> MemberGradeAdjustments { get; set; } = new List<TeamMemberGradeAdjustment>();
    }
}
