using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Controllers;

[ApiController, Route("api/accounts")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class AccountsController(AccountService accounts) : ControllerBase
{
    [HttpGet("pending")]
    public Task<IReadOnlyCollection<UserSummary>> Pending() => accounts.PendingAsync();

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, ApproveAccountRequest request)
    {
        await accounts.ApproveAsync(id, request.Roles, User.UserId());
        return NoContent();
    }

    [HttpPut("{id:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid id, AdminResetPasswordRequest request)
    {
        await accounts.ResetPasswordAsync(id, request.NewPassword, User.UserId());
        return NoContent();
    }
}
