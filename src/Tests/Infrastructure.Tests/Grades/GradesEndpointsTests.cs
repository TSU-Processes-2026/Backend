using Application.Submissions.Models;
using FluentAssertions;
using Infrastructure.Persistence.Entities;
using Infrastructure.Tests.Participants;
using Infrastructure.Tests.TestHost;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Infrastructure.Tests.Grades
{
    public class GradesEndpointsTests
    {
        private readonly HttpClient _client;

        public GradesEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetGrade_ShouldReturnOk()
        {
            var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.AccessToken);

            var subjectId = await CreateSubjectAsync(owner.AccessToken, "Test", "Test");
            var assignmentId = await CreateAssignmentAndGetIdAsync(owner.AccessToken, subjectId);

            var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", student.AccessToken);

            await JoinSubjectAsync(student.AccessToken, subjectId, "Student");

            SubmissionCreateRequest request = CreateSubmissionRequest();

            var submissionResponse =
                await _client.PostAsJsonAsync($"/api/assignments/{assignmentId}/submissions?isStudent=true", request);

            var submission = await submissionResponse.Content.ReadFromJsonAsync<Submission>();

            var response =
                await _client.GetAsync($"/api/submissions/{submission!.id}/grade");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
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
        private static SubmissionCreateRequest CreateSubmissionRequest()
        {
            return new SubmissionCreateRequest
            {
                answers = new List<AnswerItemDto>
            {
                new AnswerItemDto
                {
                    assignmentQuestionId = Guid.NewGuid(),
                    answerType = AnswerTypeEnum.SingleChoiceAnswer,
                    selectedOptionId = Guid.NewGuid()
                },
                new AnswerItemDto
                {
                    assignmentQuestionId = Guid.NewGuid(),
                    answerType = AnswerTypeEnum.MultipleChoiceAnswer,
                    selectedOptionIds = new List<Guid> { Guid.NewGuid() }
                },
                new AnswerItemDto
                {
                    assignmentQuestionId = Guid.NewGuid(),
                    answerType = AnswerTypeEnum.TextAnswer,
                    text = "My answer"
                }
            }
            };
        }
        private async Task JoinSubjectAsync(string accessToken, string subjectId, string role)
        {
            var request = new { role };
            var response = await _client.PostAsJsonAsync($"/api/subjects/{subjectId}/join", request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        private sealed record AuthUser(string UserId, string AccessToken);
    }
}
