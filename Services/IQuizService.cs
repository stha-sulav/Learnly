using System.Collections.Generic;
using System.Threading.Tasks;
using Learnly.ViewModels;

namespace Learnly.Services
{
    public interface IQuizService
    {
        // Student-facing methods
        Task<QuizViewModel> GetQuizByLessonId(int lessonId);
        Task<GradeResultDto> GradeAttemptAsync(int attemptId, CancellationToken ct = default);
        Task<int> StartQuizAttempt(int quizId, string userId);
        Task SubmitQuizAttempt(int attemptId, QuizSubmissionViewModel submission);

        // Instructor-facing methods
        Task<int> CreateQuizAsync(QuizEditViewModel model);
        Task UpdateQuizAsync(QuizEditViewModel model);
        Task DeleteQuizAsync(int quizId);
        Task<QuizEditViewModel> GetQuizForEditAsync(int quizId);
        Task<List<QuestionViewModel>> GetQuestionsForQuizAsync(int quizId);
        Task<int> AddQuestionToQuizAsync(int quizId, QuestionEditViewModel model);
        Task UpdateQuestionAsync(QuestionEditViewModel model);
        Task DeleteQuestionAsync(int questionId);
        Task<QuestionEditViewModel> GetQuestionForEditAsync(int questionId);
    }
}
