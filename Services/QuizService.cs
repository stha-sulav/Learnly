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

        public async Task<QuizViewModel?> GetQuizByLessonId(int lessonId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.LessonId == lessonId);

            if (quiz == null)
            {
                return null;
            }

            return new QuizViewModel
            {
                Id = quiz.Id,
                LessonId = quiz.LessonId,
                Title = quiz.Title!,
                Questions = quiz.Questions.Select(q => new QuestionViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type.ToString(),
                    Options = !string.IsNullOrEmpty(q.Options) 
                        ? JsonSerializer.Deserialize<List<string>>(q.Options) 
                        : new List<string>()
                }).ToList()
            };
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
                    .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

            if (attempt == null)
            {
                throw new ArgumentException("Attempt not found.");
            }

            if (attempt.IsGraded)
            {
                // If already graded, just return the existing result
                var existingFeedback = string.IsNullOrEmpty(attempt.Feedback)
                    ? new List<QuestionFeedbackDto>()
                    : JsonSerializer.Deserialize<List<QuestionFeedbackDto>>(attempt.Feedback) ?? new List<QuestionFeedbackDto>();

                return new GradeResultDto
                {
                    AttemptId = attempt.Id,
                    QuizId = attempt.QuizId,
                    QuizTitle = attempt.Quiz!.Title ?? "Unknown Quiz",
                    Score = attempt.Score,
                    PassingScore = attempt.Quiz!.PassingScore,
                    Passed = attempt.Score >= attempt.Quiz!.PassingScore,
                    GradedAt = attempt.GradedAt ?? DateTime.UtcNow, // Use existing GradedAt if present
                    QuestionFeedbacks = existingFeedback
                };
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
                    QuizTitle = attempt.Quiz!.Title ?? "Unknown Quiz",
                    Score = 0,
                    PassingScore = attempt.Quiz!.PassingScore,
                    Passed = false,
                    GradedAt = attempt.GradedAt.Value,
                    QuestionFeedbacks = new List<QuestionFeedbackDto>()
                };
            }

            var submittedAnswers = JsonSerializer.Deserialize<List<AnswerViewModel>>(attempt.Answers) ?? new List<AnswerViewModel>();
            
            decimal totalRawPoints = 0;
            decimal totalPossiblePoints = 0;
            var questionFeedbacks = new List<QuestionFeedbackDto>();

            foreach (var question in attempt.Quiz!.Questions.OrderBy(q => q.Id)) // Ensure consistent order
            {
                var userAnswer = submittedAnswers.FirstOrDefault(sa => sa.QuestionId == question.Id);
                
                decimal earnedPoints = 0;
                decimal possiblePoints = 1; // Default to 1 point per question

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
                            // For MVP, assume the first option in Question.Options is the single correct one.
                            // For ShortAnswer, assume the first option in Question.Options is the exact correct answer.
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
                            // Partial credit logic for MultiSelect
                            if (userAnswer.SelectedOptions != null && correctOptions.Any())
                            {
                                var correctSelected = userAnswer.SelectedOptions.Where(s => correctOptions.Contains(s, StringComparer.OrdinalIgnoreCase)).Count();
                                var incorrectSelected = userAnswer.SelectedOptions.Where(s => !correctOptions.Contains(s, StringComparer.OrdinalIgnoreCase)).Count();
                                
                                decimal scoreRatio = (decimal)correctSelected / correctOptions.Count;
                                decimal penalty = incorrectSelected * 0.1m; // 0.1 penalty per incorrect selection
                                
                                earnedPoints = Math.Max(0, scoreRatio - penalty); // Clamp at 0
                                earnedPoints = Math.Round(earnedPoints, 2); // Round to 2 decimal places

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
                else
                {
                    feedbackMessage = "Not answered.";
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

            return new GradeResultDto
            {
                AttemptId = attempt.Id,
                QuizId = attempt.QuizId,
                QuizTitle = attempt.Quiz!.Title ?? "Unknown Quiz",
                Score = attempt.Score,
                PassingScore = attempt.Quiz!.PassingScore,
                Passed = attempt.Score >= attempt.Quiz!.PassingScore,
                GradedAt = attempt.GradedAt.Value,
                QuestionFeedbacks = questionFeedbacks
            };
        }


        // Instructor-facing methods
        public async Task<int> CreateQuizAsync(QuizEditViewModel model)
        {
            var quiz = new Quiz
            {
                LessonId = model.LessonId,
                Title = model.Title,
                PassingScore = model.PassingScore,
                AttemptsAllowed = model.AttemptsAllowed
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
                quiz.PassingScore = model.PassingScore;
                quiz.AttemptsAllowed = model.AttemptsAllowed;
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
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) return null;

            return new QuizEditViewModel
            {
                Id = quiz.Id,
                LessonId = quiz.LessonId,
                Title = quiz.Title,
                PassingScore = quiz.PassingScore,
                AttemptsAllowed = quiz.AttemptsAllowed
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
