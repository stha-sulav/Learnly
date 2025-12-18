using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Learnly.Models;
using System.Security.Claims;

namespace Learnly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ICourseService courseService, UserManager<ApplicationUser> userManager)
        {
            _courseService = courseService;
            _userManager = userManager;
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseReviews(int courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reviews = await _courseService.GetCourseReviewsAsync(courseId, userId);
            return Ok(reviews);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            if (request == null || request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest("Invalid request. Rating must be between 1 and 5.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check if user is enrolled in the course
            var isEnrolled = await _courseService.IsUserEnrolledAsync(request.CourseId, userId);
            if (!isEnrolled)
            {
                return BadRequest("You must be enrolled in this course to leave a review.");
            }

            try
            {
                var review = await _courseService.CreateReviewAsync(request.CourseId, userId, request.Rating, request.Comment);
                return Ok(review);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] UpdateReviewRequest request)
        {
            if (request == null || request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest("Invalid request. Rating must be between 1 and 5.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var review = await _courseService.UpdateReviewAsync(reviewId, userId, request.Rating, request.Comment);
            if (review == null)
            {
                return NotFound("Review not found or you don't have permission to update it.");
            }

            return Ok(review);
        }

        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _courseService.DeleteReviewAsync(reviewId, userId);
            if (!result)
            {
                return NotFound("Review not found or you don't have permission to delete it.");
            }

            return Ok(new { message = "Review deleted successfully." });
        }
    }
}
