using Application.Assignments.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Submissions.Models
{
    public class AnswerItem
    {
        public Guid id { get; set; }
        public AnswerTypeEnum answerType { get; set; }
        public Guid? selectedOptionId { get; set; }
        public List<Guid>? selectedOptionsId { get; set; }
        public string? text { get; set; }
    }
}
