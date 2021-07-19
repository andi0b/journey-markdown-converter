using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
[GitHubActions(name: "NUKEBuild", 
               image: GitHubActionsImage.UbuntuLatest, 
               images: new GitHubActionsImage[]{},
               On = new[] { GitHubActionsTrigger.Push },
               InvokedTargets=new[]
               {
                   nameof(Test),
                   nameof(PublishPortable)
               })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]                         readonly Solution      Solution;
    [GitRepository]                    readonly GitRepository GitRepository;
    [GitVersion(Framework = "net5.0")] readonly GitVersion    GitVersion;

    Project MainProject => Solution.GetProject("journey-markdown-converter");

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath PublishDirectory => RootDirectory / "publish";

    Target Clean => _ =>
        _.Before(Restore)
         .Executes(() =>
          {
              SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
              TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
              EnsureCleanDirectory(OutputDirectory);
              EnsureCleanDirectory(PublishDirectory);
          });

    Target Restore => _ =>
        _.Executes(() =>
        {
            DotNetRestore(s => s
                             .SetProjectFile(Solution));
        });

    Target Compile => _ =>
        _.DependsOn(Restore)
         .Executes(() =>
          {
              DotNetBuild(s => s
                              .SetProjectFile(Solution)
                              .SetConfiguration(Configuration)
                              .SetAssemblyVersion(GitVersion.AssemblySemVer)
                              .SetFileVersion(GitVersion.AssemblySemFileVer)
                              .SetInformationalVersion(GitVersion.InformationalVersion)
                              .EnableNoRestore());
          });

    Target Test => _ =>
        _.DependsOn(Compile)
         .Produces(TestsDirectory+"/TestResults/*trx")
         .Executes(() =>
          {
              DotNetTest(s => s
                             .SetProjectFile(Solution)
                             .SetConfiguration(Configuration)
                             .EnableNoBuild()
                             .SetLogger("trx")
              );
          });

    Target PublishPortable => _ =>
        _.DependsOn(Compile)
         .Produces(PublishDirectory)
         .Executes(() =>
          {
              DotNetPublish(s => s.SetProject(MainProject)
                                  .SetOutput(PublishDirectory + "/portable")
                                  .EnableNoBuild()
              );
          });


    Target PublishSelfContained => _ =>
        _.DependsOn(Compile)
         .Produces(PublishDirectory)
         .Executes(() =>
          {
              var publishCombinations =
                  from runtime in new[] { "win-x86", "linux-x64", "osx-x64", "osx-arm64" }
                  select new { runtime };

              DotNetPublish(s => s.SetProject(MainProject)
                                  .EnableSelfContained()
                                  .EnablePublishSingleFile()
                                  .CombineWith(publishCombinations, (oo, v) =>
                                                   oo.SetRuntime(v.runtime)
                                                     .SetOutput(PublishDirectory + $"/{v.runtime}")
                                   )
              );
          });
}