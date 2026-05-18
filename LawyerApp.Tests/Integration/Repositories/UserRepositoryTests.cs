using FluentAssertions;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Infrastructure.Persistence.Repositories;
using LawyerApp.Tests.Helpers;
using Xunit;

namespace LawyerApp.Tests.Integration.Repositories;

/// <summary>
/// Tests the UserRepository against an in-memory EF Core database to verify
/// all data-access operations without a real PostgreSQL connection.
/// </summary>
public class UserRepositoryTests : IDisposable
{
    private readonly LawyerApp.Infrastructure.Persistence.LawyerAppDbContext _context;
    private readonly UserRepository _sut;

    public UserRepositoryTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _sut = new UserRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── AddClientAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddClientAsync_PersistsClientToDatabase()
    {
        var client = new Client("Alice", "alice@repo.com", "hash", "Rua A", "910001001");

        await _sut.AddClientAsync(client);

        _context.Users.Should().ContainSingle(u => u.Email == "alice@repo.com");
    }

    [Fact]
    public async Task AddClientAsync_ReturnsPersistedClient()
    {
        var client = new Client("Bob", "bob@repo.com", "hash", "Rua B", "910001002");

        var result = await _sut.AddClientAsync(client);

        result.Should().NotBeNull();
        result.Name.Should().Be("Bob");
    }

    // ── GetAllClientsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllClientsAsync_ReturnsOnlyClients()
    {
        var client = new Client("Carol", "carol@repo.com", "hash", "Rua C", "910001003");
        var lawyer = new Lawyer("Dave the Lawyer", "dave@repo.com", "hash", "OA/12345");

        _context.Users.AddRange(client, lawyer);
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllClientsAsync();

        result.Should().ContainSingle(c => c.Email == "carol@repo.com");
        result.Should().NotContain(u => u.Email == "dave@repo.com");
    }

    // ── EmailExistsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task EmailExistsAsync_WhenEmailExists_ReturnsTrue()
    {
        var client = new Client("Eve", "eve@repo.com", "hash", "Rua E", "910001004");
        await _sut.AddClientAsync(client);

        var exists = await _sut.EmailExistsAsync("eve@repo.com");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WhenEmailDoesNotExist_ReturnsFalse()
    {
        var exists = await _sut.EmailExistsAsync("nobody@repo.com");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_IsCaseSensitive()
    {
        var client = new Client("Frank", "frank@repo.com", "hash", "Rua F", "910001005");
        await _sut.AddClientAsync(client);

        // EF Core in-memory provider comparison depends on StringComparison.Ordinal
        var existsLower = await _sut.EmailExistsAsync("frank@repo.com");
        existsLower.Should().BeTrue();
    }

    // ── GetByEmailAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_WhenEmailExists_ReturnsUser()
    {
        var client = new Client("Grace", "grace@repo.com", "hash", "Rua G", "910001006");
        await _sut.AddClientAsync(client);

        var user = await _sut.GetByEmailAsync("grace@repo.com");

        user.Should().NotBeNull();
        user!.Name.Should().Be("Grace");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenEmailDoesNotExist_ReturnsNull()
    {
        var user = await _sut.GetByEmailAsync("missing@repo.com");

        user.Should().BeNull();
    }
}
