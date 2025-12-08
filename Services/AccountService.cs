using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ManageAccountViewModel> GetAccountInfoAsync(ApplicationUser user)
        {
            var model = new ManageAccountViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };
            return await Task.FromResult(model);
        }

        public async Task<bool> UpdateAccountInfoAsync(ApplicationUser user, ManageAccountViewModel model)
        {
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }
}
