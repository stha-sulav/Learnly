using System.ComponentModel.DataAnnotations;

namespace Learnly.ViewModels
{
    public class QuizEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public int ModuleId { get; set; }

        public string? ModuleTitle { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public required string Title { get; set; }
    }
}
