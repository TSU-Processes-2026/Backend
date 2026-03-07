namespace Infrastructure.Tests.Auth;

public sealed record LoginTokens(string AccessToken, string RefreshToken, string SessionId);
