using LawyerApp.Application.Interfaces.User;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Dto;
using LawyerApp.Domain.Interfaces.Security;

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

        public async Task<ClientDto> CreateClientAsync(CreateClientDto createUserObject)
        {
            // Verificar se o email já existe
            if (await _userRepository.EmailExistsAsync(createUserObject.Email))
            {
                throw new Exception("Email já está em uso.");
            }
            // Hash da password (exemplo simples, em produção use um método mais robusto)
            var hashedPassword = _passwordHasher.HashPassword(createUserObject.Password);

            // Criar o utilizador
            var user = new Client(createUserObject.Name, createUserObject.Email, hashedPassword, createUserObject.BillingAddress, createUserObject.PhoneNumber);
            // Adicionar ao repositório
            Client result = await _userRepository.AddClientAsync(user);

            return new ClientDto(result.Name, result.Email, result.BillingAddress, result.PhoneNumber);
        }

        public async Task<List<ClientDto>> GetAllClientsAsync()
        {
            var clientList = await _userRepository.GetAllClientsAsync();
            return clientList.Select(c => new ClientDto(c.Name, c.Email, c.BillingAddress, c.PhoneNumber)).ToList();
        }
    }
}
