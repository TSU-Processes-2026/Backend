using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Tests.TestHost;
using Xunit;

namespace Infrastructure.Tests.Participants;

public sealed class ParticipantsEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ParticipantsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task JoinSubject_ShouldReturnParticipant_WhenSubjectExists()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Joinable", "Joinable");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);

        var response = await _client.PostAsync($"/api/subjects/{subjectId}/join", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("userId").GetString().Should().Be(student.UserId);
        payload.GetProperty("role").GetString().Should().Be("Student");
    }

    [Fact]
    public async Task JoinSubject_ShouldReturnNotFound_WhenSubjectMissing()
    {
        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);

        var response = await _client.PostAsync($"/api/subjects/{Guid.NewGuid()}/join", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task JoinSubject_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.PostAsync($"/api/subjects/{Guid.NewGuid()}/join", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task AddParticipant_ShouldReturnCreated_WhenRequesterIsAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Participants", "Participants");

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/participants", new
        {
            userId = teacher.UserId,
            role = "Teacher"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("userId").GetString().Should().Be(teacher.UserId);
        payload.GetProperty("role").GetString().Should().Be("Teacher");
    }

    [Fact]
    public async Task AddParticipant_ShouldReturnForbidden_WhenRequesterIsNotAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Participants", "Participants");

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/participants", new
        {
            userId = teacher.UserId,
            role = "Teacher"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetParticipants_ShouldReturnList_WhenRequesterIsParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Participants", "Participants");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

        var response = await _client.GetAsync($"/api/subjects/{subjectId}/participants?limit=50&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetParticipants_ShouldReturnForbidden_WhenRequesterIsNotParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Participants", "Participants");

        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        var response = await _client.GetAsync($"/api/subjects/{subjectId}/participants");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateParticipantRole_ShouldReturnOk_WhenRequesterIsAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Participants", "Participants");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/participants", new { userId = student.UserId, role = "Student" });

        var response = await _client.PatchAsJsonAsync($"/api/subjects/{subjectId}/participants/{student.UserId}", new { role = "Teacher" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("role").GetString().Should().Be("Teacher");
    }

    [Fact]
    public async Task UpdateParticipantRole_ShouldReturnForbidden_WhenRoleIsAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Participants", "Participants");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/participants", new { userId = student.UserId, role = "Student" });

        var response = await _client.PatchAsJsonAsync($"/api/subjects/{subjectId}/participants/{student.UserId}", new { role = "Admin" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateParticipantRole_ShouldReturnForbidden_WhenRequesterIsNotAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Participants", "Participants");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/participants", new { userId = student.UserId, role = "Student" });

        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        var response = await _client.PatchAsJsonAsync($"/api/subjects/{subjectId}/participants/{student.UserId}", new { role = "Teacher" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteParticipant_ShouldReturnNoContent_WhenRequesterIsAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Participants", "Participants");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/participants", new { userId = student.UserId, role = "Student" });

        var response = await _client.DeleteAsync($"/api/subjects/{subjectId}/participants/{student.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteParticipant_ShouldReturnForbidden_WhenRequesterIsNotAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Participants", "Participants");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/participants", new { userId = student.UserId, role = "Student" });

        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        var response = await _client.DeleteAsync($"/api/subjects/{subjectId}/participants/{student.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<AuthUser> RegisterAndLoginAsync(string username)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userId = registerPayload.GetProperty("id").GetString();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "StrongP@ssw0rd!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginPayload.GetProperty("accessToken").GetString();

        userId.Should().NotBeNullOrWhiteSpace();
        accessToken.Should().NotBeNullOrWhiteSpace();

        return new AuthUser(userId!, accessToken!);
    }

    private async Task<string> CreateSubjectAsync(string accessToken, string title, string description)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title,
            description
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var subjectId = payload.GetProperty("id").GetString();
        subjectId.Should().NotBeNullOrWhiteSpace();
        return subjectId!;
    }
}
