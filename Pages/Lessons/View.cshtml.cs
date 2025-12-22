using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Learnly.Models;
using Microsoft.AspNetCore.Authorization;

namespace Learnly.Pages.Lessons
{
    [Authorize]
    public class LessonModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly IQuizService _quizService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LessonModel(ICourseService courseService, IQuizService quizService, UserManager<ApplicationUser> userManager)
        {
            _courseService = courseService;
            _quizService = quizService;
            _userManager = userManager;
        }

        public LessonWithCurriculumVm? Lesson { get; set; }

        public async Task<IActionResult> OnGetAsync(int? lessonId)
        {
            if (!lessonId.HasValue)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Identity/Account/Login", new { area = "Identity" });
            }

            Lesson = await _courseService.GetLessonWithCurriculum(lessonId.Value, userId);

            if (Lesson == null)
            {
                return NotFound();
            }

            // Check if user is enrolled in the course
            var isEnrolled = await _courseService.IsUserEnrolledAsync(Lesson.CourseId, userId);
            if (!isEnrolled)
            {
                TempData["ErrorMessage"] = "You must be enrolled in this course to access its lessons.";
                return RedirectToPage("/Courses/Details", new { id = Lesson.CourseId });
            }

            // Check if user can access this module (based on quiz completion)
            var canAccessModule = await _quizService.CanUserAccessModuleAsync(Lesson.ModuleId, userId);
            if (!canAccessModule)
            {
                TempData["ErrorMessage"] = "You must complete the previous module's quiz to access this content.";

                // Stay on the same page - redirect back to referer or course details
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToPage("/Courses/Details", new { id = Lesson.CourseId });
            }

            return Page();
        }
    }
}
