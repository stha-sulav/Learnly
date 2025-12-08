using System.ComponentModel.DataAnnotations;

namespace Learnly.Models
{
    public class Question
    {
        public int Id { get; set; }

        public int QuizId { get; set; }
        public Quiz? Quiz { get; set; }

        [Required]
        public required string Text { get; set; }

        public QuestionType Type { get; set; }

        // Storing options as JSON for flexibility
        public string? Options { get; set; } 
    }

    public enum QuestionType
    {
        MultipleChoice,
        MultipleSelect,
        ShortAnswer
    }
}
