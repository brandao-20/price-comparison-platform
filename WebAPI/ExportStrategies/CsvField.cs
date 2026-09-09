namespace WebAPI.ExportStrategies;

public static class CsvField
{
    public static string Escape(string? value, char delimiter = ',')
    {
        value ??= "";
        // Prevent user-provided text from being interpreted as a spreadsheet formula.
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0]) || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n'))
            value = "'" + value;
        return value.IndexOfAny(new[] { delimiter, '"', '\r', '\n' }) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}
