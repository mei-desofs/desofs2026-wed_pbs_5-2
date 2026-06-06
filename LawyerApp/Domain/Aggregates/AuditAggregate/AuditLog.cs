using LawyerApp.Domain.Shared;

namespace LawyerApp.Domain.Aggregates.AuditAggregate;

public class AuditLog
{
    public int Id { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public Guid UserId { get; private set; }
    public string UserRole { get; private set; } = string.Empty;
    public string Operation { get; private set; } = string.Empty; // e.g., Create Process, Upload Document
    public string Resource { get; private set; } = string.Empty; // e.g., Process, Document
    public string ResourceId { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public int StatusCode { get; private set; }
    public string Details { get; private set; } = string.Empty;

    private AuditLog() { }

    public AuditLog(Guid userId, string userRole, string operation, string resource, string resourceId, string ipAddress, bool success, int statusCode, string details)
    {
        TimestampUtc = DateTime.UtcNow;
        UserId = userId;
        UserRole = userRole;
        Operation = operation;
        Resource = resource;
        ResourceId = resourceId;
        IpAddress = ipAddress;
        Success = success;
        StatusCode = statusCode;
        Details = details;
    }
}
