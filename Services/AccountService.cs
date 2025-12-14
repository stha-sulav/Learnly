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
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfilePicturePath = user.ProfilePicturePath
            };
            return await Task.FromResult(model);
        }

        public async Task<bool> UpdateAccountInfoAsync(ApplicationUser user, ManageAccountViewModel model)
        {
            bool isUpdated = false;

            if (user.FirstName != model.FirstName)
            {
                user.FirstName = model.FirstName;
                isUpdated = true;
            }

            if (user.LastName != model.LastName)
            {
                user.LastName = model.LastName;
                isUpdated = true;
            }

            if (user.Email != model.Email)
            {
                user.Email = model.Email;
                isUpdated = true;
            }


            if (model.ProfilePictureFile != null)
            {
                user.ProfilePicturePath = await SaveProfilePictureAsync(model.ProfilePictureFile);
                isUpdated = true;
            }

            if(isUpdated)
            {
                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }

            return true;
        }

        public async Task<bool> RemoveProfilePictureAsync(ApplicationUser user)
        {
            if (!string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            user.ProfilePicturePath = null;
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
