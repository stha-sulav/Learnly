using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Learnly.Models;

namespace Learnly.Pages.Lessons
{
    public class LessonModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LessonModel(ICourseService courseService, UserManager<ApplicationUser> userManager)
        {
            _courseService = courseService;
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
            Lesson = await _courseService.GetLessonWithCurriculum(lessonId.Value, userId);

            if (Lesson == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
