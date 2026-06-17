using FluentAssertions;
using LawyerApp.Application.Services.Login;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using LawyerApp.Application.Interfaces.Security;
using LawyerApp.Domain.Shared;
using LawyerApp.Shared.Abstractions;
using Moq;
using Xunit;

namespace LawyerApp.Tests.Unit.Services;

public class LoginServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<LawyerApp.Application.Interfaces.User.IClient> _clientServiceMock;
    private readonly LoginService _sut;

    public LoginServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _hasherMock = new Mock<IPasswordHasher>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _clientServiceMock = new Mock<LawyerApp.Application.Interfaces.User.IClient>();

        _sut = new LoginService(
            _userRepoMock.Object,
            _hasherMock.Object,
            _jwtProviderMock.Object,
            _clientServiceMock.Object);
    }

    // ── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ReturnsFailureWith401()
    {
        _userRepoMock
            .Setup(r => r.GetByEmailAsync("unknown@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.Login("unknown@test.com", "pass", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("401");
    }

    [Fact]
    public async Task Login_WhenPasswordIsWrong_ReturnsFailureWith401()
    {
        var client = new Client("Alice", "alice@test.com", "hash", "Rua A", "910000001");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("alice@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _hasherMock
            .Setup(h => h.VerifyPassword("wrongpass", "hash"))
            .Returns(false);

        var result = await _sut.Login("alice@test.com", "wrongpass", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("401");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var client = new Client("Alice", "alice@test.com", "hash", "Rua A", "910000001");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("alice@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _hasherMock
            .Setup(h => h.VerifyPassword("correct!", "hash"))
            .Returns(true);
        _jwtProviderMock
            .Setup(j => j.Generate(client))
            .Returns("jwt_token_value");

        var result = await _sut.Login("alice@test.com", "correct!", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("jwt_token_value");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsUserRole()
    {
        var client = new Client("Bob", "bob@test.com", "hash", "Rua B", "910000002");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("bob@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _hasherMock
            .Setup(h => h.VerifyPassword("pass", "hash"))
            .Returns(true);
        _jwtProviderMock
            .Setup(j => j.Generate(client))
            .Returns("tok");

        var result = await _sut.Login("bob@test.com", "pass", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.User_Role.Should().Be(Roles.Client.ToString());
    }

    [Fact]
    public async Task Login_NeverCallsJwtProvider_WhenUserNotFound()
    {
        _userRepoMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await _sut.Login("ghost@test.com", "pass", CancellationToken.None);

        _jwtProviderMock.Verify(j => j.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_NeverCallsJwtProvider_WhenPasswordIsWrong()
    {
        var client = new Client("Carol", "carol@test.com", "hash", "Rua C", "910000003");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("carol@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _hasherMock
            .Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        await _sut.Login("carol@test.com", "wrong", CancellationToken.None);

        _jwtProviderMock.Verify(j => j.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithLawyerAccount_ReturnsLawyerRole()
    {
        var lawyer = new Lawyer("Dave", "dave@test.com", "hash", "OA/99999");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("dave@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(lawyer);
        _hasherMock
            .Setup(h => h.VerifyPassword("pass", "hash"))
            .Returns(true);
        _jwtProviderMock
            .Setup(j => j.Generate(lawyer))
            .Returns("lawyer_token");

        var result = await _sut.Login("dave@test.com", "pass", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.User_Role.Should().Be(Roles.Lawyer.ToString());
    }
}
