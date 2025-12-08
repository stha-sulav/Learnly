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
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizController(IQuizService quizService, UserManager<ApplicationUser> userManager)
        {
            _quizService = quizService;
            _userManager = userManager;
        }

        // GET: /Quiz/Take/{lessonId}
        public async Task<IActionResult> Take(int lessonId)
        {
            var quiz = await _quizService.GetQuizByLessonId(lessonId);
            if (quiz == null)
            {
                // No quiz for this lesson, maybe redirect back to the lesson page or show a message
                return NotFound();
            }

            var userId = _userManager.GetUserId(User)!;
            var attemptId = await _quizService.StartQuizAttempt(quiz.Id, userId);

            ViewBag.AttemptId = attemptId; // Pass attemptId to the view

            return View(quiz);
        }

        // POST: /Quiz/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(QuizSubmissionViewModel submission)
        {
            if (!ModelState.IsValid)
            {
                // This shouldn't happen with client-side validation, but as a fallback
                // we'd need to rebuild the quiz view model and return it.
                // For now, redirecting to a generic error or the home page.
                return RedirectToAction("Index", "Home");
            }
            
            await _quizService.SubmitQuizAttempt(submission.AttemptId, submission);
            var gradeResult = await _quizService.GradeAttemptAsync(submission.AttemptId);

            return RedirectToAction("Result", new { attemptId = gradeResult.AttemptId });
        }

        // GET: /Quiz/Result/{attemptId}
        public async Task<IActionResult> Result(int attemptId)
        {
            var gradeResult = await _quizService.GradeAttemptAsync(attemptId); // Fetching existing grade
            if (gradeResult == null)
            {
                return NotFound();
            }

            return View(gradeResult);
        }
    }
}
