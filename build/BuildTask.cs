using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Frosting;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.DotNet.MSBuild;
using System;

[TaskName("RestoreWorkloads")]
public sealed class RestoreWorkloadsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.IsRunningInCI)
        {
            context.DotNetWorkloadRestore(context.ProjectNameToPath(context.ProjectName).FullPath);
        }
    }
}

[TaskName("Clean")]
[IsDependentOn(typeof(RestoreWorkloadsTask))]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var settings = new DotNetCleanSettings
        {
            NoLogo = true,
            Verbosity = context.Verbosity,
        };

        context.CleanDirectories(context.RootDirectory
            .Combine(context.ProjectName).Combine("obj").FullPath);
        context.CleanDirectories(context.RootDirectory
            .Combine(context.ProjectName).Combine("bin").Combine(context.MsBuildConfiguration).FullPath);

        context.DotNetClean(context.ProjectNameToPath(context.ProjectName).FullPath, settings);
    }
}

[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var settings = new DotNetRestoreSettings
        {
            Verbosity = context.Verbosity
        };
        context.DotNetRestore(context.ProjectNameToPath(context.ProjectName).FullPath, settings);
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(UpdateVersionTask))]
[IsDependentOn(typeof(RestoreTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (string.IsNullOrEmpty(context.SigningKeyPass))
        {
            throw new Exception($"{nameof(context.SigningKeyPass)} not provided");
        }

        var settings = new DotNetPublishSettings
        {
            NoRestore = true,
            Configuration = context.MsBuildConfiguration,
            NoLogo = true,
            Verbosity = context.Verbosity,
            MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("AndroidSigningKeyPass", context.SigningKeyPass)
                .WithProperty("AndroidSigningStorePass", context.SigningKeyPass)
        };
        context.DotNetPublish(context.ProjectNameToPath(context.ProjectName).FullPath, settings);
    }
}
