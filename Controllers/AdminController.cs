using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Learnly.Services; // Added for IAdminService
using Learnly.Constants;

namespace Learnly.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAdminService _adminService; // Injected IAdminService
        private readonly IAccountService _accountService; // Injected IAccountService

        public AdminController(UserManager<ApplicationUser> userManager, IAdminService adminService, IAccountService accountService) // Added IAdminService and IAccountService to constructor
        {
            _userManager = userManager;
            _adminService = adminService;
            _accountService = accountService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _adminService.GetDashboardStats(); // Use the service to get the model
            return View(model);
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var usersWithRoles = new List<UserWithRolesViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new UserWithRolesViewModel
                {
                    User = user,
                    Roles = roles
                });
            }
            return View(usersWithRoles);
        }

        // GET: Admin/EditUser/{id}
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = await _accountService.GetAccountInfoAsync(user);
            // Optionally, add roles to the model if you want to manage them here
            return View(model);
        }

        // POST: Admin/EditUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, ManageAccountViewModel model)
        {
            if (id != model.Email) // Assuming Email is unique and used as ID for simplicity here, though it's better to use actual User.Id
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound();
            }

            var success = await _accountService.UpdateAccountInfoAsync(user, model);
            if (success)
            {
                TempData["StatusMessage"] = "User profile updated successfully.";
                return RedirectToAction(nameof(Users));
            }

            ModelState.AddModelError(string.Empty, "An error occurred while updating the user profile.");
            return View(model);
        }

        // POST: Admin/UpdateUserStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserStatus(string userId, Learnly.Models.Enums.UserStatus status)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var success = await _adminService.UpdateUserStatusAsync(userId, status);

            if (success)
            {
                TempData["StatusMessage"] = $"User status updated to {status.ToString()}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error updating user status.";
            }

            return RedirectToAction(nameof(Users));
        }
    }
}

