using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniversalGitHubInstaller.Core.Models;

namespace UniversalGitHubInstaller.Core.Detection;

public class JavaDetector : IProjectDetector
{
    public string Name => "Java";
    public int Priority => 60;

    public Task<DetectionResult> DetectAsync(string projectPath, CancellationToken ct = default)
    {
        var result = new DetectionResult();
        bool isMaven = File.Exists(Path.Combine(projectPath, "pom.xml"));
        bool isGradle = File.Exists(Path.Combine(projectPath, "build.gradle")) ||
                        File.Exists(Path.Combine(projectPath, "build.gradle.kts"));

        if (!isMaven && !isGradle)
        {
            result.Detected = false;
            return Task.FromResult(result);
        }

        result.Detected = true;
        var evidence = new List<string>();

        bool hasMvnw = File.Exists(Path.Combine(projectPath, "mvnw.cmd")) ||
                       File.Exists(Path.Combine(projectPath, "mvnw"));
        bool hasGradlew = File.Exists(Path.Combine(projectPath, "gradlew.bat")) ||
                          File.Exists(Path.Combine(projectPath, "gradlew"));

        string buildTool, installCmd, buildCmd, testCmd;

        if (isMaven)
        {
            evidence.Add("pom.xml");
            buildTool = hasMvnw ? "Maven (wrapper)" : "Maven";
            var mvn = hasMvnw ? ".\\mvnw.cmd" : "mvn";
            installCmd = $"{mvn} dependency:resolve";
            buildCmd = $"{mvn} package -DskipTests";
            testCmd = $"{mvn} test";
        }
        else
        {
            evidence.Add(File.Exists(Path.Combine(projectPath, "build.gradle")) ? "build.gradle" : "build.gradle.kts");
            buildTool = hasGradlew ? "Gradle (wrapper)" : "Gradle";
            var gradle = hasGradlew ? ".\\gradlew.bat" : "gradle";
            installCmd = $"{gradle} dependencies";
            buildCmd = $"{gradle} build -x test";
            testCmd = $"{gradle} test";
        }

        result.Technology = new DetectedTechnology
        {
            Name = "Java",
            Category = TechCategory.Language,
            EvidenceFiles = evidence
        };

        result.Runtime = new DetectedRuntime { Name = "Java", RequiredVersion = "17+", Status = RuntimeStatus.Unknown };
        result.PackageManager = new DetectedPackageManager
        {
            Name = buildTool,
            ManifestFile = evidence[0],
            InstallCommand = installCmd
        };

        result.BuildCommand = buildCmd;
        result.TestCommand = testCmd;

        return Task.FromResult(result);
    }
}
