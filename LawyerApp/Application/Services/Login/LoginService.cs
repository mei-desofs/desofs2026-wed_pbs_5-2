using LawyerApp.Application.DTOS.Login;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.Interfaces.Login;
using LawyerApp.Application.Interfaces.Security;
using LawyerApp.Application.Interfaces.User;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using LawyerApp.Shared;
using LawyerApp.Shared.Abstractions;

namespace LawyerApp.Application.Services.Login
{
    public class LoginService : ILogin
    {
        private readonly IUserRepository userRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly IJwtProvider jwtProvider;
        private readonly IClient clientService;
        public LoginService(IUserRepository userRepository, IPasswordHasher passwordHasher,IJwtProvider jwtProvider,IClient _service)
        {
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.jwtProvider = jwtProvider;
            this.clientService = _service;
        }

        public Task<Result<string>> AuthenticateAsync(string email, string password, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<LoginOutputDTO>> Login(string email, string password, CancellationToken cancellation)
        {
            // Verify if the email is already in use
            User? userExists = await userRepository.GetByEmailAsync(email,cancellation);

            if (userExists is null)
            {
                return Result<LoginOutputDTO>.Failure(401,"User doesn´t exist!");
            }

            if (!passwordHasher.VerifyPassword(password, userExists.PasswordHash))
            {
                return Result<LoginOutputDTO>.Failure(401,"Invalid password!");
            }
            string token = jwtProvider.Generate(userExists);

            return Result<LoginOutputDTO>.Ok(new LoginOutputDTO(token,userExists.userRole.ToString()));
        }

    }
}
