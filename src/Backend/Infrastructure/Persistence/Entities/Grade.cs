using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Infrastructure.Persistence.Entities
{
    public class Grade
    {
        public Guid id { get; set; }
        public Guid submissionId { get; set; }
        [JsonIgnore]
        public Submission submission { get; set; }
        public int score { get; set; }
        public string verdictText { get; set; }
        public DateTime verdictedAt { get; set; }

    }
}
