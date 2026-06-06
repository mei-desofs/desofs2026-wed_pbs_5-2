using LawyerApp.Application.DTOS.Login;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.Interfaces.Login;
using LawyerApp.Application.Interfaces.Security;
using LawyerApp.Application.Interfaces.User;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using LawyerApp.Domain.Aggregates.AuditAggregate;
using LawyerApp.Domain.Aggregates.AuditAggregate.Interfaces;
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
        private readonly IAuditRepository auditRepository;
        public LoginService(IUserRepository userRepository, IPasswordHasher passwordHasher,IJwtProvider jwtProvider,IClient _service, IAuditRepository auditRepository)
        {
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.jwtProvider = jwtProvider;
            this.clientService = _service;
            this.auditRepository = auditRepository;
        }

        public Task<Result<string>> AuthenticateAsync(string email, string password, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<LoginOutputDTO>> Login(string email, string password, string ipAddress, CancellationToken cancellation)
        {
            // Verify if the email is already in use
            User? userExists = await userRepository.GetByEmailAsync(email,cancellation);

            if (userExists is null)
            {
                await auditRepository.AddAsync(new AuditLog(Guid.Empty, "None", "Login", "User", email, ipAddress, false, 401, "User doesn't exist"), cancellation);
                return Result<LoginOutputDTO>.Failure(401,"Invalid credentials!");
            }

            if (userExists.LockoutEnd.HasValue && userExists.LockoutEnd.Value > DateTime.UtcNow)
            {
                await auditRepository.AddAsync(new AuditLog(userExists.Id, userExists.userRole.ToString(), "Login", "User", email, ipAddress, false, 403, "Account locked"), cancellation);
                return Result<LoginOutputDTO>.Failure(403, "Account is temporarily locked due to multiple failed login attempts.");
            }

            if (!passwordHasher.VerifyPassword(password, userExists.PasswordHash))
            {
                userExists.FailedLoginAttempts++;
                if (userExists.FailedLoginAttempts >= 5)
                {
                    userExists.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await userRepository.UpdateAsync(userExists, cancellation);

                await auditRepository.AddAsync(new AuditLog(userExists.Id, userExists.userRole.ToString(), "Login", "User", email, ipAddress, false, 401, "Invalid password"), cancellation);
                return Result<LoginOutputDTO>.Failure(401,"Invalid credentials!");
            }

            userExists.FailedLoginAttempts = 0;
            userExists.LockoutEnd = null;
            await userRepository.UpdateAsync(userExists, cancellation);

            string token = jwtProvider.Generate(userExists);

            await auditRepository.AddAsync(new AuditLog(userExists.Id, userExists.userRole.ToString(), "Login", "User", email, ipAddress, true, 200, "Logged in successfully"), cancellation);

            return Result<LoginOutputDTO>.Ok(new LoginOutputDTO(token,userExists.userRole.ToString()));
        }

    }
}
