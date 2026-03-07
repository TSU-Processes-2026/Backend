using Api.Controllers;
using Application.Auth.Contracts;
using Application.Auth.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Infrastructure.Tests.Auth;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_ShouldReturnConflict_WhenServiceReturnsConflict()
    {
        var authService = new Mock<IAuthService>();
        authService
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RegisterAuthResult.Conflict("User with this username already exists."));

        var controller = new AuthController(authService.Object);

        var response = await controller.Register(new RegisterRequest
        {
            Username = "existing",
            Password = "StrongP@ssw0rd!"
        }, CancellationToken.None);

        response.Should().BeOfType<ConflictObjectResult>();

        var problemDetails = ((ConflictObjectResult)response).Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(409);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenServiceReturnsUnauthorized()
    {
        var authService = new Mock<IAuthService>();
        authService
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginAuthResult.Unauthorized());

        var controller = new AuthController(authService.Object);

        var response = await controller.Login(new LoginRequest
        {
            Username = "user",
            Password = "wrong"
        }, CancellationToken.None);

        response.Should().BeOfType<UnauthorizedObjectResult>();

        var problemDetails = ((UnauthorizedObjectResult)response).Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(401);
    }
}
