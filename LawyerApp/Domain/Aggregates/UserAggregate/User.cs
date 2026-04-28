using LawyerApp.Domain.Aggregates.UserAggregate.Dto;

namespace LawyerApp.Domain.Aggregates.UserAggregate;

public abstract class User
{
    public int UserId { get; protected set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Armazenamos apenas o Hash, nunca a password em plain-text!
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Construtor protegido para uso do EF Core e classes filhas
    protected User() { }

    protected User(string name, string email, string passwordHash)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
    }

}