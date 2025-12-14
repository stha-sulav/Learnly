using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Learnly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;
        private readonly IWebHostEnvironment _environment;

        public LessonsController(ILessonService lessonService, IWebHostEnvironment environment)
        {
            _lessonService = lessonService;
            _environment = environment;
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
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)] // 500MB
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
            catch (System.Exception ex)
            {
                // Generic catch-all for other errors - include message for debugging
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        // PUT: api/Lessons/5
        [HttpPut("{lessonId}")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<ActionResult<Lesson>> UpdateLesson(int lessonId, [FromBody] LessonUpdateDto lessonDto)
        {
            var lesson = await _lessonService.UpdateLessonAsync(lessonId, lessonDto.Title);
            if (lesson == null)
            {
                return NotFound("Lesson not found.");
            }
            return Ok(lesson);
        }

        // POST: api/Lessons/5/Thumbnail
        [HttpPost("{lessonId}/Thumbnail")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<ActionResult<Lesson>> UploadLessonThumbnail(int lessonId, IFormFile thumbnail)
        {
            if (thumbnail == null || thumbnail.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(thumbnail.FileName).ToLowerInvariant();
            if (!Array.Exists(allowedExtensions, ext => ext == extension))
            {
                return BadRequest("Invalid file type. Allowed types: jpg, jpeg, png, gif, webp");
            }

            // Validate file size (max 5MB)
            if (thumbnail.Length > 5 * 1024 * 1024)
            {
                return BadRequest("File size exceeds 5MB limit.");
            }

            var lesson = await _lessonService.GetLessonByIdAsync(lessonId);
            if (lesson == null)
            {
                return NotFound("Lesson not found.");
            }

            // Delete old thumbnail if exists
            if (!string.IsNullOrEmpty(lesson.ThumbnailPath))
            {
                var oldPath = Path.Combine(_environment.WebRootPath, lesson.ThumbnailPath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            // Save new thumbnail
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "lesson-thumbnails");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await thumbnail.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/lesson-thumbnails/{uniqueFileName}";
            var updatedLesson = await _lessonService.UpdateLessonThumbnailAsync(lessonId, relativePath);

            return Ok(updatedLesson);
        }

        // DELETE: api/Lessons/5
        [HttpDelete("{lessonId}")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> DeleteLesson(int lessonId)
        {
            var lesson = await _lessonService.GetLessonByIdAsync(lessonId);
            if (lesson == null)
            {
                return NotFound("Lesson not found.");
            }

            // Delete thumbnail if exists
            if (!string.IsNullOrEmpty(lesson.ThumbnailPath))
            {
                var thumbnailPath = Path.Combine(_environment.WebRootPath, lesson.ThumbnailPath.TrimStart('/'));
                if (System.IO.File.Exists(thumbnailPath))
                {
                    System.IO.File.Delete(thumbnailPath);
                }
            }

            // Delete video if exists
            if (!string.IsNullOrEmpty(lesson.VideoPath))
            {
                var videoPath = Path.Combine(_environment.WebRootPath, lesson.VideoPath.TrimStart('/'));
                if (System.IO.File.Exists(videoPath))
                {
                    System.IO.File.Delete(videoPath);
                }
            }

            var result = await _lessonService.DeleteLessonAsync(lessonId);
            if (!result)
            {
                return StatusCode(500, "Failed to delete lesson.");
            }

            return NoContent();
        }
    }
}
