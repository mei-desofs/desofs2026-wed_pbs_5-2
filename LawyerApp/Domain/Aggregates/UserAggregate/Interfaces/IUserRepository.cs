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
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellation);
        Task<IEnumerable<Client>> GetAllClientsAsync(CancellationToken cancellation);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellation);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellation);

        // ADD
        Task<User> AddAsync(User user, CancellationToken cancellation);
        Task<User> AddClientAsync(CreateClientDto user, CancellationToken cancellation);

        // UPDATE
        Task UpdateAsync(User user, CancellationToken cancellation);

        // DELETE
        Task DeleteAsync(Guid id, CancellationToken cancellation);
    }
}