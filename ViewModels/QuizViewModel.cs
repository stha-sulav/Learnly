using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class QuizViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int ModuleId { get; set; }
        public string? ModuleTitle { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
    }

    public class QuestionViewModel
    {
        public int Id { get; set; }
        public required string Text { get; set; }
        public required string Type { get; set; } // "MultipleChoice", "MultipleSelect", "ShortAnswer"
        public List<string> Options { get; set; } = new List<string>(); // Only for MultipleChoice/MultipleSelect
    }
}
