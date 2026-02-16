namespace WindowsDiamondFile.Core;

public sealed record FileRecord(
    string SourcePath,
    string RelativePath,
    string Extension,
    long Size,
    DateTime LastWriteUtc,
    string Category,
    bool IsPhoto);

public sealed record CopyOperation(
    FileRecord Record,
    string PrimaryDestination,
    string? PhotoMirrorDestination,
    string? QuarantineDestination,
    bool IsDuplicateSkipped);
