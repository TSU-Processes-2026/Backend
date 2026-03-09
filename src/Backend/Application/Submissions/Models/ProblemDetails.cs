using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Submissions.Models
{
    public class ProblemDetails
    {
        public string type { get; set; }
        public string title { get; set; }
        public int status { get; set; }
        public string detail { get; set; }
    }
}
