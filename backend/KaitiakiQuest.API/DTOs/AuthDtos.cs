using System.ComponentModel.DataAnnotations;

namespace KaitiakiQuest.API.DTOs
{
    public class AuthDtos
    {
        // Register request
        public class RegisterDto
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [MinLength(6)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [MinLength(2)]
            public string Username { get; set; } = string.Empty;
        }

        // Login request
        public class LoginDto
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;
        }

        // Login response (return Token)
        public class AuthResponseDto
        {
            public string Token { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string UserName {  get; set; } = string.Empty;
            public int TotalXP { get; set; }
            public int Level { get; set; }
            public List<string> Roles { get; set; } = new List<string>();

        }

    }
}
