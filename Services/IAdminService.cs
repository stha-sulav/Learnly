using System.Collections.Generic; // Added for IEnumerable
using Learnly.Models; // Added for Category
using Learnly.ViewModels;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardStats();
        Task<IEnumerable<Category>> GetCategoriesAsync(); // New method
        Task<bool> UpdateUserStatusAsync(string userId, Models.Enums.UserStatus status);
    }
}
