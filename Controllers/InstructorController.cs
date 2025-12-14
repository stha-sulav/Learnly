using System.Threading.Tasks;
using System.IO;
using Learnly.Constants;
using Learnly.Data;
using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public InstructorController(ICourseService courseService, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _courseService = courseService;
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get instructor's courses
            var courses = await _context.Courses
                .Where(c => c.InstructorId == userId)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .ToListAsync();

            // Get total students enrolled in instructor's courses
            var courseIds = courses.Select(c => c.Id).ToList();
            var totalStudents = await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();

            var model = new InstructorDashboardViewModel
            {
                TotalCourses = courses.Count,
                PublishedCourses = courses.Count(c => c.IsPublished),
                DraftCourses = courses.Count(c => !c.IsPublished),
                TotalStudents = totalStudents,
                TotalModules = courses.Sum(c => c.Modules.Count),
                TotalLessons = courses.Sum(c => c.Modules.Sum(m => m.Lessons.Count))
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

            return View(new CourseCreateUpdateDto());
        }

        // POST: Instructor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseCreateUpdateDto courseDto, IFormFile? ThumbnailFile)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Set the InstructorId to the current user
            courseDto.InstructorId = userId;

            // Clear ModelState errors for fields we set server-side
            ModelState.Remove("InstructorId");
            ModelState.Remove("ThumbnailPath");

            // Handle thumbnail upload
            if (ThumbnailFile != null && ThumbnailFile.Length > 0)
            {
                var thumbnailPath = await SaveThumbnailAsync(ThumbnailFile);
                if (thumbnailPath != null)
                {
                    courseDto.ThumbnailPath = thumbnailPath;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var createdCourse = await _courseService.CreateCourseAsync(courseDto);
                    TempData["SuccessMessage"] = "Course created successfully!";
                    // Redirect to the Razor Page CourseEdit for managing the course
                    return RedirectToPage("/Courses/CourseEdit", new { area = "Instructor", id = createdCourse.Id });
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("Slug", ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating course: {ex.Message}");
                }
            }
            else
            {
                // Log validation errors for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine($"Validation error: {error}");
                }
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

        private async Task<string?> SaveThumbnailAsync(IFormFile file)
        {
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Invalid image format. Allowed formats: JPG, PNG, GIF, WebP");
                    return null;
                }

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Image file size must be less than 5MB.");
                    return null;
                }

                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "thumbnails");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Return relative path for storage
                return $"/uploads/thumbnails/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error uploading thumbnail: {ex.Message}");
                return null;
            }
        }
    }
}
