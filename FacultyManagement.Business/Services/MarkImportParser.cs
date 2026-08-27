using System.Globalization;
using ClosedXML.Excel;
using FacultyManagement.Business.Contracts;
using FacultyManagement.Data.Domain;

namespace FacultyManagement.Business.Services;

public sealed class MarkImportParser
{
    public async Task<IReadOnlyCollection<ImportMarkRow>> ParseAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) return await ParseCsvAsync(stream, ct);
        if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) return ParseExcel(stream);
        throw new BusinessException("Only .csv and .xlsx files are supported.");
    }

    private static async Task<IReadOnlyCollection<ImportMarkRow>> ParseCsvAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var header = await reader.ReadLineAsync(ct) ?? throw new BusinessException("Import file is empty.");
        var columns = header.Split(',').Select((name, index) => (name: name.Trim(), index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);
        ValidateColumns(columns.Keys);
        var rows = new List<ImportMarkRow>();
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = line.Split(',').Select(x => x.Trim().Trim('"')).ToArray();
            rows.Add(ParseRow(values[columns["UniversityNumber"]], values[columns["CourseCode"]],
                values[columns["Result"]], values[columns["Mark"]]));
        }
        return rows;
    }

    private static IReadOnlyCollection<ImportMarkRow> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var headerRow = sheet.FirstRowUsed() ?? throw new BusinessException("Import worksheet is empty.");
        var columns = headerRow.CellsUsed().ToDictionary(x => x.GetString().Trim(), x => x.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
        ValidateColumns(columns.Keys);
        var rows = new List<ImportMarkRow>();
        foreach (var row in sheet.RowsUsed().Skip(1))
            rows.Add(ParseRow(row.Cell(columns["UniversityNumber"]).GetString(), row.Cell(columns["CourseCode"]).GetString(),
                row.Cell(columns["Result"]).GetString(), row.Cell(columns["Mark"]).GetFormattedString()));
        return rows;
    }

    private static ImportMarkRow ParseRow(string universityNumber, string courseCode, string result, string mark)
    {
        if (!Enum.TryParse<ExamResultKind>(result, true, out var kind))
            throw new BusinessException($"Unknown result kind '{result}'.");
        decimal? numeric = null;
        if (!string.IsNullOrWhiteSpace(mark))
        {
            if (!decimal.TryParse(mark, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                throw new BusinessException($"Invalid mark '{mark}'.");
            numeric = parsed;
        }
        return new ImportMarkRow(universityNumber.Trim(), courseCode.Trim(), kind, numeric);
    }

    private static void ValidateColumns(IEnumerable<string> columns)
    {
        var actual = columns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = new[] { "UniversityNumber", "CourseCode", "Result", "Mark" }.Where(x => !actual.Contains(x)).ToArray();
        if (missing.Length > 0) throw new BusinessException($"Missing import columns: {string.Join(", ", missing)}");
    }
}
