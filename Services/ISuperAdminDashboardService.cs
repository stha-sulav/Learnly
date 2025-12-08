using Learnly.ViewModels;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface ISuperAdminDashboardService
    {
        Task<SuperAdminDashboardViewModel> GetSuperAdminDashboardViewModel();
    }
}


