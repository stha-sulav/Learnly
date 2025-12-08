using Learnly.Data;
using Learnly.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public class SuperAdminDashboardService : ISuperAdminDashboardService
    {
        private readonly ApplicationDbContext _context;

        public SuperAdminDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SuperAdminDashboardViewModel> GetSuperAdminDashboardViewModel()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalCourses = await _context.Courses.CountAsync();
            var totalEnrollments = await _context.Enrollments.CountAsync();

            var model = new SuperAdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalCourses = totalCourses,
                TotalEnrollments = totalEnrollments
            };

            return model;
        }
    }
}
