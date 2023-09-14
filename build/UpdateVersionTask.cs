using Cake.Core.Diagnostics;
using Cake.Frosting;
using System.IO;
using System.Text.RegularExpressions;

[TaskName("UpdateVersion")]
public sealed class UpdateVersionTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var projectPath = context.ProjectNameToPath(context.ProjectName);

        var versionCodeRegex = new Regex("<ApplicationVersion>(\\d+)</ApplicationVersion>");
        var text = File.ReadAllText(projectPath.FullPath);
        var versionCode = versionCodeRegex.Match(text).Groups[1].Value;
        var newVersionCode = int.Parse(versionCode) + 1;
        text = versionCodeRegex.Replace(text, $"<ApplicationVersion>{newVersionCode}</ApplicationVersion>");
        context.Log.Information($"Version code updated from: {versionCode} to: {newVersionCode}");

        if (!string.IsNullOrEmpty(context.VersionName))
        {
            var versionNameRegex = new Regex("<ApplicationDisplayVersion>([0-9a-zA-Z.-_]+)</ApplicationDisplayVersion>");
            var oldVersionName= versionNameRegex.Match(text).Groups[1].Value;
            text = versionNameRegex.Replace(text, $"<ApplicationDisplayVersion>{context.VersionName}</ApplicationDisplayVersion>");
            context.Log.Information($"Version name updated from: {oldVersionName} to: {context.VersionName}");
        }

        File.WriteAllText(projectPath.FullPath, text);
        
        context.Log.Information("csproj updated with new version");
    }
}
