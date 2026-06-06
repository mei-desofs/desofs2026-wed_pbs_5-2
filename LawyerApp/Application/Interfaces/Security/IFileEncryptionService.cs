namespace LawyerApp.Application.Interfaces.Security
{
    public interface IFileEncryptionService
    {
        Task EncryptStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken);
        Task DecryptStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken);
    }
}