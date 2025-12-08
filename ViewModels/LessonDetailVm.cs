using Learnly.Models; // For ContentType enum
using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class LessonDetailVm
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Slug { get; set; }
        public int CourseId { get; set; }
        public required string CourseTitle { get; set; }
        public required string CourseSlug { get; set; }
        public int ModuleId { get; set; }
        public required string ModuleTitle { get; set; }
        public ContentType ContentType { get; set; }
        public required string ContentPath { get; set; } // Path to video, or direct HTML/Markdown content
        public int DurationSeconds { get; set; }
        public bool IsCompleted { get; set; }
        public int? NextLessonId { get; set; }
        public string? NextLessonSlug { get; set; }
        public string? NextLessonTitle { get; set; }
        public int? PreviousLessonId { get; set; }
        public string? PreviousLessonSlug { get; set; }
        public string? PreviousLessonTitle { get; set; }
        public bool HasQuiz { get; set; } // Indicates if this lesson has an associated quiz
        public string? Transcript { get; set; }
        public int PositionSeconds { get; set; }
    }
}
