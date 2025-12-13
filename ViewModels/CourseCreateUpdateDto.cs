using System.ComponentModel.DataAnnotations;

namespace Learnly.ViewModels
{
    public class CourseCreateUpdateDto
    {
        public int Id { get; set; } // Added

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty; // Initialized

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Slug { get; set; } = string.Empty; // Initialized

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty; // Initialized

        public string InstructorId { get; set; } = string.Empty; // Initialized

        public int? CategoryId { get; set; } // Made nullable

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public string ThumbnailPath { get; set; } = string.Empty; // Initialized

        public bool IsPublished { get; set; } = false;
    }
}
