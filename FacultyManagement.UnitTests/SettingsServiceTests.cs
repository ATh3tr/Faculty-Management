using FacultyManagement.Business.Services;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.UnitTests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task GetAll_includes_default_faculty_names_when_rows_are_missing()
    {
        await using var db = CreateDatabase();
        var settings = await new SettingsService(db).GetAllAsync();

        Assert.Equal(BrandingDefaults.FacultyNameArabic, settings[SettingKeys.FacultyNameArabic]);
        Assert.Equal(BrandingDefaults.FacultyNameEnglish, settings[SettingKeys.FacultyNameEnglish]);
    }

    [Fact]
    public async Task Set_persists_a_custom_faculty_name()
    {
        await using var db = CreateDatabase();
        var service = new SettingsService(db);

        await service.SetAsync(SettingKeys.FacultyNameEnglish, "Faculty of Science", Guid.NewGuid());

        var saved = await db.SystemSettings.SingleAsync(x => x.Key == SettingKeys.FacultyNameEnglish);
        Assert.Equal("Faculty of Science", saved.Value);
    }

    private static FacultyDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<FacultyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FacultyDbContext(options);
    }
}
