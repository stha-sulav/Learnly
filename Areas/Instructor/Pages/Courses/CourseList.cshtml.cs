using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public IList<CourseSummaryVm> FilteredCourses { get; set; } = new List<CourseSummaryVm>();
        public SelectList Categories { get; set; } = new SelectList(new List<Category>(), "Id", "Name");

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                Courses = (await _courseService.GetInstructorCourseSummaries(userId)).ToList();
            }

            var categories = await _adminService.GetCategoriesAsync();
            Categories = new SelectList(categories, "Id", "Name");

            // Apply filters
            var filtered = Courses.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                filtered = filtered.Where(c =>
                    c.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (c.ShortDescription?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(Status))
            {
                filtered = Status switch
                {
                    "published" => filtered.Where(c => c.IsPublished),
                    "draft" => filtered.Where(c => !c.IsPublished),
                    _ => filtered
                };
            }

            if (CategoryId.HasValue)
            {
                var categoryName = categories.FirstOrDefault(c => c.Id == CategoryId.Value)?.Name;
                if (!string.IsNullOrEmpty(categoryName))
                {
                    filtered = filtered.Where(c => c.CategoryName == categoryName);
                }
            }

            // Apply sorting
            filtered = SortBy switch
            {
                "newest" => filtered.OrderByDescending(c => c.CreatedAt),
                "oldest" => filtered.OrderBy(c => c.CreatedAt),
                "title_asc" => filtered.OrderBy(c => c.Title),
                "title_desc" => filtered.OrderByDescending(c => c.Title),
                "popular" => filtered.OrderByDescending(c => c.EnrolledStudents),
                _ => filtered.OrderByDescending(c => c.CreatedAt)
            };

            FilteredCourses = filtered.ToList();
        }
    }
}
