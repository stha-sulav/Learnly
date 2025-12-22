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
        private readonly ICourseService _courseService;
        private readonly IModuleService _moduleService;

        public InstructorQuizController(IQuizService quizService, ICourseService courseService, IModuleService moduleService)
        {
            _quizService = quizService;
            _courseService = courseService;
            _moduleService = moduleService;
        }

        // GET: Instructor/Quiz/Manage/{moduleId}
        [HttpGet("Manage/{moduleId}")]
        public async Task<IActionResult> Manage(int moduleId)
        {
            var module = await _moduleService.GetModuleByIdAsync(moduleId);
            if (module == null) return NotFound();

            var quiz = await _quizService.GetQuizByModuleIdAsync(moduleId);
            ViewBag.ModuleId = moduleId;
            ViewBag.ModuleTitle = module.Title;
            ViewBag.CourseId = module.CourseId;
            return View(quiz);
        }

        // GET: Instructor/Quiz/Create/{moduleId}
        [HttpGet("Create/{moduleId}")]
        public async Task<IActionResult> Create(int moduleId)
        {
            var module = await _moduleService.GetModuleByIdAsync(moduleId);
            if (module == null) return NotFound();

            var model = new QuizEditViewModel
            {
                ModuleId = moduleId,
                ModuleTitle = module.Title,
                Title = string.Empty
            };
            return View(model);
        }

        // POST: Instructor/Quiz/Create/{moduleId}
        [HttpPost("Create/{moduleId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int moduleId, QuizEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var quizId = await _quizService.CreateQuizAsync(model);
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
            return RedirectToAction("Manage", new { moduleId = model.ModuleId });
        }

        // POST: Instructor/Quiz/Delete/{quizId}
        [HttpPost("Delete/{quizId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int quizId, int moduleId)
        {
            await _quizService.DeleteQuizAsync(quizId);
            return RedirectToAction("Manage", new { moduleId });
        }

        // GET: Instructor/Quiz/ManageQuestions/{quizId}
        [HttpGet("ManageQuestions/{quizId}")]
        public async Task<IActionResult> ManageQuestions(int quizId)
        {
            var quiz = await _quizService.GetQuizForEditAsync(quizId);
            if (quiz == null) return NotFound();

            var questions = await _quizService.GetQuestionsForQuizAsync(quizId);
            ViewBag.QuizId = quizId;
            ViewBag.ModuleId = quiz.ModuleId;
            ViewBag.QuizTitle = quiz.Title;
            return View(questions);
        }

        // GET: Instructor/Quiz/AddQuestion/{quizId}
        [HttpGet("AddQuestion/{quizId}")]
        public IActionResult AddQuestion(int quizId)
        {
            var model = new QuestionEditViewModel { QuizId = quizId, Text = string.Empty, Type = Learnly.Models.QuestionType.MultipleChoice };
            return View(model);
        }

        // POST: Instructor/Quiz/AddQuestion/{quizId}
        [HttpPost("AddQuestion/{quizId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int quizId, QuestionEditViewModel model)
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
