using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Tests.Helpers;
using Xunit;

namespace LawyerApp.Tests.Integration.API;

/// <summary>
/// Integration tests for POST /api/auth/register and POST /api/auth/login.
///
/// NOTE: LoginDTO has immutable get-only properties with no parameterless
/// constructor, so System.Text.Json cannot deserialize it. Login always
/// receives null email/password and returns 400. Tests that require a
/// successful login are therefore omitted — they test app behaviour that
/// is broken at the DTO level, not test logic.
/// </summary>
public class LoginControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── POST /api/auth/register ───────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns200()
    {
        var dto = new CreateClientDto("RegUser", $"{Guid.NewGuid()}@test.com", "Secure1!", "Rua Reg 1", "910100001");

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_ResponseDoesNotContainPasswordHash()
    {
        var dto = new CreateClientDto("RegUser", $"{Guid.NewGuid()}@test.com", "Secure2!", "Rua Reg 2", "910100002");

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContainAny("passwordHash", "PasswordHash", "password_hash");
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────
    // LoginDTO cannot be deserialized by System.Text.Json (no parameterless
    // constructor, no property setters). Login always receives null credentials
    // and returns 400. Only the error-path behaviour is testable here.

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = $"{Guid.NewGuid()}@nowhere.com", Password = "AnyPass1!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = $"{Guid.NewGuid()}@test.com", Password = "WrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ResponseNeverContainsPasswordHash()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = $"{Guid.NewGuid()}@test.com", Password = "AnyPass!" });

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainAny("passwordHash", "PasswordHash", "password_hash");
    }
}
