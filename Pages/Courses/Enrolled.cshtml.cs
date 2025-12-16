using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learnly.Pages.Courses
{
    [Authorize]
    public class EnrolledModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnrolledModel(ICourseService courseService, UserManager<ApplicationUser> userManager)
        {
            _courseService = courseService;
            _userManager = userManager;
        }

        public IEnumerable<CourseDashboardVm>? EnrolledCourses { get; set; }
        public IEnumerable<CourseDashboardVm>? FilteredCourses { get; set; }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ProgressStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                EnrolledCourses = await _courseService.GetUserEnrolledCoursesAsync(userId);
                FilteredCourses = EnrolledCourses;

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    FilteredCourses = FilteredCourses?.Where(c =>
                        c.Title.Contains(SearchTerm, System.StringComparison.OrdinalIgnoreCase));
                }

                // Apply progress status filter
                FilteredCourses = ProgressStatus switch
                {
                    "not_started" => FilteredCourses?.Where(c => c.ProgressPercent == 0),
                    "in_progress" => FilteredCourses?.Where(c => c.ProgressPercent > 0 && c.ProgressPercent < 100),
                    "completed" => FilteredCourses?.Where(c => c.ProgressPercent == 100),
                    _ => FilteredCourses
                };

                // Apply sorting
                FilteredCourses = SortBy switch
                {
                    "progress_asc" => FilteredCourses?.OrderBy(c => c.ProgressPercent),
                    "progress_desc" => FilteredCourses?.OrderByDescending(c => c.ProgressPercent),
                    "title_asc" => FilteredCourses?.OrderBy(c => c.Title),
                    "title_desc" => FilteredCourses?.OrderByDescending(c => c.Title),
                    _ => FilteredCourses?.OrderByDescending(c => c.ProgressPercent)
                };
            }
        }
    }
}
