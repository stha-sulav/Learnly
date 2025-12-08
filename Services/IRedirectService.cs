using Learnly.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface IRedirectService
    {
        Task<IActionResult> GetRedirectResult(ApplicationUser user, string returnUrl);
    }
}
