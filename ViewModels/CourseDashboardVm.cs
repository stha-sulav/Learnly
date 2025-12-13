namespace Learnly.ViewModels
{
    public class CourseDashboardVm
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string ThumbnailPath { get; set; }
        public int ProgressPercent { get; set; }
        public int? FirstIncompleteLessonId { get; set; }
        public string? FirstIncompleteLessonTitle { get; set; }
    }
}
