using Learnly.Data;
using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

using System.Security.Claims;

namespace Learnly.Controllers
{
    [ApiController]
    [Route("api/lessons/{lessonId}/progress")]
    public class ProgressController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProgressController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> PostProgress(int lessonId, [FromBody] LessonProgressDto progressDto)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Find the lesson to get the CourseId
            var lesson = await _context.Lessons.Include(l => l.Module).ThenInclude(m => m.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null || lesson.Module == null || lesson.Module.Course == null)
            {
                return BadRequest("Lesson, Module, or Course not found for the given lesson ID.");
            }

            var lessonProgress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.LessonId == lessonId);

            if (lessonProgress == null)
            {
                // Create new progress record
                lessonProgress = new LessonProgress
                {
                    UserId = userId,
                    LessonId = lessonId,
                    IsCompleted = progressDto.Completed,
                    PositionSeconds = progressDto.PositionSeconds,
                    LastAccessed = System.DateTime.UtcNow
                };
                if (progressDto.Completed)
                {
                    lessonProgress.CompletedAt = System.DateTime.UtcNow;
                }
                _context.LessonProgresses.Add(lessonProgress);
            }
            else
            {
                // Update existing progress record
                lessonProgress.IsCompleted = progressDto.Completed;
                lessonProgress.PositionSeconds = progressDto.PositionSeconds;
                lessonProgress.LastAccessed = System.DateTime.UtcNow;
                if (progressDto.Completed && lessonProgress.CompletedAt == null)
                {
                    lessonProgress.CompletedAt = System.DateTime.UtcNow;
                }
                _context.LessonProgresses.Update(lessonProgress);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPatch("update-position")]
        public async Task<IActionResult> UpdatePosition([FromBody] LessonPositionUpdateRequest request)
        {
            if (request == null || request.LessonId <= 0)
            {
                return BadRequest("Invalid request.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var lessonProgress = await _context.LessonProgresses
                .FirstOrDefaultAsync(p => p.LessonId == request.LessonId && p.UserId == userId);

            if (lessonProgress == null)
            {
                lessonProgress = new LessonProgress
                {
                    LessonId = request.LessonId,
                    UserId = userId,
                    PositionSeconds = request.PositionSeconds,
                    LastAccessed = DateTime.UtcNow,
                    IsCompleted = false // Assume not completed on position update
                };
                _context.LessonProgresses.Add(lessonProgress);
            }
            else
            {
                lessonProgress.PositionSeconds = request.PositionSeconds;
                lessonProgress.LastAccessed = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        public class LessonCompletionRequest
        {
            public int LessonId { get; set; }
        }

        public class LessonPositionUpdateRequest
        {
            public int LessonId { get; set; }
            public int PositionSeconds { get; set; }
        }
    }
}
