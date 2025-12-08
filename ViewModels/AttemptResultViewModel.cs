namespace Learnly.ViewModels
{
    public class AttemptResultViewModel
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public required string QuizTitle { get; set; }
        public decimal Score { get; set; }
        public int PassingScore { get; set; }
        public bool Passed => Score >= PassingScore;
        // We could add a list of correct/incorrect answers for detailed feedback later
    }
}
