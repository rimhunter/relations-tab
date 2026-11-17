using System.Collections;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionPageSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class FactionPageAtThirtyFactions
    {
        [Scenario("faction-page-shows-leaderless-factions-in-the-dynamic-layout-at-30-factions", MapSize = 60, Windowed = true, OnlyOn = "1.7", MaxSeconds = 160)]
        static IEnumerator LeaderlessAtThirty(ScenarioContext s) {
            s.SpawnColonist(s.Cell(0, 0));
            HideWorldFactions();
            foreach (var i in Enumerable.Range(0, 30))
                FactionGenerator.CreateFactionAndAddToManager(DefDatabase<FactionDef>.GetNamed(FactionDefNames[i % FactionDefNames.Length]));
            var insect = Find.FactionManager.OfInsects;
            insect.hidden = false;
            insect.color = InsectColor;
            var leaderless = MakeLeaderlessHumanlike();
            leaderless.color = LeaderlessColor;
            s.Check(insect.leader == null && leaderless.leader == null && !leaderless.TryGenerateNewLeader(), "both test factions should have no leader");
            s.Tick(30);
            var labels = VisibleLabels();
            s.Check(Find.FactionManager.AllFactionsVisible.Count(f => !f.IsPlayer && f.leader != null) >= 30, "at least 30 visible factions with a leader should exist");
            s.Check(labels.Count >= 32, "at least 32 visible factions should exist; there are " + labels.Count);

            using (var log = new LogWatch()) {
                var page = new OpenPage();
                var open = OpenFactionsPage(s, page);
                while (open.MoveNext()) yield return open.Current;

                foreach (var wait in new[] { 1, 40, 120 }) {
                    yield return ScenarioContext.Frames(wait);
                    var frame = s.DrawFrame();
                    yield return frame;
                    var read = ReadSlots(frame.Texts, labels);
                    s.AreEqual(string.Join(",", labels.OrderBy(n => n)), string.Join(",", read.Slots.Select(n => n.name).OrderBy(n => n)), "the faction node labels drawn " + wait + " frames in");
                    var outside = OutsideGraphArea(GraphAreaPixels(page.Window), read);
                    s.Check(outside.Length == 0, "every faction node, the leaderless ones included, should be inside the graph area " + wait + " frames in; outside: " + outside);
                }

                var settled = new SlotRead();
                var settle = Settle(s, labels, settled);
                while (settle.MoveNext()) yield return settle.Current;
                var settledOutside = OutsideGraphArea(GraphAreaPixels(page.Window), settled);
                s.Check(settledOutside.Length == 0, "every faction node should be inside the graph area once the layout settled; outside: " + settledOutside);
                var area = GraphAreaPixels(page.Window);
                var overlapping = Overlapping(settled);
                s.Check(overlapping.Length == 0, "no two faction slots should overlap once the 30-faction layout settled; overlapping: " + overlapping);
                var compact = TooCompact(area, settled);
                s.Check(compact.Length == 0, "the settled 30-faction layout should give each node about " + SlotsPerNode + " slots of room: " + compact);
                foreach (var f in new[] { insect, leaderless })
                    s.Check(settled.Slots.Any(n => n.name == LabelOf(f)), "the leaderless faction '" + LabelOf(f) + "' should be on the settled graph");

                var shot = new Capture();
                var take = TakeCapture(s, "faction-page-30-factions", shot);
                while (take.MoveNext()) yield return take.Current;
                var inShot = ReadSlots(shot.Texts, labels);
                foreach (var (faction, color, what) in new[] { (insect, InsectColor, "the Insect faction"), (leaderless, LeaderlessColor, "the leaderless humanlike faction") }) {
                    var slot = inShot.Slots.Single(n => n.name == LabelOf(faction)).slot;
                    var pixels = PixelsNear(shot, IconArea(slot), color);
                    var needed = 50 * Prefs.UIScale * Prefs.UIScale;
                    s.Check(pixels >= needed, what + " has no leader, so its faction icon in its own color should fill the middle of its slot on the settled 30-faction layout (at least " + needed + " pixels); there are " + pixels);
                }

                var click = ClickNode(s, LabelOf(insect), labels);
                while (click.MoveNext()) yield return click.Current;
                settle = Settle(s, labels, settled);
                while (settle.MoveNext()) yield return settle.Current;
                var afterClick = OutsideGraphArea(GraphAreaPixels(page.Window), settled);
                s.Check(afterClick.Length == 0, "every faction node should stay inside the graph area after the Insect faction was selected; outside: " + afterClick);
                var details = s.DrawFrame();
                yield return details;
                s.Check(Details(details).Any(t => t.Text == LabelOf(insect)), "selecting the Insect faction should show its details");

                s.Check(log.NoLeaderLines.Length == 0, "the log should hold no 'has no leader' line; it holds: " + log.NoLeaderLines);
            }
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("faction-page-spreads-six-factions-without-overlap", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 150)]
        static IEnumerator SpreadAtSix(ScenarioContext s) {
            s.SpawnColonist(s.Cell(0, 0));
            HideWorldFactions();
            foreach (var i in Enumerable.Range(0, 5))
                FactionGenerator.CreateFactionAndAddToManager(DefDatabase<FactionDef>.GetNamed(FactionDefNames[i]));
            var labels = VisibleLabels();
            s.AreEqual(6, labels.Count, "visible factions");
            s.Tick(30);
            var page = new OpenPage();
            var open = OpenFactionsPage(s, page);
            while (open.MoveNext()) yield return open.Current;
            var settled = new SlotRead();
            var settle = Settle(s, labels, settled);
            while (settle.MoveNext()) yield return settle.Current;
            var area = GraphAreaPixels(page.Window);
            var outside = OutsideGraphArea(area, settled);
            s.Check(outside.Length == 0, "every faction node should be inside the graph area at 6 factions; outside: " + outside);
            var overlapping = Overlapping(settled);
            s.Check(overlapping.Length == 0, "no two faction slots should overlap at 6 factions; overlapping: " + overlapping);
            var compact = TooCompact(area, settled);
            s.Check(compact.Length == 0, "the settled 6-faction layout should give each node about " + SlotsPerNode + " slots of room: " + compact);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
