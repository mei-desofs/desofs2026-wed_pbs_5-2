using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Tests.Helpers;
using Xunit;

namespace LawyerApp.Tests.Integration.API;

/// <summary>
/// Integration tests for POST /api/client/create.
/// GET /api/client/get/all is excluded — two controllers share that route
/// causing AmbiguousMatchException at runtime.
/// </summary>
public class ClientControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClientControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsOk()
    {
        var dto = new CreateClientDto("NewClient", $"{Guid.NewGuid()}@example.com", "Secure1!", "Rua Nova 5", "920000001");

        var response = await _client.PostAsJsonAsync("/api/client/create", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ResponseDoesNotContainPasswordHash()
    {
        var dto = new CreateClientDto("SecureUser", $"{Guid.NewGuid()}@example.com", "S3cr3t!", "Rua Segura 1", "930000001");

        var response = await _client.PostAsJsonAsync("/api/client/create", dto);
        var raw = await response.Content.ReadAsStringAsync();

        raw.Should().NotContainAny("passwordHash", "PasswordHash", "password_hash");
    }

    [Fact]
    public async Task Create_ResponseContainsEmail()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var dto = new CreateClientDto("Visible", email, "Pass1!", "Rua V 1", "950000001");

        var response = await _client.PostAsJsonAsync("/api/client/create", dto);
        var raw = await response.Content.ReadAsStringAsync();

        raw.Should().Contain(email);
    }
}
