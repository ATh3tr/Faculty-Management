using FacultyManagement.Business;

namespace FacultyManagement.UnitTests;

public sealed class InputRulesTests
{
    [Theory]
    [InlineData("student@faculty.demo")]
    [InlineData("name.surname@example.edu")]
    public void Complete_email_is_accepted(string email) => InputRules.ValidateEmail(email);

    [Theory]
    [InlineData("osama@faculty")]
    [InlineData("osama@faculty.x")]
    [InlineData("not-an-email")]
    public void Incomplete_email_is_rejected(string email) =>
        Assert.Throws<BusinessException>(() => InputRules.ValidateEmail(email));

    [Fact]
    public void University_number_accepts_only_ascii_digits()
    {
        InputRules.ValidateUniversityNumber("20260001");
        Assert.Throws<BusinessException>(() => InputRules.ValidateUniversityNumber("٢٠٢٦٠٠٠١"));
        Assert.Throws<BusinessException>(() => InputRules.ValidateUniversityNumber("2026A001"));
    }

    [Fact]
    public void Bilingual_fields_enforce_their_script()
    {
        InputRules.ValidateBilingual("هندسة البرمجيات", "Software Engineering", "name");
        Assert.Throws<BusinessException>(() => InputRules.ValidateBilingual("Software", "Engineering", "name"));
        Assert.Throws<BusinessException>(() => InputRules.ValidateBilingual("هندسة", "هندسة", "name"));
    }
}
