using Learnly.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface ICourseService
    {
        Task<IEnumerable<CourseSummaryVm>> GetPublicCourseSummaries();
        Task<CourseDetailVm> GetCourseWithCurriculum(string slug);
        Task<LessonDetailVm> GetLessonDetailsBySlug(string slug);
        Task<CourseDetailVm> CreateCourse(CreateCourseDto courseDto);
    }
}
