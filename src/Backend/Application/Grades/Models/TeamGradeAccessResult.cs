using Application.Submissions.Models;

namespace Application.Grades.Models
{
    public sealed class TeamGradeAccessResult
    {
        public GradesAccessStatus Status { get; }
        public TeamGradeDto? Grade { get; }

        private TeamGradeAccessResult(GradesAccessStatus status, TeamGradeDto? grade = null)
        {
            Status = status;
            Grade = grade;
        }

        public static TeamGradeAccessResult Success(TeamGradeDto grade)
        {
            return new TeamGradeAccessResult(GradesAccessStatus.Success, grade);
        }

        public static TeamGradeAccessResult NotFound()
        {
            return new TeamGradeAccessResult(GradesAccessStatus.NotFound);
        }

        public static TeamGradeAccessResult Forbidden()
        {
            return new TeamGradeAccessResult(GradesAccessStatus.Forbidden);
        }
    }
}