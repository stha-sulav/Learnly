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
        public async Task<IActionResult> Users(string? searchTerm, string? status, string? role, string? sortBy)
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

            // Apply filters
            var filtered = usersWithRoles.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filtered = filtered.Where(u =>
                    (u.User.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.User.FirstName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.User.LastName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Models.Enums.UserStatus>(status, out var userStatus))
            {
                filtered = filtered.Where(u => u.User.Status == userStatus);
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                filtered = filtered.Where(u => u.Roles.Contains(role));
            }

            // Apply sorting
            filtered = sortBy switch
            {
                "email_asc" => filtered.OrderBy(u => u.User.Email),
                "email_desc" => filtered.OrderByDescending(u => u.User.Email),
                "name_asc" => filtered.OrderBy(u => u.User.FirstName).ThenBy(u => u.User.LastName),
                "name_desc" => filtered.OrderByDescending(u => u.User.FirstName).ThenByDescending(u => u.User.LastName),
                "status" => filtered.OrderBy(u => u.User.Status),
                _ => filtered.OrderBy(u => u.User.Email)
            };

            // Pass filter values to view
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.Role = role;
            ViewBag.SortBy = sortBy;
            ViewBag.TotalCount = usersWithRoles.Count;

            return View(filtered.ToList());
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

            // Prevent editing admin users
            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                TempData["ErrorMessage"] = "Admin users cannot be edited.";
                return RedirectToAction(nameof(Users));
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

            // Prevent editing admin users
            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                TempData["ErrorMessage"] = "Admin users cannot be edited.";
                return RedirectToAction(nameof(Users));
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

            // Prevent changing status of admin users
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                TempData["ErrorMessage"] = "Admin users cannot be blocked or have their status changed.";
                return RedirectToAction(nameof(Users));
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

        // POST: Admin/DeleteUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting admin users
            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                TempData["ErrorMessage"] = "Admin users cannot be deleted.";
                return RedirectToAction(nameof(Users));
            }

            var success = await _adminService.DeleteUserAsync(id);

            if (success)
            {
                TempData["StatusMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting user.";
            }

            return RedirectToAction(nameof(Users));
        }

        #region Category CRUD

        // GET: Admin/Categories
        public async Task<IActionResult> Categories()
        {
            var categories = await _adminService.GetAllCategoriesAsync();
            return View(categories);
        }

        // GET: Admin/CreateCategory
        [HttpGet]
        public async Task<IActionResult> CreateCategory()
        {
            ViewBag.Categories = await _adminService.GetCategoriesAsync();
            return View(new CategoryViewModel());
        }

        // POST: Admin/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _adminService.GetCategoriesAsync();
                return View(model);
            }

            await _adminService.CreateCategoryAsync(model);
            TempData["StatusMessage"] = "Category created successfully.";
            return RedirectToAction(nameof(Categories));
        }

        // GET: Admin/EditCategory/{id}
        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _adminService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            ViewBag.Categories = (await _adminService.GetCategoriesAsync())
                .Where(c => c.Id != id); // Exclude current category from parent options
            return View(category);
        }

        // POST: Admin/EditCategory/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, CategoryViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = (await _adminService.GetCategoriesAsync())
                    .Where(c => c.Id != id);
                return View(model);
            }

            var success = await _adminService.UpdateCategoryAsync(model);
            if (success)
            {
                TempData["StatusMessage"] = "Category updated successfully.";
                return RedirectToAction(nameof(Categories));
            }

            TempData["ErrorMessage"] = "Error updating category.";
            ViewBag.Categories = (await _adminService.GetCategoriesAsync())
                .Where(c => c.Id != id);
            return View(model);
        }

        // GET: Admin/DeleteCategory/{id}
        [HttpGet]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _adminService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Admin/DeleteCategory/{id}
        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(int id)
        {
            var success = await _adminService.DeleteCategoryAsync(id);
            if (success)
            {
                TempData["StatusMessage"] = "Category deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Cannot delete category. It may have courses or subcategories associated with it.";
            }

            return RedirectToAction(nameof(Categories));
        }

        #endregion
    }
}

