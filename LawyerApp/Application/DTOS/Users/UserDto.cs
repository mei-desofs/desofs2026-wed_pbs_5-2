using LawyerApp.Domain.Shared;

namespace LawyerApp.Application.DTOS.Users
{
    public class UserDto
    {
        public Guid Id { get; }
        public string Name { get; } = string.Empty;
        public string Email { get; } = string.Empty;
        public string Role { get; } = string.Empty;

        public UserDto() { }
        public UserDto(Guid id, string name, string email, string role)
        {
            Id = id;
            Name = name;
            Email = email;
            Role = role;
        }
    }
}
