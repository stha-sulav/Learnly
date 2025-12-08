using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class QuizSubmissionViewModel
    {
        public int AttemptId { get; set; }
        public List<AnswerViewModel> Answers { get; set; } = new List<AnswerViewModel>();
    }

    public class AnswerViewModel
    {
        public int QuestionId { get; set; }
        public string? Answer { get; set; } // For ShortAnswer
        public List<string> SelectedOptions { get; set; } = new List<string>(); // For MultipleChoice/Select
    }
}
