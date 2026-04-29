namespace LawyerApp.Domain.Aggregates.UserAggregate.Dto
{
    public class CreateClientDto
    {
        public string Name { get;  } = string.Empty;
        public string Email { get; } = string.Empty;

        public string Password { get; } = string.Empty;
        public string BillingAddress { get; } = string.Empty;
        public string PhoneNumber { get; } = string.Empty;
        public CreateClientDto() { }
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
