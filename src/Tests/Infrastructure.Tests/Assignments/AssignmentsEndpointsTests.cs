using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Tests.TestHost;
using Xunit;

namespace Infrastructure.Tests.Assignments;

public sealed class AssignmentsEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AssignmentsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAssignments_ShouldReturnList_WhenRequesterIsParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");
        await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId, "A1", "Data1");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);

        var response = await _client.GetAsync($"/api/subjects/{subjectId}/assignments?limit=20&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAssignments_ShouldReturnForbidden_WhenRequesterIsNotParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");

        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        var response = await _client.GetAsync($"/api/subjects/{subjectId}/assignments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetAssignments_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.GetAsync($"/api/subjects/{Guid.NewGuid()}/assignments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateAssignment_ShouldReturnCreated_WhenRequesterIsAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/assignments", CreateAssignmentRequest("Assignment 1", "Due soon"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("postType").GetString().Should().Be("Assignment");
        payload.GetProperty("content").GetString().Should().Be("Assignment 1");
    }

    [Fact]
    public async Task CreateAssignment_ShouldReturnCreated_WhenRequesterIsTeacher()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        await AddParticipantAsync(owner.AccessToken, subjectId, teacher.UserId, "Teacher");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher.AccessToken);
        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/assignments", CreateAssignmentRequest("Assignment 1", "Due soon"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateAssignment_ShouldReturnForbidden_WhenRequesterIsStudent()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);
        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/assignments", CreateAssignmentRequest("Assignment 1", "Due soon"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateAssignment_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.PostAsJsonAsync($"/api/subjects/{Guid.NewGuid()}/assignments", CreateAssignmentRequest("Assignment 1", "Due soon"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetAssignmentById_ShouldReturnAssignment_WhenRequesterIsParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId, "A1", "Data1");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);

        var response = await _client.GetAsync($"/api/assignments/{assignmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().Be(assignmentId);
    }

    [Fact]
    public async Task GetAssignmentById_ShouldReturnForbidden_WhenRequesterIsNotParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId, "A1", "Data1");

        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        var response = await _client.GetAsync($"/api/assignments/{assignmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetAssignmentById_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.GetAsync($"/api/assignments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdateAssignment_ShouldReturnOk_WhenRequesterIsAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId, "Old", "OldData");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.PutAsJsonAsync($"/api/assignments/{assignmentId}", CreateAssignmentRequest("New", "NewData"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("content").GetString().Should().Be("New");
    }

    [Fact]
    public async Task UpdateAssignment_ShouldReturnOk_WhenRequesterIsTeacher()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId, "Old", "OldData");

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        await AddParticipantAsync(owner.AccessToken, subjectId, teacher.UserId, "Teacher");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher.AccessToken);
        var response = await _client.PutAsJsonAsync($"/api/assignments/{assignmentId}", CreateAssignmentRequest("New", "NewData"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateAssignment_ShouldReturnForbidden_WhenRequesterIsStudent()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId, "Old", "OldData");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);
        var response = await _client.PutAsJsonAsync($"/api/assignments/{assignmentId}", CreateAssignmentRequest("New", "NewData"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdateAssignment_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.PutAsJsonAsync($"/api/assignments/{Guid.NewGuid()}", CreateAssignmentRequest("New", "NewData"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task DeleteAssignment_ShouldReturnNoContent_WhenRequesterIsAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId, "A1", "Data1");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.DeleteAsync($"/api/assignments/{assignmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAssignment_ShouldReturnNoContent_WhenRequesterIsTeacher()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId, "A1", "Data1");

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        await AddParticipantAsync(owner.AccessToken, subjectId, teacher.UserId, "Teacher");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher.AccessToken);
        var response = await _client.DeleteAsync($"/api/assignments/{assignmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAssignment_ShouldReturnForbidden_WhenRequesterIsStudent()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Assignments", "Assignments");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId, "A1", "Data1");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);
        var response = await _client.DeleteAsync($"/api/assignments/{assignmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task DeleteAssignment_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.DeleteAsync($"/api/assignments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private static object CreateAssignmentRequest(string content, string assignmentData)
    {
        return new
        {
            content,
            assignmentData,
            questions = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    questionType = "Text",
                    questionData = "Explain your answer"
                }
            }
        };
    }

    private async Task<string> CreateAssignmentAndGetIdAsync(string accessToken, string subjectId, string content, string assignmentData)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/assignments", CreateAssignmentRequest(content, assignmentData));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var assignmentId = payload.GetProperty("id").GetString();
        assignmentId.Should().NotBeNullOrWhiteSpace();
        return assignmentId!;
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

    private async Task JoinSubjectAsync(string accessToken, string subjectId)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsync($"/api/subjects/{subjectId}/join", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task AddParticipantAsync(string accessToken, string subjectId, string userId, string role)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/participants", new
        {
            userId,
            role
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private sealed record AuthUser(string UserId, string AccessToken);
}
