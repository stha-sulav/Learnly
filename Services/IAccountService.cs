using Learnly.Models;
using Learnly.ViewModels;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface IAccountService
    {
        Task<ManageAccountViewModel> GetAccountInfoAsync(ApplicationUser user);
        Task<bool> UpdateAccountInfoAsync(ApplicationUser user, ManageAccountViewModel model);
        Task<bool> RemoveProfilePictureAsync(ApplicationUser user);
    }
}
