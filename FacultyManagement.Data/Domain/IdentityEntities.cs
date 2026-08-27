using Microsoft.AspNetCore.Identity;

namespace FacultyManagement.Data.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullNameArabic { get; set; } = string.Empty;
    public string FullNameEnglish { get; set; } = string.Empty;
    public AccountKind AccountKind { get; set; }
    public bool IsApproved { get; set; }
    public string PreferredLanguage { get; set; } = "ar";
    public string? RequestedRolesCsv { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public StudentProfile? StudentProfile { get; set; }
    public StaffProfile? StaffProfile { get; set; }
}

public sealed class StudentProfile
{
    public Guid UserId { get; set; }
    public string UniversityNumber { get; set; } = string.Empty;
    public int CurrentStudyYear { get; set; } = 1;
    public AcademicStanding Standing { get; set; } = AcademicStanding.Active;
    public ApplicationUser User { get; set; } = null!;
}

public sealed class StaffProfile
{
    public Guid UserId { get; set; }
    public string? StaffNumber { get; set; }
    public ApplicationUser User { get; set; } = null!;
}

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
