using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Submissions.Models
{
    public class AnswerItemDto
    {
        public Guid id { get; set; }
        public Guid assignmentQuestionId { get; set; }
        public AnswerTypeEnum answerType { get; set; }
        public Guid? selectedOptionId { get; set; }
        public List<Guid>? selectedOptionIds { get; set; }
        public string? text { get; set; }

    }
}
