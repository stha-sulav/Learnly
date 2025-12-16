using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class DashboardViewModel
    {
        // Student stats
        public int TotalEnrolledCourses { get; set; }
        public int CompletedCourses { get; set; }
        public int InProgressCourses { get; set; }
        public int TotalLessonsCompleted { get; set; }
        public int OverallProgress { get; set; }
        public int CertificatesEarned { get; set; }
        public List<CourseDashboardVm> EnrolledCourses { get; set; } = new List<CourseDashboardVm>();

        // Instructor stats
        public bool IsInstructor { get; set; }
        public int TotalCreatedCourses { get; set; }
        public int PublishedCourses { get; set; }
        public int DraftCourses { get; set; }
        public int TotalStudentsEnrolled { get; set; }
        public int TotalModulesCreated { get; set; }
        public int TotalLessonsCreated { get; set; }
        public List<CourseSummaryVm> InstructorCourses { get; set; } = new List<CourseSummaryVm>();
    }
}
