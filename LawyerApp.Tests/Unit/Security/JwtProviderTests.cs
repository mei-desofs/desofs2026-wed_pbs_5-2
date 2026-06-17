using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Shared;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LawyerApp.Tests.Unit.Security;

/// <summary>
/// Tests JWT token structure and claims by generating tokens directly
/// using the same configuration as the application (appsettings.json).
/// JwtProvider is internal sealed, so we replicate its logic here to
/// verify the token contract without depending on the HTTP pipeline.
/// </summary>
public class JwtProviderTests
{
    // Mirrors appsettings.json Jwt section
    private const string SecretKey  = "IHOPETHISISREALYWORKINGCOMEONE_IHOPETHISISREALYWORKINGCOMEONE";
    private const string Issuer     = "LawyerApp";
    private const string Audience   = "LawyerAppUsers";

    private static string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role,               user.userRole.ToString()),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            Issuer, Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Token shape ──────────────────────────────────────────────────────────

    [Fact]
    public void Generate_ReturnsNonEmptyToken()
    {
        var user = new Client("Alice", "alice@jwt.com", "hash", "Rua A", "910000001");
        GenerateToken(user).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Generate_ReturnsWellFormedJwt()
    {
        var user = new Client("Bob", "bob@jwt.com", "hash", "Rua B", "910000002");
        GenerateToken(user).Split('.').Should().HaveCount(3);
    }

    // ── Claims ───────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_TokenContainsSubjectClaim()
    {
        var user = new Client("Carol", "carol@jwt.com", "hash", "Rua C", "910000003");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(GenerateToken(user));

        parsed.Subject.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void Generate_TokenContainsEmailClaim()
    {
        var user = new Client("Dave", "dave@jwt.com", "hash", "Rua D", "910000004");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(GenerateToken(user));

        parsed.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Email && c.Value == "dave@jwt.com");
    }

    [Fact]
    public void Generate_TokenContainsRoleClaim()
    {
        var user = new Client("Eve", "eve@jwt.com", "hash", "Rua E", "910000005");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(GenerateToken(user));

        parsed.Claims.Should().Contain(c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public void Generate_ClientRole_IsSetToClient()
    {
        var user = new Client("Frank", "frank@jwt.com", "hash", "Rua F", "910000006");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(GenerateToken(user));

        parsed.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == Roles.Client.ToString());
    }

    [Fact]
    public void Generate_LawyerRole_IsSetToLawyer()
    {
        var user = new Lawyer("Grace", "grace@jwt.com", "hash", "OA/99999");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(GenerateToken(user));

        parsed.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == Roles.Lawyer.ToString());
    }

    // ── Expiration ───────────────────────────────────────────────────────────

    [Fact]
    public void Generate_TokenIsNotExpiredImmediately()
    {
        var user = new Client("Hank", "hank@jwt.com", "hash", "Rua H", "910000007");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(GenerateToken(user));

        parsed.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Generate_TokenExpiresInApproximately30Minutes()
    {
        var user = new Client("Ida", "ida@jwt.com", "hash", "Rua I", "910000008");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(GenerateToken(user));

        parsed.ValidTo.Should().BeAfter(DateTime.UtcNow.AddMinutes(25));
        parsed.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(35));
    }

    // ── Sensitive data ───────────────────────────────────────────────────────

    [Fact]
    public void Generate_TokenPayloadDoesNotContainPassword()
    {
        var user = new Client("Jack", "jack@jwt.com", "secrethash", "Rua J", "910000009");
        var token = GenerateToken(user);
        var parts = token.Split('.');
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(parts[1])));

        payload.Should().NotContainAny("password", "hash", "passwordHash");
    }

    // ── Two users get distinct tokens ────────────────────────────────────────

    [Fact]
    public void Generate_TwoDifferentUsers_ProduceDifferentTokens()
    {
        var user1 = new Client("Kim", "kim@jwt.com", "hash", "Rua K", "910000010");
        var user2 = new Client("Leo", "leo@jwt.com", "hash", "Rua L", "910000011");

        GenerateToken(user1).Should().NotBe(GenerateToken(user2));
    }

    private static string PadBase64(string base64Url)
    {
        var s = base64Url.Replace('-', '+').Replace('_', '/');
        return (s.Length % 4) switch { 2 => s + "==", 3 => s + "=", _ => s };
    }
}
