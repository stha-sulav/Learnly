using Learnly.Data;
using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Learnly.Constants; // For Roles

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
            var course = await _courseService.GetCourseWithCurriculum(slug);

            if (course == null)
            {
                return NotFound();
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

            // Check if slug already exists
            if (await _context.Courses.AnyAsync(c => c.Slug == courseDto.Slug))
            {
                ModelState.AddModelError("Slug", "A course with this slug already exists.");
                return Conflict(ModelState);
            }

            var course = new Course
            {
                Title = courseDto.Title,
                Slug = courseDto.Slug,
                Description = courseDto.Description,
                InstructorId = courseDto.InstructorId,
                CategoryId = courseDto.CategoryId,
                Price = courseDto.Price,
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
                Price = course.Price,
                IsEnrolled = false, // Default for creation, will be set dynamically
                Modules = new List<ModuleVm>()
            };

            return CreatedAtAction(nameof(GetCourse), new { slug = course.Slug }, courseDetailVm);
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
            
            // Check for slug uniqueness if it's being changed
            if (course.Slug != courseDto.Slug && await _context.Courses.AnyAsync(c => c.Slug == courseDto.Slug && c.Id != id))
            {
                ModelState.AddModelError("Slug", "A course with this slug already exists.");
                return Conflict(ModelState);
            }

            course.Title = courseDto.Title;
            course.Slug = courseDto.Slug;
            course.Description = courseDto.Description;
            course.CategoryId = courseDto.CategoryId;
            course.Price = courseDto.Price;
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

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
