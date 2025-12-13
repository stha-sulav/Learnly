using System.Collections.Generic;
using Learnly.Models; // Assuming ContentType is an enum or similar in Models

namespace Learnly.ViewModels
{
    public class LessonVm
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public ContentType ContentType { get; set; } // e.g., video, article, markdown
        public int DurationSeconds { get; set; }
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class CourseDetailVm
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Slug { get; set; }
        public required string Description { get; set; }
        public required string InstructorName { get; set; }
        public required string ThumbnailPath { get; set; }
        public decimal Price { get; set; } // Placeholder for pricing
        public bool IsEnrolled { get; set; }
        public List<ModuleVm> Modules { get; set; } = new List<ModuleVm>();
    }
}
