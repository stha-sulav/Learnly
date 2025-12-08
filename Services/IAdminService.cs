using Learnly.ViewModels;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardStats();
    }
}
