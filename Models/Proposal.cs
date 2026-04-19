using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Models
{
    public class Proposal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, MinimumLength = 5)]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Title can only contain letters, numbers, spaces, and hyphens.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Abstract is required.")]
        [StringLength(1000)]
        public string Abstract { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tech Stack is required.")]
        [StringLength(200)]
        public string TechStack { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        [Required]
        public string StudentId { get; set; } = string.Empty;
        public ApplicationUser? Student { get; set; }

        [Required]
        public int TagId { get; set; }
        public Tag? Tag { get; set; }
    }
}
