namespace Application.Auth.Models;

public sealed record RegisterAuthResult(
    RegisterAuthStatus Status,
    UserResponse? User,
    IReadOnlyDictionary<string, string[]>? Errors,
    string? ErrorDetail)
{
    public static RegisterAuthResult Created(UserResponse user)
    {
        return new RegisterAuthResult(RegisterAuthStatus.Created, user, null, null);
    }

    public static RegisterAuthResult Conflict(string detail)
    {
        return new RegisterAuthResult(RegisterAuthStatus.Conflict, null, null, detail);
    }

    public static RegisterAuthResult BadRequest(IReadOnlyDictionary<string, string[]> errors)
    {
        return new RegisterAuthResult(RegisterAuthStatus.BadRequest, null, errors, null);
    }
}
