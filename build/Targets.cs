using Cake.Frosting;

[TaskName("Default")]
[IsDependentOn(typeof(BuildTask))]
public sealed class DefaultTarget : FrostingTask<BuildContext> { }

[TaskName("Publish")]
[IsDependentOn(typeof(PublishLocalTask))]
public sealed class PublishTarget : FrostingTask<BuildContext> { }
