using Learnly.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface ICourseService
    {
        Task<IEnumerable<CourseSummaryVm>> GetPublicCourseSummaries();
        Task<CourseDetailVm> GetCourseWithCurriculum(string slug);
        Task<LessonDetailVm> GetLessonDetailsById(int lessonId);
        Task<CourseDetailVm> CreateCourse(CreateCourseDto courseDto);
    }
}
