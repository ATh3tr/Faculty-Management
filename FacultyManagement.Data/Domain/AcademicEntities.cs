namespace FacultyManagement.Data.Domain;

public sealed class AcademicYear
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool IsCurrent { get; set; }
    public ICollection<Semester> Semesters { get; set; } = [];
}

public sealed class Semester
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AcademicYearId { get; set; }
    public int Number { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool IsPublished { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
}

public sealed class NonTeachingDay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AcademicYearId { get; set; }
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string NameArabic { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public int StudyYear { get; set; }
    public int SemesterNumber { get; set; }
    public int TheoreticalSessionsPerWeek { get; set; }
    public int PracticalSessionsPerDivisionPerWeek { get; set; }
    public bool RequiresProjector { get; set; }
    public bool RequiresLab { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CourseOffering
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid SemesterId { get; set; }
    public Course Course { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public ICollection<StaffCourseAssignment> StaffAssignments { get; set; } = [];
}

public sealed class StaffCourseAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseOfferingId { get; set; }
    public Guid StaffUserId { get; set; }
    public StaffCourseRole Role { get; set; }
    public CourseOffering CourseOffering { get; set; } = null!;
    public ApplicationUser StaffUser { get; set; } = null!;
}

public sealed class StudentCourseRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentUserId { get; set; }
    public Guid CourseId { get; set; }
    public Guid AssignedAcademicYearId { get; set; }
    public CourseResultStatus Status { get; set; } = CourseResultStatus.InProgress;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PassedAtUtc { get; set; }
    public ApplicationUser StudentUser { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public ICollection<MarkAttempt> MarkAttempts { get; set; } = [];
}

public sealed class Division
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AcademicYearId { get; set; }
    public int StudyYear { get; set; }
    public int Number { get; set; }
    public int Capacity { get; set; } = 30;
    public AcademicYear AcademicYear { get; set; } = null!;
    public ICollection<DivisionMembership> Memberships { get; set; } = [];
}

public sealed class DivisionMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DivisionId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid StudentUserId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public Division Division { get; set; } = null!;
    public ApplicationUser StudentUser { get; set; } = null!;
}

public sealed class ExamPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AcademicYearId { get; set; }
    public string NameArabic { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool IsRetake { get; set; }
    public bool IsClosed { get; set; }
}

public sealed class MarkAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentCourseRecordId { get; set; }
    public Guid ExamPeriodId { get; set; }
    public ExamResultKind ResultKind { get; set; }
    public decimal? Mark { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime EnteredAtUtc { get; set; } = DateTime.UtcNow;
    public Guid EnteredByUserId { get; set; }
    public StudentCourseRecord StudentCourseRecord { get; set; } = null!;
    public ExamPeriod ExamPeriod { get; set; } = null!;
    public ICollection<MarkCorrection> Corrections { get; set; } = [];
}

public sealed class MarkCorrection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MarkAttemptId { get; set; }
    public ExamResultKind OldResultKind { get; set; }
    public decimal? OldMark { get; set; }
    public ExamResultKind NewResultKind { get; set; }
    public decimal? NewMark { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid CorrectedByUserId { get; set; }
    public DateTime CorrectedAtUtc { get; set; } = DateTime.UtcNow;
    public MarkAttempt MarkAttempt { get; set; } = null!;
}

public sealed class MarkAppeal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MarkAttemptId { get; set; }
    public Guid StudentUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public AppealStatus Status { get; set; } = AppealStatus.Submitted;
    public string? ProfessorComment { get; set; }
    public Guid? ProfessorUserId { get; set; }
    public string? DecisionComment { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }
    public MarkAttempt MarkAttempt { get; set; } = null!;
}

public sealed class PromotionRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AcademicYearId { get; set; }
    public Guid ExecutedByUserId { get; set; }
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsPreview { get; set; }
    public ICollection<PromotionResult> Results { get; set; } = [];
}

public sealed class PromotionResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PromotionRunId { get; set; }
    public Guid StudentUserId { get; set; }
    public int PreviousStudyYear { get; set; }
    public int NewStudyYear { get; set; }
    public int OutstandingFailureCount { get; set; }
    public AcademicStanding NewStanding { get; set; }
    public PromotionRun PromotionRun { get; set; } = null!;
}
