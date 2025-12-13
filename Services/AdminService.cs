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
    }
}
