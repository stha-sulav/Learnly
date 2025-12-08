using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Learnly.Models;

namespace Learnly.ViewModels
{
    public class QuestionEditViewModel
    {
        public int Id { get; set; }
        
        [Required]
        public int QuizId { get; set; }

        [Required]
        public required string Text { get; set; }

        public QuestionType Type { get; set; }

        // For MultipleChoice/MultipleSelect, a list of all possible options.
        // For ShortAnswer, this should contain the single correct answer.
        // For MultipleChoice, convention is that the first option is the correct one.
        public List<string> Options { get; set; } = new List<string>();
    }
}
