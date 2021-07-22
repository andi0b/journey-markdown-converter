using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Docker.DockerTasks;
using LogLevel = Nuke.Common.LogLevel;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
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

    string ProjectName => "journey-markdown-converter";
    
    Project MainProject => Solution.GetProject(ProjectName);

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath PublishDirectory => RootDirectory / "publish";
    AbsolutePath DeliverablesDirectory => RootDirectory / "deliverables";

    public bool IsReleaseVersion => GitVersion.PreReleaseTag == string.Empty;

    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();
        Logger.Log(LogLevel.Normal, $"Building '{ProjectName}' version {GitVersion.SemVer} ({GitVersion.InformationalVersion})");
        Logger.Log(LogLevel.Normal, $"IsLocalBuild = {IsLocalBuild}");
        Logger.Log(LogLevel.Normal, $"IsReleaseVersion = {IsReleaseVersion}");
        Logger.Log(LogLevel.Normal, $"Configuration = {Configuration}");

        Console.WriteLine($"::set-output name={nameof(GitVersion.SemVer)}::{GitVersion.SemVer}");
        Console.WriteLine($"::set-output name=IsRelease::{(IsReleaseVersion ? "true" : "false")}");
    }

    Target Clean => _ =>
        _.Before(Restore)
         .DependsOn(CleanDeliverables, CleanPublished)
         .Executes(() =>
          {
              SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
              TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
              EnsureCleanDirectory(OutputDirectory);
          });

    Target CleanDeliverables => _ => _.Executes(() =>
    {
        EnsureCleanDirectory(DeliverablesDirectory);
    });

    Target CleanPublished => _ => _.Executes(() =>
    {
        EnsureCleanDirectory(PublishDirectory);
    });

    Target Restore => _ =>
        _.Executes(() =>
        {
            DotNetRestore(s => s
                             .SetProjectFile(Solution)
                //.When(Configuration == Configuration.Release, s1 => s1.EnableLockedMode())
            );
        });

    Target Compile => _ =>
        _.DependsOn(Restore)
         .Executes(() =>
          {
              DotNetBuild(s => s
                              .SetProjectFile(Solution)
                              .SetConfiguration(Configuration)
                              .MyGitVersionBuild(GitVersion)
                              .EnableNoRestore());
          });

    Target Test => _ =>
        _.DependsOn(Compile)
         .Produces(TestsDirectory + "/TestResults/*trx")
         .Executes(() =>
          {
              DotNetTest(
                  s => s
                      .SetProjectFile(Solution)
                      .SetConfiguration(Configuration)
                      .EnableNoBuild()
                      .SetLogger("trx")
              );
          });

    Target PublishPortable => _ =>
        _.DependsOn(Compile)
         .DependsOn(CleanPublished)
         .After(Test)
         .Produces(PublishDirectory)
         .Executes(() =>
          {
              DotNetPublish(
                  s => s.SetProject(MainProject)
                        .SetConfiguration(Configuration)
                        .MyGitVersionPublish(GitVersion)
                        .SetOutput(PublishDirectory / "portable")
                        .EnableNoBuild()
              );
          });


    Target PublishSelfContained => _ =>
        _.DependsOn(Compile)
         .DependsOn(CleanPublished)
         .After(Test)
         .Produces(PublishDirectory)
         .Executes(() =>
          {
              var publishCombinations =
                  from runtime in new[] { "win-x86", "linux-x64", "osx-x64", "osx-arm64" }
                  select new { runtime };

              DotNetPublish(s => s.SetProject(MainProject)
                                  .SetConfiguration(Configuration)
                                  .MyGitVersionPublish(GitVersion)
                                  .EnableSelfContained()
                                  .EnablePublishSingleFile()
                                  .CombineWith(publishCombinations, (oo, v) =>
                                                   oo.SetRuntime(v.runtime)
                                                     .SetOutput(PublishDirectory / v.runtime)
                                   )
              );
          });


    Target ArchivePublished => _ =>
        _.DependsOn(CleanDeliverables)
         .After(PublishSelfContained, PublishPortable)
         .Executes(() =>
          {
              var publishDirectories = PublishDirectory.GlobDirectories("*");

              foreach (var dir in publishDirectories)
              {
                  CopyFileToDirectory(RootDirectory / "NOTICE.md", dir, FileExistsPolicy.Overwrite);
                  CopyFileToDirectory(RootDirectory / "LICENSE", dir, FileExistsPolicy.Overwrite);

                  var dirName = Path.GetFileName(dir);
                  var extension = (dirName.StartsWith("win") || dirName == "portable") ? ".zip" : ".tar.gz";

                  Compress(dir, DeliverablesDirectory / $"{ProjectName}-{dirName}-{GitVersion.SemVer}{extension}");
              }
          });

    Target CreateDeliverables => _ =>
        _.DependsOn(PublishPortable, PublishSelfContained, ArchivePublished);

    Target CreatePortableDeliverable => _ =>
        _.DependsOn(PublishPortable, ArchivePublished);

    Target DockerBuild => _ =>
        _.DependsOn(PublishPortable)
         .Executes(() =>
          {
              var tags = from version in new[]
                         {
                             GitVersion.SemVer,
                             IsReleaseVersion ? "latest" : null
                         }
                         where version != null
                         select $"{ProjectName}:{version}";

              DockerBuildxBuild(
                  s => s.SetPath(RootDirectory)
                        .SetTag(tags)
                        .EnableLoad());
          });
    

    Target QuickCi => _ =>
        _.DependsOn(Test, CreatePortableDeliverable);

    Target FullCi => _ =>
        _.DependsOn(Test, CreateDeliverables);
}

static class MyGitVersionExtensions
{
    private static string AssemblyVersion(this GitVersion gitVersion) => gitVersion.AssemblySemVer;
    private static string FileVersion(this GitVersion gitVersion) => gitVersion.AssemblySemFileVer;
    private static string InformationalVersion(this GitVersion gitVersion) => gitVersion.InformationalVersion;

    public static T MyGitVersionBuild<T>(this T toolSettings, GitVersion gitVersion) where T : DotNetBuildSettings =>
        toolSettings.SetAssemblyVersion(gitVersion.AssemblyVersion())
                    .SetFileVersion(gitVersion.FileVersion())
                    .SetInformationalVersion(gitVersion.InformationalVersion());

    public static T MyGitVersionPublish<T>(this T toolSettings, GitVersion gitVersion) where T : DotNetPublishSettings =>
        toolSettings.SetAssemblyVersion(gitVersion.AssemblyVersion())
                    .SetFileVersion(gitVersion.FileVersion())
                    .SetInformationalVersion(gitVersion.InformationalVersion());
}