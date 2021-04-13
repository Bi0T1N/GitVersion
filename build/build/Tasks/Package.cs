using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(Package))]
    [TaskDescription("Creates the packages (nuget, chocolatey or tar.gz)")]
    [IsDependentOn(typeof(PackageChocolatey))]
    [IsDependentOn(typeof(PackageNuget))]
    [IsDependentOn(typeof(PackageZip))]
    public class Package : FrostingTask<BuildContext>
    {
    }
}
