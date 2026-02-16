using System.Text.Json.Serialization;

namespace WindowsDiamondFile.Core;

public sealed class BackupProfile
{
    public string JobName { get; set; } = "Default Windows Diamond Job";

    public List<string> SourceDrives { get; set; } = new();

    public string OutputRoot { get; set; } = string.Empty;

    public bool EnablePhotoDatabaseMirror { get; set; } = true;

    public string PhotoDatabaseRoot { get; set; } = "Photo-Database";

    public bool UseFastFileSizeDuplicateCheck { get; set; } = true;

    public bool VerifyCopiedFiles { get; set; } = true;

    public bool UseHashCheckForMismatchedTimestamp { get; set; } = true;

    public bool PreserveWorkingDirectoryPattern { get; set; } = true;

    public bool PreserveTimestamps { get; set; } = true;

    public bool ContinueOnAccessDenied { get; set; } = true;

    public bool DryRun { get; set; }

    public int MaxParallelCopies { get; set; } = Math.Max(2, Environment.ProcessorCount);

    public SecurityProfile Security { get; set; } = new();

    public DuplicateHandling DuplicateHandling { get; set; } = DuplicateHandling.SkipOnlyWhenContentMatches;

    public List<string> IgnoreDirectories { get; set; } = new() { "$Recycle.Bin", "System Volume Information" };

    [JsonIgnore]
    public List<string> SupportedPhotoExtensions { get; } = new() { ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".heic", ".webp", ".gif", ".bmp", ".raw", ".dng" };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DuplicateHandling
{
    SkipBySizeAndName = 0,
    SkipOnlyWhenContentMatches = 1,
    KeepBothWithRename = 2
}

public sealed class SecurityProfile
{
    public bool AllowEncryptedDestination { get; set; } = true;

    public bool BlockHiddenSystemFiles { get; set; }

    public bool QuarantineUntrustedExecutables { get; set; } = true;

    public string QuarantineFolderName { get; set; } = "Quarantine";
}
