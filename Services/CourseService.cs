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
                .Where(c => c.Published)
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
                    Modules = c.Modules.OrderBy(m => m.Order).Select(m => new ModuleVm
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Order = m.Order,
                        Lessons = m.Lessons.OrderBy(l => l.Order).Select(l => new LessonVm
                        {
                            Id = l.Id,
                            Title = l.Title,
                            Slug = l.Slug,
                            ContentType = l.ContentType,
                            DurationSeconds = l.DurationSeconds,
                            Order = l.Order,
                            IsCompleted = false // This would be dynamic based on user progress
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<LessonDetailVm?> GetLessonDetailsBySlug(string slug)
        {
            return await _context.Lessons
                .Where(l => l.Slug == slug)
                .Select(l => new LessonDetailVm
                {
                    Id = l.Id,
                    Title = l.Title,
                    Slug = l.Slug,
                    CourseId = l.Module!.Course!.Id,
                    CourseTitle = l.Module!.Course!.Title,
                    CourseSlug = l.Module!.Course!.Slug,
                    ModuleId = l.ModuleId,
                    ModuleTitle = l.Module!.Title,
                    ContentType = l.ContentType,
                    ContentPath = l.ContentPath ?? string.Empty,
                    DurationSeconds = l.DurationSeconds,
                    IsCompleted = false, // Dynamic
                    // These would require more complex queries for prev/next and user progress
                    NextLessonId = (int?)_context.Lessons.Where(next => next.ModuleId == l.ModuleId && next.Order > l.Order).OrderBy(next => next.Order).Select(next => next.Id).FirstOrDefault(),
                    NextLessonSlug = _context.Lessons.Where(next => next.ModuleId == l.ModuleId && next.Order > l.Order).OrderBy(next => next.Order).Select(next => next.Slug).FirstOrDefault(),
                    NextLessonTitle = _context.Lessons.Where(next => next.ModuleId == l.ModuleId && next.Order > l.Order).OrderBy(next => next.Order).Select(next => next.Title).FirstOrDefault(),
                    PreviousLessonId = (int?)_context.Lessons.Where(prev => prev.ModuleId == l.ModuleId && prev.Order < l.Order).OrderByDescending(prev => prev.Order).Select(prev => prev.Id).FirstOrDefault(),
                    PreviousLessonSlug = _context.Lessons.Where(prev => prev.ModuleId == l.ModuleId && prev.Order < l.Order).OrderByDescending(prev => prev.Order).Select(prev => prev.Slug).FirstOrDefault(),
                    PreviousLessonTitle = _context.Lessons.Where(prev => prev.ModuleId == l.ModuleId && prev.Order < l.Order).OrderByDescending(prev => prev.Order).Select(prev => prev.Title).FirstOrDefault(),
                    HasQuiz = false, // Dynamic
                    Transcript = null, // Needs to be loaded from somewhere or part of content
                    PositionSeconds = 0 // Dynamic
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CourseDetailVm> CreateCourse(CreateCourseDto courseDto)
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
                Published = courseDto.Published
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