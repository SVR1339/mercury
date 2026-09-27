using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UniversalGitHubInstaller.ViewModels;

namespace UniversalGitHubInstaller.Views;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        TrySetWindowIcon();
    }

    private void TrySetWindowIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/assets/icon.png", UriKind.RelativeOrAbsolute);
            Icon = System.Windows.Media.Imaging.BitmapFrame.Create(uri);
        }
        catch { /* Fallback: no icon */ }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (args.Length > 0)
            await ViewModel.HandleDropAsync(args);
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || PageHome == null) return;

        var pages = new[]
        {
            PageHome, PageProject, PageEnvironment,
            PageSecurity, PageBuild, PageLogs, PageDiagnostics, PageSettings
        };
        foreach (var page in pages)
            if (page != null) page.Visibility = Visibility.Collapsed;

        var pageName = rb.Name.Replace("Nav", "");
        var target = pageName switch
        {
            "Home"        => PageHome,
            "Project"     => PageProject,
            "Environment" => PageEnvironment,
            "Security"    => PageSecurity,
            "Build"       => PageBuild,
            "Logs"        => PageLogs,
            "Diagnostics" => PageDiagnostics,
            "Settings"    => PageSettings,
            _             => PageHome
        };

        if (target != null)
            target.Visibility = Visibility.Visible;
    }

    /// <summary>Navigate to the Activity Log tab programmatically.</summary>
    internal void NavLogs_Click(object sender, RoutedEventArgs e)
    {
        NavLogs.IsChecked = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            await ViewModel.HandleDropAsync(files);
        }
        else if (e.Data.GetDataPresent(DataFormats.Text))
        {
            var text = (string)e.Data.GetData(DataFormats.Text)!;
            if (!string.IsNullOrWhiteSpace(text))
                await ViewModel.HandleDropAsync(new[] { text.Trim() });
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                     e.Data.GetDataPresent(DataFormats.Text))
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }
}
