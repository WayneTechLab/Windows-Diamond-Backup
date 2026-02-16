namespace WindowsDiamondFile.Core;

public sealed class BackupReport
{
    public int ScannedFiles { get; set; }
    public int CopiedFiles { get; set; }
    public int SkippedDuplicates { get; set; }
    public int FailedFiles { get; set; }
    public long CopiedBytes { get; set; }

    public List<string> Errors { get; } = new();

    public override string ToString() =>
        $"Scanned: {ScannedFiles}, Copied: {CopiedFiles}, Duplicates: {SkippedDuplicates}, Failed: {FailedFiles}, Bytes: {CopiedBytes}";
}
