using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Tests.TestHost;
using Xunit;

namespace Infrastructure.Tests.Comments;

public sealed class CommentsEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CommentsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetComments_ShouldReturnComments_WhenRequesterHasAccessToPost()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Comments", "Comments");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Post for comments");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);

        await CreateCommentAndGetIdAsync(owner.AccessToken, "Post", postId, "Owner comment");
        await CreateCommentAndGetIdAsync(student.AccessToken, "Post", postId, "Student comment");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);
        var response = await _client.GetAsync($"/api/comments?targetType=Post&targetId={postId}&limit=20&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetArrayLength().Should().Be(2);
        payload.EnumerateArray().All(x => x.GetProperty("targetType").GetString() == "Post").Should().BeTrue();
        payload.EnumerateArray().All(x => x.GetProperty("targetId").GetString() == postId).Should().BeTrue();
    }

    [Fact]
    public async Task GetComments_ShouldReturnForbidden_WhenRequesterHasNoAccessToPost()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Comments", "Comments");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Post for comments");
        await CreateCommentAndGetIdAsync(owner.AccessToken, "Post", postId, "Owner comment");

        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        var response = await _client.GetAsync($"/api/comments?targetType=Post&targetId={postId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetComments_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.GetAsync($"/api/comments?targetType=Post&targetId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateComment_ShouldReturnCreated_WhenRequesterHasAccessToPost()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Comments", "Comments");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Post for comments");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);
        var response = await _client.PostAsJsonAsync("/api/comments", new
        {
            targetType = "Post",
            targetId = postId,
            text = "Student comment"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("targetType").GetString().Should().Be("Post");
        payload.GetProperty("targetId").GetString().Should().Be(postId);
        payload.GetProperty("authorId").GetString().Should().Be(student.UserId);
        payload.GetProperty("text").GetString().Should().Be("Student comment");
    }

    [Fact]
    public async Task CreateComment_ShouldReturnForbidden_WhenRequesterHasNoAccessToPost()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Comments", "Comments");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Post for comments");

        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/comments", new
        {
            targetType = "Post",
            targetId = postId,
            text = "Should fail"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateComment_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/comments", new
        {
            targetType = "Post",
            targetId = Guid.NewGuid(),
            text = "No token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task DeleteComment_ShouldReturnNoContent_WhenRequesterIsAuthor()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Comments", "Comments");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Post for comments");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        var commentId = await CreateCommentAndGetIdAsync(student.AccessToken, "Post", postId, "Own comment");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);
        var response = await _client.DeleteAsync($"/api/comments/{commentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteComment_ShouldReturnNoContent_WhenRequesterIsTeacher()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Comments", "Comments");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Post for comments");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        var commentId = await CreateCommentAndGetIdAsync(student.AccessToken, "Post", postId, "Student comment");

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        await AddParticipantAsync(owner.AccessToken, subjectId, teacher.UserId, "Teacher");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher.AccessToken);
        var response = await _client.DeleteAsync($"/api/comments/{commentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteComment_ShouldReturnNoContent_WhenRequesterIsAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Comments", "Comments");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Post for comments");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        var commentId = await CreateCommentAndGetIdAsync(student.AccessToken, "Post", postId, "Student comment");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.DeleteAsync($"/api/comments/{commentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteComment_ShouldReturnForbidden_WhenRequesterIsStudentNotAuthor()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Comments", "Comments");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Post for comments");

        var studentAuthor = await RegisterAndLoginAsync($"student_author_{Guid.NewGuid():N}");
        await JoinSubjectAsync(studentAuthor.AccessToken, subjectId);
        var commentId = await CreateCommentAndGetIdAsync(studentAuthor.AccessToken, "Post", postId, "Author comment");

        var studentOther = await RegisterAndLoginAsync($"student_other_{Guid.NewGuid():N}");
        await JoinSubjectAsync(studentOther.AccessToken, subjectId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentOther.AccessToken);
        var response = await _client.DeleteAsync($"/api/comments/{commentId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task DeleteComment_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.DeleteAsync($"/api/comments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact(Skip = "TODO: Нужна реализация submissions")]
    public async Task CreateComment_ShouldReturnCreated_WhenTargetTypeIsSubmissionAndRequesterHasAccess()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "TODO: Нужна реализация submissions")]
    public async Task GetComments_ShouldReturnComments_WhenTargetTypeIsSubmissionAndRequesterHasAccess()
    {
        throw new NotImplementedException();
    }

    private async Task<string> CreateCommentAndGetIdAsync(string accessToken, string targetType, string targetId, string text)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/comments", new
        {
            targetType,
            targetId,
            text
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var commentId = payload.GetProperty("id").GetString();
        commentId.Should().NotBeNullOrWhiteSpace();
        return commentId!;
    }

    private async Task<string> CreateAnnouncementPostAndGetIdAsync(string accessToken, string subjectId, string content)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Announcement"), "postType" },
            { new StringContent(content), "content" }
        };

        var response = await _client.PostAsync($"/api/subjects/{subjectId}/posts", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var postId = payload.GetProperty("id").GetString();
        postId.Should().NotBeNullOrWhiteSpace();
        return postId!;
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
