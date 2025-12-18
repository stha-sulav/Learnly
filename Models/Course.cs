using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty; // URL friendly version of the title

        [StringLength(1000)]
        public string? Description { get; set; }

        [ForeignKey("Instructor")]
        public string InstructorId { get; set; }
        public ApplicationUser Instructor { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? ThumbnailPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsPublished { get; set; } = false;

        public ICollection<Module> Modules { get; set; } = new HashSet<Module>();
        public ICollection<Enrollment> Enrollments { get; set; } = new HashSet<Enrollment>();
        public ICollection<Review> Reviews { get; set; } = new HashSet<Review>();
    }
}