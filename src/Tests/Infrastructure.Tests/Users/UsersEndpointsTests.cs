using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Tests.TestHost;
using Xunit;

namespace Infrastructure.Tests.Users;

public sealed class UsersEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMe_ShouldReturnCurrentUser_WhenAuthenticated()
    {
        var username = $"user_{Guid.NewGuid():N}";
        await RegisterAsync(username);
        var accessToken = await LoginAsync(username);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("username").GetString().Should().Be(username);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private async Task RegisterAsync(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<string> LoginAsync(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = payload.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();
        return accessToken!;
    }
}
