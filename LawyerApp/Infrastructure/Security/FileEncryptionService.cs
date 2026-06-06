using System.Security.Cryptography;
using System.Security.Cryptography;
using LawyerApp.Application.Interfaces.Security;

namespace LawyerApp.Infrastructure.Security
{
    public class FileEncryptionService : IFileEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public FileEncryptionService(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var keyStr = configuration["FileEncryption:Key"];
            var ivStr = configuration["FileEncryption:IV"];

            if (string.IsNullOrEmpty(keyStr) || string.IsNullOrEmpty(ivStr))
            {
                // Fallback for demo
                _key = System.Text.Encoding.UTF8.GetBytes("a-very-secret-key-32-chars-long!!");
                _iv = System.Text.Encoding.UTF8.GetBytes("a-secret-iv-16-!");
            }
            else
            {
                _key = Convert.FromBase64String(keyStr);
                _iv = Convert.FromBase64String(ivStr);
            }
        }

        public async Task EncryptStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var cryptoStream = new CryptoStream(destination, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await source.CopyToAsync(cryptoStream, cancellationToken);
            await cryptoStream.FlushFinalBlockAsync(cancellationToken);
        }

        public async Task DecryptStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var cryptoStream = new CryptoStream(source, aes.CreateDecryptor(), CryptoStreamMode.Read);
            await cryptoStream.CopyToAsync(destination, cancellationToken);
        }
    }
}