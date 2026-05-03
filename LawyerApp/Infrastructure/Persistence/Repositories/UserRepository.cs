using LawyerApp.Application.DTOS.Users;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LawyerApp.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LawyerAppDbContext _context;

        // Injeção do DbContext via construtor
        public UserRepository(LawyerAppDbContext context)
        {
            _context = context;
        }

        // GET 
        public async Task<IEnumerable<Client>> GetAllClientsAsync(CancellationToken cancellation)
        {
            // Retorna todos os utilizadores (Atenção: em produção pode precisar de paginação)
            return await _context.Users.OfType<Client>().ToListAsync(cancellation);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellation)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellation);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellation)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellation);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellation)
        {
            // O AnyAsync é mais leve na base de dados do que ir buscar o registo inteiro
            return await _context.Users.AnyAsync(u => u.Email == email, cancellation);
        }

        // ADD
        public async Task<User> AddAsync(User user, CancellationToken cancellation)
        {
            await _context.Users.AddAsync(user, cancellation);
            await _context.SaveChangesAsync(cancellation);
            return user;
        }

        public async Task<User> AddClientAsync(CreateClientDto user, CancellationToken cancellation)
        {
            Client client = new Client(user.Name, user.Email, user.Password, user.BillingAddress, user.PhoneNumber);
            var result = await _context.Users.AddAsync(client, cancellation);
            await _context.SaveChangesAsync(cancellation);
            return result.Entity;
        }

        // UPDATE
        public async Task UpdateAsync(User user, CancellationToken cancellation)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellation);
        }

        // DELETE
        public async Task DeleteAsync(Guid id, CancellationToken cancellation)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellation);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(cancellation);
            }
        }
    }
}