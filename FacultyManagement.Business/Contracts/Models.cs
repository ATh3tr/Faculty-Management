using FacultyManagement.Data.Domain;

namespace FacultyManagement.Business.Contracts;

public sealed record RegisterStudentRequest(
    string UniversityNumber, string Email, string Password,
    string FullNameArabic, string FullNameEnglish, string PreferredLanguage = "ar");

public sealed record RegisterStaffRequest(
    string Email, string Password, string FullNameArabic, string FullNameEnglish,
    string? StaffNumber, IReadOnlyCollection<string> RequestedRoles, string PreferredLanguage = "ar");

public sealed record ApproveAccountRequest(IReadOnlyCollection<string> Roles);
public sealed record AdminResetPasswordRequest(string NewPassword);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record TokenResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc);
public sealed record UserSummary(Guid Id, string Email, string FullNameArabic, string FullNameEnglish, bool IsApproved, IReadOnlyCollection<string> Roles);

public sealed record CreateAcademicYearRequest(
    string Name, DateOnly StartsOn, DateOnly EndsOn,
    DateOnly FirstSemesterStartsOn, DateOnly FirstSemesterEndsOn,
    DateOnly SecondSemesterStartsOn, DateOnly SecondSemesterEndsOn);
public sealed record SetNonTeachingDayRequest(DateOnly Date, string Reason);
public sealed record CreateExamPeriodRequest(
    Guid AcademicYearId, string NameArabic, string NameEnglish,
    DateOnly StartsOn, DateOnly EndsOn, bool IsRetake);
public sealed record CreateCourseRequest(
    string Code, string NameArabic, string NameEnglish, int StudyYear, int SemesterNumber,
    int TheoreticalSessionsPerWeek, int PracticalSessionsPerDivisionPerWeek,
    bool RequiresProjector, bool RequiresLab);
public sealed record UpdateCourseRequest(
    string NameArabic, string NameEnglish, int StudyYear, int SemesterNumber,
    int TheoreticalSessionsPerWeek, int PracticalSessionsPerDivisionPerWeek,
    bool RequiresProjector, bool RequiresLab, bool IsActive);
public sealed record CreateOfferingRequest(Guid CourseId, Guid AcademicYearId, Guid SemesterId);
public sealed record AssignStaffRequest(Guid StaffUserId, StaffCourseRole Role);

public sealed record DivisionAssignmentResult(Guid DivisionId, int DivisionNumber, int StudyYear, int Capacity, int MemberCount, bool DivisionCreated);

public sealed record CreateRoomRequest(string Code, int Capacity, bool IsLab, bool HasProjector);
public sealed record UpdateRoomRequest(string Code, int Capacity, bool IsLab, bool HasProjector, bool IsActive);
public sealed record SetRoomUnavailabilityRequest(DateOnly StartsOn, DateOnly EndsOn, string Reason);

public sealed record CreateScheduleRequest(
    ActivityType ActivityType, string TitleArabic, string TitleEnglish,
    Guid? CourseOfferingId, Guid? DivisionId, int? AudienceStudyYear,
    Guid RoomId, Guid StaffUserId, int TimeSlotId, DayOfWeek DayOfWeek,
    DateOnly StartsOn, DateOnly EndsOn, bool IsRecurring, ScheduleStatus Status = ScheduleStatus.Published);
public sealed record RescheduleRequest(Guid RoomId, int TimeSlotId, DayOfWeek DayOfWeek, DateOnly StartsOn, DateOnly EndsOn);
public sealed record ScheduleConflict(string Code, string Message);
public sealed record ScheduleResult(Guid Id, ScheduleStatus Status, int OccurrenceCount);
public sealed record ScheduleView(
    Guid SeriesId, ActivityType ActivityType, string Title, string RoomCode,
    DateOnly Date, TimeOnly StartsAt, TimeOnly EndsAt, bool IsCancelled);

public sealed record EnterMarkRequest(
    Guid StudentCourseRecordId, Guid ExamPeriodId, ExamResultKind ResultKind, decimal? Mark, bool Publish);
public sealed record CorrectMarkRequest(ExamResultKind ResultKind, decimal? Mark, string Reason);
public sealed record MarkView(Guid AttemptId, Guid CourseId, string CourseCode, ExamResultKind ResultKind, decimal? Mark, bool IsPublished, DateTime EnteredAtUtc);
public sealed record ImportMarkRow(string UniversityNumber, string CourseCode, ExamResultKind ResultKind, decimal? Mark);
public sealed record ImportMarksRequest(Guid ExamPeriodId, bool Publish, IReadOnlyCollection<ImportMarkRow> Rows);
public sealed record ImportError(int Row, string Message);
public sealed record ImportResult(int Imported, IReadOnlyCollection<ImportError> Errors);

public sealed record CreateAppealRequest(Guid MarkAttemptId, string Reason);
public sealed record ProfessorReviewRequest(string Comment);
public sealed record AppealDecisionRequest(bool Accept, string Comment);

public sealed record PromotionStudentResult(Guid StudentUserId, int PreviousStudyYear, int NewStudyYear, int OutstandingFailures, AcademicStanding Standing);
public sealed record PromotionRunResult(Guid RunId, bool IsPreview, IReadOnlyCollection<PromotionStudentResult> Students);

public sealed record CreateAnnouncementRequest(
    string TitleArabic, string TitleEnglish, string BodyArabic, string BodyEnglish,
    AnnouncementAudience Audience, int? StudyYear, Guid? DivisionId, Guid? StudentUserId);
public sealed record NotificationView(Guid Id, NotificationType Type, string Title, string Body, string? Link, DateTime CreatedAtUtc, bool IsRead);

public sealed record GenerateTimetableRequest(Guid AcademicYearId, Guid SemesterId, int MaximumSeconds = 30);
public sealed record GeneratedSession(
    Guid ScheduleSeriesId, Guid CourseOfferingId, ActivityType ActivityType, Guid? DivisionId, int StudyYear,
    Guid StaffUserId, Guid RoomId, DayOfWeek DayOfWeek, int TimeSlotId);
public sealed record GeneratedTimetableResult(Guid? PlanId, bool IsFeasible, string Status, IReadOnlyCollection<GeneratedSession> Sessions);

public sealed record AcademicYearView(Guid Id, string Name, DateOnly StartsOn, DateOnly EndsOn, bool IsCurrent,
    IReadOnlyCollection<SemesterView> Semesters);
public sealed record SemesterView(Guid Id, int Number, DateOnly StartsOn, DateOnly EndsOn, bool IsPublished);
public sealed record CourseView(Guid Id, string Code, string NameArabic, string NameEnglish, int StudyYear, int SemesterNumber,
    int TheoreticalSessionsPerWeek, int PracticalSessionsPerDivisionPerWeek, bool RequiresProjector, bool RequiresLab, bool IsActive);
public sealed record OfferingView(Guid Id, Guid CourseId, string CourseCode, Guid AcademicYearId, Guid SemesterId,
    IReadOnlyCollection<StaffAssignmentView> Staff);
public sealed record StaffAssignmentView(Guid StaffUserId, string NameArabic, string NameEnglish, StaffCourseRole Role);
public sealed record DivisionView(Guid Id, Guid AcademicYearId, int StudyYear, int Number, int Capacity, int MemberCount);
public sealed record RoomView(Guid Id, string Code, int Capacity, bool IsLab, bool HasProjector, bool IsActive);
public sealed record TimeSlotView(int Id, TimeOnly StartsAt, TimeOnly EndsAt);
public sealed record ExamPeriodView(Guid Id, Guid AcademicYearId, string NameArabic, string NameEnglish,
    DateOnly StartsOn, DateOnly EndsOn, bool IsRetake, bool IsClosed);
public sealed record StudentView(Guid UserId, string UniversityNumber, string NameArabic, string NameEnglish, int StudyYear, AcademicStanding Standing);
public sealed record StudentCourseRecordView(Guid Id, Guid StudentUserId, string UniversityNumber,
    string StudentNameArabic, string StudentNameEnglish, Guid CourseId, string CourseCode,
    string CourseNameArabic, string CourseNameEnglish, CourseResultStatus Status, Guid AssignedAcademicYearId);
public sealed record ExamMarkView(Guid AttemptId, Guid StudentCourseRecordId, Guid StudentUserId,
    string UniversityNumber, string StudentNameArabic, string StudentNameEnglish, string CourseCode,
    Guid ExamPeriodId, ExamResultKind ResultKind, decimal? Mark, bool IsPublished, DateTime EnteredAtUtc);
public sealed record StaffView(Guid UserId, string NameArabic, string NameEnglish, string? StaffNumber, IReadOnlyCollection<string> Roles);
public sealed record AppealView(Guid Id, Guid MarkAttemptId, Guid StudentUserId, string StudentName, string CourseCode,
    string Reason, AppealStatus Status, string? ProfessorComment, string? DecisionComment, DateTime SubmittedAtUtc);
public sealed record AuditView(long Id, Guid? UserId, string Action, string EntityType, string EntityId, DateTime CreatedAtUtc);
