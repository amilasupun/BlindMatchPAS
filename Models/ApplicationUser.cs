using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Full name can only contain letters.")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
