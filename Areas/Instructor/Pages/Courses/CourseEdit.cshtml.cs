using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using Learnly.Constants;
using System.Linq;
using System;

namespace Learnly.Areas.Instructor.Pages.Courses
{
    [Authorize(Roles = Roles.Instructor)]
    public class CourseEditModel : PageModel
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAdminService _adminService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CourseEditModel(ICourseService courseService, UserManager<ApplicationUser> userManager, IAdminService adminService, IWebHostEnvironment webHostEnvironment)
        {
            _courseService = courseService;
            _userManager = userManager;
            _adminService = adminService;
            _webHostEnvironment = webHostEnvironment;
        }

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public CourseCreateUpdateDto Course { get; set; } = new CourseCreateUpdateDto();

        [BindProperty]
        public IFormFile? ThumbnailFile { get; set; }

        public SelectList Categories { get; set; } = new SelectList(new List<Category>(), "Id", "Name");

        public bool IsEditMode => Course.Id != 0;

        public async Task<IActionResult> OnGetAsync(int? id, bool created = false)
        {
            await LoadCategoriesAsync();

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            // Only show success message if just created (via query param)
            if (!created)
            {
                SuccessMessage = null;
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

            // Clear ModelState errors for fields we set server-side
            ModelState.Remove("Course.InstructorId");
            ModelState.Remove("Course.ThumbnailPath");

            if (!ModelState.IsValid)
            {
                // Get specific validation errors
                var errors = ModelState.Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"));
                ErrorMessage = "Please correct the errors: " + string.Join("; ", errors);
                return Page();
            }

            // Handle thumbnail upload
            if (ThumbnailFile != null && ThumbnailFile.Length > 0)
            {
                var thumbnailPath = await SaveThumbnailAsync(ThumbnailFile);
                if (thumbnailPath != null)
                {
                    Course.ThumbnailPath = thumbnailPath;
                }
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
                catch (InvalidOperationException ex) // For business logic errors
                {
                    ModelState.AddModelError("", ex.Message);
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
                    var createdCourse = await _courseService.CreateCourseAsync(Course);
                    SuccessMessage = "Course created successfully! You can now add modules and lessons.";
                    // Redirect to the same page in edit mode to add modules/lessons
                    return RedirectToPage("./CourseEdit", new { id = createdCourse.Id, created = true });
                }
                catch (InvalidOperationException ex) // For business logic errors
                {
                    ModelState.AddModelError("", ex.Message);
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
            var categories = await _adminService.GetCategoriesAsync();
            Categories = new SelectList(categories, "Id", "Name", Course.CategoryId);
        }

        private async Task<string?> SaveThumbnailAsync(IFormFile file)
        {
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ErrorMessage = "Invalid image format. Allowed formats: JPG, PNG, GIF, WebP";
                    return null;
                }

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    ErrorMessage = "Image file size must be less than 5MB.";
                    return null;
                }

                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "thumbnails");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Return relative path for storage
                return $"/uploads/thumbnails/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error uploading thumbnail: {ex.Message}";
                return null;
            }
        }
    }
}
