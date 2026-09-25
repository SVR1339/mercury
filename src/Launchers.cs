using System;
using System.Diagnostics;
using System.IO;

class Launcher
{
    /// <summary>Find the best PowerShell executable available on this system.</summary>
    static string FindPowerShell()
    {
        // Prefer pwsh.exe (PowerShell 7+) — it's more reliable on modern Windows
        foreach (string candidate in new[] {
            "pwsh.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
            "powershell.exe"
        })
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "-NoProfile -Command \"exit 0\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var p = Process.Start(psi);
                p.WaitForExit(5000);
                if (p.ExitCode == 0) return candidate;
            }
            catch { }
        }
        return "pwsh.exe"; // fallback
    }

    static int Main(string[] args)
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string psExe = FindPowerShell();

#if GUI_LAUNCHER
        string ps1 = Path.Combine(exeDir, "Universal-GitHub-GUI.ps1");
        string extra = args.Length > 0 ? " -InitialPath \"" + args[0] + "\"" : "";
        var psi = new ProcessStartInfo
        {
            FileName = psExe,
            Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + ps1 + "\"" + extra,
            WorkingDirectory = exeDir,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        try { Process.Start(psi); }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                "Failed to launch Universal GitHub Installer.\n\n" +
                "PowerShell (" + psExe + ") could not start.\n\n" +
                "Error: " + ex.Message + "\n\n" +
                "Please ensure PowerShell 7 (pwsh) is installed:\n" +
                "  winget install Microsoft.PowerShell",
                "Launch Error", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
            return 1;
        }
        return 0;
#else
        string ps1 = Path.Combine(exeDir, "Universal-GitHub-Installer.ps1");
        string allArgs = string.Join(" ", args);
        var psi2 = new ProcessStartInfo
        {
            FileName = psExe,
            Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + ps1 + "\" " + allArgs,
            WorkingDirectory = exeDir,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        try
        {
            var p = Process.Start(psi2);
            p.WaitForExit();
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Launch failed: " + ex.Message);
            return 1;
        }
#endif
    }
}
