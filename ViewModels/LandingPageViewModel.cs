namespace Learnly.ViewModels
{
    public class LandingPageViewModel
    {
        public IEnumerable<CourseSummaryVm> FeaturedCourses { get; set; } = new List<CourseSummaryVm>();
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalInstructors { get; set; }
        public int TotalLessons { get; set; }
    }

    public class PlatformStatsDto
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalInstructors { get; set; }
        public int TotalLessons { get; set; }
    }
}
