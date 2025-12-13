using Learnly.Models;
using System.Collections.Generic;

namespace Learnly.ViewModels
{
    public class UserWithRolesViewModel
    {
        public required ApplicationUser User { get; set; }
        public required IList<string> Roles { get; set; }
    }
}
