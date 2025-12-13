using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity; // Added
using Learnly.Models; // Added
using System.Security.Claims; // Added

namespace Learnly.Pages.Courses
{
    public class DetailsModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager; // Added

        public DetailsModel(ICourseService courseService, UserManager<ApplicationUser> userManager) // Modified
        {
            _courseService = courseService;
            _userManager = userManager; // Added
        }

        public CourseDetailVm? Course { get; set; }

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User); // Added
            Course = await _courseService.GetCourseWithCurriculum(slug, userId); // Modified

            if (Course == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
