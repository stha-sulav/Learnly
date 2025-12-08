using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
        [Required, MaxLength(255)]
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ContentType ContentType { get; set; }
        public string? ContentPath { get; set; } // Path to video file, or markdown content
        public int DurationSeconds { get; set; } // For video content
        public byte[]? VideoData { get; set; } // Raw video data
        public int Order { get; set; }

        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!; // Required navigation property
    }
}
