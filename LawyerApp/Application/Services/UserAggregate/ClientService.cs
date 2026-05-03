using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.Interfaces.Security;
using LawyerApp.Application.Interfaces.User;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using LawyerApp.Shared;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LawyerApp.Application.Services.UserAggregate
{
    public class ClientService : IClient
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public ClientService(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result<ClientDto>> CreateClientAsync(CreateClientDto client, CancellationToken cancellationToken)
        {
            var emailExists = _userRepository.EmailExistsAsync(client.Email, cancellationToken).Result;
            if (emailExists)
            {
                return Result<ClientDto>.Failure(400, "Email already in use!");
            }
            var passwordHash = _passwordHasher.HashPassword(client.Password);

            var clientToCreate = new CreateClientDto(client.Name, client.Email, passwordHash,client.BillingAddress,client.PhoneNumber);
            var createdClient = await _userRepository.AddClientAsync(clientToCreate, cancellationToken);

            return Result<ClientDto>.Success(new ClientDto(createdClient.Name, createdClient.Email));
        }

        public async Task<Result<List<ClientDto>>> GetAllClientsAsync(CancellationToken cancellation)
        {
            var clientList = await _userRepository.GetAllClientsAsync(cancellation);
            var result = clientList.Select(c => new ClientDto(c.Name, c.Email)).ToList();
            return Result<List<ClientDto>>.Success(result);
        }

    }
}
