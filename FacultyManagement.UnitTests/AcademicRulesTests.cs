using FacultyManagement.Business;
using FacultyManagement.Data.Domain;

namespace FacultyManagement.UnitTests;

public sealed class AcademicRulesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Up_to_four_failures_promotes_student(int failures)
    {
        var result = AcademicRules.DecidePromotion(2, failures, 4, 5);
        Assert.Equal(3, result.NewStudyYear);
        Assert.Equal(AcademicStanding.Active, result.Standing);
    }

    [Fact]
    public void Fifth_failure_repeats_year()
    {
        var result = AcademicRules.DecidePromotion(2, 5, 4, 5);
        Assert.Equal(2, result.NewStudyYear);
        Assert.Equal(AcademicStanding.Repeating, result.Standing);
    }

    [Fact]
    public void Final_year_requires_zero_failures_to_graduate()
    {
        Assert.Equal(AcademicStanding.Graduated, AcademicRules.DecidePromotion(5, 0, 4, 5).Standing);
        Assert.Equal(AcademicStanding.Repeating, AcademicRules.DecidePromotion(5, 1, 4, 5).Standing);
    }

    [Theory]
    [InlineData(59, CourseResultStatus.Failed)]
    [InlineData(60, CourseResultStatus.Passed)]
    [InlineData(100, CourseResultStatus.Passed)]
    public void Sixty_is_the_pass_threshold(decimal mark, CourseResultStatus expected) =>
        Assert.Equal(expected, AcademicRules.ResultAfterAttempt(ExamResultKind.Numeric, mark));

    [Fact]
    public void Withheld_result_does_not_fail_course() =>
        Assert.Equal(CourseResultStatus.InProgress, AcademicRules.ResultAfterAttempt(ExamResultKind.Withheld, null));
}
