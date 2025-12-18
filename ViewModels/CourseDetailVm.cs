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
        public string? ThumbnailPath { get; set; }
    }

    public class CourseDetailVm
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Slug { get; set; }
        public required string Description { get; set; }
        public required string InstructorName { get; set; }
        public required string ThumbnailPath { get; set; }
        public bool IsEnrolled { get; set; }
        public List<ModuleVm> Modules { get; set; } = new List<ModuleVm>();

        // Additional course information
        public int TotalModules { get; set; }
        public int TotalLessons { get; set; }
        public int TotalDurationSeconds { get; set; }
        public int EnrolledStudents { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CategoryName { get; set; }

        // Review information
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ReviewVm> Reviews { get; set; } = new List<ReviewVm>();
        public ReviewVm? CurrentUserReview { get; set; }

        // Computed properties
        public string TotalDurationFormatted
        {
            get
            {
                if (TotalDurationSeconds == 0)
                {
                    return "—"; // No content uploaded yet
                }
                var timeSpan = TimeSpan.FromSeconds(TotalDurationSeconds);
                if (timeSpan.TotalHours >= 1)
                {
                    return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
                }
                if (timeSpan.Minutes > 0)
                {
                    return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
                }
                return $"{timeSpan.Seconds}s";
            }
        }
    }
}
