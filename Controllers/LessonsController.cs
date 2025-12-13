using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learnly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonsController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        // GET: api/Lessons/ByModule/5
        [HttpGet("ByModule/{moduleId}")]
        public async Task<ActionResult<IEnumerable<Lesson>>> GetLessonsByModule(int moduleId)
        {
            var lessons = await _lessonService.GetLessonsByModuleAsync(moduleId);
            return Ok(lessons);
        }

        // POST: api/Lessons/ByModule/5
        [HttpPost("ByModule/{moduleId}")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<ActionResult<Lesson>> PostLesson(int moduleId, LessonCreateDto lessonDto)
        {
            var lesson = await _lessonService.CreateLessonAsync(moduleId, lessonDto);

            if (lesson == null)
            {
                return NotFound("Module not found.");
            }

            return CreatedAtAction(nameof(GetLessonsByModule), new { moduleId = lesson.ModuleId }, lesson);
        }

        [HttpPost("{lessonId}/upload-video")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> UploadVideo(int lessonId, IFormFile file)
        {
            try
            {
                var publicUrl = await _lessonService.UploadVideoAsync(lessonId, file);
                return Ok(new { videoUrl = publicUrl });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "The lesson could not be found. Please refresh and try again." });
            }
            catch (System.ArgumentException ex)
            {
                // This could be for invalid file types, sizes, etc.
                return BadRequest(new { error = ex.Message });
            }
            catch (System.Exception)
            {
                // Generic catch-all for other errors
                return StatusCode(500, new { error = "An unexpected error occurred during the upload. Please try again later." });
            }
        }
    }
}
