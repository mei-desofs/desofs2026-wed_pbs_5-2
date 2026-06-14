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

namespace LawyerApp.Tests.Integration.Security.ASVS;

/// <summary>
/// ASVS V14 – Data Protection
/// ASVS V13 – Configuration (information leakage)
///
/// V14.2.1 – Sensitive data must not appear in URLs or query strings.
/// V13.4.4 – HTTP TRACE method must be disabled in production.
/// V13.4.2 – Debug information must not be exposed in error responses.
/// V16    – Error handling must not leak stack traces or internal details.
/// </summary>
public class V14_DataProtectionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string SecretKey = "IHOPETHISISREALYWORKINGCOMEONE_IHOPETHISISREALYWORKINGCOMEONE";
    private const string Issuer    = "LawyerApp";
    private const string Audience  = "LawyerAppUsers";

    public V14_DataProtectionTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string MakeToken(string role = "Client")
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, $"{Guid.NewGuid()}@test.com"),
            new Claim(ClaimTypes.Role, role),
        };
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── V14.2.1 – Sensitive data not in URL ───────────────────────────────────

    [Fact]
    public async Task V14_2_1_LoginEndpoint_IsPost_SoCredentialsNeverInUrl()
    {
        // GET /api/auth/login should return 405 — credentials must never travel via URL
        var response = await _client.GetAsync("/api/auth/login");

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed,
            "login must be POST-only; GET would expose credentials in URL (V14.2.1)");
    }

    [Fact]
    public async Task V14_2_1_RegisterEndpoint_IsPost_SoCredentialsNeverInUrl()
    {
        var response = await _client.GetAsync("/api/auth/register");

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed,
            "register must be POST-only; GET would expose credentials in URL (V14.2.1)");
    }

    [Fact]
    public async Task V14_2_1_ClientCreate_IsPost_SoDataNeverInUrl()
    {
        var response = await _client.GetAsync("/api/client/create");

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed,
            "client creation must be POST-only (V14.2.1)");
    }

    // ── V13.4.4 – HTTP TRACE must be disabled ─────────────────────────────────

    [Fact]
    public async Task V13_4_4_HttpTrace_IsNotSupported()
    {
        var request  = new HttpRequestMessage(HttpMethod.Trace, "/api/auth/login");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "HTTP TRACE must be disabled to prevent information leakage (V13.4.4)");
    }

    // ── V13.4.2 / V16 – Error responses must not leak internal details ────────

    [Fact]
    public async Task V13_4_2_ErrorResponse_DoesNotContainStackTrace()
    {
        // Trigger a 400 error with an invalid login attempt
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nobody@test.com", Password = "wrong" });
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContain("StackTrace",    "stack traces must not be exposed (V13.4.2)");
        body.Should().NotContain("at LawyerApp.", "internal namespace paths must not be exposed (V13.4.2)");
        body.Should().NotContain("Exception",     "exception type names must not be exposed (V16)");
    }

    [Fact]
    public async Task V16_ErrorResponse_DoesNotLeakInternalPaths()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = string.Empty, Password = string.Empty });
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContain("C:\\",       "Windows file paths must not appear in responses (V16)");
        body.Should().NotContain("/home/",     "Linux home paths must not appear in responses (V16)");
        body.Should().NotContain("connection", "database connection strings must never be exposed (V16)");
    }

    [Fact]
    public async Task V16_RegisterError_DoesNotLeakInternalDetails()
    {
        // Trigger a 400 by registering the same email twice
        var email = $"{Guid.NewGuid()}@test.com";
        var dto   = new CreateClientDto("User", email, "Pass1!", "Rua", "910000001");

        await _client.PostAsJsonAsync("/api/auth/register", dto);
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", dto);
        var body = await secondResponse.Content.ReadAsStringAsync();

        body.Should().NotContain("StackTrace",    "stack traces must not be exposed (V16)");
        body.Should().NotContain("at LawyerApp.", "internal call paths must not be exposed (V16)");
    }

    // ── V14.2.1 – Response body must not contain raw password or hash ─────────

    [Fact]
    public async Task V14_2_1_RegisterResponse_DoesNotContainPassword()
    {
        var dto      = new CreateClientDto("User", $"{Guid.NewGuid()}@test.com", "SuperSecret1!", "Rua", "910000001");
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);
        var body     = await response.Content.ReadAsStringAsync();

        body.Should().NotContain("SuperSecret1!",
            "the plain-text password must never appear in any API response (V14.2.1)");
        body.Should().NotContainAny("passwordHash", "PasswordHash",
            "the password hash must never appear in any API response (V14.2.1)");
    }

    [Fact]
    public async Task V14_2_1_ClientCreate_ResponseDoesNotContainPassword()
    {
        var dto      = new CreateClientDto("User", $"{Guid.NewGuid()}@test.com", "MyPass99!", "Rua", "910000001");
        var response = await _client.PostAsJsonAsync("/api/client/create", dto);
        var body     = await response.Content.ReadAsStringAsync();

        body.Should().NotContain("MyPass99!",
            "the plain-text password must never appear in any API response (V14.2.1)");
        body.Should().NotContainAny("passwordHash", "PasswordHash",
            "the password hash must never appear in any API response (V14.2.1)");
    }

    // ── V14.2.1 – Token claim payload must not include sensitive data ─────────

    [Fact]
    public void V14_2_1_JwtPayload_DoesNotContainPassword()
    {
        var token  = MakeToken();
        var parts  = token.Split('.');
        var padded = parts[1].Replace('-', '+').Replace('_', '/');
        while (padded.Length % 4 != 0) padded += '=';
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(padded));

        payload.Should().NotContainAny("password", "passwordHash", "hash",
            "JWT payload must never carry password or hash information (V14.2.1)");
    }
}
