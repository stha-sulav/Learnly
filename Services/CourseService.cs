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
                    InstructorName = c.Instructor!.DisplayName ?? "Unknown Instructor",
                    ShortDescription = c.Description != null && c.Description.Length > 150 ? c.Description.Substring(0, 150) + "..." : c.Description ?? string.Empty,
                    ProgressPercent = null,
                    IsPublished = c.IsPublished,
                    // Additional details
                    ModuleCount = c.Modules.Count,
                    LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(),
                    EnrolledStudents = c.Enrollments.Count,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    CreatedAt = c.CreatedAt
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CourseSummaryVm>> GetAvailableCoursesForUser(string? userId)
        {
            // Get list of course IDs the user is enrolled in
            var enrolledCourseIds = new List<int>();
            if (!string.IsNullOrEmpty(userId))
            {
                enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.UserId == userId)
                    .Select(e => e.CourseId)
                    .ToListAsync();
            }

            return await _context.Courses
                .Where(c => c.IsPublished && !enrolledCourseIds.Contains(c.Id))
                .Select(c => new CourseSummaryVm
                {
                    Id = c.Id,
                    Title = c.Title,
                    Slug = c.Slug,
                    ThumbnailPath = c.ThumbnailPath ?? string.Empty,
                    InstructorName = c.Instructor!.DisplayName ?? "Unknown Instructor",
                    ShortDescription = c.Description != null && c.Description.Length > 150 ? c.Description.Substring(0, 150) + "..." : c.Description ?? string.Empty,
                    ProgressPercent = null,
                    IsPublished = c.IsPublished,
                    ModuleCount = c.Modules.Count,
                    LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(),
                    EnrolledStudents = c.Enrollments.Count,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    CreatedAt = c.CreatedAt
                })
                .OrderByDescending(c => c.CreatedAt)
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
                IsEnrolled = isEnrolled,
                Modules = course.Modules.OrderBy(m => m.OrderIndex).Select(m => new ModuleVm
                {
                    Id = m.Id,
                    Title = m.Title,
                    Order = m.OrderIndex,
                    ThumbnailPath = m.ThumbnailPath,
                    Lessons = m.Lessons.OrderBy(l => l.OrderIndex).Select(l => new LessonVm
                    {
                        Id = l.Id,
                        Title = l.Title,
                        ContentType = l.ContentType,
                        DurationSeconds = l.DurationSeconds,
                        Order = l.OrderIndex,
                        IsCompleted = completedLessonIds.Contains(l.Id), // Set IsCompleted dynamically
                        ThumbnailPath = l.ThumbnailPath
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
            // Auto-generate slug from title
            var slug = GenerateSlug(courseDto.Title);

            // Ensure slug uniqueness by appending a number if needed
            var baseSlug = slug;
            var counter = 1;
            while (await _context.Courses.AnyAsync(c => c.Slug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            var course = new Course
            {
                Title = courseDto.Title,
                Slug = slug,
                Description = courseDto.Description,
                InstructorId = courseDto.InstructorId.ToString(),
                CategoryId = courseDto.CategoryId,
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
                    Description = c.Description,
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
                throw new KeyNotFoundException($"Course with ID {courseDto.Id} not found.");
            }

            // Keep existing slug - don't change URLs after course is created
            course.Title = courseDto.Title;
            course.Description = courseDto.Description;
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

        public async Task<CourseSummaryVm?> GetCourseByIdAsync(int courseId)
        {
            return await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => new CourseSummaryVm
                {
                    Id = c.Id,
                    Title = c.Title,
                    Slug = c.Slug,
                    ThumbnailPath = c.ThumbnailPath ?? string.Empty,
                    InstructorName = c.Instructor!.DisplayName ?? "Unknown Instructor",
                    ShortDescription = c.Description != null && c.Description.Length > 150 ? c.Description.Substring(0, 150) + "..." : c.Description ?? string.Empty,
                    ProgressPercent = null,
                    IsPublished = c.IsPublished,
                    // Additional details
                    ModuleCount = c.Modules.Count,
                    LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(),
                    EnrolledStudents = c.Enrollments.Count,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    CreatedAt = c.CreatedAt
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
                    ProgressPercent = null, // Not applicable for instructor view
                    IsPublished = c.IsPublished,
                    // Additional details
                    ModuleCount = c.Modules.Count,
                    LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(),
                    EnrolledStudents = c.Enrollments.Count,
                    CategoryName = c.Category != null ? c.Category.Name : null,
                    CreatedAt = c.CreatedAt
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        private static string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            // Convert to lowercase, remove special characters, replace spaces with hyphens
            var slug = title.ToLowerInvariant().Trim();

            // Remove special characters (keep only letters, numbers, spaces, and hyphens)
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^\w\s-]", "");

            // Replace spaces with hyphens
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");

            // Replace multiple hyphens with single hyphen
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

            // Trim hyphens from start and end
            slug = slug.Trim('-');

            return slug;
        }
    }
}