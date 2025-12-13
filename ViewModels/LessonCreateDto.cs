using System.ComponentModel.DataAnnotations;
using Learnly.Models;

namespace Learnly.ViewModels
{
    public class LessonCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        public ContentType ContentType { get; set; }

        public string? ContentPath { get; set; }

        public string? VideoId { get; set; }

        public int DurationSeconds { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Order must be a positive integer.")]
        public int OrderIndex { get; set; }
    }
}
