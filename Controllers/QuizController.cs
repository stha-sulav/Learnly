using Learnly.Data;
using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Learnly.Controllers
{
    [Authorize]
    public class QuizController : Controller
    {
        private readonly IQuizService _quizService;
        private readonly IModuleService _moduleService;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizController(IQuizService quizService, IModuleService moduleService, UserManager<ApplicationUser> userManager)
        {
            _quizService = quizService;
            _moduleService = moduleService;
            _userManager = userManager;
        }

        // GET: /Quiz/Take/{moduleId}
        public async Task<IActionResult> Take(int moduleId)
        {
            var userId = _userManager.GetUserId(User)!;

            // Check if user can access this module
            var canAccess = await _quizService.CanUserAccessModuleAsync(moduleId, userId);
            if (!canAccess)
            {
                TempData["ErrorMessage"] = "You must complete the previous module's quiz before accessing this content.";

                // Stay on the same page - redirect back to referer or course details
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }

                var module = await _moduleService.GetModuleByIdAsync(moduleId);
                if (module != null)
                {
                    return RedirectToPage("/Courses/Details", new { id = module.CourseId });
                }
                return RedirectToAction("Index", "Home");
            }

            var quiz = await _quizService.GetQuizByModuleIdAsync(moduleId);
            if (quiz == null)
            {
                TempData["ErrorMessage"] = "No quiz found for this module.";
                return NotFound();
            }

            var attemptId = await _quizService.StartQuizAttempt(quiz.Id, userId);
            ViewBag.AttemptId = attemptId;
            ViewBag.ModuleId = moduleId;

            return View(quiz);
        }

        // POST: /Quiz/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(QuizSubmissionViewModel submission)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index", "Home");
            }

            await _quizService.SubmitQuizAttempt(submission.AttemptId, submission);
            var gradeResult = await _quizService.GradeAttemptAsync(submission.AttemptId);

            return RedirectToAction("Result", new { attemptId = gradeResult.AttemptId });
        }

        // GET: /Quiz/Result/{attemptId}
        public async Task<IActionResult> Result(int attemptId)
        {
            var gradeResult = await _quizService.GradeAttemptAsync(attemptId);
            if (gradeResult == null)
            {
                return NotFound();
            }

            return View(gradeResult);
        }
    }
}
