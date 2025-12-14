using System.ComponentModel.DataAnnotations;

namespace Learnly.ViewModels
{
    public class LessonUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
    }
}
