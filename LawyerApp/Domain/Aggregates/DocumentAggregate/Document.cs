namespace LawyerApp.Domain.Aggregates.DocumentAggregate;

public class Document
{
    public int DocumentId { get; private set; }
    public string FileName { get; set; } = string.Empty;

    public string StoredFileName { get; private set; } = string.Empty;

    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DocCategory Category { get; set; }
    public DateTime UploadedAt { get; private set; }

    // Chave Estrangeira para o Processo Jurídico
    public Guid LegalProcessId { get; set; }

    // EF Core
    private Document() { }

    public Document(string fileName, long fileSize, string contentType, DocCategory category, Guid legalProcessId)
    {
        FileName = fileName;
        FileSize = fileSize;
        ContentType = contentType;
        Category = category;
        LegalProcessId = legalProcessId;

        // Geramos um nome único para o disco
        // Assim o atacante não consegue prever ou manipular o caminho do ficheiro
        StoredFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        UploadedAt = DateTime.UtcNow;
    }
}