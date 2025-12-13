using System.Threading.Tasks; // Required for async operations
using Learnly.Constants;
using Learnly.Data;
using Learnly.Models; // Ensure this is present and correct
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Moved before Learnly.Models
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Learnly.Controllers
{
    [Authorize(Roles = Roles.Instructor)]
    public class InstructorController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Inject UserManager

        public InstructorController(ICourseService courseService, ApplicationDbContext context, UserManager<ApplicationUser> userManager) // Add UserManager to constructor
        {
            _courseService = courseService;
            _context = context;
            _userManager = userManager; // Initialize UserManager
        }

        public IActionResult Index()
        {
            var model = new InstructorDashboardViewModel
            {
                TotalCourses = 0, // Replace with actual data later
                TotalStudents = 0  // Replace with actual data later
            };
            return View(model);
        }

        // GET: Instructor/MyCourses
        public async Task<IActionResult> MyCourses()
        {
            var userId = _userManager.GetUserId(User); // Get current instructor's ID
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var instructorCourses = await _courseService.GetInstructorCourseSummaries(userId);
            return View(instructorCourses);
        }

        // GET: Instructor/Create
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

            return View(new CourseCreateUpdateDto
            {
                Title = "",
                Slug = "",
                Description = "",
                ThumbnailPath = "",
                InstructorId = "" // Set a default empty string for string type
            });
        }

        // POST: Instructor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseCreateUpdateDto courseDto)
        {
            if (ModelState.IsValid)
            {
                await _courseService.CreateCourseAsync(courseDto);
                return RedirectToAction(nameof(Index));
            }

            // If model state is invalid, re-populate categories for the dropdown
            ViewBag.Categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

            return View(courseDto);
        }
    }
}
