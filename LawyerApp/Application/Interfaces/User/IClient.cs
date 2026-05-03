using LawyerApp.Application.DTOS.Users;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Shared;

namespace LawyerApp.Application.Interfaces.User
{
    public interface IClient
    {
        Task<Result<List<ClientDto>>> GetAllClientsAsync(CancellationToken cancellation);
        Task<Result<ClientDto>> CreateClientAsync(CreateClientDto client, CancellationToken cancellationToken);
    }
}
