using LawyerApp.Domain.Shared;

namespace LawyerApp.Domain.Aggregates.UserAggregate;

public abstract class User
{
    public Guid Id { get; protected set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Armazenamos apenas o Hash, nunca a password em plain-text!
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public Roles userRole { get; set; }

    // Construtor protegido para uso do EF Core e classes filhas
    protected User() { }

    protected User(string name, string email, string passwordHash, Roles userRole)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        this.userRole = userRole;
    }

}