using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Tests.TestHost;
using Xunit;

namespace Infrastructure.Tests.Posts;

public sealed class PostsEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PostsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPosts_ShouldReturnFilteredPosts_WhenRequesterIsParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");

        await CreatePostAsync(owner.AccessToken, subjectId, "Announcement", "Announcement content");
        await CreatePostAsync(owner.AccessToken, subjectId, "Material", "Material content", Encoding.UTF8.GetBytes("file"), "material.txt");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.GetAsync($"/api/subjects/{subjectId}/posts?postType=Announcement&limit=20&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetArrayLength().Should().Be(1);
        payload[0].GetProperty("postType").GetString().Should().Be("Announcement");
    }

    [Fact]
    public async Task GetPosts_ShouldReturnForbidden_WhenRequesterIsNotParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");

        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        var response = await _client.GetAsync($"/api/subjects/{subjectId}/posts");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetPosts_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.GetAsync($"/api/subjects/{Guid.NewGuid()}/posts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreatePost_ShouldReturnCreatedAnnouncement_WhenRequesterIsParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");

        var response = await CreatePostAsync(owner.AccessToken, subjectId, "Announcement", "Release notes");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("authorId").GetString().Should().Be(owner.UserId);
        payload.GetProperty("postType").GetString().Should().Be("Announcement");
        payload.GetProperty("content").GetString().Should().Be("Release notes");
    }

    [Fact]
    public async Task CreatePost_ShouldReturnCreatedMaterial_WhenRequesterIsParticipantAndFileProvided()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");

        var response = await CreatePostAsync(
            owner.AccessToken,
            subjectId,
            "Material",
            "Lesson file",
            Encoding.UTF8.GetBytes("material-bytes"),
            "lesson-1.txt");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("postType").GetString().Should().Be("Material");
        payload.GetProperty("fileName").GetString().Should().Be("lesson-1.txt");
    }

    [Fact]
    public async Task CreatePost_ShouldReturnForbidden_WhenRequesterIsNotParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");

        var other = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}");
        var response = await CreatePostAsync(other.AccessToken, subjectId, "Announcement", "Should fail");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreatePost_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Announcement"), "postType" },
            { new StringContent("No token"), "content" }
        };

        var response = await _client.PostAsync($"/api/subjects/{Guid.NewGuid()}/posts", form);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }


    [Fact]
    public async Task GetPosts_ShouldReturnAnnouncementAndMaterialShapes_WhenPostsHaveDifferentTypes()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");

        await CreatePostAsync(owner.AccessToken, subjectId, "Announcement", "Announcement body");
        await CreatePostAsync(owner.AccessToken, subjectId, "Material", "Material body", Encoding.UTF8.GetBytes("file"), "topic-1.txt");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.GetAsync($"/api/subjects/{subjectId}/posts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetArrayLength().Should().Be(2);

        var announcement = payload.EnumerateArray().Single(x => x.GetProperty("postType").GetString() == "Announcement");
        var material = payload.EnumerateArray().Single(x => x.GetProperty("postType").GetString() == "Material");

        announcement.TryGetProperty("fileName", out _).Should().BeFalse();
        announcement.TryGetProperty("storagePath", out _).Should().BeFalse();
        announcement.TryGetProperty("fileSize", out _).Should().BeFalse();

        material.TryGetProperty("fileName", out _).Should().BeTrue();
        material.TryGetProperty("storagePath", out _).Should().BeTrue();
        material.TryGetProperty("fileSize", out _).Should().BeTrue();
    }

    [Fact(Skip = "TODO: Для написания необходим Assignment")]
    public async Task CreatePost_ShouldReturnCreatedAssignment_WhenPostTypeIsAssignmentAndPayloadIsValid()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "TODO: Для написания необходим Assignment")]
    public async Task GetPosts_ShouldContainAssignmentShape_WhenAssignmentPostExists()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "TODO: Для написания необходим Assignment")]
    public async Task GetPostById_ShouldReturnAssignment_WhenPostIsAssignment()
    {
        throw new NotImplementedException();
    }
    [Fact]
    public async Task GetPostById_ShouldReturnPost_WhenRequesterIsParticipant()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Visible post");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);

        var response = await _client.GetAsync($"/api/posts/{postId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().Be(postId);
    }

    [Fact]
    public async Task GetPostById_ShouldReturnNotFound_WhenPostMissing()
    {
        var user = await RegisterAndLoginAsync($"user_{Guid.NewGuid():N}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

        var response = await _client.GetAsync($"/api/posts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }


    [Fact]
    public async Task GetPostById_ShouldReturnAnnouncement_WhenPostIsAnnouncement()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");
        var announcementId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Announcement post");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.GetAsync($"/api/posts/{announcementId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().Be(announcementId);
        payload.GetProperty("postType").GetString().Should().Be("Announcement");
        payload.TryGetProperty("fileName", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetPostById_ShouldReturnMaterial_WhenPostIsMaterial()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");
        var materialId = await CreateMaterialPostAndGetIdAsync(owner.AccessToken, subjectId, "Material post", "material.pdf");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.GetAsync($"/api/posts/{materialId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("id").GetString().Should().Be(materialId);
        payload.GetProperty("postType").GetString().Should().Be("Material");
        payload.GetProperty("fileName").GetString().Should().Be("material.pdf");
    }
    [Fact]
    public async Task GetPostById_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.GetAsync($"/api/posts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnOk_WhenRequesterIsAuthor()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Old content");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.PutAsJsonAsync($"/api/posts/{postId}", new { content = "Updated content" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("content").GetString().Should().Be("Updated content");
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnOk_WhenRequesterIsTeacher()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        var postId = await CreateAnnouncementPostAndGetIdAsync(student.AccessToken, subjectId, "Student post");

        var teacher = await RegisterAndLoginAsync($"teacher_{Guid.NewGuid():N}");
        await AddParticipantAsync(owner.AccessToken, subjectId, teacher.UserId, "Teacher");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher.AccessToken);

        var response = await _client.PutAsJsonAsync($"/api/posts/{postId}", new { content = "Teacher edited post" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("content").GetString().Should().Be("Teacher edited post");
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnForbidden_WhenRequesterIsStudentNotAuthor()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Owner post");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);

        var response = await _client.PutAsJsonAsync($"/api/posts/{postId}", new { content = "Should fail" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.PutAsJsonAsync($"/api/posts/{Guid.NewGuid()}", new { content = "No token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }


    [Fact]
    public async Task UpdatePost_ShouldNotChangePostType_WhenRequesterUpdatesContent()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");
        var postId = await CreateMaterialPostAndGetIdAsync(owner.AccessToken, subjectId, "Material before", "material.bin");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var updateResponse = await _client.PutAsJsonAsync($"/api/posts/{postId}", new { content = "Material after" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchResponse = await _client.GetAsync($"/api/posts/{postId}");
        fetchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await fetchResponse.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("postType").GetString().Should().Be("Material");
        payload.GetProperty("content").GetString().Should().Be("Material after");
    }
    [Fact]
    public async Task DeletePost_ShouldReturnNoContent_WhenRequesterIsAuthor()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "To delete");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.DeleteAsync($"/api/posts/{postId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletePost_ShouldReturnNoContent_WhenRequesterIsAdmin()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        var postId = await CreateAnnouncementPostAndGetIdAsync(student.AccessToken, subjectId, "Student post");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var response = await _client.DeleteAsync($"/api/posts/{postId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletePost_ShouldReturnForbidden_WhenRequesterIsStudentNotAuthor()
    {
        var owner = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}");
        var subjectId = await CreateSubjectAsync(owner.AccessToken, "Posts", "Posts");
        var postId = await CreateAnnouncementPostAndGetIdAsync(owner.AccessToken, subjectId, "Owner post");

        var student = await RegisterAndLoginAsync($"student_{Guid.NewGuid():N}");
        await JoinSubjectAsync(student.AccessToken, subjectId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);

        var response = await _client.DeleteAsync($"/api/posts/{postId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task DeletePost_ShouldReturnUnauthorized_WhenAccessTokenMissing()
    {
        var response = await _client.DeleteAsync($"/api/posts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
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

    private async Task<string> CreateAnnouncementPostAndGetIdAsync(string accessToken, string subjectId, string content)
    {
        var response = await CreatePostAsync(accessToken, subjectId, "Announcement", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var postId = payload.GetProperty("id").GetString();
        postId.Should().NotBeNullOrWhiteSpace();
        return postId!;
    }


    private async Task<string> CreateMaterialPostAndGetIdAsync(string accessToken, string subjectId, string content, string fileName)
    {
        var response = await CreatePostAsync(accessToken, subjectId, "Material", content, Encoding.UTF8.GetBytes("material"), fileName);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var postId = payload.GetProperty("id").GetString();
        postId.Should().NotBeNullOrWhiteSpace();
        return postId!;
    }
    private async Task<HttpResponseMessage> CreatePostAsync(
        string accessToken,
        string subjectId,
        string postType,
        string content,
        byte[]? fileBytes = null,
        string fileName = "material.bin")
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(postType), "postType" },
            { new StringContent(content), "content" }
        };

        if (fileBytes is not null)
        {
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", fileName);
        }

        return await _client.PostAsync($"/api/subjects/{subjectId}/posts", form);
    }

    private sealed record AuthUser(string UserId, string AccessToken);
}



