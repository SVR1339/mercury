using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Runtime;

public class WindowsEnvironmentScanner
{
    public WindowsEnvironmentInfo Scan()
    {
        var info = new WindowsEnvironmentInfo
        {
            WindowsVersion = Environment.OSVersion.VersionString,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            CpuArchitecture = RuntimeInformation.ProcessArchitecture.ToString()
        };

        // Disk space
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) ?? "C:");
            info.AvailableDiskSpaceMB = drive.AvailableFreeSpace / (1024 * 1024);
        }
        catch { }

        // RAM
        try
        {
            info.TotalRamMB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);
        }
        catch { }

        // Git
        var gitVer = GetCommandVersion("git", "--version");
        info.HasGit = gitVer != null;
        info.GitVersion = gitVer;

        // PowerShell
        info.HasPowerShell = GetCommandVersion("powershell", "-NoProfile -Command $PSVersionTable.PSVersion.ToString()") != null;

        // winget
        info.HasWinget = GetCommandVersion("winget", "--version") != null;

        // Chocolatey
        info.HasChocolatey = GetCommandVersion("choco", "--version") != null;

        // Scoop
        info.HasScoop = GetCommandVersion("scoop", "--version") != null;

        // Check common runtimes
        var runtimes = new Dictionary<string, (string cmd, string args)>
        {
            ["Python"] = ("python", "--version"),
            ["Node.js"] = ("node", "--version"),
            ["npm"] = ("npm", "--version"),
            ["pnpm"] = ("pnpm", "--version"),
            ["yarn"] = ("yarn", "--version"),
            ["bun"] = ("bun", "--version"),
            [".NET"] = ("dotnet", "--version"),
            ["Java"] = ("java", "--version"),
            ["Rust"] = ("rustc", "--version"),
            ["Cargo"] = ("cargo", "--version"),
            ["Go"] = ("go", "version"),
            ["PHP"] = ("php", "--version"),
            ["Composer"] = ("composer", "--version"),
            ["Ruby"] = ("ruby", "--version"),
            ["Docker"] = ("docker", "--version"),
            ["Docker Compose"] = ("docker", "compose version"),
            ["CMake"] = ("cmake", "--version"),
        };

        foreach (var (name, (cmd, args)) in runtimes)
        {
            var ver = GetCommandVersion(cmd, args);
            info.Runtimes[name] = new RuntimeInfo
            {
                Installed = ver != null,
                Version = ver,
                Path = GetCommandPath(cmd)
            };
        }

        return info;
    }

    public RuntimeStatus CheckRuntime(string name, string? requiredVersion, WindowsEnvironmentInfo env)
    {
        if (!env.Runtimes.TryGetValue(name, out var runtime))
            return RuntimeStatus.Unknown;

        if (!runtime.Installed)
            return RuntimeStatus.Missing;

        if (requiredVersion != null && runtime.Version != null)
        {
            // Simple version compatibility check
            // This is a basic implementation; real version comparison would be more sophisticated
            return RuntimeStatus.Installed;
        }

        return RuntimeStatus.Installed;
    }

    private string? GetCommandVersion(string command, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(command, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            var stderr = proc.StandardError.ReadToEnd().Trim();
            proc.WaitForExit(10000);
            if (proc.ExitCode != 0 && string.IsNullOrEmpty(output))
                output = stderr; // Some tools write version to stderr
            return string.IsNullOrEmpty(output) ? null : output.Split('\n')[0].Trim();
        }
        catch { return null; }
    }

    private string? GetCommandPath(string command)
    {
        try
        {
            var psi = new ProcessStartInfo("where", command)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(5000);
            return string.IsNullOrEmpty(output) ? null : output.Split('\n')[0].Trim();
        }
        catch { return null; }
    }
}
