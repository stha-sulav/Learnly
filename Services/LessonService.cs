using Learnly.Data;
using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public class LessonService : ILessonService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LessonService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IEnumerable<Lesson>> GetLessonsByModuleAsync(int moduleId)
        {
            return await _context.Lessons
                                 .Where(l => l.ModuleId == moduleId)
                                 .OrderBy(l => l.OrderIndex)
                                 .ToListAsync();
        }

        public async Task<Lesson> CreateLessonAsync(int moduleId, LessonCreateDto lessonDto)
        {
            var module = await _context.Modules.FindAsync(moduleId);
            if (module == null)
            {
                return null;
            }

            var lesson = new Lesson
            {
                Title = lessonDto.Title,
                ContentType = lessonDto.ContentType,
                DurationSeconds = lessonDto.DurationSeconds,
                OrderIndex = lessonDto.OrderIndex,
                ModuleId = moduleId
            };

            switch (lessonDto.ContentType)
            {
                case ContentType.Article:
                case ContentType.Markdown:
                    lesson.Content = lessonDto.ContentPath;
                    break;
                default:
                    break;
            }

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return lesson;
        }

        public async Task<string> UploadVideoAsync(int lessonId, IFormFile videoFile)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                throw new KeyNotFoundException("Lesson not found.");
            }

            if (videoFile == null || videoFile.Length == 0)
            {
                throw new System.ArgumentException("File is empty.", nameof(videoFile));
            }

            if (videoFile.ContentType != "video/mp4")
            {
                throw new System.ArgumentException("Invalid file type. Only MP4 is allowed.", nameof(videoFile));
            }

            if (videoFile.Length > 500 * 1024 * 1024)
            {
                throw new System.ArgumentException("File size exceeds 500 MB.", nameof(videoFile));
            }

            var courseId = lesson.Module.CourseId;
            var moduleId = lesson.ModuleId;

            var fileName = $"{lessonId}.mp4";
            var relativeFolderPath = Path.Combine("videos", $"course_{courseId}", $"module_{moduleId}");
            var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, relativeFolderPath);
            var filePath = Path.Combine(folderPath, fileName);

            Directory.CreateDirectory(folderPath);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream);
            }

            var relativePath = Path.Combine("/", relativeFolderPath, fileName).Replace('\\', '/');
            lesson.VideoPath = relativePath;
            await _context.SaveChangesAsync();

            return relativePath;
        }

        public async Task<Lesson> GetLessonByIdAsync(int lessonId)
        {
            return await _context.Lessons.FindAsync(lessonId);
        }
    }
}
