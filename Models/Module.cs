using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class Module
    {
        public int Id { get; set; }

        [ForeignKey("Course")]
        public int CourseId { get; set; }
        public Course Course { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        public string? ThumbnailPath { get; set; }

        public ICollection<Lesson> Lessons { get; set; } = new HashSet<Lesson>();
    }
}