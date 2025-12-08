using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Learnly.Services; // Added for IAdminService

namespace Learnly.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAdminService _adminService; // Injected IAdminService

        public AdminController(UserManager<ApplicationUser> userManager, IAdminService adminService) // Added IAdminService to constructor
        {
            _userManager = userManager;
            _adminService = adminService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _adminService.GetDashboardStats(); // Use the service to get the model
            return View(model);
        }
    }
}
