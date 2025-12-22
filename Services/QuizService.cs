using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Learnly.Data;
using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Learnly.Services
{
    public class QuizService : IQuizService
    {
        private readonly ApplicationDbContext _context;

        public QuizService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<QuizViewModel?> GetQuizByModuleIdAsync(int moduleId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Module)
                .FirstOrDefaultAsync(q => q.ModuleId == moduleId);

            if (quiz == null)
            {
                return null;
            }

            var random = new Random();

            return new QuizViewModel
            {
                Id = quiz.Id,
                ModuleId = quiz.ModuleId,
                ModuleTitle = quiz.Module?.Title,
                Title = quiz.Title!,
                Questions = quiz.Questions.Select(q => new QuestionViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type.ToString(),
                    Options = ShuffleOptions(
                        !string.IsNullOrEmpty(q.Options)
                            ? JsonSerializer.Deserialize<List<string>>(q.Options) ?? new List<string>()
                            : new List<string>(),
                        random)
                }).ToList()
            };
        }

        private static List<string> ShuffleOptions(List<string> options, Random random)
        {
            if (options.Count <= 1) return options;

            // Fisher-Yates shuffle
            var shuffled = new List<string>(options);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            return shuffled;
        }

        public async Task<int> StartQuizAttempt(int quizId, string userId)
        {
            var attempt = new Attempt
            {
                QuizId = quizId,
                UserId = userId,
                StartedAt = DateTime.UtcNow
            };

            _context.Attempts.Add(attempt);
            await _context.SaveChangesAsync();
            return attempt.Id;
        }

        public async Task SubmitQuizAttempt(int attemptId, QuizSubmissionViewModel submission)
        {
            var attempt = await _context.Attempts.FindAsync(attemptId);
            if (attempt == null)
            {
                throw new ArgumentException("Attempt not found.");
            }

            attempt.CompletedAt = DateTime.UtcNow;
            attempt.Answers = JsonSerializer.Serialize(submission.Answers);

            await _context.SaveChangesAsync();
        }

        public async Task<GradeResultDto> GradeAttemptAsync(int attemptId, CancellationToken ct = default)
        {
            var attempt = await _context.Attempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q!.Questions)
                .Include(a => a.Quiz)
                    .ThenInclude(q => q!.Module)
                .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

            if (attempt == null)
            {
                throw new ArgumentException("Attempt not found.");
            }

            var passingScore = Quiz.FixedPassingScore;

            if (attempt.IsGraded)
            {
                // If already graded, just return the existing result
                var existingFeedback = string.IsNullOrEmpty(attempt.Feedback)
                    ? new List<QuestionFeedbackDto>()
                    : JsonSerializer.Deserialize<List<QuestionFeedbackDto>>(attempt.Feedback) ?? new List<QuestionFeedbackDto>();

                var result = new GradeResultDto
                {
                    AttemptId = attempt.Id,
                    QuizId = attempt.QuizId,
                    ModuleId = attempt.Quiz!.ModuleId,
                    QuizTitle = attempt.Quiz!.Title ?? "Unknown Quiz",
                    Score = attempt.Score,
                    PassingScore = passingScore,
                    Passed = attempt.Score >= passingScore,
                    GradedAt = attempt.GradedAt ?? DateTime.UtcNow,
                    QuestionFeedbacks = existingFeedback
                };

                // Check if passing unlocks next module
                if (result.Passed)
                {
                    await PopulateNextModuleInfo(result, attempt.Quiz!.ModuleId, ct);
                }

                return result;
            }

            if (string.IsNullOrEmpty(attempt.Answers))
            {
                // No answers submitted, grade as 0
                attempt.Score = 0;
                attempt.IsGraded = true;
                attempt.GradedAt = DateTime.UtcNow;
                attempt.Feedback = JsonSerializer.Serialize(new List<QuestionFeedbackDto>());
                await _context.SaveChangesAsync(ct);

                return new GradeResultDto
                {
                    AttemptId = attempt.Id,
                    QuizId = attempt.QuizId,
                    ModuleId = attempt.Quiz!.ModuleId,
                    QuizTitle = attempt.Quiz!.Title ?? "Unknown Quiz",
                    Score = 0,
                    PassingScore = passingScore,
                    Passed = false,
                    GradedAt = attempt.GradedAt.Value,
                    QuestionFeedbacks = new List<QuestionFeedbackDto>()
                };
            }

            var submittedAnswers = JsonSerializer.Deserialize<List<AnswerViewModel>>(attempt.Answers) ?? new List<AnswerViewModel>();

            decimal totalRawPoints = 0;
            decimal totalPossiblePoints = 0;
            var questionFeedbacks = new List<QuestionFeedbackDto>();

            foreach (var question in attempt.Quiz!.Questions.OrderBy(q => q.Id))
            {
                var userAnswer = submittedAnswers.FirstOrDefault(sa => sa.QuestionId == question.Id);

                decimal earnedPoints = 0;
                decimal possiblePoints = 1;

                var correctOptions = !string.IsNullOrEmpty(question.Options)
                    ? JsonSerializer.Deserialize<List<string>>(question.Options) ?? new List<string>()
                    : new List<string>();

                string feedbackMessage = "Not answered.";

                if (userAnswer != null)
                {
                    bool isCorrect = false;
                    switch (question.Type)
                    {
                        case QuestionType.MultipleChoice:
                        case QuestionType.ShortAnswer:
                            if (correctOptions.Any() && userAnswer.SelectedOptions != null && userAnswer.SelectedOptions.Count == 1)
                            {
                                isCorrect = string.Equals(userAnswer.SelectedOptions.First().Trim(), correctOptions.First().Trim(), StringComparison.OrdinalIgnoreCase);
                            }
                            else if (question.Type == QuestionType.ShortAnswer && correctOptions.Any() && !string.IsNullOrEmpty(userAnswer.Answer))
                            {
                                 isCorrect = string.Equals(userAnswer.Answer.Trim(), correctOptions.First().Trim(), StringComparison.OrdinalIgnoreCase);
                            }

                            if (isCorrect)
                            {
                                earnedPoints = 1;
                                feedbackMessage = "Correct!";
                            }
                            else
                            {
                                feedbackMessage = "Incorrect.";
                            }
                            break;

                        case QuestionType.MultipleSelect:
                            if (userAnswer.SelectedOptions != null && correctOptions.Any())
                            {
                                var correctSelected = userAnswer.SelectedOptions.Where(s => correctOptions.Contains(s, StringComparer.OrdinalIgnoreCase)).Count();
                                var incorrectSelected = userAnswer.SelectedOptions.Where(s => !correctOptions.Contains(s, StringComparer.OrdinalIgnoreCase)).Count();

                                decimal scoreRatio = (decimal)correctSelected / correctOptions.Count;
                                decimal penalty = incorrectSelected * 0.1m;

                                earnedPoints = Math.Max(0, scoreRatio - penalty);
                                earnedPoints = Math.Round(earnedPoints, 2);

                                if (earnedPoints >= 1)
                                {
                                    feedbackMessage = "Fully correct!";
                                }
                                else if (earnedPoints > 0)
                                {
                                    feedbackMessage = $"Partially correct. Earned {earnedPoints * 100}% of points.";
                                }
                                else
                                {
                                    feedbackMessage = "Incorrect.";
                                }
                            }
                            else
                            {
                                 feedbackMessage = "Incorrect or not answered properly.";
                            }
                            break;
                    }
                }

                totalRawPoints += earnedPoints;
                totalPossiblePoints += possiblePoints;

                questionFeedbacks.Add(new QuestionFeedbackDto
                {
                    QuestionId = question.Id,
                    EarnedPoints = earnedPoints,
                    PossiblePoints = possiblePoints,
                    Message = feedbackMessage
                });
            }

            attempt.Score = totalPossiblePoints > 0 ? Math.Round((totalRawPoints / totalPossiblePoints) * 100, 2) : 0;
            attempt.IsGraded = true;
            attempt.GradedAt = DateTime.UtcNow;
            attempt.Feedback = JsonSerializer.Serialize(questionFeedbacks);

            await _context.SaveChangesAsync(ct);

            var gradeResult = new GradeResultDto
            {
                AttemptId = attempt.Id,
                QuizId = attempt.QuizId,
                ModuleId = attempt.Quiz!.ModuleId,
                QuizTitle = attempt.Quiz!.Title ?? "Unknown Quiz",
                Score = attempt.Score,
                PassingScore = passingScore,
                Passed = attempt.Score >= passingScore,
                GradedAt = attempt.GradedAt.Value,
                QuestionFeedbacks = questionFeedbacks
            };

            // Check if passing unlocks next module
            if (gradeResult.Passed)
            {
                await PopulateNextModuleInfo(gradeResult, attempt.Quiz!.ModuleId, ct);
            }

            return gradeResult;
        }

        private async Task PopulateNextModuleInfo(GradeResultDto result, int currentModuleId, CancellationToken ct)
        {
            var currentModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Id == currentModuleId, ct);

            if (currentModule == null) return;

            var nextModule = await _context.Modules
                .Where(m => m.CourseId == currentModule.CourseId && m.OrderIndex > currentModule.OrderIndex)
                .OrderBy(m => m.OrderIndex)
                .FirstOrDefaultAsync(ct);

            if (nextModule != null)
            {
                result.UnlocksNextModule = true;
                result.NextModuleId = nextModule.Id;
                result.NextModuleTitle = nextModule.Title;
            }
        }

        // Module access control methods
        public async Task<bool> HasUserPassedModuleQuizAsync(int moduleId, string userId)
        {
            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.ModuleId == moduleId);

            // No quiz means module is automatically "passed"
            if (quiz == null) return true;

            return await _context.Attempts
                .AnyAsync(a => a.QuizId == quiz.Id
                            && a.UserId == userId
                            && a.IsGraded
                            && a.Score >= Quiz.FixedPassingScore);
        }

        public async Task<bool> CanUserAccessModuleAsync(int moduleId, string userId)
        {
            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null) return false;

            // Find the previous module (the one with the largest OrderIndex that's still less than current)
            var previousModule = await _context.Modules
                .Where(m => m.CourseId == module.CourseId && m.OrderIndex < module.OrderIndex)
                .OrderByDescending(m => m.OrderIndex)
                .FirstOrDefaultAsync();

            // If no previous module found, this is the first module - always accessible
            if (previousModule == null) return true;

            // Check if previous module has a quiz
            var previousModuleQuiz = await _context.Quizzes
                .FirstOrDefaultAsync(q => q.ModuleId == previousModule.Id);

            // If previous module has no quiz, current module is accessible
            if (previousModuleQuiz == null) return true;

            // Check if user has passed the previous module's quiz
            return await _context.Attempts
                .AnyAsync(a => a.QuizId == previousModuleQuiz.Id
                            && a.UserId == userId
                            && a.IsGraded
                            && a.Score >= Quiz.FixedPassingScore);
        }

        public async Task<List<int>> GetAccessibleModuleIdsAsync(int courseId, string userId)
        {
            var modules = await _context.Modules
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.OrderIndex)
                .Include(m => m.Quiz)
                .ToListAsync();

            var accessibleIds = new List<int>();
            bool previousModulePassed = true;

            foreach (var module in modules)
            {
                // First module (in sorted order) or previous module passed = accessible
                if (previousModulePassed)
                {
                    accessibleIds.Add(module.Id);
                }

                // Check if this module's quiz is passed for next iteration
                if (module.Quiz != null)
                {
                    previousModulePassed = await _context.Attempts
                        .AnyAsync(a => a.QuizId == module.Quiz.Id
                                    && a.UserId == userId
                                    && a.IsGraded
                                    && a.Score >= Quiz.FixedPassingScore);
                }
                else
                {
                    // No quiz means automatically passed
                    previousModulePassed = true;
                }
            }

            return accessibleIds;
        }

        // Instructor-facing methods
        public async Task<int> CreateQuizAsync(QuizEditViewModel model)
        {
            var quiz = new Quiz
            {
                ModuleId = model.ModuleId,
                Title = model.Title
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            return quiz.Id;
        }

        public async Task UpdateQuizAsync(QuizEditViewModel model)
        {
            var quiz = await _context.Quizzes.FindAsync(model.Id);
            if (quiz != null)
            {
                quiz.Title = model.Title;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteQuizAsync(int quizId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz != null)
            {
                _context.Quizzes.Remove(quiz);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<QuizEditViewModel?> GetQuizForEditAsync(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Module)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return null;

            return new QuizEditViewModel
            {
                Id = quiz.Id,
                ModuleId = quiz.ModuleId,
                ModuleTitle = quiz.Module?.Title,
                Title = quiz.Title
            };
        }

        public async Task<List<QuestionViewModel>> GetQuestionsForQuizAsync(int quizId)
        {
            var questions = await _context.Questions
                .Where(q => q.QuizId == quizId)
                .ToListAsync();

            return questions.Select(q => new QuestionViewModel
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type.ToString(),
                Options = !string.IsNullOrEmpty(q.Options)
                    ? JsonSerializer.Deserialize<List<string>>(q.Options) ?? new List<string>()
                    : new List<string>()
            }).ToList();
        }

        public async Task<int> AddQuestionToQuizAsync(int quizId, QuestionEditViewModel model)
        {
            var question = new Question
            {
                QuizId = quizId,
                Text = model.Text,
                Type = model.Type,
                Options = JsonSerializer.Serialize(model.Options)
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return question.Id;
        }

        public async Task UpdateQuestionAsync(QuestionEditViewModel model)
        {
            var question = await _context.Questions.FindAsync(model.Id);
            if (question != null)
            {
                question.Text = model.Text;
                question.Type = model.Type;
                question.Options = JsonSerializer.Serialize(model.Options);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteQuestionAsync(int questionId)
        {
            var question = await _context.Questions.FindAsync(questionId);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<QuestionEditViewModel?> GetQuestionForEditAsync(int questionId)
        {
            var question = await _context.Questions.FindAsync(questionId);
            if (question == null) return null;

            return new QuestionEditViewModel
            {
                Id = question.Id,
                QuizId = question.QuizId,
                Text = question.Text,
                Type = question.Type,
                Options = !string.IsNullOrEmpty(question.Options)
                    ? JsonSerializer.Deserialize<List<string>>(question.Options) ?? new List<string>()
                    : new List<string>()
            };
        }
    }
}
