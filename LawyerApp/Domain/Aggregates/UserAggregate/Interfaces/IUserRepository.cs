using LawyerApp.Domain.Aggregates.UserAggregate;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IUserRepository
{
    // Standard CRUD
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<Client>> GetAllClientsAsync();
    Task<Client> AddClientAsync(Client user);
  
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);

    // Domain-Specific Methods
    Task<User?> GetByEmailAsync(string email); // Essential for Login/Auth
    Task<bool> EmailExistsAsync(string email); // Essential for Registration validation
}