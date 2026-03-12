using Application.Submissions.Models;
using FluentAssertions;
using Infrastructure.Persistence.Entities;
using Infrastructure.Tests.TestHost;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Infrastructure.Tests.Submissions;

public sealed class SubmissionsEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SubmissionsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateSubmissionDraft_ShouldReturnCreated_WhenUserIsStudent()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", owner.AccessToken);

        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Test", "Test");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId);//post

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", student.AccessToken);

        await JoinSubjectAsync(student.AccessToken, subjectId, "Student");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", student.AccessToken);

        var request = CreateSubmissionRequest();

        var response =
            await _client.PostAsJsonAsync($"/api/assignments/{assignmentId}/submissions?isStudent=true", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateSubmissionDraft_ShouldReturnForbidden_WhenUserIsNotStudent()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");

        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Submissions", "Submissions");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId);

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", teacher.AccessToken);

        await JoinSubjectAsync(teacher.AccessToken, subjectId, "Teacher");

        var request = CreateSubmissionRequest();

        var response =
            await _client.PostAsJsonAsync($"/api/assignments/{assignmentId}/submissions?isStudent=false", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSubmissions_ShouldReturnOk_WhenUserIsTeacher()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");

        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Submissions", "Submissions");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId);

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", teacher.AccessToken);

        await JoinSubjectAsync(teacher.AccessToken, subjectId, "Teacher");

        var response =
            await _client.GetAsync($"/api/assignments/{assignmentId}/submissions?limit=20&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSubmission_ShouldReturnOk()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");

        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Submissions", "Submissions");
        var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId);

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", student.AccessToken);

        await JoinSubjectAsync(student.AccessToken, subjectId, "Student");

        var request = CreateSubmissionRequest();
        var createResponse =
            await _client.PostAsJsonAsync($"/api/assignments/{assignmentId}/submissions?isStudent=true", request);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdSubmission = await createResponse.Content.ReadFromJsonAsync<Submission>();

        // Получаем конкретный submission по id
        var response = await _client.GetAsync($"/api/submissions/{createdSubmission!.id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var submission = await response.Content.ReadFromJsonAsync<Submission>();
        submission!.id.Should().Be(createdSubmission.id);
    }

    private static object CreateSubmissionRequest()
    {
        return new
        {
            answers = new object[]
            {
                new
                {
                    questionId = Guid.NewGuid(),
                    answerType = AnswerTypeEnum.SingleChoiceAnswer,
                    selectedOptionId = Guid.NewGuid()
                },
                new
                {
                    questionId = Guid.NewGuid(),
                    answerType = AnswerTypeEnum.MultipleChoiceAnswer,
                    selectedOptionIds = new[] { Guid.NewGuid() }
                },
                new
                {
                    questionId = Guid.NewGuid(),
                    answerType = AnswerTypeEnum.TextAnswer,
                    text = "My answer"
                }
            }
        };
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
        return payload.GetProperty("id").GetString()!;
    }
    private async Task<string> CreateAssignmentAndGetIdAsync(string accessToken, string subjectId)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/assignments", new
        {
            content = "Assignment",
            assignmentData = "Data",
            questions = new[]
            {
                new { id = Guid.NewGuid(), questionType = "Text", questionData = "Explain" }
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetString()!;
    }
    private async Task JoinSubjectAsync(string accessToken, string subjectId, string role)
    {
        var request = new { role };
        var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/join", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    private sealed record AuthUser(string UserId, string AccessToken);
}