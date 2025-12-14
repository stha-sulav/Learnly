using System.ComponentModel.DataAnnotations;

namespace Learnly.ViewModels
{
    public class ModuleCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Order must be a positive integer.")]
        public int OrderIndex { get; set; }

        public string? ThumbnailPath { get; set; }
    }
}
