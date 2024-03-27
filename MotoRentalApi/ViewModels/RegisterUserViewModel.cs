using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MotoRentalApi.ViewModels
{
    public class RegisterUserViewModel
    {

        [Required]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "{0} must have between {2} and {1} characters")]
        public required string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public required string ConfirmPassword { get; set; }
    }
}
