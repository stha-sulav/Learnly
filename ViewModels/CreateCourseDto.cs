using System.ComponentModel.DataAnnotations;

namespace Learnly.ViewModels
{
    public class CreateCourseDto
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public required string Title { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public required string Slug { get; set; }

        [StringLength(1000)]
        public required string Description { get; set; }

        public int InstructorId { get; set; } // Assuming InstructorId will be passed

        public int CategoryId { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public required string ThumbnailPath { get; set; }

        public bool Published { get; set; } = false;
    }
}
