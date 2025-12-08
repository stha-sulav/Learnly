using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class Module
    {
        public int Id { get; set; }
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; }
        
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!; // Required navigation property
        
        public ICollection<Lesson> Lessons { get; set; } = new HashSet<Lesson>();
    }
}
