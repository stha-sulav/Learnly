using Learnly.Constants;
using Learnly.Data;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
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

        public InstructorController(ICourseService courseService, ApplicationDbContext context)
        {
            _courseService = courseService;
            _context = context;
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

        // GET: Instructor/Create
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

            return View(new CreateCourseDto
            {
                Title = "",
                Slug = "",
                Description = "",
                ThumbnailPath = ""
            });
        }

        // POST: Instructor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCourseDto courseDto)
        {
            if (ModelState.IsValid)
            {
                await _courseService.CreateCourse(courseDto);
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
