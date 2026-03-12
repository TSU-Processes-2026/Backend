using Application.Submissions.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

namespace Infrastructure.Persistence.Entities
{
    public class AnswerItem
    {
        public Guid id { get; set; }
        public Guid assignmentQuestionId { get; set; }
        [JsonIgnore]
        public AssignmentQuestion assignmentQuestion { get; set; }
        public AnswerTypeEnum answerType { get; set; }
        public Guid? selectedOptionId { get; set; }
        public List<Guid>? selectedOptionsId { get; set; }
        public string? text { get; set; }
    }
}
