using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalEnrolledCourses { get; set; }
        public int CompletedCourses { get; set; }
        public int InProgressCourses { get; set; }
        public int TotalLessonsCompleted { get; set; }
        public int OverallProgress { get; set; }
        public int CertificatesEarned { get; set; }
        public List<CourseDashboardVm> EnrolledCourses { get; set; } = new List<CourseDashboardVm>();
    }
}
