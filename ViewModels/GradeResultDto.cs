using System;
using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class GradeResultDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal PassingScore { get; set; }
        public bool Passed { get; set; }
        public DateTime GradedAt { get; set; }
        public List<QuestionFeedbackDto> QuestionFeedbacks { get; set; } = new List<QuestionFeedbackDto>();
    }
}
