namespace WindowsDiamondFile.Core;

public static class FileCategorizer
{
    private static readonly Dictionary<string, string> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        [".doc"] = "Documents", [".docx"] = "Documents", [".pdf"] = "Documents", [".txt"] = "Documents",
        [".xls"] = "Spreadsheets", [".xlsx"] = "Spreadsheets", [".csv"] = "Spreadsheets",
        [".ppt"] = "Presentations", [".pptx"] = "Presentations",
        [".zip"] = "Archives", [".rar"] = "Archives", [".7z"] = "Archives", [".tar"] = "Archives", [".gz"] = "Archives",
        [".mp4"] = "Video", [".mov"] = "Video", [".avi"] = "Video", [".mkv"] = "Video",
        [".mp3"] = "Audio", [".wav"] = "Audio", [".flac"] = "Audio",
        [".cs"] = "SourceCode", [".js"] = "SourceCode", [".ts"] = "SourceCode", [".java"] = "SourceCode", [".py"] = "SourceCode",
        [".exe"] = "Software", [".msi"] = "Software", [".dll"] = "Software", [".bat"] = "Automation"
    };

    public static string Categorize(string extension, string fileName)
    {
        if (Categories.TryGetValue(extension, out var category))
        {
            return category;
        }

        var lower = fileName.ToLowerInvariant();
        if (lower.Contains("invoice") || lower.Contains("receipt")) return "Finance";
        if (lower.Contains("contract") || lower.Contains("legal")) return "Legal";
        if (lower.Contains("backup") || lower.Contains("archive")) return "Backups";

        return "General";
    }
}
