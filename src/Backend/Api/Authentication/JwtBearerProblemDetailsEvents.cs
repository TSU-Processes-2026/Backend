using System.Text.Json;
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

        var payload = JsonSerializer.Serialize(new ProblemDetails
        {
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Authentication failed."
        });

        return context.Response.WriteAsync(payload, context.HttpContext.RequestAborted);
    }

    public override Task Forbidden(ForbiddenContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        var payload = JsonSerializer.Serialize(new ProblemDetails
        {
            Title = "Forbidden",
            Status = StatusCodes.Status403Forbidden,
            Detail = "Access denied."
        });

        return context.Response.WriteAsync(payload, context.HttpContext.RequestAborted);
    }
}
