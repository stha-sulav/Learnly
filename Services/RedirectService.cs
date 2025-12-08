using Learnly.Constants;
using Learnly.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public class RedirectService : IRedirectService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ILogger<RedirectService> _logger;

        public RedirectService(
            UserManager<ApplicationUser> userManager,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,
            ILogger<RedirectService> logger)
        {
            _userManager = userManager;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
            _logger = logger;
        }

        public async Task<IActionResult> GetRedirectResult(ApplicationUser user, string returnUrl)
        {
            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("User {Email} has roles: {Roles}", user.Email, string.Join(", ", roles));

            if (_actionContextAccessor.ActionContext == null)
            {
                _logger.LogWarning("ActionContext is null, cannot perform role-based redirect.");
                return new LocalRedirectResult("~/");
            }
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            if (roles.Any(r => r.Equals(Roles.Admin, System.StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Redirecting user {Email} to Admin dashboard.", user.Email);
                return new RedirectToActionResult("Index", "Admin", null);
            }

            if (roles.Any(r => r.Equals(Roles.Instructor, System.StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Redirecting user {Email} to Instructor dashboard.", user.Email);
                return new RedirectToActionResult("Index", "Instructor", null);
            }

            if (roles.Any(r => r.Equals(Roles.User, System.StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Redirecting user {Email} to returnUrl: {ReturnUrl}", user.Email, returnUrl);
                return new LocalRedirectResult(urlHelper.Content(returnUrl));
            }

            _logger.LogInformation("User {Email} has no specific role for redirection, redirecting to default.", user.Email);
            return new LocalRedirectResult(urlHelper.Content("~/"));
        }
    }
}
