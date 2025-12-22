using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Learnly.Models
{
    public class Quiz
    {
        public const int FixedPassingScore = 70; // Fixed passing score percentage

        public int Id { get; set; }

        public int ModuleId { get; set; }
        public Module? Module { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Title { get; set; }

        public ICollection<Question> Questions { get; set; } = new HashSet<Question>();
        public ICollection<Attempt> Attempts { get; set; } = new HashSet<Attempt>();
    }
}
