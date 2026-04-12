namespace Infrastructure.Persistence.Entities
{
    public sealed class TeamMemberGradeAdjustment
    {
        public Guid Id { get; set; }
        public Guid TeamGradeId { get; set; }
        public Guid StudentId { get; set; }
        public int Score { get; set; }
        public DateTime AdjustedAt { get; set; }

        public TeamGrade TeamGrade { get; set; } = null!;
    }
}