using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Tests.TestHost;
using Xunit;

namespace Infrastructure.Tests.Criteria;

public sealed class CriteriaEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CriteriaEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCriterion_ShouldCreateActiveAndPassiveCriteria_WhenRequesterIsTeacher()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken);
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId);

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        await AddParticipantAsync(owner.AccessToken, subjectId, teacher.UserId, "Teacher");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher.AccessToken);

        var activeResponse = await _client.PostAsJsonAsync($"/api/tasks/{assignmentId}/criteria", CreateCriterionRequest("Code quality", "active", "scale", 0.6m, 1));
        var passiveResponse = await _client.PostAsJsonAsync($"/api/tasks/{assignmentId}/criteria", CreateCriterionRequest("Submitted on time", "passive", "boolean", 0.4m, 2));

        activeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        passiveResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var active = await activeResponse.Content.ReadFromJsonAsync<JsonElement>();
        active.GetProperty("title").GetString().Should().Be("Code quality");
        active.GetProperty("criterionType").GetString().Should().Be("active");
        active.GetProperty("valueType").GetString().Should().Be("scale");

        var passive = await passiveResponse.Content.ReadFromJsonAsync<JsonElement>();
        passive.GetProperty("title").GetString().Should().Be("Submitted on time");
        passive.GetProperty("criterionType").GetString().Should().Be("passive");
        passive.GetProperty("valueType").GetString().Should().Be("boolean");
    }

    [Fact]
    public async Task UpdateCriterion_ShouldChangeCriterionFields_WhenRequesterIsTeacher()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken);
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId);
        var criterionId = await CreateCriterionAndGetIdAsync(owner.AccessToken, assignmentId);

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        await AddParticipantAsync(owner.AccessToken, subjectId, teacher.UserId, "Teacher");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher.AccessToken);
        var response = await _client.PatchAsJsonAsync($"/api/criteria/{criterionId}", new
        {
            title = "Updated criterion",
            criterionType = "passive",
            valueType = "boolean",
            weight = 0.5m,
            isRequired = false,
            appliesTo = "team"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("title").GetString().Should().Be("Updated criterion");
        payload.GetProperty("criterionType").GetString().Should().Be("passive");
        payload.GetProperty("valueType").GetString().Should().Be("boolean");
        payload.GetProperty("isRequired").GetBoolean().Should().BeFalse();
        payload.GetProperty("appliesTo").GetString().Should().Be("team");
    }

    [Fact]
    public async Task DeleteCriterion_ShouldRemoveCriterion_WhenRequesterIsTeacher()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken);
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId);
        var criterionId = await CreateCriterionAndGetIdAsync(owner.AccessToken, assignmentId);

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        await AddParticipantAsync(owner.AccessToken, subjectId, teacher.UserId, "Teacher");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher.AccessToken);
        var response = await _client.DeleteAsync($"/api/criteria/{criterionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await _client.GetAsync($"/api/tasks/{assignmentId}/criteria");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("criteria").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task CreateCriterion_ShouldReturnForbidden_WhenRequesterIsStudent()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken);
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId);

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);
        var response = await _client.PostAsJsonAsync($"/api/tasks/{assignmentId}/criteria", CreateCriterionRequest("Code quality", "active", "scale", 1m, 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static object CreateCriterionRequest(string title, string criterionType, string valueType, decimal weight, int order)
    {
        return new
        {
            title,
            description = $"{title} description",
            criterionType,
            valueType,
            weight,
            minValue = 0,
            isRequired = true,
            isHiddenUntilVisibility = false,
            appliesTo = "both",
            order
        };
    }

    private async Task<string> CreateCriterionAndGetIdAsync(string accessToken, string assignmentId)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync($"/api/tasks/{assignmentId}/criteria", CreateCriterionRequest("Initial criterion", "active", "scale", 1m, 1));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var criterionId = payload.GetProperty("id").GetString();
        criterionId.Should().NotBeNullOrWhiteSpace();
        return criterionId!;
    }

    private async Task<string> CreateAssignmentAndGetIdAsync(string accessToken, string subjectId)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/assignments", new
        {
            content = "Assignment",
            assignmentData = "Solve task",
            questions = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    questionType = "Text",
                    questionData = "Explain your answer"
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var assignmentId = payload.GetProperty("id").GetString();
        assignmentId.Should().NotBeNullOrWhiteSpace();
        return assignmentId!;
    }

    private async Task<string> CreateSubjectAsync(string accessToken)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/subjects", new
        {
            title = "Criteria subject",
            description = "Criteria subject",
            gradingMode = "five_point",
            selfAssessmentEnabled = false
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

    private sealed record AuthUser(string UserId, string AccessToken);
}
