using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Learnly.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        
        public int LessonId { get; set; }
        public Lesson? Lesson { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Title { get; set; }

        public int PassingScore { get; set; } // As a percentage, e.g., 70
        
        public int? AttemptsAllowed { get; set; } // Nullable for unlimited attempts

        public ICollection<Question> Questions { get; set; } = new HashSet<Question>();
        public ICollection<Attempt> Attempts { get; set; } = new HashSet<Attempt>();
    }
}
