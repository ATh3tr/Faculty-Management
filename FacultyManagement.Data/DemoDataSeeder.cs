using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Data;

public static class DemoDataSeeder
{
    private const string SeedVersionKey = "DemoData.Version";
    private const string SeedVersion = "1";

    public static async Task SeedAsync(
        FacultyDbContext db,
        UserManager<ApplicationUser> userManager,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (await db.SystemSettings.AnyAsync(x => x.Key == SeedVersionKey && x.Value == SeedVersion, cancellationToken))
            return;

        var admin = await userManager.FindByEmailAsync("admin@faculty.demo")
            ?? await userManager.FindByEmailAsync("admin@faculty.local")
            ?? throw new InvalidOperationException("Seed the administrator before demo data.");

        var professor = await EnsureStaffAsync(userManager, db, "professor@faculty.demo", "الأستاذة ليلى حمود",
            "Professor Layla Hammoud", "PROF-001", AppRoles.Professor, password, cancellationToken);
        var teacher = await EnsureStaffAsync(userManager, db, "teacher@faculty.demo", "المهندس سامر خليل",
            "Teacher Samer Khalil", "TCH-001", AppRoles.Teacher, password, cancellationToken);
        var examsOfficer = await EnsureStaffAsync(userManager, db, "exams@faculty.demo", "رنا المصري",
            "Rana Al-Masri", "EXAM-001", AppRoles.ExamsOfficer, password, cancellationToken);
        var studentOne = await EnsureStudentAsync(userManager, db, "student1@faculty.demo", "أحمد الحلبي",
            "Ahmad Al-Halabi", "20260001", 1, password, cancellationToken);
        var studentTwo = await EnsureStudentAsync(userManager, db, "student2@faculty.demo", "نور الشامي",
            "Nour Al-Shami", "20260002", 1, password, cancellationToken);
        var studentThree = await EnsureStudentAsync(userManager, db, "student3@faculty.demo", "كريم الدروبي",
            "Karim Al-Droubi", "20250003", 2, password, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var academicStartYear = today.Month >= 8 ? today.Year : today.Year - 1;
        var academicYear = new AcademicYear
        {
            Name = $"{academicStartYear}/{academicStartYear + 1}",
            StartsOn = new DateOnly(academicStartYear, 8, 1),
            EndsOn = new DateOnly(academicStartYear + 1, 7, 31),
            IsCurrent = true
        };
        var firstSemester = new Semester
        {
            AcademicYearId = academicYear.Id,
            Number = 1,
            StartsOn = academicYear.StartsOn,
            EndsOn = new DateOnly(academicStartYear + 1, 1, 31),
            IsPublished = true
        };
        var secondSemester = new Semester
        {
            AcademicYearId = academicYear.Id,
            Number = 2,
            StartsOn = new DateOnly(academicStartYear + 1, 2, 1),
            EndsOn = academicYear.EndsOn,
            IsPublished = true
        };
        academicYear.Semesters.Add(firstSemester);
        academicYear.Semesters.Add(secondSemester);

        var courses = new[]
        {
            Course("PRG101", "البرمجة 1", "Programming I", 1, 1, 2, 1, false, true),
            Course("MTH101", "الرياضيات المتقطعة", "Discrete Mathematics", 1, 1, 2, 0, true, false),
            Course("CS102", "مقدمة في المعلوماتية", "Introduction to Informatics", 1, 1, 1, 1, true, true),
            Course("DS201", "بنى المعطيات", "Data Structures", 2, 1, 2, 1, true, true),
            Course("DB201", "قواعد البيانات 1", "Databases I", 2, 1, 2, 1, true, true),
            Course("SE301", "هندسة البرمجيات", "Software Engineering", 3, 1, 2, 1, true, false)
        };

        var offerings = courses.Select(course => new CourseOffering
        {
            CourseId = course.Id,
            AcademicYearId = academicYear.Id,
            SemesterId = course.SemesterNumber == 1 ? firstSemester.Id : secondSemester.Id
        }).ToArray();

        var rooms = new[]
        {
            Room("H-A", 80, false, true),
            Room("H-B", 60, false, true),
            Room("AUD", 100, false, true),
            Room("LAB-1", 30, true, true),
            Room("LAB-2", 25, true, false)
        };
        var divisionOne = new Division { AcademicYearId = academicYear.Id, StudyYear = 1, Number = 1, Capacity = 30 };
        var divisionTwo = new Division { AcademicYearId = academicYear.Id, StudyYear = 2, Number = 1, Capacity = 30 };

        db.AcademicYears.Add(academicYear);
        db.Courses.AddRange(courses);
        db.CourseOfferings.AddRange(offerings);
        db.Rooms.AddRange(rooms);
        db.Divisions.AddRange(divisionOne, divisionTwo);
        db.NonTeachingDays.Add(new NonTeachingDay
        {
            AcademicYearId = academicYear.Id,
            Date = today.AddDays(21),
            Reason = "University holiday / عطلة جامعية"
        });
        db.RoomUnavailabilities.Add(new RoomUnavailability
        {
            RoomId = rooms[4].Id,
            StartsOn = today.AddDays(10),
            EndsOn = today.AddDays(12),
            Reason = "Laboratory maintenance / صيانة المخبر"
        });

        db.DivisionMemberships.AddRange(
            Membership(divisionOne, academicYear, studentOne),
            Membership(divisionOne, academicYear, studentTwo),
            Membership(divisionTwo, academicYear, studentThree));

        foreach (var offering in offerings)
        {
            db.StaffCourseAssignments.Add(new StaffCourseAssignment
            {
                CourseOfferingId = offering.Id,
                StaffUserId = professor.Id,
                Role = StaffCourseRole.Professor
            });
            db.StaffCourseAssignments.Add(new StaffCourseAssignment
            {
                CourseOfferingId = offering.Id,
                StaffUserId = teacher.Id,
                Role = StaffCourseRole.Teacher
            });
        }

        var records = new[]
        {
            Record(studentOne, courses[0], academicYear, CourseResultStatus.Passed, today.AddDays(-20)),
            Record(studentOne, courses[1], academicYear, CourseResultStatus.Failed),
            Record(studentOne, courses[2], academicYear, CourseResultStatus.InProgress),
            Record(studentTwo, courses[0], academicYear, CourseResultStatus.Passed, today.AddDays(-20)),
            Record(studentTwo, courses[1], academicYear, CourseResultStatus.Passed, today.AddDays(-20)),
            Record(studentTwo, courses[2], academicYear, CourseResultStatus.InProgress),
            Record(studentThree, courses[3], academicYear, CourseResultStatus.Passed, today.AddDays(-20)),
            Record(studentThree, courses[4], academicYear, CourseResultStatus.Failed)
        };
        db.StudentCourseRecords.AddRange(records);

        var examPeriod = new ExamPeriod
        {
            AcademicYearId = academicYear.Id,
            NameArabic = "الدورة الامتحانية الأولى",
            NameEnglish = "First examination period",
            StartsOn = today.AddDays(-30),
            EndsOn = today.AddDays(-15),
            IsRetake = false,
            IsClosed = false
        };
        db.ExamPeriods.Add(examPeriod);
        var marks = new[]
        {
            Mark(records[0], examPeriod, examsOfficer, 78),
            Mark(records[1], examPeriod, examsOfficer, 52),
            Mark(records[3], examPeriod, examsOfficer, 64),
            Mark(records[4], examPeriod, examsOfficer, 88),
            Mark(records[6], examPeriod, examsOfficer, 71),
            Mark(records[7], examPeriod, examsOfficer, 55)
        };
        db.MarkAttempts.AddRange(marks);
        db.MarkAppeals.Add(new MarkAppeal
        {
            MarkAttemptId = marks[1].Id,
            StudentUserId = studentOne.Id,
            Reason = "Please review question four; I believe two marks were omitted.",
            Status = AppealStatus.Submitted,
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-1)
        });

        var slots = await db.TimeSlots.AsNoTracking().OrderBy(x => x.StartsAt).ToArrayAsync(cancellationToken);
        if (slots.Length < 3) throw new InvalidOperationException("The standard time slots must be seeded first.");
        var scheduleSeries = new[]
        {
            Series(ActivityType.TheoreticalLecture, "البرمجة 1 - نظري", "Programming I - Theory",
                offerings[0], null, 1, rooms[0], professor, slots[0], DayOfWeek.Sunday, admin, today),
            Series(ActivityType.PracticalLecture, "البرمجة 1 - عملي", "Programming I - Practical",
                offerings[0], divisionOne, null, rooms[3], teacher, slots[1], DayOfWeek.Sunday, admin, today),
            Series(ActivityType.TheoreticalLecture, "الرياضيات المتقطعة", "Discrete Mathematics",
                offerings[1], null, 1, rooms[1], professor, slots[1], DayOfWeek.Monday, admin, today),
            Series(ActivityType.TheoreticalLecture, "بنى المعطيات", "Data Structures",
                offerings[3], null, 2, rooms[2], professor, slots[2], DayOfWeek.Tuesday, admin, today),
            Series(ActivityType.PracticalLecture, "قواعد البيانات 1 - عملي", "Databases I - Practical",
                offerings[4], divisionTwo, null, rooms[4], teacher, slots[0], DayOfWeek.Wednesday, admin, today)
        };
        db.ScheduleSeries.AddRange(scheduleSeries);
        foreach (var series in scheduleSeries)
        {
            series.Occurrences.Add(new ScheduleOccurrence
            {
                RoomId = series.RoomId,
                StaffUserId = series.StaffUserId,
                TimeSlotId = series.TimeSlotId,
                Date = NextDate(today, series.DayOfWeek)
            });
        }

        db.Announcements.AddRange(
            new Announcement
            {
                TitleArabic = "مرحباً بكم في نظام إدارة الكلية",
                TitleEnglish = "Welcome to the Faculty Management System",
                BodyArabic = "يمكن للطلاب والكادر الاطلاع على الجداول والإعلانات من لوحة التحكم.",
                BodyEnglish = "Students and staff can view schedules and announcements from their dashboards.",
                Audience = AnnouncementAudience.Everyone,
                CreatedByUserId = admin.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
            },
            new Announcement
            {
                TitleArabic = "تثبيت الشعب للسنة الأولى",
                TitleEnglish = "First-year division registration",
                BodyArabic = "تم توزيع طلاب السنة الأولى على الشعب العملية.",
                BodyEnglish = "First-year students have been assigned to practical divisions.",
                Audience = AnnouncementAudience.StudyYear,
                StudyYear = 1,
                CreatedByUserId = admin.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            });

        var everyoneNotification = Notification(NotificationType.Announcement,
            "إعلان تجريبي", "Demo announcement",
            "البيانات التجريبية جاهزة لاستعراض النظام.", "The demo data is ready for exploring the system.");
        everyoneNotification.Recipients = new[] { admin, professor, teacher, examsOfficer, studentOne, studentTwo, studentThree }
            .Select(user => new NotificationRecipient { UserId = user.Id }).ToList();
        var markNotification = Notification(NotificationType.MarkPublished,
            "تم نشر علامة", "A mark was published",
            "تم نشر علامة البرمجة 1.", "Your Programming I mark has been published.");
        markNotification.Recipients.Add(new NotificationRecipient { UserId = studentOne.Id });
        var scheduleNotification = Notification(NotificationType.ScheduleCreated,
            "محاضرات جديدة", "New lectures scheduled",
            "تمت إضافة محاضرات الأسبوع القادم.", "Lectures for next week have been added.");
        scheduleNotification.Recipients.Add(new NotificationRecipient { UserId = studentTwo.Id });
        scheduleNotification.Recipients.Add(new NotificationRecipient { UserId = teacher.Id });
        db.Notifications.AddRange(everyoneNotification, markNotification, scheduleNotification);

        db.AuditEntries.Add(new AuditEntry
        {
            UserId = admin.Id,
            Action = "SeedDemoData",
            EntityType = "DemoEnvironment",
            EntityId = SeedVersion,
            NewValuesJson = "{\"status\":\"ready\"}"
        });
        db.SystemSettings.Add(new SystemSetting { Key = SeedVersionKey, Value = SeedVersion, UpdatedByUserId = admin.Id });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<ApplicationUser> EnsureStaffAsync(
        UserManager<ApplicationUser> userManager, FacultyDbContext db, string email,
        string arabicName, string englishName, string staffNumber, string role, string password,
        CancellationToken cancellationToken)
    {
        var user = await EnsureUserAsync(userManager, email, arabicName, englishName, AccountKind.Staff, role, password);
        if (!await db.StaffProfiles.AnyAsync(x => x.UserId == user.Id, cancellationToken))
        {
            db.StaffProfiles.Add(new StaffProfile { UserId = user.Id, StaffNumber = staffNumber });
            await db.SaveChangesAsync(cancellationToken);
        }
        return user;
    }

    private static async Task<ApplicationUser> EnsureStudentAsync(
        UserManager<ApplicationUser> userManager, FacultyDbContext db, string email,
        string arabicName, string englishName, string universityNumber, int studyYear, string password,
        CancellationToken cancellationToken)
    {
        var user = await EnsureUserAsync(userManager, email, arabicName, englishName, AccountKind.Student, AppRoles.Student, password);
        if (!await db.StudentProfiles.AnyAsync(x => x.UserId == user.Id, cancellationToken))
        {
            db.StudentProfiles.Add(new StudentProfile
            {
                UserId = user.Id,
                UniversityNumber = universityNumber,
                CurrentStudyYear = studyYear,
                Standing = AcademicStanding.Active
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        return user;
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, string email, string arabicName,
        string englishName, AccountKind accountKind, string role, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullNameArabic = arabicName,
                FullNameEnglish = englishName,
                AccountKind = accountKind,
                IsApproved = true,
                PreferredLanguage = "en"
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        if (!await userManager.IsInRoleAsync(user, role))
        {
            var result = await userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        return user;
    }

    private static Course Course(string code, string arabicName, string englishName, int year, int semester,
        int theoretical, int practical, bool projector, bool lab) => new()
    {
        Code = code,
        NameArabic = arabicName,
        NameEnglish = englishName,
        StudyYear = year,
        SemesterNumber = semester,
        TheoreticalSessionsPerWeek = theoretical,
        PracticalSessionsPerDivisionPerWeek = practical,
        RequiresProjector = projector,
        RequiresLab = lab
    };

    private static Room Room(string code, int capacity, bool isLab, bool projector) => new()
    {
        Code = code,
        Capacity = capacity,
        IsLab = isLab,
        HasProjector = projector
    };

    private static DivisionMembership Membership(Division division, AcademicYear year, ApplicationUser student) => new()
    {
        DivisionId = division.Id,
        AcademicYearId = year.Id,
        StudentUserId = student.Id
    };

    private static StudentCourseRecord Record(ApplicationUser student, Course course, AcademicYear year,
        CourseResultStatus status, DateOnly? passedOn = null) => new()
    {
        StudentUserId = student.Id,
        CourseId = course.Id,
        AssignedAcademicYearId = year.Id,
        Status = status,
        AssignedAtUtc = DateTime.UtcNow.AddMonths(-2),
        PassedAtUtc = passedOn is null ? null : passedOn.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
    };

    private static MarkAttempt Mark(StudentCourseRecord record, ExamPeriod period, ApplicationUser officer, decimal value) => new()
    {
        StudentCourseRecordId = record.Id,
        ExamPeriodId = period.Id,
        ResultKind = ExamResultKind.Numeric,
        Mark = value,
        IsPublished = true,
        PublishedAtUtc = DateTime.UtcNow.AddDays(-3),
        EnteredAtUtc = DateTime.UtcNow.AddDays(-4),
        EnteredByUserId = officer.Id
    };

    private static ScheduleSeries Series(ActivityType activityType, string arabicTitle, string englishTitle,
        CourseOffering offering, Division? division, int? audienceYear, Room room, ApplicationUser staff,
        TimeSlot slot, DayOfWeek day, ApplicationUser creator, DateOnly today) => new()
    {
        ActivityType = activityType,
        Status = ScheduleStatus.Published,
        Source = ScheduleSource.Manual,
        TitleArabic = arabicTitle,
        TitleEnglish = englishTitle,
        CourseOfferingId = offering.Id,
        DivisionId = division?.Id,
        AudienceStudyYear = audienceYear,
        RoomId = room.Id,
        StaffUserId = staff.Id,
        TimeSlotId = slot.Id,
        DayOfWeek = day,
        StartsOn = today,
        EndsOn = today.AddMonths(4),
        IsRecurring = true,
        CreatedByUserId = creator.Id
    };

    private static Notification Notification(NotificationType type, string arabicTitle, string englishTitle,
        string arabicBody, string englishBody) => new()
    {
        Type = type,
        TitleArabic = arabicTitle,
        TitleEnglish = englishTitle,
        BodyArabic = arabicBody,
        BodyEnglish = englishBody,
        CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
    };

    private static DateOnly NextDate(DateOnly start, DayOfWeek day)
    {
        var offset = ((int)day - (int)start.DayOfWeek + 7) % 7;
        return start.AddDays(offset);
    }
}
