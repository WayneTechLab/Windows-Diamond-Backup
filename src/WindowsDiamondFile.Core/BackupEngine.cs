using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace WindowsDiamondFile.Core;

public sealed record EngineProgress(string Phase, int ScannedFiles, int CopiedFiles, int SkippedDuplicates, int FailedFiles, string Message);

public sealed class BackupEngine
{
    public async Task<BackupReport> RunAsync(BackupProfile profile, IProgress<EngineProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ValidateProfile(profile);

        Directory.CreateDirectory(profile.OutputRoot);
        var report = new BackupReport();

        progress?.Report(new EngineProgress("Initialize", 0, 0, 0, 0, "Starting scan"));
        var records = IndexFiles(profile, report, cancellationToken);
        progress?.Report(new EngineProgress("Index", report.ScannedFiles, 0, 0, 0, $"Indexed {report.ScannedFiles} files"));
        var knownDestinations = BuildDestinationDuplicateIndex(profile);

        var operations = new List<CopyOperation>(records.Count);
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            operations.Add(BuildCopyOperation(profile, record, knownDestinations));
        }

        await CopyFilesAsync(profile, operations, report, progress, cancellationToken).ConfigureAwait(false);
        progress?.Report(new EngineProgress("Complete", report.ScannedFiles, report.CopiedFiles, report.SkippedDuplicates, report.FailedFiles, "Backup complete"));
        return report;
    }

    private static void ValidateProfile(BackupProfile profile)
    {
        if (profile.SourceDrives.Count == 0) throw new ArgumentException("At least one source drive is required.");
        if (string.IsNullOrWhiteSpace(profile.OutputRoot)) throw new ArgumentException("OutputRoot is required.");
    }

    private static List<FileRecord> IndexFiles(BackupProfile profile, BackupReport report, CancellationToken cancellationToken)
    {
        var records = new List<FileRecord>();

        foreach (var source in profile.SourceDrives)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(source))
            {
                report.Errors.Add($"Source does not exist: {source}");
                continue;
            }

            var driveRootName = GetDriveSafeRootName(source);
            EnumerateFilesSafe(source, profile.ContinueOnAccessDenied, report.Errors, cancellationToken, file =>
            {
                var info = new FileInfo(file);
                if (ShouldSkip(profile, info)) return;

                var relativeInDrive = Path.GetRelativePath(source, file);
                var relativePath = profile.PreserveWorkingDirectoryPattern
                    ? Path.Combine(driveRootName, relativeInDrive)
                    : relativeInDrive;

                var extension = info.Extension.ToLowerInvariant();
                var isPhoto = profile.SupportedPhotoExtensions.Contains(extension);
                var category = FileCategorizer.Categorize(extension, info.Name);

                records.Add(new FileRecord(
                    file,
                    relativePath,
                    extension,
                    info.Length,
                    info.LastWriteTimeUtc,
                    category,
                    isPhoto));

                lock (report)
                {
                    report.ScannedFiles++;
                }
            });
        }

        return records;
    }

    private static void EnumerateFilesSafe(
        string root,
        bool continueOnAccessDenied,
        List<string> errors,
        CancellationToken cancellationToken,
        Action<string> onFile)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();

            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex) when (continueOnAccessDenied && (ex is UnauthorizedAccessException || ex is IOException))
            {
                lock (errors)
                {
                    errors.Add($"Skipping directory due to access issue: {current} ({ex.Message})");
                }
                continue;
            }

            foreach (var dir in directories)
            {
                pending.Push(dir);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(current, "*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex) when (continueOnAccessDenied && (ex is UnauthorizedAccessException || ex is IOException))
            {
                lock (errors)
                {
                    errors.Add($"Skipping files in directory due to access issue: {current} ({ex.Message})");
                }
                continue;
            }

            foreach (var file in files)
            {
                onFile(file);
            }
        }
    }

    private static bool ShouldSkip(BackupProfile profile, FileInfo info)
    {
        var fullPath = info.FullName;
        if (profile.IgnoreDirectories.Any(skip => fullPath.Contains(Path.DirectorySeparatorChar + skip + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (profile.Security.BlockHiddenSystemFiles &&
            (info.Attributes.HasFlag(FileAttributes.Hidden) || info.Attributes.HasFlag(FileAttributes.System)))
        {
            return true;
        }

        return false;
    }

    private static string GetDriveSafeRootName(string source)
    {
        var root = Path.GetPathRoot(source) ?? source;
        var trimmed = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (trimmed.EndsWith(":", StringComparison.Ordinal))
        {
            return trimmed.Replace(':', '_');
        }

        var name = new DirectoryInfo(source).Name;
        return string.IsNullOrWhiteSpace(name) ? "Drive" : name;
    }

    private static ConcurrentDictionary<(long Size, string Name), List<string>> BuildDestinationDuplicateIndex(BackupProfile profile)
    {
        var map = new ConcurrentDictionary<(long Size, string Name), List<string>>();
        if (!Directory.Exists(profile.OutputRoot)) return map;

        foreach (var file in Directory.EnumerateFiles(profile.OutputRoot, "*", SearchOption.AllDirectories))
        {
            var info = new FileInfo(file);
            var key = (info.Length, info.Name);
            var bucket = map.GetOrAdd(key, _ => new List<string>());
            lock (bucket)
            {
                bucket.Add(file);
            }
        }

        return map;
    }

    private static CopyOperation BuildCopyOperation(
        BackupProfile profile,
        FileRecord record,
        ConcurrentDictionary<(long Size, string Name), List<string>> knownDestinations)
    {
        var primaryDestination = Path.Combine(profile.OutputRoot, "Merged-By-Project", record.RelativePath);

        string? photoDestination = null;
        if (profile.EnablePhotoDatabaseMirror && record.IsPhoto)
        {
            var year = record.LastWriteUtc.Year.ToString("0000");
            var month = record.LastWriteUtc.Month.ToString("00");
            photoDestination = Path.Combine(profile.OutputRoot, profile.PhotoDatabaseRoot, year, month, Path.GetFileName(record.SourcePath));
        }

        string? quarantineDestination = null;
        if (profile.Security.QuarantineUntrustedExecutables && (record.Extension == ".exe" || record.Extension == ".msi"))
        {
            quarantineDestination = Path.Combine(profile.OutputRoot, profile.Security.QuarantineFolderName, record.Category, Path.GetFileName(record.SourcePath));
        }

        var duplicateKey = (record.Size, Path.GetFileName(record.SourcePath));
        var duplicateExists = profile.UseFastFileSizeDuplicateCheck && knownDestinations.ContainsKey(duplicateKey);

        return new CopyOperation(record, primaryDestination, photoDestination, quarantineDestination, duplicateExists);
    }

    private static async Task CopyFilesAsync(
        BackupProfile profile,
        List<CopyOperation> operations,
        BackupReport report,
        IProgress<EngineProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(Math.Max(1, profile.MaxParallelCopies));

        var tasks = operations.Select(async operation =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await CopyOperationWithPolicyAsync(profile, operation, report, cancellationToken).ConfigureAwait(false);
                progress?.Report(new EngineProgress("Copy", report.ScannedFiles, report.CopiedFiles, report.SkippedDuplicates, report.FailedFiles, Path.GetFileName(operation.Record.SourcePath)));
            }
            catch (Exception ex)
            {
                lock (report) { report.FailedFiles++; }
                lock (report.Errors)
                {
                    report.Errors.Add($"{operation.Record.SourcePath} -> {ex.Message}");
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task CopyOperationWithPolicyAsync(BackupProfile profile, CopyOperation operation, BackupReport report, CancellationToken cancellationToken)
    {
        if (operation.IsDuplicateSkipped && profile.DuplicateHandling == DuplicateHandling.SkipBySizeAndName)
        {
            lock (report) { report.SkippedDuplicates++; }
            return;
        }

        var primaryDestination = await ResolveDuplicatePolicyDestinationAsync(profile, operation.Record.SourcePath, operation.PrimaryDestination, report, cancellationToken).ConfigureAwait(false);
        if (primaryDestination is null)
        {
            return;
        }

        await CopyWithVerificationAsync(profile, operation.Record.SourcePath, primaryDestination, cancellationToken).ConfigureAwait(false);

        if (operation.PhotoMirrorDestination is not null)
        {
            var photoDestination = await ResolveDuplicatePolicyDestinationAsync(profile, operation.Record.SourcePath, operation.PhotoMirrorDestination, report, cancellationToken).ConfigureAwait(false);
            if (photoDestination is not null)
            {
                await CopyWithVerificationAsync(profile, operation.Record.SourcePath, photoDestination, cancellationToken).ConfigureAwait(false);
            }
        }

        if (operation.QuarantineDestination is not null)
        {
            var quarantineDestination = await ResolveDuplicatePolicyDestinationAsync(profile, operation.Record.SourcePath, operation.QuarantineDestination, report, cancellationToken).ConfigureAwait(false);
            if (quarantineDestination is not null)
            {
                await CopyWithVerificationAsync(profile, operation.Record.SourcePath, quarantineDestination, cancellationToken).ConfigureAwait(false);
            }
        }

        lock (report)
        {
            report.CopiedFiles++;
            report.CopiedBytes += operation.Record.Size;
        }
    }

    private static async Task<string?> ResolveDuplicatePolicyDestinationAsync(
        BackupProfile profile,
        string source,
        string destination,
        BackupReport report,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(destination))
        {
            return destination;
        }

        if (profile.DuplicateHandling == DuplicateHandling.KeepBothWithRename)
        {
            return NextAvailablePath(destination);
        }

        if (profile.DuplicateHandling == DuplicateHandling.SkipBySizeAndName)
        {
            lock (report) { report.SkippedDuplicates++; }
            return null;
        }

        var sourceInfo = new FileInfo(source);
        var destinationInfo = new FileInfo(destination);
        if (sourceInfo.Length != destinationInfo.Length)
        {
            return NextAvailablePath(destination);
        }

        var same = await AreFilesContentEqualAsync(source, destination, cancellationToken).ConfigureAwait(false);
        if (same)
        {
            lock (report) { report.SkippedDuplicates++; }
            return null;
        }

        return NextAvailablePath(destination);
    }

    private static string NextAvailablePath(string fullPath)
    {
        var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
        var extension = Path.GetExtension(fullPath);

        var index = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(directory, $"{fileNameWithoutExtension} ({index}){extension}");
            index++;
        } while (File.Exists(candidate));

        return candidate;
    }

    private static async Task<bool> AreFilesContentEqualAsync(string leftFile, string rightFile, CancellationToken cancellationToken)
    {
        var left = await ComputeSha256Async(leftFile, cancellationToken).ConfigureAwait(false);
        var right = await ComputeSha256Async(rightFile, cancellationToken).ConfigureAwait(false);
        return left.SequenceEqual(right);
    }

    private static async Task CopyWithVerificationAsync(BackupProfile profile, string source, string destination, CancellationToken cancellationToken)
    {
        if (profile.DryRun) return;

        var destinationDir = Path.GetDirectoryName(destination);
        if (destinationDir is not null)
        {
            Directory.CreateDirectory(destinationDir);
        }

        const int bufferSize = 1024 * 1024;
        const FileOptions options = FileOptions.SequentialScan;

        using (var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, options))
        using (var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, options))
        {
            await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken).ConfigureAwait(false);
        }

        if (profile.PreserveTimestamps)
        {
            var sourceInfo = new FileInfo(source);
            File.SetCreationTimeUtc(destination, sourceInfo.CreationTimeUtc);
            File.SetLastWriteTimeUtc(destination, sourceInfo.LastWriteTimeUtc);
        }

        if (!profile.VerifyCopiedFiles) return;

        var sourceInfoForVerify = new FileInfo(source);
        var destinationInfo = new FileInfo(destination);
        if (sourceInfoForVerify.Length != destinationInfo.Length)
        {
            throw new IOException("Verification failed: size mismatch.");
        }

        if (!profile.UseHashCheckForMismatchedTimestamp) return;
        if (sourceInfoForVerify.LastWriteTimeUtc == destinationInfo.LastWriteTimeUtc) return;

        var sourceHash = await ComputeSha256Async(source, cancellationToken).ConfigureAwait(false);
        var destinationHash = await ComputeSha256Async(destination, cancellationToken).ConfigureAwait(false);
        if (!sourceHash.SequenceEqual(destinationHash))
        {
            throw new IOException("Verification failed: checksum mismatch.");
        }
    }

    private static async Task<byte[]> ComputeSha256Async(string file, CancellationToken cancellationToken)
    {
        const int bufferSize = 1024 * 64;
        var buffer = new byte[bufferSize];

        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
        using var sha = SHA256.Create();

        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
        while (bytesRead > 0)
        {
            sha.TransformBlock(buffer, 0, bytesRead, null, 0);
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return sha.Hash ?? Array.Empty<byte>();
    }

}
