namespace FacultyManagement.Api.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "FacultyManagement";
    public string Audience { get; set; } = "FacultyManagement.React";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
