using System;
using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class GradeResultDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public int ModuleId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal PassingScore { get; set; }
        public bool Passed { get; set; }
        public DateTime GradedAt { get; set; }
        public List<QuestionFeedbackDto> QuestionFeedbacks { get; set; } = new List<QuestionFeedbackDto>();

        // Module unlock information
        public bool UnlocksNextModule { get; set; }
        public int? NextModuleId { get; set; }
        public string? NextModuleTitle { get; set; }
    }
}
