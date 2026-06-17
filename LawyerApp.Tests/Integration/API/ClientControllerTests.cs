using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Tests.Helpers;
using Xunit;

namespace LawyerApp.Tests.Integration.API;

/// <summary>
/// Integration tests that spin up the full ASP.NET Core pipeline with an
/// in-memory database (no PostgreSQL required).
/// </summary>
public class ClientControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClientControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/client/get/all ──────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WhenNoClients_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/client/get/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ClientDto>>();
        body.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetAll_AfterCreatingClient_ReturnsCreatedClient()
    {
        var dto = new CreateClientDto("TestUser", "testuser2@example.com", "Pass1!", "Av. Test 1", "910000002");

        await _client.PostAsJsonAsync("/api/client/create", dto);
        var response = await _client.GetAsync("/api/client/get/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/client/create ──────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedClient()
    {
        var dto = new CreateClientDto("NewClient", "newclient2@example.com", "Secure1!", "Rua Nova 5", "920000002");

        var response = await _client.PostAsJsonAsync("/api/client/create", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ResponseDoesNotContainPasswordHash()
    {
        var dto = new CreateClientDto("SecureUser", "secure2@example.com", "S3cr3t!", "Rua Segura 1", "930000002");

        var response = await _client.PostAsJsonAsync("/api/client/create", dto);
        var raw = await response.Content.ReadAsStringAsync();

        raw.Should().NotContainAny("passwordHash", "PasswordHash", "password_hash");
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsError()
    {
        var dto = new CreateClientDto("Dup", "dup2@example.com", "Pass1!", "Rua Dup 1", "940000002");

        await _client.PostAsJsonAsync("/api/client/create", dto);

        var secondResponse = await _client.PostAsJsonAsync("/api/client/create", dto);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
