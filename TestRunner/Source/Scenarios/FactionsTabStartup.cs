using System.Collections;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionsTabSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class FactionsTabStartup
    {
        const string BothTabs = "Architect Work Schedule Assign Animals Wildlife Research Quests World History Factions RelationsTab Menu";

        [Scenario("factions-tab-kept-in-a-game-started-with-the-setting-on", MapSize = 40, Windowed = true, SelfTest = true, OnlyOn = "1.7")]
        static IEnumerator StartedWithTheSettingOn(ScenarioContext s) {
            s.SpawnColonist(s.Cell(0, 0));
            s.Tick(30);
            var start = s.DrawFrame();
            yield return start;
            s.AreEqual(BothTabs, VisibleButtons(), "the bottom bar buttons of a game started with the setting already on, in order");
            s.Check(FactionsButton.Worker.Visible && RelationsButton.Worker.Visible, "both buttons should be visible from the start");
            CheckBarLayoutMatchesDrawing(s, start);

            yield return ClickBarButton(s, FactionsButton);
            s.Check(WindowOfType(FactionsWindowType) != null, "clicking the vanilla Factions button should open the vanilla Factions tab");
            yield return CloseOpenTab(s);
            yield return ClickBarButton(s, RelationsButton);
            s.Check(RelationsWindow != null, "clicking the Relations button should open the Relations tab");
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
