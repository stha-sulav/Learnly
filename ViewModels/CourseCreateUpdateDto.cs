using System.ComponentModel.DataAnnotations;

namespace Learnly.ViewModels
{
    public class CourseCreateUpdateDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public string InstructorId { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        public string? ThumbnailPath { get; set; }

        public bool IsPublished { get; set; } = false;
    }
}
