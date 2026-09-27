using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UniversalGitHubInstaller.Core.Security;

namespace UniversalGitHubInstaller.Views;

/// <summary>
/// Professional consent dialog for medium/high-risk operations.
/// Presents threat findings, risk level, and requires explicit user confirmation.
/// </summary>
public class SafetyConsentDialog : Window
{
    private bool _confirmed;

    public static bool RequestConsent(SafetyAssessment assessment, string operationDescription, Window? owner = null)
    {
        var dlg = new SafetyConsentDialog(assessment, operationDescription);
        if (owner != null) dlg.Owner = owner;
        dlg.ShowDialog();
        return dlg._confirmed;
    }

    private SafetyConsentDialog(SafetyAssessment assessment, string operationDescription)
    {
        Title                  = "Mercury — Security Review Required";
        Width                  = 560;
        SizeToContent          = SizeToContent.Height;
        ResizeMode             = ResizeMode.NoResize;
        WindowStartupLocation  = WindowStartupLocation.CenterOwner;
        Background             = new SolidColorBrush(Color.FromRgb(0x0A, 0x0F, 0x1E));

        var root = new StackPanel { Margin = new Thickness(32, 28, 32, 24) };

        // Risk badge
        var badgeColor = assessment.Risk switch
        {
            RiskLevel.Critical => Color.FromRgb(0xEF, 0x44, 0x44),
            RiskLevel.High     => Color.FromRgb(0xF5, 0x9E, 0x0B),
            RiskLevel.Medium   => Color.FromRgb(0x38, 0xBD, 0xF8),
            _                  => Color.FromRgb(0x10, 0xB9, 0x81)
        };

        var badge = new Border
        {
            Background       = new SolidColorBrush(Color.FromArgb(40, badgeColor.R, badgeColor.G, badgeColor.B)),
            BorderBrush      = new SolidColorBrush(badgeColor),
            BorderThickness  = new Thickness(1),
            CornerRadius     = new CornerRadius(6),
            Padding          = new Thickness(12, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin           = new Thickness(0, 0, 0, 16)
        };
        badge.Child = new TextBlock
        {
            Text       = $"Risk Level: {assessment.Risk.ToString().ToUpper()}",
            Foreground = new SolidColorBrush(badgeColor),
            FontWeight = FontWeights.Bold,
            FontSize   = 12
        };
        root.Children.Add(badge);

        // Title
        root.Children.Add(new TextBlock
        {
            Text       = "Security Review Required",
            FontSize   = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)),
            Margin     = new Thickness(0, 0, 0, 8)
        });

        // Operation
        root.Children.Add(new TextBlock
        {
            Text         = operationDescription,
            FontSize     = 13,
            Foreground   = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            TextWrapping = TextWrapping.Wrap,
            Margin       = new Thickness(0, 0, 0, 20)
        });

        // Findings
        if (assessment.Findings.Count > 0)
        {
            root.Children.Add(new TextBlock
            {
                Text       = "Detected Concerns",
                FontSize   = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)),
                Margin     = new Thickness(0, 0, 0, 8)
            });

            var findingsBox = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0x11, 0x1D, 0x30)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x5F)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(16, 12, 16, 12),
                Margin          = new Thickness(0, 0, 0, 20)
            };

            var findingsPanel = new StackPanel();
            foreach (var finding in assessment.Findings)
            {
                findingsPanel.Children.Add(new TextBlock
                {
                    Text         = finding,
                    FontSize     = 12,
                    FontFamily   = new FontFamily("Consolas"),
                    Foreground   = new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin       = new Thickness(0, 2, 0, 2)
                });
            }
            findingsBox.Child = findingsPanel;
            root.Children.Add(findingsBox);
        }

        // Summary
        root.Children.Add(new TextBlock
        {
            Text         = assessment.Summary,
            FontSize     = 12,
            Foreground   = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            TextWrapping = TextWrapping.Wrap,
            Margin       = new Thickness(0, 0, 0, 24)
        });

        // Blocked message
        if (assessment.IsBlocked)
        {
            root.Children.Add(new TextBlock
            {
                Text         = "This operation has been BLOCKED by Mercury's safety policy. It cannot be executed.",
                FontSize     = 13,
                FontWeight   = FontWeights.SemiBold,
                Foreground   = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 0, 0, 20)
            });

            var okBtn = CreateButton("Close", Color.FromRgb(0x1E, 0x3A, 0x5F));
            okBtn.Click += (_, _) => { _confirmed = false; Close(); };
            root.Children.Add(okBtn);
        }
        else
        {
            // Consent buttons
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var cancelBtn = CreateButton("Cancel Operation", Color.FromRgb(0x1E, 0x3A, 0x5F));
            cancelBtn.Margin = new Thickness(0, 0, 12, 0);
            cancelBtn.Click += (_, _) => { _confirmed = false; Close(); };

            var proceedBtn = CreateButton(
                assessment.Risk >= RiskLevel.High ? "Proceed — I Accept the Risk" : "Proceed",
                Color.FromRgb(0x02, 0x84, 0xC7));
            proceedBtn.Click += (_, _) => { _confirmed = true; Close(); };

            btnRow.Children.Add(cancelBtn);
            btnRow.Children.Add(proceedBtn);
            root.Children.Add(btnRow);
        }

        Content = root;
    }

    private static Button CreateButton(string text, Color bgColor) => new()
    {
        Content         = text,
        Background      = new SolidColorBrush(bgColor),
        Foreground      = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)),
        BorderThickness = new Thickness(0),
        Padding         = new Thickness(20, 10, 20, 10),
        FontSize        = 13,
        FontWeight      = FontWeights.SemiBold,
        Cursor          = System.Windows.Input.Cursors.Hand
    };
}
