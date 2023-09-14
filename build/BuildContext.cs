using Cake.Common.Build;
using Cake.Common.Tools.DotNet;
using Cake.Common;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.IO;

public class BuildContext : FrostingContext
{
    public string MsBuildConfiguration => this.ArgumentOrEnvironment("configuration", Constants.DefaultBuildConfiguration);
    public string GitHubSecretToken => this.ArgumentOrEnvironment<string>("GITHUB_TOKEN");
    public string SigningKeyPass => this.ArgumentOrEnvironment<string>("SigningKeyPass");

    public string VersionName => this.Argument("appVersion", "");

    public DirectoryPath RootDirectory { get; }
    public FilePath SolutionFile { get; }
    public string ProjectName { get; } = "RacingCarsControllerAndroid";
    public DirectoryPath ArtifactsDirectory { get; }
    public DotNetVerbosity Verbosity =>
        Enum.Parse<DotNetVerbosity>(ArgumentOrEnvironment("LogVerbosity", DotNetVerbosity.Minimal.ToString()));

    public bool IsRunningInCI
        => this.GitHubActions()?.IsRunningOnGitHubActions ?? false;

    public BuildContext(ICakeContext context)
        : base(context)
    {
        if (this.IsRunningInCI)
        {
            RootDirectory = this.GitHubActions().Environment.Workflow.Workspace;
        }
        else
        {
            RootDirectory = this.Environment.WorkingDirectory.GetParent();
        }

        SolutionFile = RootDirectory.CombineWithFilePath("YouTubeStreamsExtractor.sln");
        ArtifactsDirectory = RootDirectory.Combine("artifacts");

        Log.Information($"{nameof(RootDirectory)} {RootDirectory}");
        Log.Information($"{nameof(SolutionFile)} {SolutionFile}");
        Log.Information($"{ProjectNameToPath(ProjectName).FullPath}");
        Log.Information($"{nameof(ArtifactsDirectory)} {ArtifactsDirectory}");
        Log.Information($"{nameof(VersionName)} {VersionName}");

        Log.Information($"{nameof(SigningKeyPass)} {SigningKeyPass?.Length ?? 0}");
        Log.Information($"{nameof(GitHubSecretToken)} {GitHubSecretToken?.Length ?? 0}");
        Log.Information($"{nameof(Verbosity)} {Verbosity}");
    }

    public FilePath ProjectNameToPath(string projectName)
    {
        return RootDirectory.Combine(projectName).CombineWithFilePath($"{projectName}.csproj");
    }

    public T ArgumentOrEnvironment<T>(string name, T defaultValue = default)
        => this.HasArgument(name) ? this.Argument<T>(name) : this.EnvironmentVariable<T>(name, defaultValue);

    public string GetApplicationId()
    {
        var projectPath = ProjectNameToPath(ProjectName);
        var regex = new Regex("<ApplicationId>([a-zA-Z0-9_.]+)</ApplicationId>");
        var text = File.ReadAllText(projectPath.FullPath);
        var applicationId = regex.Match(text).Groups[1].Value;
        return applicationId;
    }
}
