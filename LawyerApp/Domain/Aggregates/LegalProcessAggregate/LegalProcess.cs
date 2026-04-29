using LawyerApp.Domain.Aggregates.UserAggregate;

namespace LawyerApp.Domain.Aggregates.LegalProcessAggregate;

public class LegalProcess
{
    // Usamos GUID conforme definido para evitar previsibilidade de IDs (Security Best Practice)
    public Guid ProcessId { get; private set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProcessStatus Status { get; set; }
    public DateTime OpenedAt { get; private set; }

    // Chaves Estrangeiras para os outros agregados
    public Guid LawyerId { get; set; }
    public Guid ClientId { get; set; }

    // EF Core
    private LegalProcess() { }

    public LegalProcess(string title, string description, Guid lawyerId, Guid clientId)
    {
        // O GUID é gerado no domínio para garantir que temos o ID antes de criar a pasta no SO
        ProcessId = Guid.NewGuid();
        Title = title;
        Description = description;
        LawyerId = lawyerId;
        ClientId = clientId;
        Status = ProcessStatus.Open;
        OpenedAt = DateTime.UtcNow;
    }

}