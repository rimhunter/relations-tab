using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionPageSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class LayoutStaysInside
    {
        const float MinCenterDistance = 10f;

        static IEnumerator CheckInside(ScenarioContext s, Window window, IList<string> names, string what) {
            foreach (var wait in new[] { 1, 40, 120 }) {
                yield return ScenarioContext.Frames(wait);
                var read = new SlotRead();
                var step = ReadNamed(s, names, what + ", " + wait + " frames in", read);
                while (step.MoveNext()) yield return step.Current;
                var area = GraphAreaPixels(window);
                var outside = OutsideGraphArea(area, read);
                s.Check(outside.Length == 0, "every node slot should be inside the graph area " + area + " " + what + ", " + wait + " frames in; outside: " + outside);
            }
        }

        static IEnumerator Settled(ScenarioContext s, Window window, IList<string> names, string what, SlotRead settled) {
            var checks = CheckInside(s, window, names, what);
            while (checks.MoveNext()) yield return checks.Current;
            var settle = SettleLayout(s, into => ReadNamed(s, names, what + ", settling", into), settled);
            while (settle.MoveNext()) yield return settle.Current;
            var area = GraphAreaPixels(window);
            var outside = OutsideGraphArea(area, settled);
            s.Check(outside.Length == 0, "every node slot should be inside the graph area " + area + " once the layout settled " + what + "; outside: " + outside);
            var stacked = Stacked(settled, MinCenterDistance);
            s.Check(stacked.Length == 0, "no two node slot centers should be within " + MinCenterDistance + " px once the layout settled " + what + "; stacked: " + stacked);
        }

        static IEnumerator ClickIcon(ScenarioContext s, Window window, int index) {
            yield return s.Click(ClickTarget(IconCenterPixels(window, index)));
            yield return ScenarioContext.Frames(2);
        }

        // Clicking a drawn text is implemented for 1.7 only (ScenarioContext.Click), so this scenario is 1.7 only.
        [Scenario("layout-stays-inside-at-40-colonists-and-30-factions", MapSize = 60, Windowed = true, OnlyOn = "1.7", MaxSeconds = 160)]
        static IEnumerator StaysInside(ScenarioContext s) {
            var startWidth = Screen.width;
            var startHeightPixels = Screen.height;
            var startMode = Screen.fullScreenMode;
            var startUiWidth = UI.screenWidth;
            var startUiHeight = UI.screenHeight;
            try {
                var body = Body(s);
                while (body.MoveNext()) yield return body.Current;
            } finally {
                Screen.SetResolution(startWidth, startHeightPixels, startMode);
            }
            var waited = 0;
            while ((Screen.width != startWidth || UI.screenWidth != startUiWidth || UI.screenHeight != startUiHeight) && waited++ < 300) yield return null;
            s.Check(Screen.width == startWidth && UI.screenWidth == startUiWidth && UI.screenHeight == startUiHeight,
                "the window should be back at " + startWidth + " pixels wide, UI " + startUiWidth + " x " + startUiHeight + "; it is " + Screen.width + " wide, UI " + UI.screenWidth + " x " + UI.screenHeight);
        }

        static IEnumerator Body(ScenarioContext s) {
            var colonists = new List<Pawn>();
            for (var dx = -14; dx <= 14; dx += 4)
                for (var dz = -8; dz <= 8; dz += 4)
                    colonists.Add(s.SpawnColonist(s.Cell(dx, dz)));
            s.AreEqual(40, colonists.Count, "colonists spawned for the layout check");

            foreach (var i in Enumerable.Range(0, 30))
                FactionGenerator.CreateFactionAndAddToManager(DefDatabase<FactionDef>.GetNamed(FactionDefNames[i % FactionDefNames.Length]));
            var factions = Find.FactionManager.AllFactionsVisible.Where(f => !f.IsPlayer && f.leader != null).ToList();
            s.Check(factions.Count >= 30, "at least 30 visible factions with a leader should exist; there are " + factions.Count);
            var player = Faction.OfPlayer;
            var factionLabels = factions.Select(f => f.GetCallLabel()).ToList();
            factionLabels.Add(player.HasName ? player.Name : "Fluffy.Relations.Colony".Translate().ToString());
            var colonistNames = colonists.Select(p => p.LabelShort).ToList();
            s.Tick(30);

            var before = s.DrawFrame();
            yield return before;
            s.Check(RelationsWindow == null, "the Relations tab should be closed before the button is pressed");
            yield return ClickRelationsButton(s);
            yield return ScenarioContext.Frames(2);
            var window = RelationsWindow;
            s.Check(window != null, "clicking the Relations button should open the Relations tab");
            if (window == null) yield break;

            var dynamicLayout = new SlotRead();
            var run = Settled(s, window, colonistNames, "on the colonist page", dynamicLayout);
            while (run.MoveNext()) yield return run.Current;

            yield return ClickIcon(s, window, 1);
            var circle = s.DrawFrame();
            yield return circle;
            var circleRead = ReadSlots(circle.Texts, colonistNames);
            var area = GraphAreaPixels(window);
            var radius = area.width / 2f - SlotSize * Prefs.UIScale / 2f;
            var offCircle = string.Join("; ", circleRead.Slots
                .Where(n => Mathf.Abs((n.slot.center - area.center).magnitude - radius) > 3f * Prefs.UIScale)
                .Select(n => "'" + n.name + "' at distance " + (n.slot.center - area.center).magnitude));
            s.AreEqual(string.Join(",", colonistNames.OrderBy(n => n)), string.Join(",", circleRead.Slots.Select(n => n.name).OrderBy(n => n)), "the colonist node labels drawn in the circle layout");
            s.Check(offCircle.Length == 0, "after the circle toggle every colonist node center should be " + radius + " px from the graph center " + area.center + "; off the circle: " + offCircle);
            s.Check(MaxMove(dynamicLayout, circleRead) > 20f * Prefs.UIScale, "the circle layout should differ from the settled dynamic layout");

            yield return ClickIcon(s, window, 1);
            yield return ClickIcon(s, window, 2);
            var afterReset = new SlotRead();
            run = Settled(s, window, colonistNames, "on the colonist page after reset", afterReset);
            while (run.MoveNext()) yield return run.Current;
            s.Check(MaxMove(dynamicLayout, afterReset) <= 2f * Prefs.UIScale, "reset should restart the dynamic layout from the circle and settle where it did before; the largest difference is " + MaxMove(dynamicLayout, afterReset) + " px");

            run = SwitchPage(s, window, "Fluffy_Relations.Colonists");
            while (run.MoveNext()) yield return run.Current;
            var factionsSettled = new SlotRead();
            run = Settled(s, window, factionLabels, "on the factions page", factionsSettled);
            while (run.MoveNext()) yield return run.Current;
            var overlapping = Overlapping(factionsSettled);
            s.Check(overlapping.Length == 0, "no two faction slots should overlap once the layout settled at " + factionLabels.Count + " factions; overlapping: " + overlapping);

            var startWidth = UI.screenWidth;
            var startPixelsWidth = Screen.width;
            var startPixels = Screen.width + " x " + Screen.height;
            Screen.SetResolution(1280, 720, false);
            var waited = 0;
            while (UI.screenWidth == startWidth && waited++ < 300) yield return null;
            yield return ScenarioContext.Frames(30);
            s.Check(Screen.width >= 1280 && Screen.width < startPixelsWidth && UI.screenWidth < startWidth,
                "the scenario should shrink the window from " + startPixels + " pixels (UI width " + startWidth + ") to at most the requested 1280 x 720 the window manager allows; it is " + Screen.width + " x " + Screen.height + " (UI width " + UI.screenWidth + ")");
            yield return ScenarioContext.Frames(5);
            window = RelationsWindow;
            s.Check(window != null, "the Relations tab should still be open after the resolution changed");
            if (window == null) yield break;
            run = Settled(s, window, factionLabels, "on the factions page at 1280 x 720", new SlotRead());
            while (run.MoveNext()) yield return run.Current;
            run = SwitchPage(s, window, "Fluffy_Relations.Factions");
            while (run.MoveNext()) yield return run.Current;
            run = Settled(s, window, colonistNames, "on the colonist page at 1280 x 720", new SlotRead());
            while (run.MoveNext()) yield return run.Current;

            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
