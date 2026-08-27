using System.Data;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Data;
using FacultyManagement.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacultyManagement.Business.Services;

public sealed class DivisionService(FacultyDbContext db, ISettingsService settings)
{
    public async Task<DivisionAssignmentResult> AssignAsync(Guid studentUserId, CancellationToken ct = default)
    {
        var student = await db.StudentProfiles.SingleOrDefaultAsync(x => x.UserId == studentUserId, ct)
            ?? throw new BusinessException("Student not found.", 404);
        if (student.Standing != AcademicStanding.Active)
            throw new BusinessException("Only active, non-repeating students receive a division.");

        var academicYearId = await db.AcademicYears.Where(x => x.IsCurrent).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw new BusinessException("No current academic year is configured.", 409);
        var existing = await db.DivisionMemberships.Include(x => x.Division)
            .SingleOrDefaultAsync(x => x.StudentUserId == studentUserId && x.AcademicYearId == academicYearId, ct);
        if (existing is not null)
            return new(existing.DivisionId, existing.Division.Number, existing.Division.StudyYear,
                existing.Division.Capacity, existing.Division.Memberships.Count, false);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var divisions = await db.Divisions.Where(x => x.AcademicYearId == academicYearId && x.StudyYear == student.CurrentStudyYear)
            .Include(x => x.Memberships).OrderBy(x => x.Number).ToListAsync(ct);
        var selected = divisions.Where(x => x.Memberships.Count < x.Capacity)
            .OrderBy(x => x.Memberships.Count).ThenBy(x => x.Number).FirstOrDefault();
        var created = false;
        if (selected is null)
        {
            selected = new Division
            {
                AcademicYearId = academicYearId,
                StudyYear = student.CurrentStudyYear,
                Number = divisions.Count == 0 ? 1 : divisions.Max(x => x.Number) + 1,
                Capacity = await settings.GetIntAsync(SettingKeys.DefaultDivisionCapacity, ct)
            };
            db.Divisions.Add(selected);
            created = true;
        }

        var membership = new DivisionMembership
        {
            Division = selected,
            AcademicYearId = academicYearId,
            StudentUserId = studentUserId
        };
        db.DivisionMemberships.Add(membership);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var memberCount = await db.DivisionMemberships.CountAsync(x => x.DivisionId == selected.Id, ct);
        return new(selected.Id, selected.Number, selected.StudyYear, selected.Capacity, memberCount, created);
    }

    public async Task TransferAsync(Guid studentUserId, Guid targetDivisionId, Guid adminUserId, CancellationToken ct = default)
    {
        var target = await db.Divisions.Include(x => x.Memberships).SingleOrDefaultAsync(x => x.Id == targetDivisionId, ct)
            ?? throw new BusinessException("Target division not found.", 404);
        if (target.Memberships.Count >= target.Capacity)
            throw new BusinessException("Target division is full.", 409);
        var membership = await db.DivisionMemberships.SingleOrDefaultAsync(
            x => x.StudentUserId == studentUserId && x.AcademicYearId == target.AcademicYearId, ct)
            ?? throw new BusinessException("Student has no division in this academic year.", 404);
        var old = membership.DivisionId;
        membership.DivisionId = targetDivisionId;
        db.AuditEntries.Add(new AuditEntry
        {
            UserId = adminUserId, Action = "DivisionTransfer", EntityType = nameof(DivisionMembership), EntityId = membership.Id.ToString(),
            OldValuesJson = $"{{\"divisionId\":\"{old}\"}}", NewValuesJson = $"{{\"divisionId\":\"{targetDivisionId}\"}}"
        });
        await db.SaveChangesAsync(ct);
    }
}
