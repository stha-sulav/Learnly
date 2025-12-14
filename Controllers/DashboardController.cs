using Learnly.Data;
using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Learnly.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(ICourseService courseService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _courseService = courseService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var enrolledCourses = await _courseService.GetDashboardCoursesWithProgressAsync(userId);
            var coursesList = enrolledCourses.ToList();

            // Calculate stats
            var completedCourses = coursesList.Count(c => c.ProgressPercent == 100);
            var inProgressCourses = coursesList.Count(c => c.ProgressPercent > 0 && c.ProgressPercent < 100);
            var totalLessonsCompleted = await _context.LessonProgresses
                .Where(lp => lp.UserId == userId && lp.IsCompleted)
                .CountAsync();

            // Calculate overall progress
            var overallProgress = coursesList.Any()
                ? (int)coursesList.Average(c => c.ProgressPercent)
                : 0;

            var viewModel = new DashboardViewModel
            {
                TotalEnrolledCourses = coursesList.Count,
                CompletedCourses = completedCourses,
                InProgressCourses = inProgressCourses,
                TotalLessonsCompleted = totalLessonsCompleted,
                OverallProgress = overallProgress,
                CertificatesEarned = completedCourses, // Certificates = completed courses for now
                EnrolledCourses = coursesList
            };

            return View(viewModel);
        }
    }
}
