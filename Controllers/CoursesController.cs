using Learnly.Data;
using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Learnly.Constants;

namespace Learnly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoursesController(ApplicationDbContext context, ICourseService courseService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _courseService = courseService;
            _userManager = userManager;
        }

        // GET: api/Courses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseSummaryVm>>> GetCourses()
        {
            var courses = await _courseService.GetPublicCourseSummaries();
            return Ok(courses);
        }

        // GET: api/Courses/5
        [HttpGet("{slug}")]
        public async Task<ActionResult<CourseDetailVm>> GetCourse(string slug)
        {
            string? userId = _userManager.GetUserId(User); // Get current user ID (can be null if not logged in)
            var course = await _courseService.GetCourseWithCurriculum(slug, userId);

            if (course == null)
            {
                return NotFound();
            }

            return Ok(course);
        }

        // GET: api/Courses/edit/5
        [HttpGet("edit/{id:int}")]
        [Authorize(Roles = Roles.Instructor)]
        public async Task<ActionResult<CourseCreateUpdateDto>> GetCourseForEdit(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var course = await _courseService.GetCourseForEditAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            // Only the instructor who created the course can edit it
            if (course.InstructorId != userId)
            {
                return Forbid("You are not authorized to edit this course.");
            }

            return Ok(course);
        }

        // POST: api/Courses
        [HttpPost]
        [Authorize(Roles = Roles.Instructor)]
        public async Task<ActionResult<CourseDetailVm>> PostCourse([FromBody] CourseCreateUpdateDto courseDto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Ensure the InstructorId in the DTO matches the authenticated user's ID
            if (courseDto.InstructorId != userId)
            {
                return Forbid("Instructor ID in request body does not match authenticated user.");
            }

            // Auto-generate slug from title
            var slug = GenerateSlug(courseDto.Title);

            // Ensure slug uniqueness by appending a number if needed
            var baseSlug = slug;
            var counter = 1;
            while (await _context.Courses.AnyAsync(c => c.Slug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            var course = new Course
            {
                Title = courseDto.Title,
                Slug = slug,
                Description = courseDto.Description,
                InstructorId = courseDto.InstructorId,
                CategoryId = courseDto.CategoryId,
                ThumbnailPath = courseDto.ThumbnailPath,
                CreatedAt = DateTime.UtcNow,
                IsPublished = courseDto.IsPublished
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Fetch the instructor to get their name for the ViewModel
            var instructor = await _userManager.FindByIdAsync(course.InstructorId);

            var courseDetailVm = new CourseDetailVm
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description ?? string.Empty,
                InstructorName = instructor?.DisplayName ?? "Unknown Instructor",
                ThumbnailPath = course.ThumbnailPath ?? string.Empty,
                IsEnrolled = false, // Default for creation, will be set dynamically
                Modules = new List<ModuleVm>()
            };

            return CreatedAtAction(nameof(GetCourse), new { slug = course.Slug }, courseDetailVm);
        }

        [HttpPost("{id}/enroll")]
        [Authorize]
        public async Task<IActionResult> Enroll(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound("Course not found.");
            }

            // Check if already enrolled
            var isEnrolled = await _courseService.IsUserEnrolledAsync(id, userId);
            if (isEnrolled)
            {
                return Conflict("User is already enrolled in this course.");
            }

            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseId = id,
                EnrolledAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok("Enrolled successfully.");
        }

        // GET: api/Users/{userId}/Courses
        [HttpGet("~/api/users/{userId}/courses")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CourseDashboardVm>>> GetUserCourses(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId != userId)
            {
                return Forbid("You are not authorized to view this user's courses.");
            }

            var courses = await _courseService.GetUserEnrolledCoursesAsync(userId);
            return Ok(courses);
        }

        // PUT: api/Courses/5
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Instructor)]
        public async Task<IActionResult> PutCourse(int id, [FromBody] CourseCreateUpdateDto courseDto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            // Only the instructor who created the course can update it
            if (course.InstructorId != userId)
            {
                return Forbid("You are not authorized to update this course.");
            }

            // Keep existing slug - don't change URLs after course is created
            course.Title = courseDto.Title;
            course.Description = courseDto.Description;
            course.CategoryId = courseDto.CategoryId;
            course.ThumbnailPath = courseDto.ThumbnailPath;
            course.IsPublished = courseDto.IsPublished;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Courses/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Instructor)]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            // Only the instructor who created the course can delete it
            if (course.InstructorId != userId)
            {
                return Forbid("You are not authorized to delete this course.");
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Courses/5/Thumbnail
        [HttpPost("{id}/Thumbnail")]
        [Authorize(Roles = Roles.Instructor)]
        public async Task<IActionResult> UploadThumbnail(int id, IFormFile thumbnail)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            if (course.InstructorId != userId)
            {
                return Forbid("You are not authorized to update this course.");
            }

            if (thumbnail == null || thumbnail.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(thumbnail.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid image format. Allowed formats: JPG, PNG, GIF, WebP");
            }

            // Validate file size (max 5MB)
            if (thumbnail.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Image file size must be less than 5MB.");
            }

            // Create uploads directory if needed
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "thumbnails");
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
                await thumbnail.CopyToAsync(fileStream);
            }

            // Update course thumbnail path
            course.ThumbnailPath = $"/uploads/thumbnails/{uniqueFileName}";
            await _context.SaveChangesAsync();

            return Ok(new { thumbnailPath = course.ThumbnailPath });
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        private static string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            // Convert to lowercase, remove special characters, replace spaces with hyphens
            var slug = title.ToLowerInvariant().Trim();

            // Remove special characters (keep only letters, numbers, spaces, and hyphens)
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^\w\s-]", "");

            // Replace spaces with hyphens
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");

            // Replace multiple hyphens with single hyphen
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

            // Trim hyphens from start and end
            slug = slug.Trim('-');

            return slug;
        }
    }
}
