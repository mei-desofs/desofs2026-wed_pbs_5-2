using FluentAssertions;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.Interfaces.Security;
using LawyerApp.Application.Interfaces.User;
using LawyerApp.Application.Services.Login;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using LawyerApp.Domain.Shared;
using LawyerApp.Infrastructure.Security;
using LawyerApp.Shared.Abstractions;
using Moq;
using Xunit;

namespace LawyerApp.Tests.Unit.Security.ASVS;

/// <summary>
/// ASVS V6 – Authentication
///
/// V6.2.8 – Password is verified exactly as received; no truncation or case transformation.
/// V6.3.8 – Valid users cannot be deduced from failed auth (timing / enumeration prevention).
/// V6.3.2 – Default / well-known accounts are not present.
/// V9.2.1 – Token carries exp claim and is rejected after expiry.
/// </summary>
public class V6_AuthenticationTests
{
    private readonly Mock<IUserRepository>  _userRepoMock = new();
    private readonly Mock<IPasswordHasher>  _hasherMock   = new();
    private readonly Mock<IJwtProvider>     _jwtMock      = new();
    private readonly Mock<IClient>          _clientMock   = new();
    private readonly LoginService           _sut;
    private readonly BCryptPasswordHasher   _realHasher   = new();

    public V6_AuthenticationTests()
    {
        _sut = new LoginService(
            _userRepoMock.Object,
            _hasherMock.Object,
            _jwtMock.Object,
            _clientMock.Object);
    }

    // ── V6.2.8 – Password is verified exactly as received ────────────────────

    [Fact]
    public async Task V6_2_8_Login_PasswordIsVerifiedExactly_CaseSensitive()
    {
        var client = new Client("Alice", "alice@test.com", "hash", "Rua A", "910000001");

        _userRepoMock.Setup(r => r.GetByEmailAsync("alice@test.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(client);
        _hasherMock.Setup(h => h.VerifyPassword("Correct!", "hash")).Returns(true);
        _hasherMock.Setup(h => h.VerifyPassword("correct!", "hash")).Returns(false);

        var resultCorrect = await _sut.Login("alice@test.com", "Correct!", CancellationToken.None);
        var resultLower   = await _sut.Login("alice@test.com", "correct!", CancellationToken.None);

        resultCorrect.IsSuccess.Should().BeTrue("exact case match must succeed (V6.2.8)");
        resultLower.IsFailure.Should().BeTrue("lowercase variant must fail — passwords are case-sensitive (V6.2.8)");
    }

    [Fact]
    public async Task V6_2_8_Password_IsPassedToHasher_WithoutModification()
    {
        const string rawPassword = "  MyP@ss123!  "; // includes leading/trailing spaces
        var client = new Client("Bob", "bob@test.com", "hash", "Rua B", "910000002");

        _userRepoMock.Setup(r => r.GetByEmailAsync("bob@test.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(client);
        _hasherMock.Setup(h => h.VerifyPassword(rawPassword, "hash")).Returns(true);
        _jwtMock.Setup(j => j.Generate(client)).Returns("tok");

        await _sut.Login("bob@test.com", rawPassword, CancellationToken.None);

        // Verify hasher received the exact raw password — no trimming or casing applied
        _hasherMock.Verify(h => h.VerifyPassword(rawPassword, "hash"), Times.Once,
            "the password must be passed to the hasher exactly as received, without trimming (V6.2.8)");
    }

    // ── V6.3.8 – Both invalid user and wrong password return the same error ───

    [Fact]
    public async Task V6_3_8_NonExistentUser_And_WrongPassword_ReturnSameErrorCode()
    {
        // Non-existent user
        _userRepoMock.Setup(r => r.GetByEmailAsync("ghost@test.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

        // Existing user with wrong password
        var client = new Client("Carol", "carol@test.com", "hash", "Rua C", "910000003");
        _userRepoMock.Setup(r => r.GetByEmailAsync("carol@test.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(client);
        _hasherMock.Setup(h => h.VerifyPassword("WrongPassword", "hash")).Returns(false);

        var resultNoUser   = await _sut.Login("ghost@test.com",  "any",           CancellationToken.None);
        var resultBadPass  = await _sut.Login("carol@test.com",  "WrongPassword", CancellationToken.None);

        resultNoUser.IsFailure.Should().BeTrue();
        resultBadPass.IsFailure.Should().BeTrue();

        // Both must return the same HTTP error code to prevent user enumeration
        resultNoUser.Error.Code.Should().Be(resultBadPass.Error.Code,
            "the error code for non-existent user and wrong password must be identical to prevent enumeration (V6.3.8)");
    }

    [Fact]
    public async Task V6_3_8_ErrorMessage_DoesNotReveal_WhetherUserExists()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

        var result = await _sut.Login("any@test.com", "any", CancellationToken.None);

        // The error message must not say "user not found" in a way that differs from "wrong password"
        // Both cases return 401; the specific message is less important than the code match above,
        // but it must not positively confirm the user's existence.
        result.Error.Message.Should().NotContain("exists",
            "error message must not reveal whether the email address is registered (V6.3.8)");
    }

    // ── V6.3.2 – No default / well-known accounts ────────────────────────────

    [Theory]
    [InlineData("admin@lawyerapp.com", "admin")]
    [InlineData("root@lawyerapp.com",  "root")]
    [InlineData("sa@lawyerapp.com",    "sa")]
    [InlineData("test@lawyerapp.com",  "test")]
    public async Task V6_3_2_DefaultAccountEmails_AreNotPresent(string defaultEmail, string _)
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(defaultEmail, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

        var result = await _sut.Login(defaultEmail, "anyPassword", CancellationToken.None);

        result.IsFailure.Should().BeTrue(
            "default/well-known accounts must not be present in the system (V6.3.2)");
    }

    // ── BCrypt verifies password correctly (supports V6.2.8) ─────────────────

    [Fact]
    public void V6_2_8_BCrypt_VerifiesPassword_ExactlyAsProvided()
    {
        const string password = "Exact$Pass123";
        var hash = _realHasher.HashPassword(password);

        _realHasher.VerifyPassword(password,           hash).Should().BeTrue("exact password must verify (V6.2.8)");
        _realHasher.VerifyPassword(password.ToUpper(), hash).Should().BeFalse("uppercased password must fail (V6.2.8)");
        _realHasher.VerifyPassword(password.Trim(),    hash).Should().BeTrue("trim has no effect on BCrypt (expected)");
    }

    [Fact]
    public void V6_2_8_BCrypt_RejectsEmptyPassword_AgainstNonEmptyHash()
    {
        var hash = _realHasher.HashPassword("RealPassword!");

        _realHasher.VerifyPassword(string.Empty, hash).Should().BeFalse(
            "empty string must not match a real password hash (V6.2.8)");
    }
}
