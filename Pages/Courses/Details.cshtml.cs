using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Learnly.Models;

namespace Learnly.Pages.Courses
{
    public class DetailsModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(ICourseService courseService, UserManager<ApplicationUser> userManager)
        {
            _courseService = courseService;
            _userManager = userManager;
        }

        public CourseDetailVm? Course { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            Course = await _courseService.GetCourseWithCurriculumById(id, userId);

            if (Course == null)
            {
                return NotFound();
            }

            // Load reviews for the course
            var reviews = await _courseService.GetCourseReviewsAsync(id, userId);
            Course.Reviews = reviews.Reviews;
            Course.AverageRating = reviews.AverageRating;
            Course.TotalReviews = reviews.TotalReviews;
            Course.CurrentUserReview = reviews.CurrentUserReview;

            return Page();
        }
    }
}
