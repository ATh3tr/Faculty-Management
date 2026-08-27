using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/promotions")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class PromotionsController(PromotionService promotions) : ControllerBase
{
    [HttpPost("preview")]
    public Task<PromotionRunResult> Preview(Guid concludedAcademicYearId) =>
        promotions.RunAsync(concludedAcademicYearId, null, true, User.UserId());

    [HttpPost("commit")]
    public Task<PromotionRunResult> Commit(Guid concludedAcademicYearId, Guid nextAcademicYearId) =>
        promotions.RunAsync(concludedAcademicYearId, nextAcademicYearId, false, User.UserId());
}
