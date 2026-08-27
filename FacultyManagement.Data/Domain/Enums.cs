namespace FacultyManagement.Data.Domain;

public enum AccountKind { Student = 1, Staff = 2 }
public enum AcademicStanding { Active = 1, Repeating = 2, Graduated = 3, Suspended = 4 }
public enum CourseResultStatus { InProgress = 1, Failed = 2, Passed = 3 }
public enum ExamResultKind { Numeric = 1, Absent = 2, NotEntered = 3, Withheld = 4 }
public enum StaffCourseRole { Professor = 1, Teacher = 2 }
public enum ActivityType { TheoreticalLecture = 1, PracticalLecture = 2, Meeting = 3, Seminar = 4, Other = 5 }
public enum ScheduleStatus { Draft = 1, Published = 2, Cancelled = 3 }
public enum ScheduleSource { Manual = 1, Generated = 2 }
public enum AppealStatus { Submitted = 1, ProfessorReviewed = 2, Accepted = 3, Rejected = 4 }
public enum AnnouncementAudience { Everyone = 1, StudyYear = 2, Division = 3, Student = 4, Staff = 5 }
public enum NotificationType { Announcement = 1, ScheduleCreated = 2, ScheduleChanged = 3, ScheduleCancelled = 4, TimetablePublished = 5, MarkPublished = 6, MarkCorrected = 7, AppealChanged = 8 }

public static class AppRoles
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string Professor = "Professor";
    public const string ExamsOfficer = "ExamsOfficer";
    public const string Admin = "Admin";
    public static readonly string[] All = [Student, Teacher, Professor, ExamsOfficer, Admin];
}

public static class SettingKeys
{
    public const string MaximumFailedCoursesForPromotion = "Academic.MaximumFailedCoursesForPromotion";
    public const string ProgramYears = "Academic.ProgramYears";
    public const string DefaultDivisionCapacity = "Academic.DefaultDivisionCapacity";
    public const string AppealDeadlineDays = "Marks.AppealDeadlineDays";
    public const string TimeZone = "System.TimeZone";
}
