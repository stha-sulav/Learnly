using System;

namespace Learnly.ViewModels
{
    public class ReviewVm
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCurrentUserReview { get; set; }
    }

    public class CreateReviewRequest
    {
        public int CourseId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class UpdateReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class CourseReviewsVm
    {
        public List<ReviewVm> Reviews { get; set; } = new List<ReviewVm>();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public ReviewVm? CurrentUserReview { get; set; }
    }
}
