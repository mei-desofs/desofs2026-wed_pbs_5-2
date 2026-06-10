using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Tests.Helpers;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LawyerApp.Tests.Integration.Security;

/// <summary>
/// Security integration tests.
///
/// NOTE: LoginDTO cannot be deserialized by System.Text.Json (immutable
/// get-only properties, no parameterless constructor), so any test that
/// requires a successful login via the HTTP pipeline is excluded.
/// Token structure tests use direct JWT generation with the app's key.
/// </summary>
public class AuthorizationSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    private const string SecretKey = "IHOPETHISISREALYWORKINGCOMEONE_IHOPETHISISREALYWORKINGCOMEONE";
    private const string Issuer    = "LawyerApp";
    private const string Audience  = "LawyerAppUsers";

    public AuthorizationSecurityTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string MakeValidToken(string email = "test@sec.com", string role = "Client")
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role,               role),
        };
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Token content security (no HTTP needed) ───────────────────────────────

    [Fact]
    public void Token_DoesNotContainPasswordInPayload()
    {
        var token = MakeValidToken();
        var parts = token.Split('.');
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(parts[1])));

        payload.Should().NotContainAny("password", "passwordHash");
    }

    [Fact]
    public void Token_PayloadContainsExpectedClaims()
    {
        var token = MakeValidToken("claims@sec.com");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        parsed.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email);
    }

    [Fact]
    public void Token_HasFutureExpiration()
    {
        var token = MakeValidToken();
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    // ── Register sensitive data ───────────────────────────────────────────────

    [Fact]
    public async Task Register_ResponseNeverLeaksPasswordHash()
    {
        var dto = new CreateClientDto("SecUser", $"{Guid.NewGuid()}@sec.com", "NoLeak1!", "Rua Sec 1", "910400001");

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContainAny("passwordHash", "PasswordHash", "password_hash");
    }

    // ── Login error-path security ─────────────────────────────────────────────

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = $"{Guid.NewGuid()}@sec.com", Password = "bad" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_NonExistentEmail_ReturnsSameStatusAsWrongPassword()
    {
        var r1 = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = $"{Guid.NewGuid()}@sec.com", Password = "Wrong!" });
        var r2 = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = $"{Guid.NewGuid()}@sec.com", Password = "AnyPass!" });

        r1.StatusCode.Should().Be(r2.StatusCode,
            because: "the API must not allow email enumeration via different status codes");
    }

    [Fact]
    public async Task Login_ErrorResponse_DoesNotLeakPasswordHash()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = $"{Guid.NewGuid()}@sec.com", Password = "Any!" });
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContainAny("passwordHash", "PasswordHash", "password_hash");
    }

    private static string PadBase64(string b)
    {
        var s = b.Replace('-', '+').Replace('_', '/');
        return (s.Length % 4) switch { 2 => s + "==", 3 => s + "=", _ => s };
    }
}
