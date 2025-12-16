using Learnly.Constants;
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

            var isInstructor = User.IsInRole(Roles.Instructor);

            var viewModel = new DashboardViewModel
            {
                IsInstructor = isInstructor
            };

            if (isInstructor)
            {
                // Fetch instructor-specific stats
                var instructorCourses = await _courseService.GetInstructorCourseSummaries(userId);
                var coursesList = instructorCourses.ToList();

                viewModel.TotalCreatedCourses = coursesList.Count;
                viewModel.PublishedCourses = coursesList.Count(c => c.IsPublished);
                viewModel.DraftCourses = coursesList.Count(c => !c.IsPublished);
                viewModel.TotalStudentsEnrolled = coursesList.Sum(c => c.EnrolledStudents);
                viewModel.TotalModulesCreated = coursesList.Sum(c => c.ModuleCount);
                viewModel.TotalLessonsCreated = coursesList.Sum(c => c.LessonCount);
                viewModel.InstructorCourses = coursesList;
            }
            else
            {
                // Fetch student-specific stats
                var enrolledCourses = await _courseService.GetDashboardCoursesWithProgressAsync(userId);
                var coursesList = enrolledCourses.ToList();

                var completedCourses = coursesList.Count(c => c.ProgressPercent == 100);
                var inProgressCourses = coursesList.Count(c => c.ProgressPercent > 0 && c.ProgressPercent < 100);
                var totalLessonsCompleted = await _context.LessonProgresses
                    .Where(lp => lp.UserId == userId && lp.IsCompleted)
                    .CountAsync();

                var overallProgress = coursesList.Any()
                    ? (int)coursesList.Average(c => c.ProgressPercent)
                    : 0;

                viewModel.TotalEnrolledCourses = coursesList.Count;
                viewModel.CompletedCourses = completedCourses;
                viewModel.InProgressCourses = inProgressCourses;
                viewModel.TotalLessonsCompleted = totalLessonsCompleted;
                viewModel.OverallProgress = overallProgress;
                viewModel.CertificatesEarned = completedCourses;
                viewModel.EnrolledCourses = coursesList;
            }

            return View(viewModel);
        }
    }
}
