using System.Collections;
using RimWorld;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionsTabSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class FactionsTabNoMap
    {
        [Scenario("relations-tab-opens-with-no-map-loaded", MapSize = 40, Windowed = true, OnlyOn = "1.7")]
        static IEnumerator OpensWithNoMap(ScenarioContext s) {
            s.SpawnColonist(s.Cell(0, 0));
            s.Tick(30);
            yield return s.DrawFrame();
            s.RequireNoLogErrors();

            Current.Game.CurrentMap = null;
            yield return s.DrawFrame();
            var frame = s.DrawFrame();
            yield return frame;
            s.Check(RelationsButton.Worker.Visible && !RelationsButton.Worker.Disabled, "the Relations button should be visible and usable with no map");

            yield return ClickBarButton(s, RelationsButton);
            var opened = RelationsWindow != null;
            var errors = s.LogErrors.Count;
            Current.Game.CurrentMap = s.Map;
            yield return CloseOpenTab(s);
            s.Check(opened, "clicking the Relations button with no map should open the Relations tab");
            s.Check(errors == 0, "opening the Relations tab with no map should log no error; logged: " + string.Join(" | ", s.LogErrors));
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
