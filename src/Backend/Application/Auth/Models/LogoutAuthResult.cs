namespace Application.Auth.Models;

public sealed record LogoutAuthResult(LogoutAuthStatus Status)
{
    public static LogoutAuthResult NoContent()
    {
        return new LogoutAuthResult(LogoutAuthStatus.NoContent);
    }

    public static LogoutAuthResult Unauthorized()
    {
        return new LogoutAuthResult(LogoutAuthStatus.Unauthorized);
    }
}
