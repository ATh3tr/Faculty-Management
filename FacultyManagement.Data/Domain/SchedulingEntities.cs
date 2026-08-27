namespace FacultyManagement.Data.Domain;

public sealed class TimeSlot
{
    public int Id { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
}

public sealed class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsLab { get; set; }
    public bool HasProjector { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class RoomUnavailability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Room Room { get; set; } = null!;
}

public sealed class ScheduleSeries
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ActivityType ActivityType { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Draft;
    public ScheduleSource Source { get; set; } = ScheduleSource.Manual;
    public string TitleArabic { get; set; } = string.Empty;
    public string TitleEnglish { get; set; } = string.Empty;
    public Guid? CourseOfferingId { get; set; }
    public Guid? TimetablePlanId { get; set; }
    public Guid? DivisionId { get; set; }
    public int? AudienceStudyYear { get; set; }
    public Guid RoomId { get; set; }
    public Guid StaffUserId { get; set; }
    public int TimeSlotId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public CourseOffering? CourseOffering { get; set; }
    public Division? Division { get; set; }
    public Room Room { get; set; } = null!;
    public ApplicationUser StaffUser { get; set; } = null!;
    public TimeSlot TimeSlot { get; set; } = null!;
    public ICollection<ScheduleOccurrence> Occurrences { get; set; } = [];
}

public sealed class TimetablePlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AcademicYearId { get; set; }
    public Guid SemesterId { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Draft;
    public Guid GeneratedByUserId { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ScheduleSeries> Series { get; set; } = [];
}

public sealed class ScheduleOccurrence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScheduleSeriesId { get; set; }
    public Guid RoomId { get; set; }
    public Guid StaffUserId { get; set; }
    public int TimeSlotId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public ScheduleSeries ScheduleSeries { get; set; } = null!;
}
