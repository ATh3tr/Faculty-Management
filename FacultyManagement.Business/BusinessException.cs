namespace FacultyManagement.Business;

public sealed class BusinessException(string message, int statusCode = 400, string? code = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code ?? "business_rule_violation";
}
