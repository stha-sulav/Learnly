using Learnly.Data;
using Learnly.Hubs;
using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<NotificationHub> _notificationHub;

        public CourseService(ApplicationDbContext context, IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _notificationHub = notificationHub;
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
                    CreatedAt = c.CreatedAt,
                    // Rating information
                    AverageRating = c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0,
                    TotalReviews = c.Reviews.Count
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
                    CreatedAt = c.CreatedAt,
                    // Rating information
                    AverageRating = c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0,
                    TotalReviews = c.Reviews.Count
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

        public async Task<CourseDetailVm?> GetCourseWithCurriculumById(int courseId, string? userId)
        {
            var course = await _context.Courses
                .Where(c => c.Id == courseId)
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync();

            if (course == null)
            {
                return null;
            }

            // Check enrollment status if userId is provided
            bool isEnrolled = false;
            if (!string.IsNullOrEmpty(userId))
            {
                isEnrolled = course.Enrollments.Any(e => e.UserId == userId);
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

            // Calculate totals
            var allLessons = course.Modules.SelectMany(m => m.Lessons).ToList();
            var totalDuration = allLessons.Sum(l => l.DurationSeconds);

            // Get instructor name with fallbacks
            var instructorName = course.Instructor?.DisplayName
                ?? (course.Instructor != null && !string.IsNullOrEmpty(course.Instructor.FirstName)
                    ? $"{course.Instructor.FirstName} {course.Instructor.LastName}".Trim()
                    : course.Instructor?.UserName ?? "Unknown Instructor");

            return new CourseDetailVm
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description ?? string.Empty,
                InstructorName = instructorName,
                ThumbnailPath = course.ThumbnailPath ?? string.Empty,
                IsEnrolled = isEnrolled,
                TotalModules = course.Modules.Count,
                TotalLessons = allLessons.Count,
                TotalDurationSeconds = totalDuration,
                EnrolledStudents = course.Enrollments.Count,
                CreatedAt = course.CreatedAt,
                CategoryName = course.Category?.Name,
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
                        IsCompleted = completedLessonIds.Contains(l.Id),
                        ThumbnailPath = l.ThumbnailPath
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<LessonWithCurriculumVm?> GetLessonWithCurriculum(int lessonId, string? userId)
        {
            var lesson = await _context.Lessons
                .Where(l => l.Id == lessonId)
                .Include(l => l.Module)
                    .ThenInclude(m => m.Course)
                        .ThenInclude(c => c.Modules)
                            .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync();

            if (lesson == null)
            {
                return null;
            }

            var course = lesson.Module!.Course!;

            // Get user's progress
            int positionSeconds = 0;
            bool isCompleted = false;
            var completedLessonIds = new HashSet<int>();

            if (!string.IsNullOrEmpty(userId))
            {
                var lessonProgress = await _context.LessonProgresses
                    .FirstOrDefaultAsync(lp => lp.LessonId == lessonId && lp.UserId == userId);

                if (lessonProgress != null)
                {
                    positionSeconds = lessonProgress.PositionSeconds;
                    isCompleted = lessonProgress.IsCompleted;
                }

                // Get all completed lessons for this course
                completedLessonIds = await _context.LessonProgresses
                    .Where(lp => lp.UserId == userId && lp.IsCompleted)
                    .Join(_context.Lessons.Where(l => l.Module!.CourseId == course.Id),
                        lp => lp.LessonId,
                        l => l.Id,
                        (lp, l) => l.Id)
                    .ToHashSetAsync();
            }

            // Get all lessons in order to find prev/next
            var allLessons = course.Modules
                .OrderBy(m => m.OrderIndex)
                .SelectMany(m => m.Lessons.OrderBy(l => l.OrderIndex))
                .ToList();

            var currentIndex = allLessons.FindIndex(l => l.Id == lessonId);
            var prevLesson = currentIndex > 0 ? allLessons[currentIndex - 1] : null;
            var nextLesson = currentIndex < allLessons.Count - 1 ? allLessons[currentIndex + 1] : null;

            return new LessonWithCurriculumVm
            {
                Id = lesson.Id,
                Title = lesson.Title,
                CourseId = course.Id,
                CourseTitle = course.Title,
                CourseSlug = course.Slug,
                ModuleId = lesson.ModuleId,
                ModuleTitle = lesson.Module!.Title,
                ContentType = lesson.ContentType,
                ContentPath = lesson.Content ?? string.Empty,
                DurationSeconds = lesson.DurationSeconds,
                IsCompleted = isCompleted,
                NextLessonId = nextLesson?.Id,
                NextLessonTitle = nextLesson?.Title,
                PreviousLessonId = prevLesson?.Id,
                PreviousLessonTitle = prevLesson?.Title,
                HasQuiz = false,
                Transcript = null,
                PositionSeconds = positionSeconds,
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
                        IsCompleted = completedLessonIds.Contains(l.Id),
                        ThumbnailPath = l.ThumbnailPath
                    }).ToList()
                }).ToList(),
                TotalLessons = allLessons.Count,
                CompletedLessons = completedLessonIds.Count
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
                .Include(e => e.Course)
                    .ThenInclude(c => c.Reviews)
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
                    FirstIncompleteLessonTitle = firstIncompleteLessonTitle,
                    // Rating information
                    AverageRating = course.Reviews.Any() ? course.Reviews.Average(r => r.Rating) : 0,
                    TotalReviews = course.Reviews.Count
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
                    CreatedAt = c.CreatedAt,
                    // Rating information
                    AverageRating = c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0,
                    TotalReviews = c.Reviews.Count
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CourseSummaryVm>> GetFeaturedCoursesAsync(int count)
        {
            return await _context.Courses
                .Where(c => c.IsPublished)
                .OrderByDescending(c => c.Enrollments.Count)
                .ThenByDescending(c => c.CreatedAt)
                .Take(count)
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
                    CreatedAt = c.CreatedAt,
                    // Rating information
                    AverageRating = c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0,
                    TotalReviews = c.Reviews.Count
                })
                .ToListAsync();
        }

        public async Task<PlatformStatsDto> GetPlatformStatsAsync()
        {
            var totalCourses = await _context.Courses.CountAsync(c => c.IsPublished);
            var totalStudents = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                .Join(_context.Roles.Where(r => r.Name == "User"), uur => uur.ur.RoleId, r => r.Id, (uur, r) => uur.u)
                .CountAsync();
            var totalInstructors = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                .Join(_context.Roles.Where(r => r.Name == "Instructor"), uur => uur.ur.RoleId, r => r.Id, (uur, r) => uur.u)
                .CountAsync();
            var totalLessons = await _context.Lessons.CountAsync();

            return new PlatformStatsDto
            {
                TotalCourses = totalCourses,
                TotalStudents = totalStudents,
                TotalInstructors = totalInstructors,
                TotalLessons = totalLessons
            };
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

        // Review methods
        public async Task<CourseReviewsVm> GetCourseReviewsAsync(int courseId, string? userId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.CourseId == courseId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewVm
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User.DisplayName ?? r.User.UserName ?? "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    IsCurrentUserReview = r.UserId == userId
                })
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            var currentUserReview = !string.IsNullOrEmpty(userId)
                ? reviews.FirstOrDefault(r => r.UserId == userId)
                : null;

            return new CourseReviewsVm
            {
                Reviews = reviews,
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = reviews.Count,
                CurrentUserReview = currentUserReview
            };
        }

        public async Task<ReviewVm> CreateReviewAsync(int courseId, string userId, int rating, string? comment)
        {
            // Check if user already has a review for this course
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId);

            if (existingReview != null)
            {
                throw new InvalidOperationException("You have already reviewed this course.");
            }

            // Get the course with instructor info for notification
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            var review = new Review
            {
                CourseId = courseId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            var reviewerName = user?.DisplayName ?? user?.UserName ?? "A student";

            // Send notification to the instructor
            if (course?.InstructorId != null && course.InstructorId != userId)
            {
                var notification = new Notification
                {
                    UserId = course.InstructorId,
                    Title = "New Course Review",
                    Body = $"{reviewerName} left a {rating}-star review on \"{course.Title}\"",
                    Url = $"/Courses/Details/{courseId}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time notification via SignalR
                await _notificationHub.Clients.User(course.InstructorId).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    body = notification.Body,
                    url = notification.Url,
                    createdAt = notification.CreatedAt
                });
            }

            return new ReviewVm
            {
                Id = review.Id,
                UserId = review.UserId,
                UserName = reviewerName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                IsCurrentUserReview = true
            };
        }

        public async Task<ReviewVm?> UpdateReviewAsync(int reviewId, string userId, int rating, string? comment)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
            {
                return null;
            }

            review.Rating = rating;
            review.Comment = comment;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ReviewVm
            {
                Id = review.Id,
                UserId = review.UserId,
                UserName = review.User?.DisplayName ?? review.User?.UserName ?? "Anonymous",
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                IsCurrentUserReview = true
            };
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, string userId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
            {
                return false;
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ReviewVm?> GetUserReviewForCourseAsync(int courseId, string userId)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId);

            if (review == null)
            {
                return null;
            }

            return new ReviewVm
            {
                Id = review.Id,
                UserId = review.UserId,
                UserName = review.User?.DisplayName ?? review.User?.UserName ?? "Anonymous",
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                IsCurrentUserReview = true
            };
        }
    }
}