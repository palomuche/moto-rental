using System.ComponentModel.DataAnnotations;

namespace MotoRentalApi.ViewModels
{
    public class LoginUserViewModel
    {

        [Required]
        public required string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "{0} must have between {2} and {1} characters")]
        public required string Password { get; set; }

    }
}
