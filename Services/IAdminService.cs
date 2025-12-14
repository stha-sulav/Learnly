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
        Task<bool> DeleteUserAsync(string userId);

        // Category CRUD
        Task<IEnumerable<CategoryViewModel>> GetAllCategoriesAsync();
        Task<CategoryViewModel?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(CategoryViewModel model);
        Task<bool> UpdateCategoryAsync(CategoryViewModel model);
        Task<bool> DeleteCategoryAsync(int id);
    }
}
