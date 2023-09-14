using Cake.Frosting;
using Cake.Common.Build;
using Cake.Common.IO;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using System.IO;
using Cake.Common.Diagnostics;

[TaskName("CopyArtifacts")]
[IsDependentOn(typeof(BuildTask))]
public sealed class CopyArtifactsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (Directory.Exists(context.ArtifactsDirectory.FullPath))
        {
            Directory.Delete(context.ArtifactsDirectory.FullPath, true);
        }
        else
        {
            Directory.CreateDirectory(context.ArtifactsDirectory.FullPath);
        }

        context.Log.Information($"Deleted artifacts folder {context.ArtifactsDirectory}");

        DirectoryPath publishDirectory = context.RootDirectory.Combine(@".\RacingCarsControllerAndroid\bin\Release\net7.0-android\publish");
        var appName = "RacingCarsController";
        var package = context.GetApplicationId();
        var architectures = new[] { "arm64-v8a", "armeabi-v7a", "x86_64" };

        foreach (var arch in architectures)
        {
            var apkName = $"{package}-{arch}-Signed.apk";
            var publishedApkName = $"{appName}-v{context.VersionName}-{arch}.apk";

            context.CopyFile(publishDirectory.CombineWithFilePath(apkName),
                context.ArtifactsDirectory.CombineWithFilePath(publishedApkName));

            context.Log.Information($"{publishedApkName} copied to {context.ArtifactsDirectory}");
        }
    }
}

[TaskName("PublishLocal")]
[IsDependentOn(typeof(CopyArtifactsTask))]
public sealed class PublishLocalTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.BuildSystem().IsLocalBuild)
        {
            DirectoryPath appStoreDirectory = @"C:\Source\AppStore";

            foreach(var file in Directory.EnumerateFiles(context.ArtifactsDirectory.FullPath))
            {
                var filename = System.IO.Path.GetFileName(file);
                context.CopyFile(file, appStoreDirectory.CombineWithFilePath(filename));
                context.Log.Information($"{file} copied to {appStoreDirectory}");
            }
        }
        else
        {
            context.Log.Warning("Not local build. Can't publish package.");
        }
    }
}
