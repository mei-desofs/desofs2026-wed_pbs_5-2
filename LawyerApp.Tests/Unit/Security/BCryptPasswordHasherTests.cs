using FluentAssertions;
using LawyerApp.Infrastructure.Security;
using Xunit;

namespace LawyerApp.Tests.Unit.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ReturnsNonEmptyHash()
    {
        var hash = _sut.HashPassword("MyPassword1!");

        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HashPassword_OutputDiffersFromInput()
    {
        var password = "MyPassword1!";
        var hash = _sut.HashPassword(password);

        hash.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_TwoCallsProduceDifferentHashes()
    {
        // BCrypt uses a random salt per call
        var hash1 = _sut.HashPassword("SamePassword!");
        var hash2 = _sut.HashPassword("SamePassword!");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var password = "Correct!1";
        var hash = _sut.HashPassword(password);

        _sut.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("Correct!1");

        _sut.VerifyPassword("Wrong!1", hash).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("ValidPassword1!");

        _sut.VerifyPassword(string.Empty, hash).Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ProducesBCryptFormatHash()
    {
        var hash = _sut.HashPassword("AnyPassword1!");

        // BCrypt hashes always start with $2a$ or $2b$
        hash.Should().MatchRegex(@"^\$2[ab]\$");
    }
}
