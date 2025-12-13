using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class DashboardViewModel
    {
        public List<CourseDashboardVm> EnrolledCourses { get; set; } = new List<CourseDashboardVm>();
    }
}
