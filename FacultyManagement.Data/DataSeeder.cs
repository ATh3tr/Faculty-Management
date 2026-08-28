using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(
        FacultyDbContext db,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        string adminEmail,
        string adminPassword,
        CancellationToken cancellationToken = default)
    {
        var migrations = db.Database.GetMigrations();
        if (migrations.Any()) await db.Database.MigrateAsync(cancellationToken);
        else await db.Database.EnsureCreatedAsync(cancellationToken);

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }

        if (!await db.SystemSettings.AnyAsync(cancellationToken))
        {
            db.SystemSettings.AddRange(
                new SystemSetting { Key = SettingKeys.MaximumFailedCoursesForPromotion, Value = "4" },
                new SystemSetting { Key = SettingKeys.ProgramYears, Value = "5" },
                new SystemSetting { Key = SettingKeys.DefaultDivisionCapacity, Value = "30" },
                new SystemSetting { Key = SettingKeys.AppealDeadlineDays, Value = "5" },
                new SystemSetting { Key = SettingKeys.TimeZone, Value = "Asia/Damascus" });
        }

        if (!await db.TimeSlots.AnyAsync(cancellationToken))
        {
            db.TimeSlots.AddRange(
                new TimeSlot { StartsAt = new TimeOnly(9, 0), EndsAt = new TimeOnly(10, 30) },
                new TimeSlot { StartsAt = new TimeOnly(10, 45), EndsAt = new TimeOnly(12, 15) },
                new TimeSlot { StartsAt = new TimeOnly(12, 30), EndsAt = new TimeOnly(14, 0) },
                new TimeSlot { StartsAt = new TimeOnly(14, 15), EndsAt = new TimeOnly(15, 45) },
                new TimeSlot { StartsAt = new TimeOnly(16, 0), EndsAt = new TimeOnly(17, 30) });
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullNameArabic = "مدير النظام",
                FullNameEnglish = "System Administrator",
                AccountKind = AccountKind.Staff,
                IsApproved = true,
                PreferredLanguage = "ar"
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            db.StaffProfiles.Add(new StaffProfile { UserId = admin.Id });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
