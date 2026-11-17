using System.Xml.Linq;
using Xunit;

namespace RelationsTab.Tests;

public class FactionsTabTests
{
    static readonly string Root = ModRoot.Path;

    static List<XElement> ButtonDefs() =>
        Directory.GetFiles(Path.Combine(Root, "Defs", "MainButtonDefs"), "*.xml")
            .SelectMany(f => XDocument.Load(f).Root.Elements("MainButtonDef"))
            .ToList();

    static XElement RelationsButton() =>
        ButtonDefs().Single(d => d.Element("tabWindowClass")?.Value == "Fluffy_Relations.MainTabWindow_Relations");

    [Fact]
    public void The_mod_does_not_redefine_the_vanilla_Factions_button() {
        Assert.DoesNotContain(ButtonDefs(), d => d.Element("defName")?.Value == "Factions");
    }

    [Fact]
    public void The_Relations_button_has_its_own_name() {
        Assert.Equal("RelationsTab", RelationsButton().Element("defName").Value);
    }

    [Fact]
    public void The_Relations_button_comes_straight_after_the_vanilla_Factions_slot_of_90() {
        Assert.Equal("91", RelationsButton().Element("order").Value);
    }

    [Fact]
    public void The_Relations_button_stays_icon_only_and_usable_without_a_map() {
        var def = RelationsButton();
        Assert.Equal("True", def.Element("minimized").Value, ignoreCase: true);
        Assert.Equal("true", def.Element("validWithoutMap").Value, ignoreCase: true);
    }

    [Fact]
    public void The_setting_has_an_English_label_and_tip() {
        var keys = XDocument.Load(Path.Combine(Root, "Languages/English/Keyed/Keys.xml")).Root.Elements().Select(e => e.Name.LocalName).ToHashSet();
        Assert.Contains("Fluffy_Relations.KeepVanillaFactionsTab", keys);
        Assert.Contains("Fluffy_Relations.KeepVanillaFactionsTab.Tip", keys);
    }
}
