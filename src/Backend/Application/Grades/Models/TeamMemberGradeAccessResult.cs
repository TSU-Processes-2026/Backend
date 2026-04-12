using Application.Submissions.Models;

namespace Application.Grades.Models
{
    public sealed class TeamMemberGradeAccessResult
    {
        public GradesAccessStatus Status { get; }
        public TeamMemberGradeDto? Grade { get; }

        private TeamMemberGradeAccessResult(GradesAccessStatus status, TeamMemberGradeDto? grade = null)
        {
            Status = status;
            Grade = grade;
        }

        public static TeamMemberGradeAccessResult Success(TeamMemberGradeDto grade)
        {
            return new TeamMemberGradeAccessResult(GradesAccessStatus.Success, grade);
        }

        public static TeamMemberGradeAccessResult NotFound()
        {
            return new TeamMemberGradeAccessResult(GradesAccessStatus.NotFound);
        }

        public static TeamMemberGradeAccessResult Forbidden()
        {
            return new TeamMemberGradeAccessResult(GradesAccessStatus.Forbidden);
        }
    }
}