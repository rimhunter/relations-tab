using System.Xml.Linq;
using Xunit;

namespace RelationsTab.Tests;

public class ModFolderTests
{
    static readonly string Root = ModRoot.Path;

    static XElement Load(string relative) => XDocument.Load(Path.Combine(Root, relative)).Root;

    static bool InBuildOutput(string path) {
        var sep = Path.DirectorySeparatorChar;
        return path.Contains($"{sep}.git{sep}") || path.Contains($"{sep}bin{sep}") || path.Contains($"{sep}obj{sep}");
    }

    [Fact]
    public void About_supports_exactly_1_6_and_1_7() {
        var about = Load("About/About.xml");
        Assert.Equal(new[] { "1.6", "1.7" }, about.Element("supportedVersions").Elements("li").Select(l => l.Value));
    }

    [Fact]
    public void About_declares_no_dependency() {
        var about = Load("About/About.xml");
        Assert.Null(about.Element("modDependencies"));
        Assert.Null(about.Element("loadAfter"));
    }

    [Fact]
    public void Load_folders_pick_the_matching_build_for_each_game_version() {
        var folders = Load("LoadFolders.xml");
        Assert.Equal(new[] { "/", "1.6" }, folders.Element("v1.6").Elements("li").Select(l => l.Value));
        Assert.Equal(new[] { "/", "1.7" }, folders.Element("v1.7").Elements("li").Select(l => l.Value));
    }

    [Fact]
    public void No_prebuilt_binary_is_committed() {
        var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", "ls-files -- \"*.dll\"") {
            WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true,
        });
        var tracked = git.StandardOutput.ReadToEnd();
        var error = git.StandardError.ReadToEnd();
        git.WaitForExit();
        Assert.True(git.ExitCode == 0, "git ls-files failed: " + error);
        Assert.Equal("", tracked.Trim());
    }

    [Fact]
    public void The_mod_project_references_only_the_game() {
        var csproj = Load("Source/Fluffy_Relations.csproj");
        var references = csproj.Descendants("PackageReference").Select(r => r.Attribute("Include").Value);
        Assert.Equal(new[] { "Krafs.Rimworld.Ref" }, references);
    }

    [Fact]
    public void Nothing_references_Harmony() {
        var offenders = Directory.GetFiles(Path.Combine(Root, "Source"), "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".cs") || p.EndsWith(".csproj"))
            .Where(p => !InBuildOutput(p))
            .Where(p => File.ReadAllText(p).Contains("Harmony"))
            .ToList();
        Assert.Empty(offenders);
    }

    [Fact]
    public void The_leader_game_component_keeps_Fluffys_class_name_and_save_field() {
        var source = File.ReadAllText(Path.Combine(Root, "Source/GameComponent_Leader.cs"));
        Assert.Contains("namespace Fluffy_Relations", source);
        Assert.Contains("class GameComponent_Leader", source);
        Assert.Contains("Scribe_References.Look(ref _leader, \"Leader\"", source);
    }
}
