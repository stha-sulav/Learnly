using Learnly.Data;
using Learnly.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity; // Added for UserManager
using Learnly.Models; // Added for ApplicationUser
using Learnly.Constants; // Added for Roles

namespace Learnly.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<AdminDashboardViewModel> GetDashboardStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalCourses = await _context.Courses.CountAsync();
            var flaggedComments = await _context.Comments.CountAsync(c => c.IsFlagged);

            var totalInstructors = (await _userManager.GetUsersInRoleAsync(Roles.Instructor)).Count;
            var totalStudents = (await _userManager.GetUsersInRoleAsync(Roles.User)).Count;
            var totalEnrollments = await _context.Enrollments.CountAsync(); // Added

            return new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalCourses = totalCourses,
                FlaggedComments = flaggedComments,
                TotalInstructors = totalInstructors,
                TotalStudents = totalStudents,
                TotalEnrollments = totalEnrollments // Added
            };
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<bool> UpdateUserStatusAsync(string userId, Models.Enums.UserStatus status)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.Status = status;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Delete related data first
            var enrollments = _context.Enrollments.Where(e => e.UserId == userId);
            _context.Enrollments.RemoveRange(enrollments);

            var lessonProgress = _context.LessonProgresses.Where(lp => lp.UserId == userId);
            _context.LessonProgresses.RemoveRange(lessonProgress);

            var comments = _context.Comments.Where(c => c.UserId == userId);
            _context.Comments.RemoveRange(comments);

            var commentLikes = _context.CommentLikes.Where(cl => cl.UserId == userId);
            _context.CommentLikes.RemoveRange(commentLikes);

            var notifications = _context.Notifications.Where(n => n.UserId == userId);
            _context.Notifications.RemoveRange(notifications);

            var quizAttempts = _context.Attempts.Where(a => a.UserId == userId);
            _context.Attempts.RemoveRange(quizAttempts);

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        // Category CRUD methods
        public async Task<IEnumerable<CategoryViewModel>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.Courses)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ParentCategoryId = c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                    CourseCount = c.Courses.Count
                })
                .ToListAsync();
        }

        public async Task<CategoryViewModel?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.Courses)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return null;

            return new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryName = category.ParentCategory?.Name,
                CourseCount = category.Courses.Count
            };
        }

        public async Task<Category> CreateCategoryAsync(CategoryViewModel model)
        {
            var category = new Category
            {
                Name = model.Name,
                Description = model.Description,
                ParentCategoryId = model.ParentCategoryId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> UpdateCategoryAsync(CategoryViewModel model)
        {
            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null)
                return false;

            category.Name = model.Name;
            category.Description = model.Description;
            category.ParentCategoryId = model.ParentCategoryId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Courses)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return false;

            // Don't delete if category has courses
            if (category.Courses.Any())
                return false;

            // Don't delete if category has subcategories
            if (category.SubCategories.Any())
                return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
