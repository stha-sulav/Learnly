using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class CommentLike
    {
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }
        public Comment Comment { get; set; } = null!; // Required navigation property

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!; // Required navigation property

        public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    }
}
