using System.ComponentModel.DataAnnotations;

namespace LawyerApp.Application.DTOS.Users
{
    public class CreateClientDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get;  } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; } = string.Empty;

        [Required]
        [MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
            ErrorMessage = "Password must be at least 8 characters long and include uppercase, lowercase, number and special character.")]
        public string Password { get; } = string.Empty;

        [Required]
        public string BillingAddress { get; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; } = string.Empty;

        public CreateClientDto(string name, string email, string password, string billingAddress, string phoneNumber)
        {
            Name = name;
            Email = email;
            Password = password;
            BillingAddress = billingAddress;
            PhoneNumber = phoneNumber;
        }
    }
}
