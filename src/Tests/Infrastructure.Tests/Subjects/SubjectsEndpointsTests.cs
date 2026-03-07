using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Tests.TestHost;
using Xunit;

namespace Infrastructure.Tests.Subjects;

public sealed class SubjectsEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SubjectsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateSubject_ShouldReturnCreated_WhenAuthenticated()
    {
        var accessToken = await RegisterAndLoginAsync($"user_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title = "Software Architecture",
            description = "Design principles"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("title").GetString().Should().Be("Software Architecture");
        payload.GetProperty("description").GetString().Should().Be("Design principles");
    }

    [Fact]
    public async Task CreateSubject_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title = "Software Architecture",
            description = "Design principles"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetSubjects_ShouldReturnOnlyCurrentUserSubjects()
    {
        var ownerToken = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        await _client.PostAsJsonAsync("/api/subjects", new { title = "A", description = "A" });
        await _client.PostAsJsonAsync("/api/subjects", new { title = "B", description = "B" });

        var otherToken = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var otherList = await _client.GetAsync("/api/subjects");
        otherList.StatusCode.Should().Be(HttpStatusCode.OK);
        var otherPayload = await otherList.Content.ReadFromJsonAsync<JsonElement>();
        otherPayload.GetArrayLength().Should().Be(0);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var ownerList = await _client.GetAsync("/api/subjects?limit=1&offset=1");
        ownerList.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownerPayload = await ownerList.Content.ReadFromJsonAsync<JsonElement>();
        ownerPayload.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetSubjectById_ShouldReturnSubject_WhenUserIsParticipant()
    {
        var accessToken = await RegisterAndLoginAsync($"user_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createResponse = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title = "Distributed Systems",
            description = "Course"
        });

        var createPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var subjectId = createPayload.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/subjects/{subjectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSubjectById_ShouldReturnForbidden_WhenUserIsNotParticipant()
    {
        var ownerToken = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var createResponse = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title = "Distributed Systems",
            description = "Course"
        });

        var createPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var subjectId = createPayload.GetProperty("id").GetString();

        var otherToken = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var response = await _client.GetAsync($"/api/subjects/{subjectId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetSubjectById_ShouldReturnNotFound_WhenSubjectMissing()
    {
        var accessToken = await RegisterAndLoginAsync($"user_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync($"/api/subjects/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdateSubject_ShouldReturnOk_WhenUserIsAdmin()
    {
        var accessToken = await RegisterAndLoginAsync($"user_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createResponse = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title = "Old",
            description = "Old"
        });

        var createPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var subjectId = createPayload.GetProperty("id").GetString();

        var updateResponse = await _client.PutAsJsonAsync($"/api/subjects/{subjectId}", new
        {
            title = "New",
            description = "New"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatePayload = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        updatePayload.GetProperty("title").GetString().Should().Be("New");
        updatePayload.GetProperty("description").GetString().Should().Be("New");
    }

    [Fact]
    public async Task UpdateSubject_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        var ownerToken = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var createResponse = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title = "Old",
            description = "Old"
        });

        var createPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var subjectId = createPayload.GetProperty("id").GetString();

        var otherToken = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var updateResponse = await _client.PutAsJsonAsync($"/api/subjects/{subjectId}", new
        {
            title = "New",
            description = "New"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSubject_ShouldReturnNotFound_WhenSubjectMissing()
    {
        var accessToken = await RegisterAndLoginAsync($"user_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PutAsJsonAsync($"/api/subjects/{Guid.NewGuid()}", new
        {
            title = "New",
            description = "New"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSubject_ShouldReturnNoContent_WhenUserIsAdmin()
    {
        var accessToken = await RegisterAndLoginAsync($"user_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createResponse = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title = "Delete",
            description = "Delete"
        });

        var createPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var subjectId = createPayload.GetProperty("id").GetString();

        var deleteResponse = await _client.DeleteAsync($"/api/subjects/{subjectId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteSubject_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        var ownerToken = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var createResponse = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title = "Delete",
            description = "Delete"
        });

        var createPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var subjectId = createPayload.GetProperty("id").GetString();

        var otherToken = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var deleteResponse = await _client.DeleteAsync($"/api/subjects/{subjectId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteSubject_ShouldReturnNotFound_WhenSubjectMissing()
    {
        var accessToken = await RegisterAndLoginAsync($"user_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var deleteResponse = await _client.DeleteAsync($"/api/subjects/{Guid.NewGuid()}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<string> RegisterAndLoginAsync(string username)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = payload.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();
        return accessToken!;
    }
}
