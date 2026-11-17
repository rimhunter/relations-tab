using System.Collections;
using System.Linq;
using RimWorld;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionsTabSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class FactionsTabToggle
    {
        const string RelationsReplacesFactions = "Architect Work Schedule Assign Animals Wildlife Research Quests World History RelationsTab Menu";
        const string BothTabs = "Architect Work Schedule Assign Animals Wildlife Research Quests World History Factions RelationsTab Menu";

        [Scenario("factions-tab-toggled-in-a-running-game", MapSize = 40, Windowed = true, OnlyOn = "1.7")]
        static IEnumerator ToggledInARunningGame(ScenarioContext s) {
            s.SpawnColonist(s.Cell(0, 0));
            s.Tick(30);

            var start = s.DrawFrame();
            yield return start;
            s.AreEqual(RelationsReplacesFactions, VisibleButtons(), "the bottom bar buttons of a fresh install, in order: Relations in the Factions slot between History and Menu");
            s.Check(!FactionsButton.Worker.Visible, "the vanilla Factions button should be hidden by default");
            CheckBarLayoutMatchesDrawing(s, start);

            yield return ClickBarButton(s, RelationsButton);
            s.Check(RelationsWindow != null, "clicking the Relations button in the fresh-install bar should open the Relations tab");
            s.Check(Find.MainTabsRoot.OpenTab == RelationsButton, "the open main tab should be the Relations tab");
            yield return CloseOpenTab(s);
            s.Check(RelationsWindow == null, "the Relations tab should close");

            const string SettingElement = "<KEEP_VANILLA_FACTIONS_TAB>True</KEEP_VANILLA_FACTIONS_TAB>";
            s.Check(!SettingsFileText().Contains(SettingElement), "the settings file should not hold the setting before it is turned on");
            yield return ToggleKeepVanillaFactionsTab(s);
            s.Check(SettingsFileText().Contains(SettingElement), "the settings file the game wrote on closing the settings window should hold " + SettingElement + "; it holds: " + SettingsFileText());
            s.AreEqual(BothTabs, VisibleButtons(), "the bottom bar buttons after the setting is turned on and the settings window closed, with no restart");
            var both = s.DrawFrame();
            yield return both;
            CheckBarLayoutMatchesDrawing(s, both);

            yield return ClickBarButton(s, FactionsButton);
            s.Check(WindowOfType(FactionsWindowType) != null, "clicking the vanilla Factions button should open the vanilla Factions tab");
            s.Check(Find.MainTabsRoot.OpenTab == FactionsButton, "the open main tab should be the vanilla Factions tab");
            yield return CloseOpenTab(s);

            yield return ClickBarButton(s, RelationsButton);
            s.Check(RelationsWindow != null, "clicking the Relations button should open the Relations tab");
            yield return CloseOpenTab(s);

            yield return ToggleKeepVanillaFactionsTab(s);
            s.Check(!SettingsFileText().Contains(SettingElement), "the settings file should no longer hold the setting after it is turned off");
            s.AreEqual(RelationsReplacesFactions, VisibleButtons(), "the bottom bar buttons after the setting is turned off again and the settings window closed");
            var off = s.DrawFrame();
            yield return off;
            CheckBarLayoutMatchesDrawing(s, off);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
