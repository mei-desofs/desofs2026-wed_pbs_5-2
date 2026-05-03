using LawyerApp.Application.DTOS.Login;
using LawyerApp.Shared;

namespace LawyerApp.Application.Interfaces.Login
{
    public interface ILogin
    {
        Task<Result<string>> AuthenticateAsync(string email, string password, CancellationToken cancellation);
        Task<Result<LoginOutputDTO>> Login(string email, string password, CancellationToken cancellation);
    }
}
