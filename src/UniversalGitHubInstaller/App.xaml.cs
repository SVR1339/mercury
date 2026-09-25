using System;
using System.IO;
using System.Windows;

namespace UniversalGitHubInstaller;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handlers
        DispatcherUnhandledException += (s, args) =>
        {
            LogError("DispatcherUnhandledException", args.Exception);
            MessageBox.Show(
                $"An error occurred:\n\n{args.Exception.Message}\n\nSee log for details.",
                "Universal GitHub Project Installer",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                LogError("UnhandledException", ex);
        };

        base.OnStartup(e);

        // Explicitly create and show the main window
        try
        {
            var mainWindow = new Views.MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            LogError("WindowCreation", ex);
            MessageBox.Show(
                $"Failed to create main window:\n\n{ex.Message}",
                "Universal GitHub Project Installer",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void LogError(string source, Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UniversalGitHubProjectInstaller", "logs");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, "crash.log");
            File.AppendAllText(logFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n{ex}\n\n");
        }
        catch { /* last resort */ }
    }
}
