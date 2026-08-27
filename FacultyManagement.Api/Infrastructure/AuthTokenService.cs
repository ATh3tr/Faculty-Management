using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FacultyManagement.Business;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FacultyManagement.Api.Infrastructure;

public sealed record IssuedTokens(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);

public sealed class AuthTokenService(
    FacultyDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
    IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public async Task<IssuedTokens> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email) ?? throw new BusinessException("Invalid credentials.", 401, "invalid_credentials");
        if (!user.IsApproved) throw new BusinessException("Account is awaiting administrator approval.", 403, "account_pending");
        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded) throw new BusinessException("Invalid credentials.", 401, "invalid_credentials");
        return await IssueAsync(user, null, ct);
    }

    public async Task<IssuedTokens> RefreshAsync(string rawToken, CancellationToken ct = default)
    {
        var hash = Hash(rawToken);
        var stored = await db.RefreshTokens.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, ct)
            ?? throw new BusinessException("Invalid refresh token.", 401, "invalid_refresh_token");
        if (stored.RevokedAtUtc is not null || stored.ExpiresAtUtc <= DateTime.UtcNow || !stored.User.IsApproved)
            throw new BusinessException("Refresh token is expired or revoked.", 401, "invalid_refresh_token");
        return await IssueAsync(stored.User, stored, ct);
    }

    public async Task RevokeAsync(string rawToken, CancellationToken ct = default)
    {
        var hash = Hash(rawToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (stored is null) return;
        stored.RevokedAtUtc ??= DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<IssuedTokens> IssueAsync(ApplicationUser user, RefreshToken? replaced, CancellationToken ct)
    {
        if (_options.SigningKey.Length < 32) throw new InvalidOperationException("JWT signing key must contain at least 32 characters.");
        var roles = await userManager.GetRolesAsync(user);
        var accessExpires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullNameEnglish),
            new("language", user.PreferredLanguage)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: accessExpires, signingCredentials: credentials);
        var rawRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshExpires = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
        var token = new RefreshToken { UserId = user.Id, TokenHash = Hash(rawRefresh), ExpiresAtUtc = refreshExpires };
        db.RefreshTokens.Add(token);
        if (replaced is not null)
        {
            replaced.RevokedAtUtc = DateTime.UtcNow;
            replaced.ReplacedByTokenHash = token.TokenHash;
        }
        await db.SaveChangesAsync(ct);
        return new IssuedTokens(new JwtSecurityTokenHandler().WriteToken(jwt), accessExpires, rawRefresh, refreshExpires);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
