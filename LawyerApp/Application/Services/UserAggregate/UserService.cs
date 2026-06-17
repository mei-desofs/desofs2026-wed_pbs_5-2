using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.Interfaces.User;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using LawyerApp.Shared;

namespace LawyerApp.Application.Services.UserAggregate
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<List<UserDto>>> GetAllUsersAsync(CancellationToken cancellation)
        {
            var users = await _userRepository.GetAllUsersAsync(cancellation);
            var result = users.Select(u => new UserDto(u.Id, u.Name, u.Email, u.userRole.ToString())).ToList();
            return Result<List<UserDto>>.Success(result);
        }
    }
}
