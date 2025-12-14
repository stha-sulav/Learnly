using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Learnly.Constants;

namespace Learnly.Areas.Instructor.Pages.Courses
{
    [Authorize(Roles = Roles.Instructor)]
    public class CourseListModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAdminService _adminService;

        public CourseListModel(ICourseService courseService, UserManager<ApplicationUser> userManager, IAdminService adminService)
        {
            _courseService = courseService;
            _userManager = userManager;
            _adminService = adminService;
        }

        public IList<CourseSummaryVm> Courses { get; set; } = new List<CourseSummaryVm>();
        public SelectList Categories { get; set; } = new SelectList(new List<Category>(), "Id", "Name");

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                Courses = (await _courseService.GetInstructorCourseSummaries(userId)).ToList();
            }

            var categories = await _adminService.GetCategoriesAsync();
            Categories = new SelectList(categories, "Id", "Name");
        }
    }
}
