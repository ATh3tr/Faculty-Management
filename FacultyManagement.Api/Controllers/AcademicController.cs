using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/academic")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class AcademicController(AcademicService academic) : ControllerBase
{
    [HttpPost("years")]
    public async Task<IActionResult> CreateYear(CreateAcademicYearRequest request)
    {
        var year = await academic.CreateAcademicYearAsync(request);
        return CreatedAtAction(nameof(CreateYear), new { id = year.Id }, new { year.Id, year.Name });
    }

    [HttpPut("years/{id:guid}/current")]
    public async Task<IActionResult> SetCurrent(Guid id)
    {
        await academic.SetCurrentAcademicYearAsync(id);
        return NoContent();
    }

    [HttpPost("years/{id:guid}/non-teaching-days")]
    public async Task<IActionResult> AddNonTeachingDay(Guid id, SetNonTeachingDayRequest request)
    {
        await academic.AddNonTeachingDayAsync(id, request);
        return NoContent();
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse(CreateCourseRequest request)
    {
        var course = await academic.CreateCourseAsync(request);
        return Created($"/api/academic/courses/{course.Id}", new { course.Id, course.Code });
    }

    [HttpPut("courses/{id:guid}")]
    public async Task<IActionResult> UpdateCourse(Guid id, UpdateCourseRequest request)
    {
        await academic.UpdateCourseAsync(id, request);
        return NoContent();
    }

    [HttpPost("offerings")]
    public async Task<IActionResult> CreateOffering(CreateOfferingRequest request)
    {
        var offering = await academic.CreateOfferingAsync(request);
        return Created($"/api/academic/offerings/{offering.Id}", new { offering.Id });
    }

    [HttpPost("offerings/{id:guid}/staff")]
    public async Task<IActionResult> AssignStaff(Guid id, AssignStaffRequest request)
    {
        await academic.AssignStaffAsync(id, request, User.UserId());
        return NoContent();
    }

    [HttpPost("exam-periods")]
    public async Task<IActionResult> CreateExamPeriod(CreateExamPeriodRequest request)
    {
        var period = await academic.CreateExamPeriodAsync(request);
        return Created($"/api/academic/exam-periods/{period.Id}", new { period.Id });
    }

    [HttpPut("exam-periods/{id:guid}/close")]
    public async Task<IActionResult> CloseExamPeriod(Guid id)
    {
        await academic.CloseExamPeriodAsync(id);
        return NoContent();
    }

    [HttpPost("years/{academicYearId:guid}/enrollments/sync/{studyYear:int}")]
    public async Task<IActionResult> SyncEnrollments(Guid academicYearId, int studyYear)
    {
        var added = await academic.SyncMandatoryEnrollmentsAsync(academicYearId, studyYear);
        return Ok(new { added });
    }
}
