using LawyerApp.Application.DTOS.Users;
using LawyerApp.Domain.Aggregates.UserAggregate;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LawyerApp.Domain.Aggregates.UserAggregate.Interfaces
{
    public interface IUserRepository
    {
        // GET
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellation = default);
        Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellation = default);
        Task<IEnumerable<Client>> GetAllClientsAsync(CancellationToken cancellation = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellation = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellation = default);

        // ADD
        Task<User> AddAsync(User user, CancellationToken cancellation = default);
        Task<User> AddClientAsync(CreateClientDto user, CancellationToken cancellation = default);

        // UPDATE
        Task UpdateAsync(User user, CancellationToken cancellation = default);

        // DELETE
        Task DeleteAsync(Guid id, CancellationToken cancellation = default);
    }
}