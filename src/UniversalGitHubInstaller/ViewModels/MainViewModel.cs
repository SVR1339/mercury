using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UniversalGitHubInstaller.Core.Detection;
using UniversalGitHubInstaller.Core.Logging;
using UniversalGitHubInstaller.Core.Models;
using UniversalGitHubInstaller.Core.Runtime;
using UniversalGitHubInstaller.Core.Security;
using UniversalGitHubInstaller.Core.Services;

namespace UniversalGitHubInstaller.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly ProjectScanner _scanner = new();
    private readonly WindowsEnvironmentScanner _envScanner = new();
    private readonly SecurityScanner _securityScanner = new();
    private readonly GitService _gitService = new();
    private readonly ZipService _zipService = new();
    private readonly ProcessRunner _processRunner = new();
    private readonly ProjectProfileService _profileService = new();
    private readonly TerminalService _terminalService = new();
    private readonly SmartCommandProcessor _smartProcessor = new();
    private readonly AppLogger _logger = new("app");

    private string _currentPage = "Home";
    private string _statusText = "Ready";
    private bool _isBusy;
    private double _progress;
    private string _projectPath = "";
    private string _gitUrl = "";
    private SmartCommandInfo? _detectedCommand;
    private string _detectedBadge = "";
    private ProjectInfo? _projectInfo;
    private WindowsEnvironmentInfo? _envInfo;
    private SecurityScanResult? _securityResult;
    private CancellationTokenSource? _cts;

    public MainViewModel()
    {
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        OpenZipCommand = new RelayCommand(OpenZip);
        CloneUrlCommand = new RelayCommand(async () => await ProcessSmartInput(), () => !IsBusy && !string.IsNullOrWhiteSpace(GitUrl));
        SmartInstallCommand = new RelayCommand(async () => await ProcessSmartInput(), () => !IsBusy && !string.IsNullOrWhiteSpace(GitUrl));
        ScanProjectCommand = new RelayCommand(async () => await ScanProject(), () => !IsBusy && !string.IsNullOrWhiteSpace(ProjectPath));
        InstallDependenciesCommand = new RelayCommand(async () => await InstallDependencies(), () => !IsBusy && ProjectInfo != null);
        BuildProjectCommand = new RelayCommand(async () => await BuildProject(), () => !IsBusy && ProjectInfo?.BuildSystem != null);
        TestProjectCommand = new RelayCommand(async () => await TestProject(), () => !IsBusy && ProjectInfo?.TestSystem != null);
        RunProjectCommand = new RelayCommand(async () => await RunProject(), () => !IsBusy && ProjectInfo?.PossibleRunCommands?.Any() == true);
        StopProjectCommand = new RelayCommand(StopProject, () => _cts != null);
        OpenLogsFolderCommand = new RelayCommand(OpenLogsFolder);
        OpenProjectFolderCommand = new RelayCommand(OpenProjectFolder, () => !string.IsNullOrWhiteSpace(ProjectPath));
        OpenPowerShellCommand = new RelayCommand(OpenPowerShell);
        OpenCmdCommand = new RelayCommand(OpenCmd);
        OpenTerminalCommand = new RelayCommand(OpenTerminal);
        OpenAppleTerminalCommand = new RelayCommand(OpenAppleTerminal);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        RepairProjectCommand = new RelayCommand(async () => await RepairProject(), () => !IsBusy && ProjectInfo != null);
        UpdateProjectCommand = new RelayCommand(async () => await UpdateProject(), () => !IsBusy && ProjectInfo?.GitRemote != null);
        RunDoctorCommand = new RelayCommand(async () => await RunDoctor(), () => !IsBusy);

        LogEntries = new ObservableCollection<string>();
        Technologies = new ObservableCollection<TechDisplayItem>();
        Services = new ObservableCollection<ServiceDisplayItem>();
        EnvironmentVars = new ObservableCollection<EnvVarDisplayItem>();
    }

    // Properties
    public string CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsBusy { get => _isBusy; set { SetProperty(ref _isBusy, value); CommandManager.InvalidateRequerySuggested(); } }
    public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
    public string ProjectPath { get => _projectPath; set { SetProperty(ref _projectPath, value); CommandManager.InvalidateRequerySuggested(); } }
    
    public string GitUrl
    {
        get => _gitUrl;
        set
        {
            if (SetProperty(ref _gitUrl, value))
            {
                UpdateDetectedCommand(value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public SmartCommandInfo? DetectedCommand { get => _detectedCommand; set => SetProperty(ref _detectedCommand, value); }
    public string DetectedBadge { get => _detectedBadge; set => SetProperty(ref _detectedBadge, value); }

    public ProjectInfo? ProjectInfo { get => _projectInfo; set { SetProperty(ref _projectInfo, value); CommandManager.InvalidateRequerySuggested(); OnPropertyChanged(nameof(HasProject)); } }
    public WindowsEnvironmentInfo? EnvInfo { get => _envInfo; set => SetProperty(ref _envInfo, value); }
    public SecurityScanResult? SecurityResult { get => _securityResult; set => SetProperty(ref _securityResult, value); }
    public bool HasProject => ProjectInfo != null;

    // Collections
    public ObservableCollection<string> LogEntries { get; }
    public ObservableCollection<TechDisplayItem> Technologies { get; }
    public ObservableCollection<ServiceDisplayItem> Services { get; }
    public ObservableCollection<EnvVarDisplayItem> EnvironmentVars { get; }

    // Commands
    public ICommand BrowseFolderCommand { get; }
    public ICommand OpenZipCommand { get; }
    public ICommand CloneUrlCommand { get; }
    public ICommand SmartInstallCommand { get; }
    public ICommand ScanProjectCommand { get; }
    public ICommand InstallDependenciesCommand { get; }
    public ICommand BuildProjectCommand { get; }
    public ICommand TestProjectCommand { get; }
    public ICommand RunProjectCommand { get; }
    public ICommand StopProjectCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }
    public ICommand OpenProjectFolderCommand { get; }
    public ICommand OpenPowerShellCommand { get; }
    public ICommand OpenCmdCommand { get; }
    public ICommand OpenTerminalCommand { get; }
    public ICommand OpenAppleTerminalCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RepairProjectCommand { get; }
    public ICommand UpdateProjectCommand { get; }
    public ICommand RunDoctorCommand { get; }

    private void UpdateDetectedCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            DetectedCommand = null;
            DetectedBadge = "";
            return;
        }

        var info = _smartProcessor.ParseInput(input);
        DetectedCommand = info;
        DetectedBadge = info.Kind switch
        {
            CommandInputKind.GitRepositoryUrl => "⚡ GitHub Repo",
            CommandInputKind.GitHubShortHand => "⚡ GitHub Repo (Shorthand)",
            CommandInputKind.GitCloneCommand => "🚀 Git Clone Command",
            CommandInputKind.PowerShellInstallCommand => "💻 PowerShell Install Script",
            CommandInputKind.AppleTerminalInstallCommand => "🍏 Apple Terminal / Bash Script",
            CommandInputKind.PackageManagerInstall => "📦 Package Manager Install",
            CommandInputKind.CompoundCommand => "⛓️ Multi-Step Pipeline",
            _ => "⚙️ Terminal Command"
        };
    }

    private void AddLog(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            if (LogEntries.Count > 500) LogEntries.RemoveAt(LogEntries.Count - 1);
        });
        _logger.Info(message);
    }

    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select GitHub Project Folder"
        };
        if (dialog.ShowDialog() == true)
        {
            ProjectPath = dialog.FolderName;
            AddLog($"Selected folder: {ProjectPath}");
            _ = ScanProject();
        }
    }

    private void OpenZip()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select GitHub ZIP Archive",
            Filter = "ZIP Archives|*.zip|All Files|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            AddLog($"Extracting ZIP: {dialog.FileName}");
            var baseName = Path.GetFileNameWithoutExtension(dialog.FileName);
            var targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mercury", "projects", baseName);

            var result = _zipService.Extract(dialog.FileName, targetDir);
            if (result.Success)
            {
                ProjectPath = result.Detail ?? targetDir;
                AddLog($"Extracted to: {ProjectPath}");
                _ = ScanProject();
            }
            else
            {
                AddLog($"Extraction failed: {result.Message}");
                StatusText = result.Message;
            }
        }
    }

    private async Task ProcessSmartInput()
    {
        if (string.IsNullOrWhiteSpace(GitUrl)) return;
        var info = _smartProcessor.ParseInput(GitUrl);
        DetectedCommand = info;

        if (info.IsDirectRepo && !string.IsNullOrWhiteSpace(info.ExtractedGitUrl))
        {
            await CloneAndSetupRepo(info.ExtractedGitUrl, info.ExtractedRepoName ?? "project");
        }
        else
        {
            await ExecuteDirectCommand(info);
        }
    }

    private async Task CloneAndSetupRepo(string gitUrl, string repoName)
    {
        IsBusy = true;
        StatusText = $"Cloning {repoName}...";
        _cts = new CancellationTokenSource();

        try
        {
            var targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mercury", "projects", repoName);

            if (Directory.Exists(targetDir))
            {
                var result = MessageBox.Show(
                    $"Directory already exists:\n{targetDir}\n\nOverwrite existing project?",
                    "Mercury - Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    IsBusy = false;
                    return;
                }
                Directory.Delete(targetDir, true);
            }

            AddLog($"Cloning repository: {gitUrl}");
            var cloneResult = await _gitService.CloneAsync(gitUrl, targetDir, _cts.Token);

            if (cloneResult.Success)
            {
                ProjectPath = targetDir;
                AddLog($"Cloned successfully to: {ProjectPath}");
                await ScanProject();

                // Auto-install dependencies if detected
                if (ProjectInfo != null && ProjectInfo.PackageManagers.Count > 0)
                {
                    AddLog("Auto-installing detected dependencies...");
                    await InstallDependencies();
                }
                StatusText = $"{repoName} ready to build or run!";
            }
            else
            {
                AddLog($"Clone failed: {cloneResult.Message}");
                StatusText = cloneResult.Message;
            }
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}");
            StatusText = $"Setup failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteDirectCommand(SmartCommandInfo info)
    {
        IsBusy = true;
        StatusText = $"Executing: {info.Description}...";
        _cts = new CancellationTokenSource();
        AddLog($"=== Executing Command ({info.Description}) ===");
        AddLog($"Command: {info.ExecutableCommand}");
        AddLog("Internet and terminal access: ENABLED (Full access granted)");

        var defaultProjectsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Mercury", "projects");
        try { Directory.CreateDirectory(defaultProjectsDir); } catch { }

        var workingDir = !string.IsNullOrWhiteSpace(ProjectPath) && Directory.Exists(ProjectPath)
            ? ProjectPath
            : defaultProjectsDir;

        try
        {
            var result = await _smartProcessor.ExecuteCommandAsync(
                info,
                workingDir,
                onOutput: line => AddLog(line),
                onError: line => AddLog($"[stderr] {line}"),
                ct: _cts.Token);

            if (result.Success)
            {
                StatusText = "Command completed successfully!";
                AddLog($"✓ Command finished successfully in {result.Duration.TotalSeconds:F1}s");
            }
            else
            {
                StatusText = $"Command exited with code {result.ExitCode}";
                AddLog($"✗ Execution failed (exit code {result.ExitCode}): {result.Message}");
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Command cancelled.";
            AddLog("Execution cancelled by user.");
        }
        catch (Exception ex)
        {
            StatusText = $"Execution error: {ex.Message}";
            AddLog($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            _cts = null;
        }
    }

    private void OpenPowerShell()
    {
        var dir = !string.IsNullOrWhiteSpace(ProjectPath) && Directory.Exists(ProjectPath) ? ProjectPath : null;
        var res = _terminalService.OpenTerminalWindow(dir, ShellType.PowerShell);
        AddLog(res.Message);
    }

    private void OpenCmd()
    {
        var dir = !string.IsNullOrWhiteSpace(ProjectPath) && Directory.Exists(ProjectPath) ? ProjectPath : null;
        var res = _terminalService.OpenTerminalWindow(dir, ShellType.Cmd);
        AddLog(res.Message);
    }

    private void OpenTerminal()
    {
        var dir = !string.IsNullOrWhiteSpace(ProjectPath) && Directory.Exists(ProjectPath) ? ProjectPath : null;
        var res = _terminalService.OpenTerminalWindow(dir, ShellType.Auto);
        AddLog(res.Message);
    }

    private void OpenAppleTerminal()
    {
        var dir = !string.IsNullOrWhiteSpace(ProjectPath) && Directory.Exists(ProjectPath) ? ProjectPath : null;
        var res = _terminalService.OpenTerminalWindow(dir, ShellType.AppleTerminal);
        AddLog(res.Message);
    }

    public async Task ScanProject()
    {
        if (string.IsNullOrWhiteSpace(ProjectPath) || !Directory.Exists(ProjectPath)) return;
        IsBusy = true;
        Progress = 0;
        StatusText = "Scanning project...";
        CurrentPage = "Project";
        _cts = new CancellationTokenSource();

        try
        {
            _scanner.OnProgress += msg => Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = msg;
                AddLog(msg);
            });

            var info = await Task.Run(() => _scanner.ScanAsync(ProjectPath, _cts.Token));
            ProjectInfo = info;
            Progress = 50;

            // Update display collections
            Application.Current.Dispatcher.Invoke(() =>
            {
                Technologies.Clear();
                foreach (var t in info.Technologies)
                {
                    Technologies.Add(new TechDisplayItem
                    {
                        Name = t.Name,
                        Framework = t.Framework,
                        Version = t.Version,
                        Status = "âœ“"
                    });
                }

                Services.Clear();
                foreach (var s in info.Services)
                {
                    Services.Add(new ServiceDisplayItem
                    {
                        Name = s.Name,
                        Type = s.Type,
                        Port = s.Port?.ToString() ?? "-",
                        Status = "Detected"
                    });
                }

                EnvironmentVars.Clear();
                foreach (var ev in info.EnvironmentVariables)
                {
                    EnvironmentVars.Add(new EnvVarDisplayItem
                    {
                        Key = ev.Key,
                        IsRequired = ev.IsRequired,
                        IsSecret = ev.IsSecret,
                        Value = ev.IsSecret ? "â€¢â€¢â€¢â€¢â€¢â€¢â€¢â€¢" : (ev.DefaultValue ?? ""),
                        Status = ev.IsRequired ? "Required" : "Optional"
                    });
                }
            });

            // Security scan
            StatusText = "Running security scan...";
            AddLog("Running security scan...");
            SecurityResult = await Task.Run(() => _securityScanner.Scan(ProjectPath));
            Progress = 75;

            if (SecurityResult.CriticalCount > 0)
                AddLog($"âš  Security scan found {SecurityResult.CriticalCount} critical issue(s)");
            else
                AddLog("âœ“ Security scan passed");

            // Environment scan
            StatusText = "Checking Windows environment...";
            AddLog("Scanning Windows environment...");
            EnvInfo = await Task.Run(() => _envScanner.Scan());
            Progress = 90;

            // Update runtime statuses
            foreach (var runtime in info.Runtimes)
            {
                var status = _envScanner.CheckRuntime(runtime.Name, runtime.RequiredVersion, EnvInfo);
                runtime.Status = status;
                if (EnvInfo.Runtimes.TryGetValue(runtime.Name, out var rtInfo))
                    runtime.InstalledVersion = rtInfo.Version;
            }

            // Save profile
            var profile = _profileService.CreateProfile(info);
            _profileService.SaveProfile(profile, ProjectPath);
            AddLog("Project profile saved.");

            Progress = 100;
            StatusText = $"Scan complete. {info.Technologies.Count} technology(ies) detected.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Scan cancelled.";
            AddLog("Scan cancelled by user.");
        }
        catch (Exception ex)
        {
            StatusText = $"Scan error: {ex.Message}";
            AddLog($"Scan error: {ex.Message}");
            _logger.Error("Scan failed", ex.ToString());
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InstallDependencies()
    {
        if (ProjectInfo == null) return;
        IsBusy = true;
        StatusText = "Installing dependencies...";
        _cts = new CancellationTokenSource();

        try
        {
            foreach (var pm in ProjectInfo.PackageManagers)
            {
                AddLog($"Running: {pm.InstallCommand}");
                StatusText = $"Installing {pm.Name} dependencies...";

                _processRunner.OnStdOut += line => AddLog($"  {line}");
                _processRunner.OnStdErr += line => AddLog($"  [err] {line}");

                var result = await _processRunner.RunShellAsync(pm.InstallCommand, ProjectPath, ct: _cts.Token);

                if (result.Success)
                    AddLog($"âœ“ {pm.Name} dependencies installed ({result.Duration.TotalSeconds:F1}s)");
                else
                {
                    AddLog($"âœ— {pm.Name} failed: {result.Message}");
                    StatusText = $"Dependency installation failed: {result.Message}";

                    if (result.ExitCode.HasValue)
                        AddLog($"  Exit code: {result.ExitCode}");
                    if (!string.IsNullOrWhiteSpace(result.StdErr))
                        AddLog($"  Error: {result.StdErr?.Substring(0, Math.Min(result.StdErr.Length, 500))}");
                    return;
                }
            }

            StatusText = "All dependencies installed successfully.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Installation cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Installation error: {ex.Message}";
            AddLog($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task BuildProject()
    {
        if (ProjectInfo?.BuildSystem == null) return;
        IsBusy = true;
        StatusText = $"Building: {ProjectInfo.BuildSystem}";
        AddLog($"Running build: {ProjectInfo.BuildSystem}");

        try
        {
            _processRunner.OnStdOut += line => AddLog($"  {line}");
            var result = await _processRunner.RunShellAsync(ProjectInfo.BuildSystem, ProjectPath);

            if (result.Success)
            {
                StatusText = $"Build succeeded ({result.Duration.TotalSeconds:F1}s)";
                AddLog($"âœ“ Build complete in {result.Duration.TotalSeconds:F1}s");
            }
            else
            {
                StatusText = $"Build failed (exit code {result.ExitCode})";
                AddLog($"âœ— Build failed: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Build error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TestProject()
    {
        if (ProjectInfo?.TestSystem == null) return;
        IsBusy = true;
        StatusText = $"Testing: {ProjectInfo.TestSystem}";
        AddLog($"Running tests: {ProjectInfo.TestSystem}");

        try
        {
            _processRunner.OnStdOut += line => AddLog($"  {line}");
            var result = await _processRunner.RunShellAsync(ProjectInfo.TestSystem, ProjectPath);

            if (result.Success)
            {
                StatusText = $"Tests passed ({result.Duration.TotalSeconds:F1}s)";
                AddLog($"âœ“ Tests passed in {result.Duration.TotalSeconds:F1}s");
            }
            else
            {
                StatusText = $"Tests failed (exit code {result.ExitCode})";
                AddLog($"âœ— Tests failed: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Test error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunProject()
    {
        if (ProjectInfo?.PossibleRunCommands?.Any() != true) return;
        var runCmd = ProjectInfo.PossibleRunCommands.First();
        IsBusy = true;
        _cts = new CancellationTokenSource();
        StatusText = $"Running: {runCmd}";
        AddLog($"Starting project: {runCmd}");

        try
        {
            _processRunner.OnStdOut += line => AddLog($"  {line}");
            _processRunner.OnStdErr += line => AddLog($"  [err] {line}");
            var result = await _processRunner.RunShellAsync(runCmd, ProjectPath, ct: _cts.Token, timeoutMs: 0);
            StatusText = result.Success ? "Project stopped." : $"Project exited: {result.Message}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Project stopped.";
            AddLog("Project stopped by user.");
        }
        finally
        {
            IsBusy = false;
            _cts = null;
        }
    }

    private void StopProject()
    {
        _cts?.Cancel();
    }

    private void Cancel()
    {
        _cts?.Cancel();
    }

    private void OpenLogsFolder()
    {
        var logDir = AppLogger.GetLogDirectory();
        if (Directory.Exists(logDir))
            System.Diagnostics.Process.Start("explorer.exe", logDir);
    }

    private void OpenProjectFolder()
    {
        if (!string.IsNullOrWhiteSpace(ProjectPath) && Directory.Exists(ProjectPath))
            System.Diagnostics.Process.Start("explorer.exe", ProjectPath);
    }

    private async Task RepairProject()
    {
        if (ProjectInfo == null) return;
        AddLog("Starting project repair...");
        StatusText = "Repairing project...";
        IsBusy = true;

        try
        {
            // Re-scan
            await ScanProject();

            // Re-install dependencies
            await InstallDependencies();

            StatusText = "Repair complete.";
            AddLog("âœ“ Repair complete.");
        }
        catch (Exception ex)
        {
            StatusText = $"Repair failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateProject()
    {
        if (ProjectInfo?.GitRemote == null) return;
        IsBusy = true;
        StatusText = "Checking for updates...";
        AddLog("Checking for updates...");

        try
        {
            bool hasChanges = await _gitService.HasLocalChangesAsync(ProjectPath);
            if (hasChanges)
            {
                var result = MessageBox.Show(
                    "Local uncommitted changes detected.\n\nPull anyway? (Changes will be preserved via merge.)",
                    "Local Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    StatusText = "Update cancelled.";
                    IsBusy = false;
                    return;
                }
            }

            var pullResult = await _gitService.PullAsync(ProjectPath);
            if (pullResult.Success)
            {
                AddLog("✓ Repository updated.");
                await ScanProject(); // Re-scan for dependency changes
            }
            else
            {
                AddLog($"✗ Update failed: {pullResult.Message}");
                StatusText = $"Update failed: {pullResult.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Update error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunDoctor()
    {
        IsBusy = true;
        CurrentPage = "Diagnostics";
        StatusText = "Running diagnostics...";
        AddLog("=== System Diagnostics ===");

        try
        {
            EnvInfo = await Task.Run(() => _envScanner.Scan());

            AddLog($"Windows: {EnvInfo.WindowsVersion}");
            AddLog($"Architecture: {EnvInfo.Architecture}");
            AddLog($"Disk Space: {EnvInfo.AvailableDiskSpaceMB} MB available");
            AddLog($"RAM: {EnvInfo.TotalRamMB} MB");
            AddLog($"Git: {(EnvInfo.HasGit ? EnvInfo.GitVersion : "NOT INSTALLED")}");
            AddLog($"winget: {(EnvInfo.HasWinget ? "Available" : "Not found")}");

            foreach (var (name, info) in EnvInfo.Runtimes)
            {
                var status = info.Installed ? $"✓ {info.Version}" : "✗ Not installed";
                AddLog($"{name}: {status}");
            }

            StatusText = "Diagnostics complete.";
        }
        catch (Exception ex)
        {
            StatusText = $"Diagnostics error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Handle drag-drop
    public async Task HandleDropAsync(string[] paths)
    {
        if (paths.Length == 0) return;
        var path = paths[0];

        if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var baseName = Path.GetFileNameWithoutExtension(path);
            var targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mercury", "projects", baseName);
            var result = _zipService.Extract(path, targetDir);
            if (result.Success)
            {
                ProjectPath = result.Detail ?? targetDir;
                await ScanProject();
            }
        }
        else if (Directory.Exists(path))
        {
            ProjectPath = path;
            await ScanProject();
        }
        else if (!string.IsNullOrWhiteSpace(path))
        {
            // Support URLs, shorthand repos, or install commands passed via command line or dropped text
            GitUrl = path.Trim();
            CurrentPage = "Home";
        }
    }
}

// Display item classes
public class TechDisplayItem
{
    public string Name { get; set; } = "";
    public string? Framework { get; set; }
    public string? Version { get; set; }
    public string Status { get; set; } = "";
    public string DisplayName => Framework != null ? $"{Name} ({Framework})" : Name;
}

public class ServiceDisplayItem
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Port { get; set; } = "";
    public string Status { get; set; } = "";
}

public class EnvVarDisplayItem
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsRequired { get; set; }
    public bool IsSecret { get; set; }
    public string Status { get; set; } = "";
}
