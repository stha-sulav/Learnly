using Learnly.Constants;
using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Learnly.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(IAccountService accountService, UserManager<ApplicationUser> userManager)
        {
            _accountService = accountService;
            _userManager = userManager;
        }

        // GET: /Account/Manage
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = await _accountService.GetAccountInfoAsync(user);

            // Populate CancelReturnUrl based on user's role
            if (User.IsInRole(Roles.Admin))
            {
                model.CancelReturnUrl = Url.Action("Index", "Admin");
            }
            else if (User.IsInRole(Roles.Instructor))
            {
                model.CancelReturnUrl = Url.Action("Index", "Instructor");
            }
            else // Default for regular users or if no specific role matched
            {
                model.CancelReturnUrl = Url.Action("MyCourses", "Home"); // Assuming MyCourses is a common student landing
            }

            return View(model);
        }

        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageAccountViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Fetch original user data for comparison to detect profile changes
            var originalUserInfo = await _accountService.GetAccountInfoAsync(user);
            bool profileChangeAttempted = (model.FirstName != originalUserInfo.FirstName ||
                                           model.LastName != originalUserInfo.LastName ||
                                           model.ProfilePictureFile != null);

            // --- Password Change Logic ---
            bool passwordChangeAttempted = !string.IsNullOrEmpty(model.NewPassword);

            // OldPassword is required if either password change or profile change is attempted
            if (passwordChangeAttempted || profileChangeAttempted)
            {
                if (string.IsNullOrEmpty(model.OldPassword))
                {
                    ModelState.AddModelError("OldPassword", "Current password is required to save changes.");
                }
                // ModelState.IsValid will now check [Compare("NewPassword")] and [StringLength("NewPassword")]
            }

            if (!ModelState.IsValid) // Check for all validation, including password if attempted
            {
                // Re-populate CancelReturnUrl and other view data if validation fails
                model = await PopulateViewModelWithUserData(model, user);
                return View(model);
            }
            
            // --- Verify Old Password for any change ---
            if (passwordChangeAttempted || profileChangeAttempted)
            {
                var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.OldPassword);
                if (!isPasswordCorrect)
                {
                    ModelState.AddModelError("OldPassword", "Current password is not correct.");
                    model = await PopulateViewModelWithUserData(model, user);
                    return View(model);
                }
            }


            // --- Attempt Password Change ---
            if (passwordChangeAttempted)
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    // Re-populate CancelReturnUrl and other view data if password change fails
                    model = await PopulateViewModelWithUserData(model, user);
                    return View(model);
                }
                TempData["StatusMessage"] = "Your password has been changed.";
            }

            // --- Profile Info Update Logic ---
            // Only attempt profile update if profile changes were detected
            if (profileChangeAttempted)
            {
                var profileUpdateSuccess = await _accountService.UpdateAccountInfoAsync(user, model);
                if (!profileUpdateSuccess) // If UpdateAccountInfoAsync failed (e.g. for profile picture save), add model error.
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while updating your profile.");
                }
                // Set generic profile update message only if no password message was set and profile update was successful
                if (profileUpdateSuccess && string.IsNullOrEmpty(TempData["StatusMessage"]?.ToString()))
                {
                    TempData["StatusMessage"] = "Your profile has been updated";
                }
            }
            
            if (!ModelState.IsValid) // Final check after all operations (including UpdateAccountInfoAsync failures)
            {
                // Re-populate CancelReturnUrl and other view data if final validation/operation fails
                model = await PopulateViewModelWithUserData(model, user);
                return View(model);
            }

            return RedirectToAction(nameof(Manage));
        }

        // Helper method to populate ViewModel data, avoiding duplication
        private async Task<ManageAccountViewModel> PopulateViewModelWithUserData(ManageAccountViewModel model, ApplicationUser user)
        {
            // Populate CancelReturnUrl
            if (User.IsInRole(Roles.Admin))
            {
                model.CancelReturnUrl = Url.Action("Index", "Admin");
            }
            else if (User.IsInRole(Roles.Instructor))
            {
                model.CancelReturnUrl = Url.Action("Index", "Instructor");
            }
            else
            {
                model.CancelReturnUrl = Url.Action("MyCourses", "Home");
            }

            // Always ensure ProfilePicturePath is up-to-date, especially after a potential profile update
            var userInfo = await _accountService.GetAccountInfoAsync(user);
            model.ProfilePicturePath = userInfo.ProfilePicturePath;
            
            // Other fields (FirstName, LastName, Email, Passwords) should retain their POSTed values if validation fails,
            // as they are typically re-rendered by the Html Helpers using ModelState values.
            // Email is read-only, so its POSTed value will be its original value.

            return model;
        }
    }
}
