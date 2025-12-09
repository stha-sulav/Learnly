using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // For IFormFile

namespace Learnly.ViewModels
{
    public class ManageAccountViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Profile Picture")]
        public string? ProfilePicturePath { get; set; }

        [Display(Name = "Upload New Profile Picture")]
        public IFormFile? ProfilePictureFile { get; set; }
    }
}
