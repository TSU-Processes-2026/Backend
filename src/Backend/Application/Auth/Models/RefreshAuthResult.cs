namespace Application.Auth.Models;

public sealed record RefreshAuthResult(RefreshAuthStatus Status, TokenResponse? Tokens)
{
    public static RefreshAuthResult Success(TokenResponse tokens)
    {
        return new RefreshAuthResult(RefreshAuthStatus.Success, tokens);
    }

    public static RefreshAuthResult Unauthorized()
    {
        return new RefreshAuthResult(RefreshAuthStatus.Unauthorized, null);
    }
}
