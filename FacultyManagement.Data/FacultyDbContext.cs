using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Data;

public sealed class FacultyDbContext(DbContextOptions<FacultyDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<NonTeachingDay> NonTeachingDays => Set<NonTeachingDay>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();
    public DbSet<StaffCourseAssignment> StaffCourseAssignments => Set<StaffCourseAssignment>();
    public DbSet<StudentCourseRecord> StudentCourseRecords => Set<StudentCourseRecord>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<DivisionMembership> DivisionMemberships => Set<DivisionMembership>();
    public DbSet<ExamPeriod> ExamPeriods => Set<ExamPeriod>();
    public DbSet<MarkAttempt> MarkAttempts => Set<MarkAttempt>();
    public DbSet<MarkCorrection> MarkCorrections => Set<MarkCorrection>();
    public DbSet<MarkAppeal> MarkAppeals => Set<MarkAppeal>();
    public DbSet<PromotionRun> PromotionRuns => Set<PromotionRun>();
    public DbSet<PromotionResult> PromotionResults => Set<PromotionResult>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomUnavailability> RoomUnavailabilities => Set<RoomUnavailability>();
    public DbSet<ScheduleSeries> ScheduleSeries => Set<ScheduleSeries>();
    public DbSet<TimetablePlan> TimetablePlans => Set<TimetablePlan>();
    public DbSet<ScheduleOccurrence> ScheduleOccurrences => Set<ScheduleOccurrence>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullNameArabic).HasMaxLength(200);
            entity.Property(x => x.FullNameEnglish).HasMaxLength(200);
            entity.Property(x => x.PreferredLanguage).HasMaxLength(2);
        });

        builder.Entity<StudentProfile>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.HasIndex(x => x.UniversityNumber).IsUnique();
            entity.Property(x => x.UniversityNumber).HasMaxLength(50);
            entity.HasOne(x => x.User).WithOne(x => x.StudentProfile)
                .HasForeignKey<StudentProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StaffProfile>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.HasIndex(x => x.StaffNumber).IsUnique().HasFilter("[StaffNumber] IS NOT NULL");
            entity.HasOne(x => x.User).WithOne(x => x.StaffProfile)
                .HasForeignKey<StaffProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        builder.Entity<AcademicYear>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<Semester>().HasIndex(x => new { x.AcademicYearId, x.Number }).IsUnique();
        builder.Entity<NonTeachingDay>().HasIndex(x => new { x.AcademicYearId, x.Date }).IsUnique();
        builder.Entity<Course>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<CourseOffering>().HasIndex(x => new { x.CourseId, x.AcademicYearId }).IsUnique();
        builder.Entity<CourseOffering>().HasOne(x => x.AcademicYear).WithMany()
            .HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CourseOffering>().HasOne(x => x.Semester).WithMany()
            .HasForeignKey(x => x.SemesterId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StaffCourseAssignment>().HasIndex(x => new { x.CourseOfferingId, x.StaffUserId, x.Role }).IsUnique();
        builder.Entity<StudentCourseRecord>().HasIndex(x => new { x.StudentUserId, x.CourseId }).IsUnique();
        builder.Entity<Division>().HasIndex(x => new { x.AcademicYearId, x.StudyYear, x.Number }).IsUnique();
        builder.Entity<DivisionMembership>().HasIndex(x => new { x.StudentUserId, x.AcademicYearId }).IsUnique();
        builder.Entity<MarkAppeal>().HasIndex(x => new { x.MarkAttemptId, x.StudentUserId }).IsUnique();
        builder.Entity<MarkAttempt>().HasIndex(x => new { x.StudentCourseRecordId, x.ExamPeriodId }).IsUnique();
        builder.Entity<MarkAttempt>().Property(x => x.Mark).HasPrecision(5, 2);
        builder.Entity<MarkCorrection>().Property(x => x.OldMark).HasPrecision(5, 2);
        builder.Entity<MarkCorrection>().Property(x => x.NewMark).HasPrecision(5, 2);

        builder.Entity<Room>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        builder.Entity<ScheduleOccurrence>(entity =>
        {
            entity.HasIndex(x => new { x.RoomId, x.Date, x.TimeSlotId })
                .IsUnique().HasFilter("[IsCancelled] = 0");
            entity.HasIndex(x => new { x.StaffUserId, x.Date, x.TimeSlotId })
                .IsUnique().HasFilter("[IsCancelled] = 0");
            entity.HasOne(x => x.ScheduleSeries).WithMany(x => x.Occurrences)
                .HasForeignKey(x => x.ScheduleSeriesId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<TimetablePlan>().HasMany(x => x.Series).WithOne()
            .HasForeignKey(x => x.TimetablePlanId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<NotificationRecipient>(entity =>
        {
            entity.HasKey(x => new { x.NotificationId, x.UserId });
            entity.HasOne(x => x.Notification).WithMany(x => x.Recipients)
                .HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SystemSetting>().HasKey(x => x.Key);
        builder.Entity<SystemSetting>().Property(x => x.Key).HasMaxLength(150);
        builder.Entity<SystemSetting>().Property(x => x.Value).HasMaxLength(1000);
        builder.Entity<AuditEntry>().Property(x => x.Action).HasMaxLength(100);
        builder.Entity<AuditEntry>().Property(x => x.EntityType).HasMaxLength(150);
        builder.Entity<AuditEntry>().Property(x => x.EntityId).HasMaxLength(100);

        RestrictAmbiguousUserRelationships(builder);
    }

    private static void RestrictAmbiguousUserRelationships(ModelBuilder builder)
    {
        builder.Entity<StaffCourseAssignment>().HasOne(x => x.StaffUser).WithMany()
            .HasForeignKey(x => x.StaffUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StudentCourseRecord>().HasOne(x => x.StudentUser).WithMany()
            .HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<DivisionMembership>().HasOne(x => x.StudentUser).WithMany()
            .HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ScheduleSeries>().HasOne(x => x.StaffUser).WithMany()
            .HasForeignKey(x => x.StaffUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
