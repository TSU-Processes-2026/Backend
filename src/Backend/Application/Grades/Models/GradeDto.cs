using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Grades.Models
{
    public class GradeDto
    {
        public Guid id { get; set; }
        public Guid submissionId { get; set; }
        public int score { get; set; }
        public string verdictText { get; set; }
        public DateTime verdictedAt { get; set; }
    }
}
