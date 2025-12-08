using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class Attempt
    {
        public int Id { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public int QuizId { get; set; }
        public Quiz? Quiz { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal Score { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Storing user's answers as JSON
        public string? Answers { get; set; }

        public bool IsGraded { get; set; } = false;
        public DateTime? GradedAt { get; set; }
        public string? Feedback { get; set; } // Stores JSON of List<QuestionFeedbackDto>
    }
}
