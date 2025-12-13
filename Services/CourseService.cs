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
                    ProgressPercent = null, // This would require user-specific enrollment data, not covered in public summaries
                    IsPublished = c.IsPublished
                })
                .ToListAsync();
        }

        public async Task<CourseDetailVm?> GetCourseWithCurriculum(string slug, string? userId)
        {
            var courseQuery = _context.Courses
                .Where(c => c.Slug == slug)
                .Include(c => c.Instructor)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .AsQueryable();

            var course = await courseQuery.FirstOrDefaultAsync();

            if (course == null)
            {
                return null;
            }

            // Check enrollment status if userId is provided
            bool isEnrolled = false;
            if (!string.IsNullOrEmpty(userId))
            {
                isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == course.Id && e.UserId == userId);
            }

            // Get user's completed lessons for this course if enrolled
            var completedLessonIds = new HashSet<int>();
            if (isEnrolled && !string.IsNullOrEmpty(userId))
            {
                completedLessonIds = await _context.LessonProgresses
                    .Where(lp => lp.UserId == userId && lp.IsCompleted)
                    .Join(_context.Lessons.Where(l => l.Module!.CourseId == course.Id),
                        lp => lp.LessonId,
                        l => l.Id,
                        (lp, l) => l.Id)
                    .ToHashSetAsync();
            }

            return new CourseDetailVm
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description ?? string.Empty,
                InstructorName = course.Instructor?.DisplayName ?? "Unknown Instructor",
                ThumbnailPath = course.ThumbnailPath ?? string.Empty,
                Price = course.Price,
                IsEnrolled = isEnrolled,
                Modules = course.Modules.OrderBy(m => m.OrderIndex).Select(m => new ModuleVm
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
                        IsCompleted = completedLessonIds.Contains(l.Id) // Set IsCompleted dynamically
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<LessonDetailVm?> GetLessonDetailsById(int lessonId, string? userId)
        {
            var lessonQuery = _context.Lessons
                .Where(l => l.Id == lessonId)
                .Include(l => l.Module)
                    .ThenInclude(m => m.Course)
                .AsQueryable();

            var lesson = await lessonQuery.FirstOrDefaultAsync();

            if (lesson == null)
            {
                return null;
            }

            int positionSeconds = 0;
            bool isCompleted = false;

            if (!string.IsNullOrEmpty(userId))
            {
                var lessonProgress = await _context.LessonProgresses
                    .FirstOrDefaultAsync(lp => lp.LessonId == lessonId && lp.UserId == userId);

                if (lessonProgress != null)
                {
                    positionSeconds = lessonProgress.PositionSeconds;
                    isCompleted = lessonProgress.IsCompleted;
                }
            }

            return new LessonDetailVm
            {
                Id = lesson.Id,
                Title = lesson.Title,
                CourseId = lesson.Module!.Course!.Id,
                CourseTitle = lesson.Module!.Course!.Title,
                CourseSlug = lesson.Module!.Course!.Slug,
                ModuleId = lesson.ModuleId,
                ModuleTitle = lesson.Module!.Title,
                ContentType = lesson.ContentType,
                ContentPath = lesson.Content ?? string.Empty, // Changed to Content
                DurationSeconds = lesson.DurationSeconds,
                IsCompleted = isCompleted, // Dynamic
                // These would require more complex queries for prev/next and user progress
                NextLessonId = (int?)_context.Lessons.Where(next => next.ModuleId == lesson.ModuleId && next.OrderIndex > lesson.OrderIndex).OrderBy(next => next.OrderIndex).Select(next => next.Id).FirstOrDefault(),
                NextLessonTitle = _context.Lessons.Where(next => next.ModuleId == lesson.ModuleId && next.OrderIndex > lesson.OrderIndex).OrderBy(next => next.OrderIndex).Select(next => next.Title).FirstOrDefault(),
                PreviousLessonId = (int?)_context.Lessons.Where(prev => prev.ModuleId == lesson.ModuleId && prev.OrderIndex < lesson.OrderIndex).OrderByDescending(prev => prev.OrderIndex).Select(prev => prev.Id).FirstOrDefault(),
                PreviousLessonTitle = _context.Lessons.Where(prev => prev.ModuleId == lesson.ModuleId && prev.OrderIndex < lesson.OrderIndex).OrderByDescending(prev => prev.OrderIndex).Select(prev => prev.Title).FirstOrDefault(),
                HasQuiz = false, // Dynamic
                Transcript = null, // Needs to be loaded from somewhere or part of content
                PositionSeconds = positionSeconds // Dynamic
            };
        }

        public async Task<CourseDetailVm> CreateCourseAsync(CourseCreateUpdateDto courseDto)
        {
            // Check for slug uniqueness
            if (await _context.Courses.AnyAsync(c => c.Slug == courseDto.Slug))
            {
                throw new InvalidOperationException($"A course with the slug '{courseDto.Slug}' already exists.");
            }

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
                IsPublished = courseDto.IsPublished
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var instructor = await _context.Users.FindAsync(course.InstructorId);

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

        public async Task<CourseCreateUpdateDto?> GetCourseForEditAsync(int courseId)
        {
            return await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => new CourseCreateUpdateDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Slug = c.Slug,
                    Description = c.Description,
                    Price = c.Price,
                    CategoryId = c.CategoryId,
                    ThumbnailPath = c.ThumbnailPath,
                    IsPublished = c.IsPublished,
                    InstructorId = c.InstructorId // Assuming InstructorId is needed for authorization checks
                })
                .FirstOrDefaultAsync();
        }

        public async Task UpdateCourseAsync(CourseCreateUpdateDto courseDto)
        {
            var course = await _context.Courses.FindAsync(courseDto.Id);

            if (course == null)
            {
                // Handle case where course is not found, e.g., throw exception or return error
                throw new KeyNotFoundException($"Course with ID {courseDto.Id} not found.");
            }

            // Check for slug uniqueness if it's being changed
            if (course.Slug != courseDto.Slug && await _context.Courses.AnyAsync(c => c.Slug == courseDto.Slug && c.Id != courseDto.Id))
            {
                throw new InvalidOperationException($"A course with the slug '{courseDto.Slug}' already exists.");
            }

            course.Title = courseDto.Title;
            course.Slug = courseDto.Slug;
            course.Description = courseDto.Description;
            course.Price = courseDto.Price;
            course.CategoryId = courseDto.CategoryId;
            course.ThumbnailPath = courseDto.ThumbnailPath;
            course.IsPublished = courseDto.IsPublished;

            _context.Courses.Update(course);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsUserEnrolledAsync(int courseId, string userId)
        {
            return await _context.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == userId);
        }

        public async Task<CourseSummaryVm?> GetCourseByIdAsync(int courseId) // Return type nullable
        {
            return await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => new CourseSummaryVm
                {
                    Id = c.Id,
                    Title = c.Title,
                    Slug = c.Slug,
                    Price = c.Price,
                    ThumbnailPath = c.ThumbnailPath ?? string.Empty,
                    InstructorName = c.Instructor!.DisplayName ?? "Unknown Instructor", // Assuming Instructor is loaded or joined
                    ShortDescription = c.Description != null && c.Description.Length > 150 ? c.Description.Substring(0, 150) + "..." : c.Description ?? string.Empty,
                    ProgressPercent = null, // This would require user-specific enrollment data, not covered in public summaries
                    IsPublished = c.IsPublished
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CourseDashboardVm>> GetUserEnrolledCoursesAsync(string userId)
        {
            var enrolledCourses = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Modules)
                        .ThenInclude(m => m.Lessons)
                .Select(e => e.Course)
                .ToListAsync();

            var dashboardCourses = new List<CourseDashboardVm>();

            foreach (var course in enrolledCourses)
            {
                var totalLessonsInCourse = course.Modules.SelectMany(m => m.Lessons).Count();
                
                var completedLessons = await _context.LessonProgresses
                    .Where(lp => lp.UserId == userId && lp.IsCompleted)
                    .Join(_context.Lessons.Where(l => l.Module!.CourseId == course.Id),
                        lp => lp.LessonId,
                        l => l.Id,
                        (lp, l) => l.Id)
                    .ToListAsync();

                var completedLessonsCount = completedLessons.Distinct().Count();
                var progressPercent = totalLessonsInCourse > 0 ? (int)Math.Round((double)completedLessonsCount / totalLessonsInCourse * 100) : 0;

                int? firstIncompleteLessonId = null;
                string? firstIncompleteLessonTitle = null;

                var allLessonsInCourse = course.Modules
                    .SelectMany(m => m.Lessons)
                    .OrderBy(l => l.Module!.OrderIndex)
                    .ThenBy(l => l.OrderIndex)
                    .ToList();

                foreach (var lesson in allLessonsInCourse)
                {
                    if (!completedLessons.Contains(lesson.Id))
                    {
                        firstIncompleteLessonId = lesson.Id;
                        firstIncompleteLessonTitle = lesson.Title;
                        break;
                    }
                }
                
                // If all lessons are completed, link to the last lesson or course details
                if (firstIncompleteLessonId == null && allLessonsInCourse.Any())
                {
                    firstIncompleteLessonId = allLessonsInCourse.Last().Id;
                    firstIncompleteLessonTitle = allLessonsInCourse.Last().Title;
                }


                dashboardCourses.Add(new CourseDashboardVm
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    Slug = course.Slug,
                    ThumbnailPath = course.ThumbnailPath ?? string.Empty,
                    ProgressPercent = progressPercent,
                    FirstIncompleteLessonId = firstIncompleteLessonId,
                    FirstIncompleteLessonTitle = firstIncompleteLessonTitle
                });
            }

            return dashboardCourses;
        }

        public async Task<IEnumerable<CourseDashboardVm>> GetDashboardCoursesWithProgressAsync(string userId)
        {
            return await GetUserEnrolledCoursesAsync(userId);
        }

        public async Task<IEnumerable<CourseSummaryVm>> GetInstructorCourseSummaries(string instructorId)
        {
            return await _context.Courses
                .Where(c => c.InstructorId == instructorId)
                .Select(c => new CourseSummaryVm
                {
                    Id = c.Id,
                    Title = c.Title,
                    Slug = c.Slug,
                    ThumbnailPath = c.ThumbnailPath ?? string.Empty,
                    InstructorName = c.Instructor!.DisplayName ?? "Unknown Instructor",
                    ShortDescription = c.Description != null && c.Description.Length > 150 ? c.Description.Substring(0, 150) + "..." : c.Description ?? string.Empty,
                    Price = c.Price,
                    ProgressPercent = null, // Not applicable for instructor view
                    IsPublished = c.IsPublished
                })
                .ToListAsync();
        }
    }
}