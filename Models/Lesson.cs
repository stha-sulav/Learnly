using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [ForeignKey("Module")]
        public int ModuleId { get; set; }
        public Module Module { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public ContentType ContentType { get; set; }

        public int DurationSeconds { get; set; } = 0; // For video lessons

        public string? VideoPath { get; set; } // Path to video file if ContentType is Video

        public string? Content { get; set; } // For article/markdown lessons

        public int OrderIndex { get; set; }

        public string? ThumbnailPath { get; set; }

        public ICollection<LessonProgress> LessonProgresses { get; set; } = new HashSet<LessonProgress>();
    }
}