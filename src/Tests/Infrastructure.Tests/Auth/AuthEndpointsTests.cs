using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Tests.TestHost;
using Xunit;

namespace Infrastructure.Tests.Auth;

public sealed class AuthEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenRequestIsValid()
    {
        var username = $"user_{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("username").GetString().Should().Be(username);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUsernameAlreadyExists()
    {
        var username = $"user_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        var duplicate = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        duplicate.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenModelIsInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "ab",
            password = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        var username = $"user_{Guid.NewGuid():N}";
        await RegisterAsync(username);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("tokenType").GetString().Should().Be("Bearer");
        payload.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("expiresIn").GetInt32().Should().Be(900);
        payload.GetProperty("refreshExpiresIn").GetInt32().Should().Be(604800);
        payload.GetProperty("userId").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("sessionId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = $"missing_{Guid.NewGuid():N}",
            password = "WrongPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenModelIsInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "",
            password = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Refresh_ShouldReturnRotatedTokens_WhenRefreshTokenIsValid()
    {
        var username = $"user_{Guid.NewGuid():N}";
        await RegisterAsync(username);
        var login = await LoginAsync(username);

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("refreshToken").GetString().Should().NotBe(login.RefreshToken);
        payload.GetProperty("sessionId").GetString().Should().Be(login.SessionId);
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorizedAndRevokeSession_WhenTokenIsReused()
    {
        var username = $"user_{Guid.NewGuid():N}";
        await RegisterAsync(username);
        var login = await LoginAsync(username);

        var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login.RefreshToken
        });
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstPayload = await firstRefresh.Content.ReadFromJsonAsync<JsonElement>();
        var secondToken = firstPayload.GetProperty("refreshToken").GetString();

        var replayAttempt = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login.RefreshToken
        });
        replayAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var tokenFromRevokedSession = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = secondToken
        });
        tokenFromRevokedSession.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldReturnBadRequest_WhenModelIsInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Logout_ShouldReturnNoContent_WhenAuthorizedAndRefreshTokenIsValid()
    {
        var username = $"user_{Guid.NewGuid():N}";
        await RegisterAsync(username);
        var login = await LoginAsync(username);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = login.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_ShouldReturnUnauthorized_WhenBearerTokenIsMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = "some-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Logout_ShouldReturnUnauthorized_WhenRefreshTokenIsInvalid()
    {
        var username = $"user_{Guid.NewGuid():N}";
        await RegisterAsync(username);
        var login = await LoginAsync(username);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = "invalid-refresh-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Logout_ShouldReturnBadRequest_WhenModelIsInvalid()
    {
        var username = $"user_{Guid.NewGuid():N}";
        await RegisterAsync(username);
        var login = await LoginAsync(username);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    private async Task<LoginTokens> LoginAsync(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = payload.GetProperty("accessToken").GetString();
        var refreshToken = payload.GetProperty("refreshToken").GetString();
        var sessionId = payload.GetProperty("sessionId").GetString();

        accessToken.Should().NotBeNullOrWhiteSpace();
        refreshToken.Should().NotBeNullOrWhiteSpace();
        sessionId.Should().NotBeNullOrWhiteSpace();

        return new LoginTokens(accessToken!, refreshToken!, sessionId!);
    }
}
