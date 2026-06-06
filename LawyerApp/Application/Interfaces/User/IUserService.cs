using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Shared;

namespace LawyerApp.Application.Interfaces.User
{
    public interface IUserService
    {
        Task<Result<List<UserDto>>> GetAllUsersAsync(CancellationToken cancellation = default);
    }
}
