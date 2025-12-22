using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class ModuleVm
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int Order { get; set; }
        public string? ThumbnailPath { get; set; }
        public List<LessonVm> Lessons { get; set; } = new List<LessonVm>();

        // Quiz-related properties for module locking
        public bool HasQuiz { get; set; }
        public int? QuizId { get; set; }
        public bool IsQuizPassed { get; set; }
        public bool IsLocked { get; set; }
    }
}
