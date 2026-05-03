using LawyerApp.Domain.Shared;

namespace LawyerApp.Domain.Aggregates.UserAggregate
{
    public class Client : User
    {
        public string BillingAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // EF Core
        private Client() : base() { }

        public Client(string name, string email, string passwordHash, string billingaddress,string phonenumber)
            : base(name, email, passwordHash,Roles.Client)
        {
            BillingAddress = billingaddress;
            PhoneNumber = phonenumber;
        }

    }
}
