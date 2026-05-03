namespace LawyerApp.Application.DTOS.Users
{
    public class ClientDto
    {
        public string Name { get; } = string.Empty;
        public string Email { get; } = string.Empty;
        public ClientDto() { }
        public ClientDto(string name, string email)
        {
            Name = name;
            Email = email;
        }
    }
}
