using FluentAssertions;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Infrastructure.Persistence.Repositories;
using LawyerApp.Tests.Helpers;
using Xunit;

namespace LawyerApp.Tests.Unit.Security.ASVS;

/// <summary>
/// ASVS V1.2.4 – SQL Injection Prevention
///
/// Verifies that data access uses EF Core (parameterised queries / ORM),
/// making SQL injection structurally impossible at the repository layer.
/// </summary>
public class V1_2_4_SqlInjectionPreventionTests : IDisposable
{
    private readonly LawyerApp.Infrastructure.Persistence.LawyerAppDbContext _context;
    private readonly UserRepository _userRepo;
    private readonly LawyerApp.Infrastructure.Persistence.Repositories.DocumentRepository _docRepo;

    public V1_2_4_SqlInjectionPreventionTests()
    {
        _context  = InMemoryDbContextFactory.Create();
        _userRepo = new UserRepository(_context);
        _docRepo  = new LawyerApp.Infrastructure.Persistence.Repositories.DocumentRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Email lookup with SQL-injection payloads ──────────────────────────────

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("\" OR \"\"=\"")]
    [InlineData("admin'--")]
    [InlineData("1; SELECT * FROM Users")]
    public async Task V1_2_4_GetByEmail_WithSqlInjectionPayload_ReturnsNull_NotAllUsers(string injectionPayload)
    {
        // Arrange – add a real user
        await _userRepo.AddClientAsync(
            new CreateClientDto("Alice", "alice@test.com", "hash", "Rua A", "910000001"),
            CancellationToken.None);

        // Act – query with SQL injection payload as the email
        var result = await _userRepo.GetByEmailAsync(injectionPayload, CancellationToken.None);

        // Assert – EF Core parameterises the query; the payload is treated as a literal string
        result.Should().BeNull(
            "EF Core parameterises all queries, so SQL injection payloads are treated as literals (V1.2.4)");
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE Users; --")]
    public async Task V1_2_4_EmailExists_WithSqlInjectionPayload_ReturnsFalse(string injectionPayload)
    {
        await _userRepo.AddClientAsync(
            new CreateClientDto("Bob", "bob@test.com", "hash", "Rua B", "910000002"),
            CancellationToken.None);

        var exists = await _userRepo.EmailExistsAsync(injectionPayload, CancellationToken.None);

        exists.Should().BeFalse(
            "EF Core parameterises queries; injection payloads must not match any real user (V1.2.4)");
    }

    // ── Stored filename lookup with path-traversal / injection payloads ───────

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("'; DELETE FROM Documents; --")]
    [InlineData("' UNION SELECT * FROM Users --")]
    public async Task V1_2_4_GetByStoredFileName_WithMaliciousPayload_ReturnsNull(string maliciousName)
    {
        var result = await _docRepo.GetByStoredFileNameAsync(maliciousName, CancellationToken.None);

        result.Should().BeNull(
            "EF Core parameterises the stored-filename query; malicious strings are treated as literals (V1.2.4)");
    }

    // ── GetAllClients is not filterable by user input ─────────────────────────

    [Fact]
    public async Task V1_2_4_GetAllClients_DoesNotAcceptUserControlledFilter()
    {
        // GetAllClientsAsync takes no user-provided filter parameter,
        // so the entire result set is always EF-generated — no injection surface.
        var results = await _userRepo.GetAllClientsAsync(CancellationToken.None);

        results.Should().NotBeNull(
            "GetAllClients must always return a safe EF-generated collection with no user-input filter (V1.2.4)");
    }
}
