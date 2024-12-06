using System.ComponentModel.DataAnnotations;

namespace Budget_Baddie.Models
{
    public class LogModel
    {
        [Key]
        public int UserId { get; set; }
        // Login Properties
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }

        // Registration Properties
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }
    }
}
