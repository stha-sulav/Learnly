using System.ComponentModel.DataAnnotations;

namespace Learnly.ViewModels
{
    public class QuizEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public required string Title { get; set; }

        [Range(0, 100)]
        public int PassingScore { get; set; } = 70;
        
        public int? AttemptsAllowed { get; set; }
    }
}
