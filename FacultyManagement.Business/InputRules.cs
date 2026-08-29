using System.Net.Mail;
using System.Text.RegularExpressions;

namespace FacultyManagement.Business;

public static partial class InputRules
{
    public static void ValidateEmail(string? email)
    {
        var value = email?.Trim() ?? string.Empty;
        if (!MailAddress.TryCreate(value, out var parsed)
            || !string.Equals(parsed.Address, value, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Enter a complete email address such as name@example.com.");
        var lastDot = parsed.Host.LastIndexOf('.');
        if (lastDot <= 0 || parsed.Host.Length - lastDot - 1 < 2)
            throw new BusinessException("Enter a complete email address such as name@example.com.");
    }

    public static void ValidateUniversityNumber(string? universityNumber)
    {
        if (string.IsNullOrWhiteSpace(universityNumber) || !AsciiDigitsRegex().IsMatch(universityNumber.Trim()))
            throw new BusinessException("University number must contain ASCII digits (0-9) only.");
    }

    public static void ValidateBilingual(string? arabic, string? english, string fieldName)
    {
        var arabicValue = arabic?.Trim() ?? string.Empty;
        var englishValue = english?.Trim() ?? string.Empty;
        if (arabicValue.Length == 0 || !ArabicRegex().IsMatch(arabicValue) || LatinRegex().IsMatch(arabicValue))
            throw new BusinessException($"Arabic {fieldName} must contain Arabic text and no English letters.");
        if (englishValue.Length == 0 || !LatinRegex().IsMatch(englishValue) || ArabicRegex().IsMatch(englishValue))
            throw new BusinessException($"English {fieldName} must contain English text and no Arabic characters.");
    }

    [GeneratedRegex("^[0-9]+$")]
    private static partial Regex AsciiDigitsRegex();

    [GeneratedRegex("[A-Za-z]")]
    private static partial Regex LatinRegex();

    [GeneratedRegex("[\\u0600-\\u06FF\\u0750-\\u077F\\u08A0-\\u08FF]")]
    private static partial Regex ArabicRegex();
}
