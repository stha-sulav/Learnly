namespace Learnly.ViewModels
{
    public class QuestionFeedbackDto
    {
        public int QuestionId { get; set; }
        public decimal EarnedPoints { get; set; }
        public decimal PossiblePoints { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
