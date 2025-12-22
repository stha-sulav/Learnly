using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Learnly.Controllers
{
    [Route("api/Quiz")]
    [ApiController]
    [Authorize]
    public class QuizApiController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizApiController(IQuizService quizService, UserManager<ApplicationUser> userManager)
        {
            _quizService = quizService;
            _userManager = userManager;
        }

        [HttpGet("ByModule/{moduleId}")]
        public async Task<ActionResult<QuizViewModel>> GetQuizByModule(int moduleId)
        {
            var quiz = await _quizService.GetQuizByModuleIdAsync(moduleId);
            if (quiz == null)
            {
                return NotFound();
            }
            return Ok(quiz);
        }

        [HttpPost("StartAttempt/{quizId}")]
        public async Task<ActionResult<StartAttemptResponse>> StartAttempt(int quizId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var attemptId = await _quizService.StartQuizAttempt(quizId, userId);
            return Ok(new StartAttemptResponse { AttemptId = attemptId });
        }

        [HttpPost("Submit")]
        public async Task<ActionResult<GradeResultDto>> Submit([FromBody] QuizSubmissionViewModel submission)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _quizService.SubmitQuizAttempt(submission.AttemptId, submission);
            var gradeResult = await _quizService.GradeAttemptAsync(submission.AttemptId);

            return Ok(gradeResult);
        }
    }

    public class StartAttemptResponse
    {
        public int AttemptId { get; set; }
    }
}
