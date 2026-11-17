using Verse;
using static RimhunterTestRunner.Scenarios.FactionsTabSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class FactionsTabReload
    {
        const string RelationsReplacesFactions = "Architect Work Schedule Assign Animals Wildlife Research Quests World History RelationsTab Menu";

        [Scenario("factions-tab-hidden-again-after-defs-are-reloaded-and-a-game-loads", MapSize = 40, SaveWithMods = "{mod}", SetupInSaveLaunch = true, ReloadInSaveLaunch = true, OnlyOn = "1.7")]
        static void HiddenAgainAfterALoad(ScenarioContext s) {
            if (s.Stage == SaveStage.Setup) {
                s.AreEqual(RelationsReplacesFactions, VisibleButtons(), "the bottom bar buttons of a fresh install before the defs are reloaded");
                FactionsButton.buttonVisible = true;
                s.Check(VisibleButtons().Contains("Factions"), "a reloaded Factions def is visible again (the game's default), until the mod acts");
                return;
            }
            s.AreEqual(RelationsReplacesFactions, VisibleButtons(), "the bottom bar buttons after a game load with the setting off (" + s.Stage + ")");
            s.RequireNoLogErrors();
        }
    }
}
