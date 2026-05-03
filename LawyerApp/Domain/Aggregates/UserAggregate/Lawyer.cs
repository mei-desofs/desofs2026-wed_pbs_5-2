using LawyerApp.Domain.Shared;

namespace LawyerApp.Domain.Aggregates.UserAggregate;

public class Lawyer : User
{
    // Cédula Profissional - Identificador único no mundo real
    public string LicenseNumber { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    // EF Core
    private Lawyer() : base() { }

    public Lawyer(string name, string email, string passwordHash, string licenseNumber)
        : base(name, email, passwordHash,Roles.Lawyer)
    {
        LicenseNumber = licenseNumber;
    }
}