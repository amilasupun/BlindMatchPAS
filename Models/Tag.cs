using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tag name is required.")]
        [StringLength(50, MinimumLength = 2)]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "Tag name can only contain letters and numbers.")]
        public string Name { get; set; } = string.Empty;
    }
}
