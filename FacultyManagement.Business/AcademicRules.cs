using FacultyManagement.Data.Domain;

namespace FacultyManagement.Business;

public static class AcademicRules
{
    public static (int NewStudyYear, AcademicStanding Standing) DecidePromotion(
        int currentStudyYear, int outstandingFailures, int maximumFailures, int programYears)
    {
        if (currentStudyYear < 1 || currentStudyYear > programYears) throw new ArgumentOutOfRangeException(nameof(currentStudyYear));
        if (outstandingFailures < 0) throw new ArgumentOutOfRangeException(nameof(outstandingFailures));
        if (currentStudyYear == programYears)
            return outstandingFailures == 0
                ? (currentStudyYear, AcademicStanding.Graduated)
                : (currentStudyYear, AcademicStanding.Repeating);
        return outstandingFailures <= maximumFailures
            ? (currentStudyYear + 1, AcademicStanding.Active)
            : (currentStudyYear, AcademicStanding.Repeating);
    }

    public static CourseResultStatus ResultAfterAttempt(ExamResultKind kind, decimal? mark)
    {
        if (kind == ExamResultKind.Numeric && mark >= 60) return CourseResultStatus.Passed;
        if (kind is ExamResultKind.Numeric or ExamResultKind.Absent) return CourseResultStatus.Failed;
        return CourseResultStatus.InProgress;
    }
}
