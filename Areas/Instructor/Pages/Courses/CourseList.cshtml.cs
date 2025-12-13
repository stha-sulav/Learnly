using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public CourseListModel(ICourseService courseService, UserManager<ApplicationUser> userManager)
        {
            _courseService = courseService;
            _userManager = userManager;
        }

        public IList<CourseSummaryVm> Courses { get; set; } = new List<CourseSummaryVm>();

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                Courses = (await _courseService.GetInstructorCourseSummaries(userId)).ToList();
            }
        }
    }
}
