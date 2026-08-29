using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class AccountService(
    FacultyDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager)
{
    public async Task<Guid> RegisterStudentAsync(RegisterStudentRequest request)
    {
        InputRules.ValidateUniversityNumber(request.UniversityNumber);
        InputRules.ValidateEmail(request.Email);
        InputRules.ValidateBilingual(request.FullNameArabic, request.FullNameEnglish, "name");
        if (await db.StudentProfiles.AnyAsync(x => x.UniversityNumber == request.UniversityNumber))
            throw new BusinessException("University number is already registered.", 409, "university_number_exists");

        var user = NewUser(request.Email, request.FullNameArabic, request.FullNameEnglish, request.PreferredLanguage, AccountKind.Student);
        var result = await userManager.CreateAsync(user, request.Password);
        EnsureIdentitySucceeded(result);
        db.StudentProfiles.Add(new StudentProfile { UserId = user.Id, UniversityNumber = request.UniversityNumber });
        await db.SaveChangesAsync();
        return user.Id;
    }

    public async Task<Guid> RegisterStaffAsync(RegisterStaffRequest request)
    {
        InputRules.ValidateEmail(request.Email);
        InputRules.ValidateBilingual(request.FullNameArabic, request.FullNameEnglish, "name");
        var invalidRoles = request.RequestedRoles.Except([AppRoles.Teacher, AppRoles.Professor, AppRoles.ExamsOfficer]).ToArray();
        if (invalidRoles.Length > 0)
            throw new BusinessException($"Invalid staff roles: {string.Join(", ", invalidRoles)}");

        var user = NewUser(request.Email, request.FullNameArabic, request.FullNameEnglish, request.PreferredLanguage, AccountKind.Staff);
        user.RequestedRolesCsv = string.Join(',', request.RequestedRoles.Distinct());
        var result = await userManager.CreateAsync(user, request.Password);
        EnsureIdentitySucceeded(result);
        db.StaffProfiles.Add(new StaffProfile { UserId = user.Id, StaffNumber = request.StaffNumber });
        await db.SaveChangesAsync();
        return user.Id;
    }

    public async Task ApproveAsync(Guid userId, IReadOnlyCollection<string> roles, Guid adminId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new BusinessException("Account not found.", 404, "account_not_found");
        if (roles.Count == 0 || roles.Any(x => !AppRoles.All.Contains(x)))
            throw new BusinessException("At least one valid role is required.");
        if (user.AccountKind == AccountKind.Student && (roles.Count != 1 || roles.Single() != AppRoles.Student))
            throw new BusinessException("Student accounts can only receive the Student role.");
        if (user.AccountKind == AccountKind.Staff && roles.Contains(AppRoles.Student))
            throw new BusinessException("Staff accounts cannot receive the Student role.");

        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                throw new BusinessException($"Role '{role}' does not exist.", 500);

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            EnsureIdentitySucceeded(await userManager.RemoveFromRolesAsync(user, currentRoles));
        EnsureIdentitySucceeded(await userManager.AddToRolesAsync(user, roles.Distinct()));
        user.IsApproved = true;
        user.RequestedRolesCsv = null;
        EnsureIdentitySucceeded(await userManager.UpdateAsync(user));
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = adminId, Action = "AccountApproved", EntityType = nameof(ApplicationUser), EntityId = user.Id.ToString(),
            NewValuesJson = $"{{\"roles\":\"{string.Join(',', roles)}\"}}"
        });
        await db.SaveChangesAsync();

        if (user.AccountKind == AccountKind.Student)
            await AssignCurrentYearCoursesAsync(user.Id, 1);
    }

    public async Task<IReadOnlyCollection<UserSummary>> PendingAsync()
    {
        var users = await db.Users.AsNoTracking().Where(x => !x.IsApproved).OrderBy(x => x.CreatedAtUtc).ToListAsync();
        return users.Select(x => new UserSummary(x.Id, x.Email!, x.FullNameArabic, x.FullNameEnglish, x.IsApproved,
            (x.RequestedRolesCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))).ToArray();
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, Guid adminId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new BusinessException("Account not found.", 404);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        EnsureIdentitySucceeded(await userManager.ResetPasswordAsync(user, token, newPassword));
        var activeTokens = await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null).ToListAsync();
        foreach (var item in activeTokens) item.RevokedAtUtc = DateTime.UtcNow;
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = adminId, Action = "PasswordResetByAdmin", EntityType = nameof(ApplicationUser), EntityId = userId.ToString()
        });
        await db.SaveChangesAsync();
    }

    private async Task AssignCurrentYearCoursesAsync(Guid studentId, int studyYear)
    {
        var academicYearId = await db.AcademicYears.Where(x => x.IsCurrent).Select(x => (Guid?)x.Id).SingleOrDefaultAsync();
        if (academicYearId is null) return;
        var courses = await db.Courses.Where(x => x.IsActive && x.StudyYear == studyYear).Select(x => x.Id).ToListAsync();
        db.StudentCourseRecords.AddRange(courses.Select(courseId => new StudentCourseRecord
        {
            StudentUserId = studentId,
            CourseId = courseId,
            AssignedAcademicYearId = academicYearId.Value
        }));
        await db.SaveChangesAsync();
    }

    private static ApplicationUser NewUser(string email, string arName, string enName, string language, AccountKind kind) => new()
    {
        UserName = email.Trim(), Email = email.Trim(), FullNameArabic = arName.Trim(), FullNameEnglish = enName.Trim(),
        PreferredLanguage = language == "en" ? "en" : "ar", AccountKind = kind, IsApproved = false
    };

    private static void EnsureIdentitySucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BusinessException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
