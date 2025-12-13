using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity; // Added
using Learnly.Models; // Added
using System.Security.Claims; // Added

namespace Learnly.Pages.Lessons
{
    public class LessonModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager; // Added

        public LessonModel(ICourseService courseService, UserManager<ApplicationUser> userManager) // Modified
        {
            _courseService = courseService;
            _userManager = userManager; // Added
        }

        public LessonDetailVm? Lesson { get; set; }

        public async Task<IActionResult> OnGetAsync(int? lessonId)
        {
            if (!lessonId.HasValue)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User); // Added
            Lesson = await _courseService.GetLessonDetailsById(lessonId.Value, userId); // Modified

            if (Lesson == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostMarkCompleteAsync(int lessonId)
        {
            // Placeholder for marking lesson complete logic
            // In a real application, this would update the user's progress in the database.
            // For now, let's just redirect back to the lesson page.
            // You might want to update the IsCompleted property in the LessonDetailVm after this action.

            // Example:
            // var success = await _progressService.MarkLessonComplete(User.Identity.Name, lessonId);
            // if (success) { /* update UI or redirect */ }

            await Task.CompletedTask; // To satisfy CS1998 warning for async method without await
            return RedirectToPage(new { lessonId = lessonId });
        }
    }
}
