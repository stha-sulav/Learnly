using Learnly.Data;
using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;

        public CourseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CourseSummaryVm>> GetPublicCourseSummaries()
        {
            return await _context.Courses
                .Where(c => c.IsPublished)
                .Select(c => new CourseSummaryVm
                {
                    Id = c.Id,
                    Title = c.Title,
                    Slug = c.Slug,
                    ThumbnailPath = c.ThumbnailPath ?? string.Empty,
                    InstructorName = c.Instructor!.DisplayName ?? "Unknown Instructor", // Assuming Instructor is loaded or joined
                    ShortDescription = c.Description != null && c.Description.Length > 150 ? c.Description.Substring(0, 150) + "..." : c.Description ?? string.Empty,
                    ProgressPercent = null // This would require user-specific enrollment data, not covered in public summaries
                })
                .ToListAsync();
        }

        public async Task<CourseDetailVm?> GetCourseWithCurriculum(string slug)
        {
            return await _context.Courses
                .Where(c => c.Slug == slug)
                .Include(c => c.Instructor) // Assuming Instructor navigation property exists
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .Select(c => new CourseDetailVm
                {
                    Id = c.Id,
                    Title = c.Title,
                    Slug = c.Slug,
                    Description = c.Description ?? string.Empty,
                    InstructorName = c.Instructor!.DisplayName ?? "Unknown Instructor",
                    ThumbnailPath = c.ThumbnailPath ?? string.Empty,
                    Price = c.Price,
                    IsEnrolled = false, // This would be dynamic based on current user enrollment
                    Modules = c.Modules.OrderBy(m => m.OrderIndex).Select(m => new ModuleVm
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Order = m.OrderIndex,
                        Lessons = m.Lessons.OrderBy(l => l.OrderIndex).Select(l => new LessonVm
                        {
                            Id = l.Id,
                            Title = l.Title,
                            ContentType = l.ContentType,
                            DurationSeconds = l.DurationSeconds,
                            Order = l.OrderIndex,
                            IsCompleted = false // This would be dynamic based on user progress
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<LessonDetailVm?> GetLessonDetailsById(int lessonId)
        {
            return await _context.Lessons
                .Where(l => l.Id == lessonId)
                .Include(l => l.Module)
                    .ThenInclude(m => m.Course)
                .Select(l => new LessonDetailVm
                {
                    Id = l.Id,
                    Title = l.Title,
                    CourseId = l.Module!.Course!.Id,
                    CourseTitle = l.Module!.Course!.Title,
                    CourseSlug = l.Module!.Course!.Slug,
                    ModuleId = l.ModuleId,
                    ModuleTitle = l.Module!.Title,
                    ContentType = l.ContentType,
                    ContentPath = l.Content ?? string.Empty, // Changed to Content
                    DurationSeconds = l.DurationSeconds,
                    IsCompleted = false, // Dynamic
                    // These would require more complex queries for prev/next and user progress
                    NextLessonId = (int?)_context.Lessons.Where(next => next.ModuleId == l.ModuleId && next.OrderIndex > l.OrderIndex).OrderBy(next => next.OrderIndex).Select(next => next.Id).FirstOrDefault(),
                    NextLessonTitle = _context.Lessons.Where(next => next.ModuleId == l.ModuleId && next.OrderIndex > l.OrderIndex).OrderBy(next => next.OrderIndex).Select(next => next.Title).FirstOrDefault(),
                    PreviousLessonId = (int?)_context.Lessons.Where(prev => prev.ModuleId == l.ModuleId && prev.OrderIndex < l.OrderIndex).OrderByDescending(prev => prev.OrderIndex).Select(prev => prev.Id).FirstOrDefault(),
                    PreviousLessonTitle = _context.Lessons.Where(prev => prev.ModuleId == l.ModuleId && prev.OrderIndex < l.OrderIndex).OrderByDescending(prev => prev.OrderIndex).Select(prev => prev.Title).FirstOrDefault(),
                    HasQuiz = false, // Dynamic
                    Transcript = null, // Needs to be loaded from somewhere or part of content
                    PositionSeconds = 0 // Dynamic
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CourseDetailVm> CreateCourse(CourseCreateUpdateDto courseDto)
        {
            var course = new Course
            {
                Title = courseDto.Title,
                Slug = courseDto.Slug,
                Description = courseDto.Description,
                InstructorId = courseDto.InstructorId.ToString(),
                CategoryId = courseDto.CategoryId,
                Price = courseDto.Price,
                ThumbnailPath = courseDto.ThumbnailPath,
                CreatedAt = DateTime.UtcNow,
                IsPublished = courseDto.IsPublished // Changed to IsPublished
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Fetch the instructor to get their name
            var instructor = await _context.Users.FindAsync(course.InstructorId);

            // Return the newly created course as a CourseDetailVm
            return new CourseDetailVm
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description,
                InstructorName = instructor?.DisplayName ?? "Unknown Instructor",
                ThumbnailPath = course.ThumbnailPath,
                Price = course.Price,
                IsEnrolled = false,
                Modules = new List<ModuleVm>()
            };
        }
    }
}