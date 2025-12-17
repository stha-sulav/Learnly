using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Learnly.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICourseService _courseService;

        public HomeController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        public async Task<IActionResult> Index()
        {
            // Redirect authenticated users to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var featuredCourses = await _courseService.GetFeaturedCoursesAsync(6);
            var stats = await _courseService.GetPlatformStatsAsync();

            var viewModel = new LandingPageViewModel
            {
                FeaturedCourses = featuredCourses,
                TotalCourses = stats.TotalCourses,
                TotalStudents = stats.TotalStudents,
                TotalInstructors = stats.TotalInstructors,
                TotalLessons = stats.TotalLessons
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
