using Learnly.Models;
using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class LessonWithCurriculumVm
    {
        // Current lesson details
        public int Id { get; set; }
        public required string Title { get; set; }
        public int CourseId { get; set; }
        public required string CourseTitle { get; set; }
        public required string CourseSlug { get; set; }
        public int ModuleId { get; set; }
        public required string ModuleTitle { get; set; }
        public ContentType ContentType { get; set; }
        public required string ContentPath { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsCompleted { get; set; }
        public int? NextLessonId { get; set; }
        public string? NextLessonTitle { get; set; }
        public int? PreviousLessonId { get; set; }
        public string? PreviousLessonTitle { get; set; }
        public bool HasQuiz { get; set; }
        public bool IsQuizPassed { get; set; }
        public bool IsLastLessonInModule { get; set; }
        public string? Transcript { get; set; }
        public int PositionSeconds { get; set; }

        // Course curriculum for sidebar
        public List<ModuleVm> Modules { get; set; } = new List<ModuleVm>();

        // Progress info
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int ProgressPercent => TotalLessons > 0 ? (int)((double)CompletedLessons / TotalLessons * 100) : 0;
    }
}
