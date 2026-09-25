using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

/// <summary>
/// Universal GitHub Installer — Native Windows Setup.exe
/// Extracts embedded payload.zip to %LOCALAPPDATA%\UniversalGitHubInstaller
/// then launches Universal-GitHub-GUI.ps1 via the pre-compiled GUI launcher EXE.
/// </summary>
class Setup
{
    const string APP_NAME = "Universal GitHub Installer";
    const string INSTALL_DIR_NAME = "UniversalGitHubInstaller";
    const string REG_UNINSTALL_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + INSTALL_DIR_NAME;

    static string installDir;

    [STAThread]
    static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool silent = Array.Exists(args, a => a.Equals("/silent", StringComparison.OrdinalIgnoreCase)
                                            || a.Equals("/s", StringComparison.OrdinalIgnoreCase));
        bool uninstall = Array.Exists(args, a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase));

        installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), INSTALL_DIR_NAME);

        if (uninstall)
        {
            return DoUninstall(silent);
        }

        // --- Interactive or Silent Install ---
        if (!silent)
        {
            var result = MessageBox.Show(
                APP_NAME + " will be installed to:\n\n" + installDir +
                "\n\nThis will install the GitHub project installer tool, letting you download, build, " +
                "and run any GitHub repository as a native Windows application.\n\nProceed?",
                APP_NAME + " Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return 1;
        }

        try
        {
            // 1. Create install directory
            if (!Directory.Exists(installDir))
                Directory.CreateDirectory(installDir);

            // 2. Extract embedded payload.zip
            ExtractPayload(installDir);

            // 3. Create Desktop shortcut
            CreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), APP_NAME + ".lnk"),
                Path.Combine(installDir, "Universal-GitHub-GUI.exe"),
                installDir,
                Path.Combine(installDir, "app.ico")
            );

            // 4. Create Start Menu shortcut
            string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "GitHub Apps");
            if (!Directory.Exists(startMenuDir))
                Directory.CreateDirectory(startMenuDir);
            CreateShortcut(
                Path.Combine(startMenuDir, APP_NAME + ".lnk"),
                Path.Combine(installDir, "Universal-GitHub-GUI.exe"),
                installDir,
                Path.Combine(installDir, "app.ico")
            );

            // 5. Register in Windows Installed Apps (Add/Remove Programs)
            RegisterUninstaller();

            // 6. Run post-install setup script if exists
            string setupScript = Path.Combine(installDir, "Setup-UniversalGitHubInstaller.ps1");
            if (File.Exists(setupScript))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + setupScript + "\"",
                    WorkingDirectory = installDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                try
                {
                    var p = Process.Start(psi);
                    p.WaitForExit(30000);
                }
                catch { }
            }

            if (!silent)
            {
                var launchResult = MessageBox.Show(
                    APP_NAME + " has been installed successfully!\n\n" +
                    "Install location: " + installDir + "\n" +
                    "Desktop shortcut and Start Menu entry created.\n\n" +
                    "Launch " + APP_NAME + " now?",
                    "Installation Complete", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (launchResult == DialogResult.Yes)
                {
                    LaunchApp();
                }
            }
            else
            {
                LaunchApp();
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                MessageBox.Show("Installation failed:\n\n" + ex.Message + "\n\n" + ex.StackTrace,
                    APP_NAME + " Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return 2;
        }
    }

    static void ExtractPayload(string targetDir)
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        string resourceName = null;

        foreach (string name in asm.GetManifestResourceNames())
        {
            if (name.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("payload.zip", StringComparison.OrdinalIgnoreCase))
            {
                resourceName = name;
                break;
            }
        }

        if (resourceName == null)
            throw new FileNotFoundException("Embedded payload.zip resource not found in Setup.exe.");

        string tempZip = Path.Combine(Path.GetTempPath(), "UGI_payload_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".zip");

        try
        {
            using (Stream resStream = asm.GetManifestResourceStream(resourceName))
            using (FileStream fs = File.Create(tempZip))
            {
                resStream.CopyTo(fs);
            }

            // Extract with overwrite
            using (ZipArchive archive = ZipFile.OpenRead(tempZip))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fullPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));

                    // Security: Prevent directory traversal
                    if (!fullPath.StartsWith(Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        // Directory entry
                        Directory.CreateDirectory(fullPath);
                    }
                    else
                    {
                        string dir = Path.GetDirectoryName(fullPath);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        entry.ExtractToFile(fullPath, overwrite: true);
                    }
                }
            }
        }
        finally
        {
            try { File.Delete(tempZip); } catch { }
        }
    }

    static void CreateShortcut(string lnkPath, string targetExe, string workingDir, string iconPath)
    {
        try
        {
            // Use WScript.Shell COM for shortcut creation
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell.CreateShortcut(lnkPath);
            shortcut.TargetPath = targetExe;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Description = APP_NAME + " - Install and manage GitHub projects on Windows";
            if (File.Exists(iconPath))
                shortcut.IconLocation = iconPath + ",0";
            shortcut.Save();
        }
        catch { }
    }

    static void RegisterUninstaller()
    {
        try
        {
            string uninstallExe = Path.Combine(installDir, "Universal-GitHub-GUI.exe");
            string setupExe = Assembly.GetExecutingAssembly().Location;
            string uninstallCmd = "\"" + setupExe + "\" /uninstall";
            string icoPath = Path.Combine(installDir, "app.ico");

            // Copy setup.exe into install directory for uninstall support
            string localSetup = Path.Combine(installDir, "Uninstall.exe");
            try { File.Copy(setupExe, localSetup, true); } catch { }

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_UNINSTALL_KEY))
            {
                key.SetValue("DisplayName", APP_NAME);
                key.SetValue("UninstallString", "\"" + localSetup + "\" /uninstall");
                key.SetValue("InstallLocation", installDir);
                key.SetValue("DisplayIcon", icoPath);
                key.SetValue("Publisher", "Universal GitHub Installer Project");
                key.SetValue("DisplayVersion", "2.0.0");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);

                // Calculate approximate size in KB
                long totalSize = 0;
                try
                {
                    foreach (string f in Directory.GetFiles(installDir, "*", SearchOption.AllDirectories))
                        totalSize += new FileInfo(f).Length;
                }
                catch { }
                key.SetValue("EstimatedSize", (int)(totalSize / 1024), RegistryValueKind.DWord);
            }
        }
        catch { }
    }

    static int DoUninstall(bool silent)
    {
        if (!silent)
        {
            var result = MessageBox.Show(
                "Are you sure you want to completely uninstall " + APP_NAME + "?\n\n" +
                "This will remove all program files, shortcuts, and registry entries.\n" +
                "(Your installed GitHub apps will NOT be removed.)",
                "Uninstall " + APP_NAME, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return 1;
        }

        try
        {
            // Remove Desktop shortcut
            string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), APP_NAME + ".lnk");
            if (File.Exists(desktopLnk)) File.Delete(desktopLnk);

            // Remove Start Menu shortcut
            string startLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs", "GitHub Apps", APP_NAME + ".lnk");
            if (File.Exists(startLnk)) File.Delete(startLnk);

            // Remove Start Menu folder if empty
            string startDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs", "GitHub Apps");
            try { if (Directory.Exists(startDir) && Directory.GetFiles(startDir).Length == 0) Directory.Delete(startDir); } catch { }

            // Remove registry entry
            try { Registry.CurrentUser.DeleteSubKeyTree(REG_UNINSTALL_KEY, false); } catch { }

            // Schedule self-deletion via cmd (files might be locked)
            if (Directory.Exists(installDir))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c timeout /t 2 /nobreak >nul & rd /s /q \"" + installDir + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
            }

            if (!silent)
            {
                MessageBox.Show(APP_NAME + " has been uninstalled successfully.\n\nYour installed GitHub apps remain untouched.",
                    "Uninstall Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (!silent)
                MessageBox.Show("Uninstall error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 2;
        }
    }

    static void LaunchApp()
    {
        string guiExe = Path.Combine(installDir, "Universal-GitHub-GUI.exe");
        if (File.Exists(guiExe))
        {
            var psi = new ProcessStartInfo
            {
                FileName = guiExe,
                WorkingDirectory = installDir,
                UseShellExecute = true
            };
            try { Process.Start(psi); } catch { }
        }
    }
}
