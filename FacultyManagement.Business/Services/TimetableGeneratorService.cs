using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Google.OrTools.Sat;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class TimetableGeneratorService(FacultyDbContext db, INotificationService notifications, IRealtimeNotifier realtime)
{
    private static readonly DayOfWeek[] Days =
        [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday];

    public async Task<GeneratedTimetableResult> GenerateAsync(GenerateTimetableRequest request, Guid adminId, CancellationToken ct = default)
    {
        var semester = await db.Semesters.SingleOrDefaultAsync(x => x.Id == request.SemesterId && x.AcademicYearId == request.AcademicYearId, ct)
            ?? throw new BusinessException("Semester not found in the selected academic year.", 404);
        var offerings = await db.CourseOfferings.Where(x => x.AcademicYearId == request.AcademicYearId && x.SemesterId == request.SemesterId)
            .Include(x => x.Course).Include(x => x.StaffAssignments).ToListAsync(ct);
        var divisions = await db.Divisions.Where(x => x.AcademicYearId == request.AcademicYearId)
            .Include(x => x.Memberships).ToListAsync(ct);
        var rooms = await db.Rooms.Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync(ct);
        if (rooms.Count == 0) return new(null, false, "No active rooms are configured.", []);

        var requirements = BuildRequirements(offerings, divisions, rooms);
        var invalid = requirements.FirstOrDefault(x => x.AllowedStaff.Count == 0 || x.AllowedRooms.Count == 0);
        if (invalid is not null)
            return new(null, false, $"No eligible staff or room for {invalid.Offering.Course.Code} ({invalid.Type}).", []);

        var model = new CpModel();
        var staffIndex = requirements.SelectMany(x => x.AllowedStaff).Distinct()
            .Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        var staffByIndex = staffIndex.ToDictionary(x => x.Value, x => x.Key);
        var variables = requirements.Select((requirement, index) => CreateVariables(model, requirement, index, staffIndex)).ToArray();
        AddPairConstraints(model, requirements, variables);
        await AddExistingScheduleConstraintsAsync(model, semester, requirements, variables, rooms, ct);
        AddBalancingObjective(model, requirements, variables, rooms);

        var solver = new CpSolver
        {
            StringParameters = $"max_time_in_seconds:{Math.Clamp(request.MaximumSeconds, 1, 180)} num_search_workers:8"
        };
        var status = solver.Solve(model);
        if (status is not CpSolverStatus.Feasible and not CpSolverStatus.Optimal)
            return new(null, false, status.ToString(), []);

        var plan = new TimetablePlan
        {
            AcademicYearId = request.AcademicYearId, SemesterId = request.SemesterId,
            GeneratedByUserId = adminId, Status = ScheduleStatus.Draft
        };
        var result = new List<GeneratedSession>();
        for (var i = 0; i < requirements.Count; i++)
        {
            var requirement = requirements[i];
            var vars = variables[i];
            var room = rooms[(int)solver.Value(vars.Room)];
            var staffId = staffByIndex[(int)solver.Value(vars.Staff)];
            var day = Days[(int)solver.Value(vars.Day)];
            var slotId = (int)solver.Value(vars.Slot) + 1;
            var series = new ScheduleSeries
            {
                TimetablePlanId = plan.Id, CourseOfferingId = requirement.Offering.Id,
                ActivityType = requirement.Type, Status = ScheduleStatus.Draft, Source = ScheduleSource.Generated,
                TitleArabic = requirement.Offering.Course.NameArabic, TitleEnglish = requirement.Offering.Course.NameEnglish,
                DivisionId = requirement.Division?.Id, AudienceStudyYear = requirement.Offering.Course.StudyYear,
                RoomId = room.Id, StaffUserId = staffId, TimeSlotId = slotId, DayOfWeek = day,
                StartsOn = semester.StartsOn, EndsOn = semester.EndsOn, IsRecurring = true, CreatedByUserId = adminId
            };
            plan.Series.Add(series);
            result.Add(new GeneratedSession(series.Id, requirement.Offering.Id, requirement.Type, requirement.Division?.Id,
                requirement.Offering.Course.StudyYear, staffId, room.Id, day, slotId));
        }
        db.TimetablePlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return new(plan.Id, true, status.ToString(), result);
    }

    public async Task PublishPlanAsync(Guid planId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await db.Database.ExecuteSqlRawAsync(
            "DECLARE @result int; EXEC @result = sp_getapplock @Resource = N'FacultyManagement.Schedule', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Could not acquire schedule lock.', 1;", ct);
        var plan = await db.TimetablePlans.Include(x => x.Series).ThenInclude(x => x.CourseOffering).ThenInclude(x => x!.Course)
            .Include(x => x.Series).ThenInclude(x => x.Division)
            .SingleOrDefaultAsync(x => x.Id == planId, ct) ?? throw new BusinessException("Timetable plan not found.", 404);
        if (plan.Status == ScheduleStatus.Published) return;
        if (plan.Status == ScheduleStatus.Cancelled) throw new BusinessException("Cancelled timetable plan cannot be published.", 409);
        var holidays = await db.NonTeachingDays.Where(x => x.AcademicYearId == plan.AcademicYearId).Select(x => x.Date).ToListAsync(ct);
        var pending = new List<(ScheduleSeries Series, DateOnly Date)>();
        foreach (var series in plan.Series)
        {
            for (var date = series.StartsOn; date <= series.EndsOn; date = date.AddDays(1))
                if (date.DayOfWeek == series.DayOfWeek && !holidays.Contains(date)) pending.Add((series, date));
        }

        var dates = pending.Select(x => x.Date).Distinct().ToArray();
        var existing = await db.ScheduleOccurrences.Where(x => dates.Contains(x.Date) && !x.IsCancelled)
            .Include(x => x.ScheduleSeries).ToListAsync(ct);
        foreach (var item in pending)
        {
            var collisions = existing.Where(x => x.Date == item.Date && x.TimeSlotId == item.Series.TimeSlotId).ToArray();
            if (collisions.Any(x => x.RoomId == item.Series.RoomId || x.StaffUserId == item.Series.StaffUserId))
                throw new BusinessException("The draft is stale because a room or staff slot was booked after generation.", 409, "stale_timetable");
            if (collisions.Any(x => PublishedAudienceConflicts(item.Series, x.ScheduleSeries)))
                throw new BusinessException("The draft is stale because an audience conflict was introduced after generation.", 409, "stale_timetable");
        }

        foreach (var item in pending)
            item.Series.Occurrences.Add(new ScheduleOccurrence
            {
                ScheduleSeriesId = item.Series.Id, RoomId = item.Series.RoomId, StaffUserId = item.Series.StaffUserId,
                TimeSlotId = item.Series.TimeSlotId, Date = item.Date
            });
        foreach (var series in plan.Series) series.Status = ScheduleStatus.Published;
        plan.Status = ScheduleStatus.Published;
        var semester = await db.Semesters.FindAsync([plan.SemesterId], ct);
        if (semester is not null) semester.IsPublished = true;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var courseIds = plan.Series.Where(x => x.CourseOffering is not null).Select(x => x.CourseOffering!.CourseId).Distinct().ToArray();
        var recipients = await db.StudentCourseRecords.Where(x => courseIds.Contains(x.CourseId) && x.Status != CourseResultStatus.Passed)
            .Select(x => x.StudentUserId).Distinct().ToArrayAsync(ct);
        await notifications.CreateAsync(NotificationType.TimetablePublished, "تم نشر الجدول", "Timetable published",
            "تم نشر جدول الفصل الدراسي.", "The semester timetable has been published.", "/schedule", recipients, ct);
        await realtime.AvailabilityChangedAsync(ct);
    }

    private static List<SessionRequirement> BuildRequirements(
        IReadOnlyCollection<CourseOffering> offerings, IReadOnlyCollection<Division> divisions, IReadOnlyList<Room> rooms)
    {
        var result = new List<SessionRequirement>();
        foreach (var offering in offerings)
        {
            var professors = offering.StaffAssignments.Where(x => x.Role == StaffCourseRole.Professor).Select(x => x.StaffUserId).Distinct().ToList();
            var teachers = offering.StaffAssignments.Where(x => x.Role == StaffCourseRole.Teacher).Select(x => x.StaffUserId).Distinct().ToList();
            var theoryRooms = rooms.Select((room, index) => (room, index)).Where(x =>
                    (!offering.Course.RequiresProjector || x.room.HasProjector) && (!offering.Course.RequiresLab || x.room.IsLab))
                .Select(x => x.index).ToList();
            for (var i = 0; i < offering.Course.TheoreticalSessionsPerWeek; i++)
                result.Add(new SessionRequirement(offering, ActivityType.TheoreticalLecture, null, professors, theoryRooms));

            foreach (var division in divisions.Where(x => x.StudyYear == offering.Course.StudyYear))
            {
                var practicalRooms = rooms.Select((room, index) => (room, index)).Where(x => x.room.IsLab
                        && x.room.Capacity >= division.Memberships.Count
                        && (!offering.Course.RequiresProjector || x.room.HasProjector))
                    .Select(x => x.index).ToList();
                for (var i = 0; i < offering.Course.PracticalSessionsPerDivisionPerWeek; i++)
                    result.Add(new SessionRequirement(offering, ActivityType.PracticalLecture, division, teachers, practicalRooms));
            }
        }
        return result;
    }

    private static SessionVariables CreateVariables(CpModel model, SessionRequirement requirement, int index, IReadOnlyDictionary<Guid, int> staffIndex)
    {
        var day = model.NewIntVar(0, 4, $"day_{index}");
        var slot = model.NewIntVar(0, 4, $"slot_{index}");
        var room = model.NewIntVarFromDomain(Domain.FromValues(requirement.AllowedRooms.Select(x => (long)x)), $"room_{index}");
        var allowedStaff = requirement.AllowedStaff.Distinct().Select(x => (long)staffIndex[x]).ToArray();
        var staff = model.NewIntVarFromDomain(Domain.FromValues(allowedStaff), $"staff_{index}");
        return new SessionVariables(day, slot, room, staff);
    }

    private static void AddPairConstraints(CpModel model, IReadOnlyList<SessionRequirement> requirements, IReadOnlyList<SessionVariables> vars)
    {
        for (var i = 0; i < requirements.Count; i++)
        for (var j = i + 1; j < requirements.Count; j++)
        {
            var sameDay = Equal(model, vars[i].Day, vars[j].Day, $"same_day_{i}_{j}");
            var sameSlot = Equal(model, vars[i].Slot, vars[j].Slot, $"same_slot_{i}_{j}");
            var sameRoom = Equal(model, vars[i].Room, vars[j].Room, $"same_room_{i}_{j}");
            var sameStaff = Equal(model, vars[i].Staff, vars[j].Staff, $"same_staff_{i}_{j}");
            model.AddBoolOr([sameDay.Not(), sameSlot.Not(), sameRoom.Not()]);
            model.AddBoolOr([sameDay.Not(), sameSlot.Not(), sameStaff.Not()]);

            if (AudienceConflicts(requirements[i], requirements[j]))
                model.AddBoolOr([sameDay.Not(), sameSlot.Not()]);
        }
    }

    private async Task AddExistingScheduleConstraintsAsync(
        CpModel model, Semester semester, IReadOnlyList<SessionRequirement> requirements,
        IReadOnlyList<SessionVariables> vars, IReadOnlyList<Room> rooms, CancellationToken ct)
    {
        var existing = await db.ScheduleSeries.Where(x => x.Status == ScheduleStatus.Published
                && x.StartsOn <= semester.EndsOn && x.EndsOn >= semester.StartsOn)
            .ToListAsync(ct);
        var roomIndex = rooms.Select((room, index) => (room.Id, index)).ToDictionary(x => x.Id, x => x.index);
        var staffIndex = requirements.SelectMany(x => x.AllowedStaff).Distinct()
            .Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        for (var i = 0; i < requirements.Count; i++)
        foreach (var fixedSeries in existing)
        {
            var day = Array.IndexOf(Days, fixedSeries.DayOfWeek);
            if (day < 0) continue;
            var slot = fixedSeries.TimeSlotId - 1;
            if (roomIndex.TryGetValue(fixedSeries.RoomId, out var room))
                model.AddForbiddenAssignments([vars[i].Day, vars[i].Slot, vars[i].Room]).AddTuple([day, slot, room]);
            if (staffIndex.TryGetValue(fixedSeries.StaffUserId, out var staff))
                model.AddForbiddenAssignments([vars[i].Day, vars[i].Slot, vars[i].Staff]).AddTuple([day, slot, staff]);
            if (AudienceConflicts(requirements[i], fixedSeries))
                model.AddForbiddenAssignments([vars[i].Day, vars[i].Slot]).AddTuple([day, slot]);
        }
    }

    private static void AddBalancingObjective(CpModel model, IReadOnlyList<SessionRequirement> requirements,
        IReadOnlyList<SessionVariables> vars, IReadOnlyList<Room> rooms)
    {
        var penalties = new List<LinearExpr>();
        var groups = requirements.Select((item, index) => (item, index)).GroupBy(x =>
            x.item.Division is null ? $"Y{x.item.Offering.Course.StudyYear}" : $"D{x.item.Division.Id}");
        foreach (var group in groups)
        {
            var loads = new List<IntVar>();
            for (var day = 0; day < 5; day++)
            {
                var indicators = group.Select(x => Equal(model, vars[x.index].Day, day, $"group_{group.Key}_{day}_{x.index}")).ToArray();
                var load = model.NewIntVar(0, indicators.Length, $"load_{group.Key}_{day}");
                model.Add(load == LinearExpr.Sum(indicators.Select(x => (LinearExpr)x)));
                loads.Add(load);
            }
            var maxLoad = model.NewIntVar(0, group.Count(), $"max_load_{group.Key}");
            model.AddMaxEquality(maxLoad, loads);
            penalties.Add(maxLoad * 100);
        }
        for (var i = 0; i < requirements.Count; i++)
        {
            penalties.Add(vars[i].Slot);
            var assignedCapacity = model.NewIntVar(0, rooms.Max(x => x.Capacity), $"capacity_{i}");
            model.AddElement(vars[i].Room, rooms.Select(x => (long)x.Capacity).ToArray(), assignedCapacity);
            var minimum = requirements[i].Division?.Memberships.Count ?? 0;
            var waste = model.NewIntVar(0, rooms.Max(x => x.Capacity), $"waste_{i}");
            model.Add(waste == assignedCapacity - minimum);
            penalties.Add(waste);
        }
        model.Minimize(LinearExpr.Sum(penalties));
    }

    private static BoolVar Equal(CpModel model, IntVar left, IntVar right, string name)
    {
        var equal = model.NewBoolVar(name);
        model.Add(left == right).OnlyEnforceIf(equal);
        model.Add(left != right).OnlyEnforceIf(equal.Not());
        return equal;
    }

    private static BoolVar Equal(CpModel model, IntVar left, long right, string name)
    {
        var equal = model.NewBoolVar(name);
        model.Add(left == right).OnlyEnforceIf(equal);
        model.Add(left != right).OnlyEnforceIf(equal.Not());
        return equal;
    }

    private static bool AudienceConflicts(SessionRequirement left, SessionRequirement right) =>
        left.Offering.Course.StudyYear == right.Offering.Course.StudyYear &&
        (left.Type == ActivityType.TheoreticalLecture || right.Type == ActivityType.TheoreticalLecture || left.Division?.Id == right.Division?.Id);

    private static bool AudienceConflicts(SessionRequirement left, ScheduleSeries right) =>
        left.Offering.Course.StudyYear == right.AudienceStudyYear &&
        (left.Type == ActivityType.TheoreticalLecture || right.ActivityType == ActivityType.TheoreticalLecture || left.Division?.Id == right.DivisionId);

    private static bool PublishedAudienceConflicts(ScheduleSeries left, ScheduleSeries right) =>
        left.AudienceStudyYear == right.AudienceStudyYear &&
        (left.ActivityType == ActivityType.TheoreticalLecture || right.ActivityType == ActivityType.TheoreticalLecture || left.DivisionId == right.DivisionId);

    private sealed class SessionRequirement(
        CourseOffering offering, ActivityType type, Division? division, List<Guid> allowedStaff, List<int> allowedRooms)
    {
        public CourseOffering Offering { get; } = offering;
        public ActivityType Type { get; } = type;
        public Division? Division { get; } = division;
        public List<Guid> AllowedStaff { get; } = allowedStaff;
        public List<int> AllowedRooms { get; } = allowedRooms;
    }

    private sealed record SessionVariables(IntVar Day, IntVar Slot, IntVar Room, IntVar Staff);
}
