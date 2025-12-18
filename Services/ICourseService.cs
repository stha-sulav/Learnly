using Learnly.ViewModels;
using Learnly.Models; // For Category
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface ICourseService
    {
        Task<IEnumerable<CourseSummaryVm>> GetPublicCourseSummaries();
        Task<IEnumerable<CourseSummaryVm>> GetAvailableCoursesForUser(string? userId);
        Task<CourseDetailVm?> GetCourseWithCurriculum(string slug, string? userId); // Modified
        Task<CourseDetailVm?> GetCourseWithCurriculumById(int courseId, string? userId);
        Task<LessonWithCurriculumVm?> GetLessonWithCurriculum(int lessonId, string? userId);
        Task<CourseDetailVm> CreateCourseAsync(CourseCreateUpdateDto courseDto); // Renamed and modified
        Task UpdateCourseAsync(CourseCreateUpdateDto courseDto); // New
        Task<CourseCreateUpdateDto?> GetCourseForEditAsync(int courseId); // New
        Task<bool> IsUserEnrolledAsync(int courseId, string userId);
        Task<CourseSummaryVm?> GetCourseByIdAsync(int courseId); // Return type nullable
        Task<IEnumerable<CourseDashboardVm>> GetUserEnrolledCoursesAsync(string userId); // Renamed
        Task<IEnumerable<CourseDashboardVm>> GetDashboardCoursesWithProgressAsync(string userId); // New
        Task<IEnumerable<CourseSummaryVm>> GetInstructorCourseSummaries(string instructorId); // New
        Task<IEnumerable<CourseSummaryVm>> GetFeaturedCoursesAsync(int count);
        Task<PlatformStatsDto> GetPlatformStatsAsync();

        // Review methods
        Task<CourseReviewsVm> GetCourseReviewsAsync(int courseId, string? userId);
        Task<ReviewVm> CreateReviewAsync(int courseId, string userId, int rating, string? comment);
        Task<ReviewVm?> UpdateReviewAsync(int reviewId, string userId, int rating, string? comment);
        Task<bool> DeleteReviewAsync(int reviewId, string userId);
        Task<ReviewVm?> GetUserReviewForCourseAsync(int courseId, string userId);
    }
}
