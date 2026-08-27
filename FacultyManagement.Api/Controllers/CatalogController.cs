using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/catalog")]
public sealed class CatalogController(CatalogQueryService catalog) : ControllerBase
{
    [HttpGet("academic-years")]
    public Task<IReadOnlyCollection<AcademicYearView>> Years() => catalog.AcademicYearsAsync();

    [HttpGet("courses")]
    public Task<IReadOnlyCollection<CourseView>> Courses(int? studyYear) => catalog.CoursesAsync(studyYear);

    [HttpGet("offerings")]
    public Task<IReadOnlyCollection<OfferingView>> Offerings(Guid academicYearId, Guid? semesterId) => catalog.OfferingsAsync(academicYearId, semesterId);

    [HttpGet("divisions")]
    public Task<IReadOnlyCollection<DivisionView>> Divisions(Guid academicYearId, int? studyYear) => catalog.DivisionsAsync(academicYearId, studyYear);

    [HttpGet("rooms")]
    public Task<IReadOnlyCollection<RoomView>> Rooms() => catalog.RoomsAsync();

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("students")]
    public Task<IReadOnlyCollection<StudentView>> Students(int? studyYear, string? search) => catalog.StudentsAsync(studyYear, search);

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("staff")]
    public Task<IReadOnlyCollection<StaffView>> Staff() => catalog.StaffAsync();

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Professor},{AppRoles.ExamsOfficer},{AppRoles.Admin}")]
    [HttpGet("appeals")]
    public Task<IReadOnlyCollection<AppealView>> Appeals() => catalog.AppealsAsync(
        User.UserId(), User.IsInRole(AppRoles.Student),
        User.IsInRole(AppRoles.Professor) && !User.IsInRole(AppRoles.ExamsOfficer) && !User.IsInRole(AppRoles.Admin));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("audit")]
    public Task<IReadOnlyCollection<AuditView>> Audit(int take = 200) => catalog.AuditAsync(take);
}
