using Common.Utilities;

namespace Artifacts.Tasks;

[TaskName(nameof(ArtifactsNativeTest))]
[TaskDescription("Tests the native executables in docker container")]
[DockerRegistryArgument]
[DockerDotnetArgument]
[DockerDistroArgument]
[IsDependentOn(typeof(ArtifactsPrepare))]
public class ArtifactsNativeTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(ArtifactsNativeTest)} works only on Docker on Linux agents.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        if (context.Version?.SemVersion == null)
            return;
        var version = context.Version.SemVersion.ToLower();
        var rootPrefix = string.Empty;

        foreach (var dockerImage in context.Images)
        {
            if (context.SkipImageTesting(dockerImage)) continue;

            var runtime = "linux";
            if (dockerImage.Distro.StartsWith("alpine"))
            {
                runtime += "-musl";
            }
            runtime += dockerImage.Architecture == Architecture.Amd64 ? "-x64" : "-arm64";


            var cmd = $"{rootPrefix}/scripts/test-native-tool.sh --version {version} --repoPath {rootPrefix}/repo --runtime {runtime}";

            context.DockerTestArtifact(dockerImage, cmd);
        }
    }
}
