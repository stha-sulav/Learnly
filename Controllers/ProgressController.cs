using Learnly.Data;
using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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

            var userProgress = await _context.UserProgresses
                .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

            if (userProgress == null)
            {
                // Create new progress record
                userProgress = new UserProgress
                {
                    UserId = userId,
                    LessonId = lessonId,
                    CourseId = lesson.Module.Course.Id, // Assuming CourseId is accessible via Lesson -> Module -> Course
                    IsCompleted = progressDto.Completed,
                    PositionSeconds = progressDto.PositionSeconds,
                    LastAccessed = System.DateTime.UtcNow
                };
                if (progressDto.Completed)
                {
                    userProgress.CompletedAt = System.DateTime.UtcNow;
                }
                _context.UserProgresses.Add(userProgress);
            }
            else
            {
                // Update existing progress record
                userProgress.IsCompleted = progressDto.Completed;
                userProgress.PositionSeconds = progressDto.PositionSeconds;
                userProgress.LastAccessed = System.DateTime.UtcNow;
                if (progressDto.Completed && userProgress.CompletedAt == null)
                {
                    userProgress.CompletedAt = System.DateTime.UtcNow;
                }
                _context.UserProgresses.Update(userProgress);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
