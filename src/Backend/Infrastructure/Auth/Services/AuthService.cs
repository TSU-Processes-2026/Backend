using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Auth.Contracts;
using Application.Auth.Models;
using Application.Auth.Options;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LmsDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        LmsDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async Task<RegisterAuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Username
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (createResult.Succeeded)
        {
            return RegisterAuthResult.Created(new UserResponse
            {
                Id = user.Id,
                Username = user.UserName!
            });
        }

        if (createResult.Errors.Any(x => x.Code.Equals("DuplicateUserName", StringComparison.OrdinalIgnoreCase)))
        {
            return RegisterAuthResult.Conflict("User with this username already exists.");
        }

        var errors = createResult.Errors
            .GroupBy(_ => "identity")
            .ToDictionary(grouping => grouping.Key, grouping => grouping.Select(x => x.Description).ToArray() as string[]);

        return RegisterAuthResult.BadRequest(errors);
    }

    public async Task<LoginAuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);

        if (user is null)
        {
            return LoginAuthResult.Unauthorized();
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            return LoginAuthResult.Unauthorized();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var sessionId = Guid.NewGuid();
        var accessToken = BuildAccessToken(user.Id, user.UserName!, sessionId, now);
        var refreshToken = GenerateRefreshToken();

        var session = new AuthSession
        {
            Id = sessionId,
            UserId = user.Id,
            CreatedAtUtc = now,
            RevokedAtUtc = null,
            RefreshTokens = new List<RefreshToken>()
        };

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = user.Id,
            TokenHash = ComputeRefreshTokenHash(refreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddSeconds(_jwtOptions.RefreshTokenLifetimeSeconds),
            UsedAtUtc = null,
            RevokedAtUtc = null,
            ReplacedByTokenId = null,
            Session = session
        };

        _dbContext.AuthSessions.Add(session);
        _dbContext.RefreshTokens.Add(refreshTokenEntity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return LoginAuthResult.Success(new TokenResponse
        {
            TokenType = "Bearer",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtOptions.AccessTokenLifetimeSeconds,
            RefreshExpiresIn = _jwtOptions.RefreshTokenLifetimeSeconds,
            UserId = user.Id,
            SessionId = sessionId
        });
    }

    public async Task<RefreshAuthResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeRefreshTokenHash(request.RefreshToken);

        var refreshToken = await _dbContext.RefreshTokens
            .Include(x => x.Session)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return RefreshAuthResult.Unauthorized();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (refreshToken.UsedAtUtc is not null)
        {
            await RevokeSessionAsync(refreshToken.SessionId, now, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return RefreshAuthResult.Unauthorized();
        }

        if (refreshToken.RevokedAtUtc is not null || refreshToken.ExpiresAtUtc <= now || refreshToken.Session.RevokedAtUtc is not null)
        {
            return RefreshAuthResult.Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());

        if (user is null)
        {
            return RefreshAuthResult.Unauthorized();
        }

        refreshToken.UsedAtUtc = now;

        var rotatedRefreshToken = GenerateRefreshToken();
        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = refreshToken.SessionId,
            UserId = refreshToken.UserId,
            TokenHash = ComputeRefreshTokenHash(rotatedRefreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddSeconds(_jwtOptions.RefreshTokenLifetimeSeconds),
            UsedAtUtc = null,
            RevokedAtUtc = null,
            ReplacedByTokenId = null,
            Session = refreshToken.Session
        };

        refreshToken.ReplacedByTokenId = newRefreshTokenEntity.Id;

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RefreshAuthResult.Success(new TokenResponse
        {
            TokenType = "Bearer",
            AccessToken = BuildAccessToken(user.Id, user.UserName!, refreshToken.SessionId, now),
            RefreshToken = rotatedRefreshToken,
            ExpiresIn = _jwtOptions.AccessTokenLifetimeSeconds,
            RefreshExpiresIn = _jwtOptions.RefreshTokenLifetimeSeconds,
            UserId = user.Id,
            SessionId = refreshToken.SessionId
        });
    }

    public async Task<LogoutAuthResult> LogoutAsync(Guid userId, RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeRefreshTokenHash(request.RefreshToken);

        var refreshToken = await _dbContext.RefreshTokens
            .Include(x => x.Session)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.UserId == userId, cancellationToken);

        if (refreshToken is null)
        {
            return LogoutAuthResult.Unauthorized();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (refreshToken.RevokedAtUtc is not null || refreshToken.ExpiresAtUtc <= now || refreshToken.Session.RevokedAtUtc is not null)
        {
            return LogoutAuthResult.Unauthorized();
        }

        await RevokeSessionAsync(refreshToken.SessionId, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return LogoutAuthResult.NoContent();
    }

    private string BuildAccessToken(Guid userId, string username, Guid sessionId, DateTime issuedAtUtc)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: BuildClaims(userId, username, sessionId),
            notBefore: issuedAtUtc,
            expires: issuedAtUtc.AddSeconds(_jwtOptions.AccessTokenLifetimeSeconds),
            signingCredentials: signingCredentials);

        return _tokenHandler.WriteToken(token);
    }

    private static IEnumerable<Claim> BuildClaims(Guid userId, string username, Guid sessionId)
    {
        return
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("sessionId", sessionId.ToString())
        ];
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string ComputeRefreshTokenHash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }

    private async Task RevokeSessionAsync(Guid sessionId, DateTime revokedAtUtc, CancellationToken cancellationToken)
    {
        var session = await _dbContext.AuthSessions
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return;
        }

        if (session.RevokedAtUtc is null)
        {
            session.RevokedAtUtc = revokedAtUtc;
        }

        var sessionTokens = await _dbContext.RefreshTokens
            .Where(x => x.SessionId == sessionId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var sessionToken in sessionTokens)
        {
            sessionToken.RevokedAtUtc = revokedAtUtc;
        }
    }
}
