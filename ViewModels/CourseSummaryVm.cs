using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Learnly.ViewModels
{
    public class CourseSummaryVm
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Slug { get; set; }
        public required string ThumbnailPath { get; set; }
        public required string InstructorName { get; set; }
        public required string ShortDescription { get; set; }
        public decimal Price { get; set; }
        public int? ProgressPercent { get; set; }
        public bool IsPublished { get; set; }
    }
}
