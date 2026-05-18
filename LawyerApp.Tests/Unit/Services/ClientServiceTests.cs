using FluentAssertions;
using LawyerApp.Application.Services.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Dto;
using LawyerApp.Domain.Interfaces.Security;
using Moq;
using Xunit;

namespace LawyerApp.Tests.Unit.Services;

public class ClientServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly ClientService _sut;

    public ClientServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _hasherMock = new Mock<IPasswordHasher>();
        _sut = new ClientService(_userRepoMock.Object, _hasherMock.Object);
    }

    // ── CreateClientAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateClientAsync_WhenEmailIsNew_ReturnsClientDto()
    {
        var dto = new CreateClientDto("Alice", "alice@example.com", "S3cur3!", "Rua A", "912000001");
        var hashed = "hashed_S3cur3!";

        _userRepoMock.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _hasherMock.Setup(h => h.HashPassword(dto.Password)).Returns(hashed);
        _userRepoMock
            .Setup(r => r.AddClientAsync(It.IsAny<Client>()))
            .ReturnsAsync((Client c) => c);

        var result = await _sut.CreateClientAsync(dto);

        result.Name.Should().Be("Alice");
        result.Email.Should().Be("alice@example.com");
        result.BillingAddress.Should().Be("Rua A");
        result.PhoneNumber.Should().Be("912000001");
    }

    [Fact]
    public async Task CreateClientAsync_WhenEmailAlreadyExists_ThrowsException()
    {
        var dto = new CreateClientDto("Bob", "bob@example.com", "Pass1!", "Rua B", "912000002");

        _userRepoMock.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        await _sut.Invoking(s => s.CreateClientAsync(dto))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Email*");
    }

    [Fact]
    public async Task CreateClientAsync_HashesPasswordBeforePersisting()
    {
        var dto = new CreateClientDto("Carol", "carol@example.com", "PlainText", "Rua C", "912000003");
        var hashed = "bcrypt_hash_value";

        _userRepoMock.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _hasherMock.Setup(h => h.HashPassword("PlainText")).Returns(hashed);
        _userRepoMock
            .Setup(r => r.AddClientAsync(It.Is<Client>(c => c.PasswordHash == hashed)))
            .ReturnsAsync((Client c) => c);

        await _sut.CreateClientAsync(dto);

        _userRepoMock.Verify(
            r => r.AddClientAsync(It.Is<Client>(c => c.PasswordHash == hashed)),
            Times.Once);
    }

    [Fact]
    public async Task CreateClientAsync_NeverStoresPlainTextPassword()
    {
        var dto = new CreateClientDto("Dave", "dave@example.com", "PlainText", "Rua D", "912000004");

        _userRepoMock.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("some_hash");
        _userRepoMock
            .Setup(r => r.AddClientAsync(It.IsAny<Client>()))
            .ReturnsAsync((Client c) => c);

        await _sut.CreateClientAsync(dto);

        _userRepoMock.Verify(
            r => r.AddClientAsync(It.Is<Client>(c => c.PasswordHash == "PlainText")),
            Times.Never);
    }

    // ── GetAllClientsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllClientsAsync_ReturnsMappedDtos()
    {
        var clients = new List<Client>
        {
            new Client("Eve", "eve@example.com", "hash1", "Rua E", "912000005"),
            new Client("Frank", "frank@example.com", "hash2", "Rua F", "912000006"),
        };

        _userRepoMock.Setup(r => r.GetAllClientsAsync()).ReturnsAsync(clients);

        var result = await _sut.GetAllClientsAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Eve");
        result[1].Email.Should().Be("frank@example.com");
    }

    [Fact]
    public async Task GetAllClientsAsync_WhenNoClients_ReturnsEmptyList()
    {
        _userRepoMock.Setup(r => r.GetAllClientsAsync()).ReturnsAsync(new List<Client>());

        var result = await _sut.GetAllClientsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllClientsAsync_DoesNotExposePasswordHash()
    {
        var clients = new List<Client>
        {
            new Client("Grace", "grace@example.com", "secret_hash", "Rua G", "912000007"),
        };

        _userRepoMock.Setup(r => r.GetAllClientsAsync()).ReturnsAsync(clients);

        var result = await _sut.GetAllClientsAsync();
        var dtoType = result[0].GetType();

        dtoType.GetProperty("PasswordHash").Should().BeNull(
            because: "ClientDto must not expose the password hash");
    }
}
