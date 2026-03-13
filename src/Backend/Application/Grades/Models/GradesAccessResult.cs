using System;
using Application.Grades.Models;
using Application.Subjects.Models;

namespace Application.Submissions.Models
{
    public class GradesAccessResult
    {
        public GradesAccessStatus Status { get; private set; }
        public GradeDto? grade { get; private set; }

        private GradesAccessResult(GradesAccessStatus status, GradeDto? grade = null)
        {
            status = status;
            grade = grade;
        }

        public static GradesAccessResult Success(GradeDto submission)
            => new GradesAccessResult(GradesAccessStatus.Success, submission);

        public static GradesAccessResult NotFound()
            => new GradesAccessResult(GradesAccessStatus.NotFound);

        public static GradesAccessResult Forbidden()
            => new GradesAccessResult(GradesAccessStatus.Forbidden);
    }
}