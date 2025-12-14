using Learnly.Models;
using Learnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace Learnly.Controllers
{
    [Authorize]
    public class VideosController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILessonService _lessonService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VideosController(ICourseService courseService, UserManager<ApplicationUser> userManager, ILessonService lessonService, IWebHostEnvironment webHostEnvironment)
        {
            _courseService = courseService;
            _userManager = userManager;
            _lessonService = lessonService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("videos/protected/{courseId}/{moduleId}/{lessonId}")]
        public async Task<IActionResult> GetProtectedVideo(int courseId, int moduleId, int lessonId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            var isEnrolled = await _courseService.IsUserEnrolledAsync(courseId, user.Id);
            if (!isEnrolled)
            {
                return Forbid();
            }

            var lesson = await _lessonService.GetLessonByIdAsync(lessonId);
            if (lesson == null || lesson.VideoPath == null)
            {
                return NotFound();
            }

            var videoPath = Path.Combine(_webHostEnvironment.WebRootPath, lesson.VideoPath.TrimStart('/'));

            if (!System.IO.File.Exists(videoPath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            await using (var stream = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return new FileStreamResult(memory, "video/mp4") { EnableRangeProcessing = true };
        }
    }
}
