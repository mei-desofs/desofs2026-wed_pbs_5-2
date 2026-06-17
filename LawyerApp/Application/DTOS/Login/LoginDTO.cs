using System.ComponentModel.DataAnnotations;

namespace LawyerApp.Application.DTOS.Auth
{
    public class LoginDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; }

        [Required]
        public string Password { get; }

        public LoginDTO(string email, string password)
        {
            Email = email;
            Password = password;
        }   
    }
}
