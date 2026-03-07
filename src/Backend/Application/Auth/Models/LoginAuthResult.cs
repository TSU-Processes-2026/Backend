namespace Application.Auth.Models;

public sealed record LoginAuthResult(LoginAuthStatus Status, TokenResponse? Tokens)
{
    public static LoginAuthResult Success(TokenResponse tokens)
    {
        return new LoginAuthResult(LoginAuthStatus.Success, tokens);
    }

    public static LoginAuthResult Unauthorized()
    {
        return new LoginAuthResult(LoginAuthStatus.Unauthorized, null);
    }
}
