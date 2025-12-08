using Learnly.Constants;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Learnly.Controllers
{
    [Authorize(Roles = Roles.Instructor)]
    [Route("Instructor/Quiz")]
    public class InstructorQuizController : Controller
    {
        private readonly IQuizService _quizService;
        private readonly ICourseService _courseService; // To verify ownership

        public InstructorQuizController(IQuizService quizService, ICourseService courseService)
        {
            _quizService = quizService;
            _courseService = courseService;
        }

        // GET: Instructor/Quiz/Manage/{lessonId}
        [HttpGet("Manage/{lessonId}")]
        public async Task<IActionResult> Manage(int lessonId)
        {
            // Here we would also verify the instructor owns the course/lesson
            var quiz = await _quizService.GetQuizByLessonId(lessonId);
            ViewBag.LessonId = lessonId;
            return View(quiz);
        }

        // GET: Instructor/Quiz/Create/{lessonId}
        [HttpGet("Create/{lessonId}")]
        public IActionResult Create(int lessonId)
        {
            var model = new QuizEditViewModel { LessonId = lessonId, Title = string.Empty };
            return View(model);
        }

        // POST: Instructor/Quiz/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var quizId = await _quizService.CreateQuizAsync(model);
            // Redirect to the question management page for the new quiz
            return RedirectToAction("ManageQuestions", new { quizId });
        }

        // GET: Instructor/Quiz/Edit/{quizId}
        [HttpGet("Edit/{quizId}")]
        public async Task<IActionResult> Edit(int quizId)
        {
            var model = await _quizService.GetQuizForEditAsync(quizId);
            if (model == null) return NotFound();
            return View(model);
        }

        // POST: Instructor/Quiz/Edit/{quizId}
        [HttpPost("Edit/{quizId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int quizId, QuizEditViewModel model)
        {
            if (quizId != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _quizService.UpdateQuizAsync(model);
            return RedirectToAction("Manage", new { lessonId = model.LessonId });
        }
        
        // POST: Instructor/Quiz/Delete/{quizId}
        [HttpPost("Delete/{quizId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int quizId, int lessonId)
        {
            await _quizService.DeleteQuizAsync(quizId);
            return RedirectToAction("Manage", new { lessonId });
        }

        // GET: Instructor/Quiz/ManageQuestions/{quizId}
        [HttpGet("ManageQuestions/{quizId}")]
        public async Task<IActionResult> ManageQuestions(int quizId)
        {
            var questions = await _quizService.GetQuestionsForQuizAsync(quizId);
            ViewBag.QuizId = quizId;
            return View(questions);
        }

        // GET: Instructor/Quiz/AddQuestion/{quizId}
        [HttpGet("AddQuestion/{quizId}")]
        public IActionResult AddQuestion(int quizId)
        {
            var model = new QuestionEditViewModel { QuizId = quizId, Text = string.Empty, Type = Learnly.Models.QuestionType.MultipleChoice };
            return View(model);
        }

        // POST: Instructor/Quiz/AddQuestion
        [HttpPost("AddQuestion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(QuestionEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _quizService.AddQuestionToQuizAsync(model.QuizId, model);
            return RedirectToAction("ManageQuestions", new { quizId = model.QuizId });
        }
        
        // GET: Instructor/Quiz/EditQuestion/{questionId}
        [HttpGet("EditQuestion/{questionId}")]
        public async Task<IActionResult> EditQuestion(int questionId)
        {
            var model = await _quizService.GetQuestionForEditAsync(questionId);
            if (model == null) return NotFound();
            return View(model);
        }

        // POST: Instructor/Quiz/EditQuestion/{questionId}
        [HttpPost("EditQuestion/{questionId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int questionId, QuestionEditViewModel model)
        {
            if (questionId != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _quizService.UpdateQuestionAsync(model);
            return RedirectToAction("ManageQuestions", new { quizId = model.QuizId });
        }

        // POST: Instructor/Quiz/DeleteQuestion/{questionId}
        [HttpPost("DeleteQuestion/{questionId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int questionId, int quizId)
        {
            await _quizService.DeleteQuestionAsync(questionId);
            return RedirectToAction("ManageQuestions", new { quizId });
        }
    }
}
