using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class Course
    {
        public int Id { get; set; }
        [Required, MaxLength(255)] 
        public string Title { get; set; } = string.Empty;
        [MaxLength(500)] 
        public string? SubTitle { get; set; }
        public string? Description { get; set; }
        
        [Required] 
        public string InstructorId { get; set; } = string.Empty;
        public ApplicationUser? Instructor { get; set; }
        
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        
        [Column(TypeName = "decimal(10,2)")] 
        public decimal Price { get; set; } = 0;
        [Column(TypeName = "decimal(10,2)")] 
        public decimal? DiscountPrice { get; set; }
        
        [Required, MaxLength(100)]
        public string Slug { get; set; } = string.Empty;
        
        public string? ThumbnailPath { get; set; }
        
        public string Language { get; set; } = "English";
        public CourseLevel Level { get; set; } = CourseLevel.Beginner;
        public CourseStatus Status { get; set; } = CourseStatus.Draft;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }

        public bool Published { get; set; }
        
        [Column(TypeName = "decimal(3,2)")] 
        public decimal AverageRating { get; set; } = 0;
        public int TotalStudents { get; set; } = 0;
        
        public ICollection<Module> Modules { get; set; } = new HashSet<Module>();
        public ICollection<Enrollment> Enrollments { get; set; } = new HashSet<Enrollment>();
    }

    public enum CourseLevel { Beginner, Intermediate, Advanced, AllLevels }
    public enum CourseStatus { Draft, UnderReview, Published, Rejected }
}
