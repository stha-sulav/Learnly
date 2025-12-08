using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnly.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] 
        public string Name { get; set; } = string.Empty; // Initialize to empty string
        [MaxLength(500)] 
        public string? Description { get; set; } // Make nullable
        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; } // Make nullable
        public ICollection<Category> SubCategories { get; set; } = new HashSet<Category>(); // Initialize collection
        public ICollection<Course> Courses { get; set; } = new HashSet<Course>(); // Initialize collection
    }
}
