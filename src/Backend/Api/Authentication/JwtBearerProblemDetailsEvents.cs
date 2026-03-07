using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace Api.Authentication;

public sealed class JwtBearerProblemDetailsEvents : JwtBearerEvents
{
    public override Task Challenge(JwtBearerChallengeContext context)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Authentication failed."
        }, context.HttpContext.RequestAborted);
    }

    public override Task Forbidden(ForbiddenContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Forbidden",
            Status = StatusCodes.Status403Forbidden,
            Detail = "Access denied."
        }, context.HttpContext.RequestAborted);
    }
}
