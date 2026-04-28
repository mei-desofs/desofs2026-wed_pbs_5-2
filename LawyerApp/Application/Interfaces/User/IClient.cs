using LawyerApp.Domain.Aggregates.UserAggregate.Dto;

namespace LawyerApp.Application.Interfaces.User
{
    public interface IClient
    {
        Task<ClientDto> CreateClientAsync(CreateClientDto createUserObject);
        Task<List<ClientDto>> GetAllClientsAsync();
    }
}
