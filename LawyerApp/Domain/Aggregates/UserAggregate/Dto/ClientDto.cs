namespace LawyerApp.Domain.Aggregates.UserAggregate.Dto
{
    public class ClientDto
    {
        public string Name { get; } = string.Empty;
        public string Email { get; } = string.Empty;
        public string BillingAddress { get; } = string.Empty;
        public string PhoneNumber { get; } = string.Empty;
        public ClientDto() { }
        public ClientDto(string name, string email, string billingAddress, string phoneNumber)
        {
            Name = name;
            Email = email;
            BillingAddress = billingAddress;
            PhoneNumber = phoneNumber;
        }
    }
}
