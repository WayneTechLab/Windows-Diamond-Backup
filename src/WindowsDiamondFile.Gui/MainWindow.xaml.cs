using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using WindowsDiamondFile.Core;
using Forms = System.Windows.Forms;

namespace WindowsDiamondFile.Gui;

public partial class MainWindow : System.Windows.Window
{
    private CancellationTokenSource? _runCts;

    public MainWindow()
    {
        InitializeComponent();
        OutputRootTextBox.Text = @"G:\WindowsDiamondOutput";
        DuplicatePolicyComboBox.ItemsSource = Enum.GetValues(typeof(DuplicateHandling));
        DuplicatePolicyComboBox.SelectedItem = DuplicateHandling.SkipOnlyWhenContentMatches;
        MaxParallelCopiesTextBox.Text = Math.Max(2, Environment.ProcessorCount).ToString();
    }

    private void AddSource_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Select source drive or folder",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK && !SourceListBox.Items.Contains(dialog.SelectedPath))
        {
            SourceListBox.Items.Add(dialog.SelectedPath);
        }
    }

    private void RemoveSource_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (SourceListBox.SelectedItem is not null)
        {
            SourceListBox.Items.Remove(SourceListBox.SelectedItem);
        }
    }

    private void ClearSources_Click(object sender, System.Windows.RoutedEventArgs e) => SourceListBox.Items.Clear();

    private void BrowseOutput_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Select output drive/folder",
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            OutputRootTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void StartBackup_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var profile = BuildProfileFromForm();
            ValidateProfile(profile);

            _runCts = new CancellationTokenSource();
            SetBusy(true);
            AppendLog($"Job started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            var progress = new Progress<EngineProgress>(p =>
            {
                StatusTextBlock.Text = $"{p.Phase}: {p.Message} | Scanned={p.ScannedFiles} Copied={p.CopiedFiles} Skipped={p.SkippedDuplicates} Failed={p.FailedFiles}";
                var totalDone = p.CopiedFiles + p.SkippedDuplicates + p.FailedFiles;
                var percent = p.ScannedFiles <= 0 ? 0 : Math.Min(100, (int)Math.Round((double)totalDone / p.ScannedFiles * 100.0));
                ProgressBar.Value = percent;
            });

            var engine = new BackupEngine();
            var report = await Task.Run(() => engine.RunAsync(profile, progress, _runCts.Token), _runCts.Token);

            AppendLog("Job completed.");
            AppendLog(report.ToString());

            if (report.Errors.Count > 0)
            {
                AppendLog("Errors:");
                foreach (var error in report.Errors)
                {
                    AppendLog($" - {error}");
                }
            }

            StatusTextBlock.Text = report.FailedFiles > 0
                ? $"Completed with warnings. Failed files: {report.FailedFiles}"
                : "Completed successfully.";

            System.Windows.MessageBox.Show(
                $"Done. Copied: {report.CopiedFiles}, Skipped duplicates: {report.SkippedDuplicates}, Failed: {report.FailedFiles}",
                "Windows Diamond File",
                System.Windows.MessageBoxButton.OK,
                report.FailedFiles > 0 ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Operation cancelled by user.");
            StatusTextBlock.Text = "Cancelled.";
        }
        catch (Exception ex)
        {
            AppendLog($"Fatal error: {ex.Message}");
            StatusTextBlock.Text = "Failed.";
            System.Windows.MessageBox.Show(ex.Message, "Fatal Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            _runCts?.Dispose();
            _runCts = null;
            SetBusy(false);
        }
    }

    private void CancelBackup_Click(object sender, System.Windows.RoutedEventArgs e) => _runCts?.Cancel();

    private void SaveProfile_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var profile = BuildProfileFromForm();
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "backup-profile.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
                AppendLog($"Saved profile: {dialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Save profile failed: {ex.Message}");
            System.Windows.MessageBox.Show(ex.Message, "Save Profile Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void LoadProfile_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true)
            {
                var json = File.ReadAllText(dialog.FileName);
                var profile = JsonSerializer.Deserialize<BackupProfile>(json) ?? throw new InvalidOperationException("Invalid profile JSON.");
                ApplyProfileToForm(profile);
                AppendLog($"Loaded profile: {dialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Load profile failed: {ex.Message}");
            System.Windows.MessageBox.Show(ex.Message, "Load Profile Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private BackupProfile BuildProfileFromForm()
    {
        var sources = SourceListBox.Items.Cast<string>().ToList();
        var parallel = int.TryParse(MaxParallelCopiesTextBox.Text, out var p) ? Math.Max(1, p) : Math.Max(2, Environment.ProcessorCount);
        var duplicatePolicy = DuplicatePolicyComboBox.SelectedItem is DuplicateHandling selected
            ? selected
            : DuplicateHandling.SkipOnlyWhenContentMatches;

        return new BackupProfile
        {
            JobName = "Windows Diamond GUI Job",
            SourceDrives = sources,
            OutputRoot = OutputRootTextBox.Text.Trim(),
            EnablePhotoDatabaseMirror = PhotoMirrorCheckBox.IsChecked == true,
            PhotoDatabaseRoot = string.IsNullOrWhiteSpace(PhotoDbRootTextBox.Text) ? "Photo-Database" : PhotoDbRootTextBox.Text.Trim(),
            VerifyCopiedFiles = VerifyCheckBox.IsChecked == true,
            ContinueOnAccessDenied = ContinueOnAccessDeniedCheckBox.IsChecked == true,
            DuplicateHandling = duplicatePolicy,
            MaxParallelCopies = parallel,
            DryRun = DryRunCheckBox.IsChecked == true,
            Security = new SecurityProfile
            {
                BlockHiddenSystemFiles = BlockHiddenSystemCheckBox.IsChecked == true,
                QuarantineUntrustedExecutables = true,
                AllowEncryptedDestination = true
            }
        };
    }

    private void ApplyProfileToForm(BackupProfile profile)
    {
        OutputRootTextBox.Text = profile.OutputRoot;
        SourceListBox.Items.Clear();
        foreach (var source in profile.SourceDrives)
        {
            SourceListBox.Items.Add(source);
        }

        DryRunCheckBox.IsChecked = profile.DryRun;
        PhotoMirrorCheckBox.IsChecked = profile.EnablePhotoDatabaseMirror;
        VerifyCheckBox.IsChecked = profile.VerifyCopiedFiles;
        ContinueOnAccessDeniedCheckBox.IsChecked = profile.ContinueOnAccessDenied;
        BlockHiddenSystemCheckBox.IsChecked = profile.Security.BlockHiddenSystemFiles;
        DuplicatePolicyComboBox.SelectedItem = profile.DuplicateHandling;
        MaxParallelCopiesTextBox.Text = profile.MaxParallelCopies.ToString();
        PhotoDbRootTextBox.Text = profile.PhotoDatabaseRoot;
    }

    private static void ValidateProfile(BackupProfile profile)
    {
        if (profile.SourceDrives.Count == 0)
        {
            throw new InvalidOperationException("Add at least one source drive/folder.");
        }

        if (string.IsNullOrWhiteSpace(profile.OutputRoot))
        {
            throw new InvalidOperationException("Select an output drive/folder.");
        }
    }

    private void SetBusy(bool busy)
    {
        StartButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        SaveProfileButton.IsEnabled = !busy;
        LoadProfileButton.IsEnabled = !busy;
    }

    private void AppendLog(string message)
    {
        LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} | {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }
}
