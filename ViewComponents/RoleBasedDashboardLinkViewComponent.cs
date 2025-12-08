using Learnly.Constants;
using Learnly.Models;
using Learnly.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Learnly.ViewComponents
{
    public class RoleBasedDashboardLinkViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleBasedDashboardLinkViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                // Not logged in, redirect to Home
                return View("Default", "/");
            }

            if (User.IsInRole(Roles.Admin))
            {
                return View("Default", "/Admin");
            }
            else if (User.IsInRole(Roles.Instructor))
            {
                return View("Default", "/Instructor");
            }
            else if (User.IsInRole(Roles.User))
            {
                return View("Default", "/"); // Default user goes to Home
            }

            // Fallback for any other case or if roles not found
            return View("Default", "/");
        }
    }
}
