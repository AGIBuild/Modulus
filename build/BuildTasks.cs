using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using Nuke.Common.Tools.DotNet;
using System.Collections.Generic;

class BuildTasks : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main ()
    {
        return Execute<BuildTasks>(x => x.BuildAll);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Executes(() =>
        {
            if (Directory.Exists(ArtifactsDirectory))
                Directory.Delete(ArtifactsDirectory, true);
            var binDirs = Directory.GetDirectories(RootDirectory / "src", "bin", SearchOption.AllDirectories);
            var objDirs = Directory.GetDirectories(RootDirectory / "src", "obj", SearchOption.AllDirectories);
            foreach (var dir in binDirs.Concat(objDirs))
            {
                Directory.Delete(dir, true);
            }
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Build => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(Build)
        .Executes(() =>
        {
            foreach (var project in Solution.AllProjects.Where(p => p.Name.Contains("Plugin") || p.Name.Contains("App")))
            {
                DotNetTasks.DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .EnableNoBuild());
            }
        });

    Target Run => _ => _
        .Executes(() =>
        {
            var desktopProject = Solution.AllProjects.FirstOrDefault(p => p.Name == "Modulus.App.Desktop");
            if (desktopProject == null)
                throw new Exception("Modulus.App.Desktop project cannot find");
            DotNetTasks.DotNetRun(s => s
                .SetProjectFile(desktopProject)
                .SetConfiguration(Configuration));
        });

    Target Test => _ => _
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoBuild(true));
        });

    Target BuildAll => _ => _
        .DependsOn(Build, Test);

    Target Default => _ => _
        .DependsOn(Build);
}
