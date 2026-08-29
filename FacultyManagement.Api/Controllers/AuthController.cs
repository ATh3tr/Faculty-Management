using FacultyManagement.Api.Infrastructure;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Business.Services;
using FacultyManagement.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FacultyManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AccountService accounts, AuthTokenService tokens,
    UserManager<ApplicationUser> userManager, IWebHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous, EnableRateLimiting("auth")]
    [HttpPost("register/student")]
    public async Task<IActionResult> RegisterStudent(RegisterStudentRequest request)
    {
        var id = await accounts.RegisterStudentAsync(request);
        return Accepted(new { id, status = "PendingApproval" });
    }

    [AllowAnonymous, EnableRateLimiting("auth")]
    [HttpPost("register/staff")]
    public async Task<IActionResult> RegisterStaff(RegisterStaffRequest request)
    {
        var id = await accounts.RegisterStaffAsync(request);
        return Accepted(new { id, status = "PendingApproval" });
    }

    [AllowAnonymous, EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request)
    {
        var issued = await tokens.LoginAsync(request.Email, request.Password);
        SetRefreshCookie(issued);
        return new TokenResponse(issued.AccessToken, issued.AccessTokenExpiresAtUtc);
    }

    [AllowAnonymous, EnableRateLimiting("refresh")]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh()
    {
        if (!Request.Cookies.TryGetValue("faculty_refresh", out var refreshToken)) return Unauthorized();
        var issued = await tokens.RefreshAsync(refreshToken);
        SetRefreshCookie(issued);
        return new TokenResponse(issued.AccessToken, issued.AccessTokenExpiresAtUtc);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("faculty_refresh", out var refreshToken)) await tokens.RevokeAsync(refreshToken);
        Response.Cookies.Delete("faculty_refresh");
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserSummary>> Me()
    {
        var user = await userManager.FindByIdAsync(User.UserId().ToString());
        if (user is null) return NotFound();
        var roles = await userManager.GetRolesAsync(user);
        return new UserSummary(user.Id, user.Email!, user.FullNameArabic, user.FullNameEnglish, user.IsApproved, roles.ToArray());
    }

    private void SetRefreshCookie(IssuedTokens issued) => Response.Cookies.Append("faculty_refresh", issued.RefreshToken, new CookieOptions
    {
        HttpOnly = true, Secure = !environment.IsDevelopment(), SameSite = SameSiteMode.Strict,
        Expires = issued.RefreshTokenExpiresAtUtc, Path = "/api/auth"
    });
}
