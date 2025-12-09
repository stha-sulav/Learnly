using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // For IWebHostEnvironment
using System.IO; // For Path.Combine
using System; // For Guid.NewGuid

namespace Learnly.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountService(UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<ManageAccountViewModel> GetAccountInfoAsync(ApplicationUser user)
        {
            var model = new ManageAccountViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfilePicturePath = user.ProfilePicturePath
            };
            return await Task.FromResult(model);
        }

        public async Task<bool> UpdateAccountInfoAsync(ApplicationUser user, ManageAccountViewModel model)
        {
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;

            if (model.ProfilePictureFile != null)
            {
                user.ProfilePicturePath = await SaveProfilePictureAsync(model.ProfilePictureFile);
            }

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        private async Task<string> SaveProfilePictureAsync(IFormFile profilePicture)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "profilepictures");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePicture.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(fileStream);
            }

            return "/profilepictures/" + uniqueFileName;
        }
    }
}
