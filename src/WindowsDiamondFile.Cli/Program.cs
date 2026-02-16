using System.Text.Json;
using WindowsDiamondFile.Core;

Console.WriteLine("Windows Diamond File - Professional Multi-Drive Backup and Index Tool");
Console.WriteLine("--------------------------------------------------------------------");

var profilePath = args.Length > 0 ? args[0] : "backup-profile.json";

if (!File.Exists(profilePath))
{
    if (LooksLikePlaceholder(profilePath))
    {
        Console.Error.WriteLine("The provided profile path looks like a placeholder. Please pass a real .json file path.");
        Console.Error.WriteLine("Example: WindowsDiamondFile.exe C:\\Backups\\backup-profile.json");
        Environment.ExitCode = 1;
        return;
    }

    try
    {
        var profileDirectory = Path.GetDirectoryName(Path.GetFullPath(profilePath));
        if (!string.IsNullOrWhiteSpace(profileDirectory))
        {
            Directory.CreateDirectory(profileDirectory);
        }

        var starter = new BackupProfile
        {
            JobName = "My Diamond Backup Job",
            SourceDrives = new List<string> { "D:\\", "E:\\", "F:\\" },
            OutputRoot = "G:\\WindowsDiamondOutput",
            DuplicateHandling = DuplicateHandling.SkipOnlyWhenContentMatches,
            DryRun = true
        };

        var starterJson = JsonSerializer.Serialize(starter, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(profilePath, starterJson);
        Console.WriteLine($"Created starter profile at {profilePath}. Edit values and run again.");
        return;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Unable to create starter profile at '{profilePath}': {ex.Message}");
        Environment.ExitCode = 1;
        return;
    }
}

try
{
    var profileJson = await File.ReadAllTextAsync(profilePath);
    var profile = JsonSerializer.Deserialize<BackupProfile>(profileJson) ?? throw new InvalidOperationException("Invalid profile.");

    var engine = new BackupEngine();
    var report = await engine.RunAsync(profile);

    Console.WriteLine($"Job: {profile.JobName}");
    Console.WriteLine(report);

    if (report.Errors.Count > 0)
    {
        Console.WriteLine("Errors:");
        foreach (var error in report.Errors)
        {
            Console.WriteLine($" - {error}");
        }
    }

    Environment.ExitCode = report.FailedFiles > 0 ? 2 : 0;
    Console.WriteLine("Done.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    Environment.ExitCode = 1;
}

static bool LooksLikePlaceholder(string path)
{
    var normalized = path.Replace('/', '\\').ToLowerInvariant();
    return normalized.Contains("path\\to\\") || normalized.Contains("your-backup-profile");
}
