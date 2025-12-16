using Learnly.Data;
using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learnly.Pages.Courses
{
    public class IndexModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(ICourseService courseService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _courseService = courseService;
            _userManager = userManager;
            _context = context;
        }

        public IEnumerable<CourseSummaryVm>? Courses { get; set; }
        public IEnumerable<CourseSummaryVm>? FilteredCourses { get; set; }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; }

        // Filter options
        public SelectList Categories { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            Courses = await _courseService.GetAvailableCoursesForUser(userId);

            // Load all categories
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            Categories = new SelectList(categories, "Id", "Name", CategoryId);

            // Apply filters
            FilteredCourses = Courses;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                FilteredCourses = FilteredCourses?.Where(c =>
                    c.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.ShortDescription.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (CategoryId.HasValue)
            {
                var categoryName = categories.FirstOrDefault(c => c.Id == CategoryId.Value)?.Name;
                if (!string.IsNullOrEmpty(categoryName))
                {
                    FilteredCourses = FilteredCourses?.Where(c => c.CategoryName == categoryName);
                }
            }

            // Apply sorting
            FilteredCourses = SortBy switch
            {
                "newest" => FilteredCourses?.OrderByDescending(c => c.CreatedAt),
                "oldest" => FilteredCourses?.OrderBy(c => c.CreatedAt),
                "title_asc" => FilteredCourses?.OrderBy(c => c.Title),
                "title_desc" => FilteredCourses?.OrderByDescending(c => c.Title),
                "popular" => FilteredCourses?.OrderByDescending(c => c.EnrolledStudents),
                _ => FilteredCourses?.OrderByDescending(c => c.CreatedAt)
            };
        }
    }
}
