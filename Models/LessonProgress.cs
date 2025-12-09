using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class LessonProgress
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!; // Required navigation property
        
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!; // Required navigation property
        
        public bool IsCompleted { get; set; } = false;
        public int PositionSeconds { get; set; } = 0; // For video/audio content, where the user left off
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
