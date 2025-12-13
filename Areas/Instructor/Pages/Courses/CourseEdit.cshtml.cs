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
using System.Linq; // Added for .Linq operations
using System; // Added for Exception

namespace Learnly.Areas.Instructor.Pages.Courses
{
    [Authorize(Roles = Roles.Instructor)]
    public class CourseEditModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAdminService _adminService; // To get categories

        public CourseEditModel(ICourseService courseService, UserManager<ApplicationUser> userManager, IAdminService adminService)
        {
            _courseService = courseService;
            _userManager = userManager;
            _adminService = adminService;
        }

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public CourseCreateUpdateDto Course { get; set; } = new CourseCreateUpdateDto();

        public SelectList Categories { get; set; } = new SelectList(new List<Category>(), "Id", "Name"); // Initialize to avoid null

        public bool IsEditMode => Course.Id != 0;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            await LoadCategoriesAsync();

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            if (id == null)
            {
                // Create mode
                Course = new CourseCreateUpdateDto { InstructorId = userId }; // Pre-set instructor
                return Page();
            }

            // Edit mode
            var courseFromDb = await _courseService.GetCourseForEditAsync(id.Value);
            if (courseFromDb == null)
            {
                ErrorMessage = "Course not found.";
                return RedirectToPage("./CourseList");
            }

            // Authorization check: Only the course instructor or an Admin can edit
            if (courseFromDb.InstructorId != userId && !User.IsInRole(Roles.Admin))
            {
                ErrorMessage = "You don't have permission to edit this course.";
                return Forbid();
            }

            Course = courseFromDb;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCategoriesAsync();
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Ensure the InstructorId is set for creation, or matches for update
            if (!IsEditMode)
            {
                Course.InstructorId = userId;
            }
            else if (Course.InstructorId != userId && !User.IsInRole(Roles.Admin)) // Re-check auth for update
            {
                ErrorMessage = "You don't have permission to edit this course.";
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please correct the errors in the form.";
                return Page();
            }

            if (IsEditMode)
            {
                // Update existing course
                try
                {
                    await _courseService.UpdateCourseAsync(Course);
                    SuccessMessage = "Course updated successfully!";
                    return RedirectToPage("./CourseList");
                }
                catch (KeyNotFoundException)
                {
                    ErrorMessage = "Course not found for update.";
                }
                catch (InvalidOperationException ex) // For slug duplication or other business logic errors
                {
                    ModelState.AddModelError("Course.Slug", ex.Message);
                    ErrorMessage = "Error updating course: " + ex.Message;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error updating course: {ex.Message}";
                }
            }
            else
            {
                // Create new course
                try
                {
                    // Call the service method that returns CourseDetailVm, but we only need it to save
                    await _courseService.CreateCourseAsync(Course);
                    SuccessMessage = "Course created successfully!";
                    return RedirectToPage("./CourseList");
                }
                catch (InvalidOperationException ex) // For slug duplication
                {
                    ModelState.AddModelError("Course.Slug", ex.Message);
                    ErrorMessage = "Error creating course: " + ex.Message;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error creating course: {ex.Message}";
                }
            }

            // If we got this far, something failed, re-display form
            return Page();
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _adminService.GetCategoriesAsync(); // Assuming GetCategoriesAsync exists
            Categories = new SelectList(categories, "Id", "Name", Course.CategoryId);
        }
    }
}
