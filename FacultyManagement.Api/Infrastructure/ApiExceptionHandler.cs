using FacultyManagement.Business;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FacultyManagement.Api.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var business = exception as BusinessException;
        var status = business?.StatusCode ?? StatusCodes.Status500InternalServerError;
        var code = business?.Code ?? "internal_error";
        if (status >= 500) logger.LogError(exception, "Unhandled API exception");
        else logger.LogInformation(exception, "Request rejected with {Code}", code);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = code,
            Detail = business?.Message ?? "An unexpected server error occurred.",
            Instance = context.Request.Path
        }, cancellationToken);
        return true;
    }
}
