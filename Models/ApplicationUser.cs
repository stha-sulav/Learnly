using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Learnly.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? FirstName { get; set; }
        [PersonalData]
        public string? LastName { get; set; }
        [PersonalData]
        public string? DisplayName { get; set; }
        public string? ProfilePicturePath { get; set; }
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }

        public ICollection<Course> CoursesCreated { get; set; } = new HashSet<Course>();
    }
}
