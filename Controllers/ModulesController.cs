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
    public class ModulesController : ControllerBase
    {
        private readonly IModuleService _moduleService;
        private readonly ILessonService _lessonService;
        private readonly IWebHostEnvironment _environment;

        public ModulesController(IModuleService moduleService, ILessonService lessonService, IWebHostEnvironment environment)
        {
            _moduleService = moduleService;
            _lessonService = lessonService;
            _environment = environment;
        }

        // GET: api/Modules/ByCourse/5
        [HttpGet("ByCourse/{courseId}")]
        public async Task<ActionResult<IEnumerable<Module>>> GetModulesByCourse(int courseId)
        {
            var modules = await _moduleService.GetModulesByCourseAsync(courseId);
            return Ok(modules);
        }

        // POST: api/Modules/ByCourse/5
        [HttpPost("ByCourse/{courseId}")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<ActionResult<Module>> PostModule(int courseId, ModuleCreateDto moduleDto)
        {
            var module = await _moduleService.CreateModuleAsync(courseId, moduleDto);

            if (module == null)
            {
                return NotFound("Course not found.");
            }

            return CreatedAtAction(nameof(GetModulesByCourse), new { courseId = module.CourseId }, module);
        }

        // PUT: api/Modules/5
        [HttpPut("{moduleId}")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<ActionResult<Module>> UpdateModule(int moduleId, [FromBody] ModuleUpdateDto moduleDto)
        {
            var module = await _moduleService.UpdateModuleAsync(moduleId, moduleDto.Title);
            if (module == null)
            {
                return NotFound("Module not found.");
            }
            return Ok(module);
        }

        // POST: api/Modules/5/Thumbnail
        [HttpPost("{moduleId}/Thumbnail")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<ActionResult<Module>> UploadModuleThumbnail(int moduleId, IFormFile thumbnail)
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

            var module = await _moduleService.GetModuleByIdAsync(moduleId);
            if (module == null)
            {
                return NotFound("Module not found.");
            }

            // Delete old thumbnail if exists
            if (!string.IsNullOrEmpty(module.ThumbnailPath))
            {
                var oldPath = Path.Combine(_environment.WebRootPath, module.ThumbnailPath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            // Save new thumbnail
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "module-thumbnails");
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

            var relativePath = $"/uploads/module-thumbnails/{uniqueFileName}";
            var updatedModule = await _moduleService.UpdateModuleThumbnailAsync(moduleId, relativePath);

            return Ok(updatedModule);
        }

        // DELETE: api/Modules/5
        [HttpDelete("{moduleId}")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> DeleteModule(int moduleId)
        {
            var module = await _moduleService.GetModuleByIdAsync(moduleId);
            if (module == null)
            {
                return NotFound("Module not found.");
            }

            // First, delete all lessons in this module (with their files)
            var lessons = await _lessonService.GetLessonsByModuleAsync(moduleId);
            foreach (var lesson in lessons)
            {
                // Delete lesson thumbnail if exists
                if (!string.IsNullOrEmpty(lesson.ThumbnailPath))
                {
                    var lessonThumbnailPath = Path.Combine(_environment.WebRootPath, lesson.ThumbnailPath.TrimStart('/'));
                    if (System.IO.File.Exists(lessonThumbnailPath))
                    {
                        System.IO.File.Delete(lessonThumbnailPath);
                    }
                }

                // Delete lesson video if exists
                if (!string.IsNullOrEmpty(lesson.VideoPath))
                {
                    var videoPath = Path.Combine(_environment.WebRootPath, lesson.VideoPath.TrimStart('/'));
                    if (System.IO.File.Exists(videoPath))
                    {
                        System.IO.File.Delete(videoPath);
                    }
                }

                // Delete the lesson from database
                await _lessonService.DeleteLessonAsync(lesson.Id);
            }

            // Delete module thumbnail if exists
            if (!string.IsNullOrEmpty(module.ThumbnailPath))
            {
                var thumbnailPath = Path.Combine(_environment.WebRootPath, module.ThumbnailPath.TrimStart('/'));
                if (System.IO.File.Exists(thumbnailPath))
                {
                    System.IO.File.Delete(thumbnailPath);
                }
            }

            var result = await _moduleService.DeleteModuleAsync(moduleId);
            if (!result)
            {
                return StatusCode(500, "Failed to delete module.");
            }

            return NoContent();
        }
    }
}
