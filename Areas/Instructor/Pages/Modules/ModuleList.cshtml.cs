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
using System.Linq;

namespace Learnly.Areas.Instructor.Pages.Modules
{
    [Authorize(Roles = Roles.Instructor)]
    public class ModuleListModel : PageModel
    {
        private readonly IModuleService _moduleService;
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ModuleListModel(IModuleService moduleService, ICourseService courseService, UserManager<ApplicationUser> userManager)
        {
            _moduleService = moduleService;
            _courseService = courseService;
            _userManager = userManager;
        }

        public CourseSummaryVm? Course { get; set; }
        public IList<ModuleVm> Modules { get; set; } = new List<ModuleVm>();

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int courseId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            Course = await _courseService.GetCourseByIdAsync(courseId);
            if (Course == null)
            {
                ErrorMessage = "Course not found.";
                return RedirectToPage("/Instructor/Courses/CourseList");
            }

            // Authorization check: Only the course instructor or an Admin can manage modules
            if (Course.InstructorName != _userManager.GetUserName(User) && !User.IsInRole(Roles.Admin)) // InstructorName from summary might not be enough, need real instructorId
            {
                 // Need to fetch full course details to get InstructorId
                var fullCourse = await _courseService.GetCourseForEditAsync(courseId);
                if (fullCourse == null || fullCourse.InstructorId != userId && !User.IsInRole(Roles.Admin))
                {
                    ErrorMessage = "You don't have permission to manage modules for this course.";
                    return Forbid();
                }
            }

            Modules = (await _moduleService.GetModulesByCourseAsync(courseId))
                .Select(m => new ModuleVm
                {
                    Id = m.Id,
                    Title = m.Title,
                    Order = m.OrderIndex,
                    Lessons = new List<LessonVm>()
                }).ToList();

            return Page();
        }
    }
}
