using LawyerApp.Domain.Shared;

namespace LawyerApp.Domain.Aggregates.UserAggregate;

public class LegalAssistant : User
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    // EF Core
    private LegalAssistant() : base() { }

    public LegalAssistant(string name, string email, string passwordHash, string employeeId)
        : base(name, email, passwordHash,Roles.LegalAssistant)
    {
        EmployeeId = employeeId;
    }
}