using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class RelationsTabOpens
    {
        [Scenario("startup-opens-relations-tab-with-every-colonist", MapSize = 40, Windowed = true, OnlyOn = "1.7")]
        static IEnumerator OpensWithEveryColonist(ScenarioContext s) {
            var spawned = new[] { s.SpawnColonist(s.Cell(-4, 0)), s.SpawnColonist(s.Cell(0, 0)), s.SpawnColonist(s.Cell(4, 0)) };
            var expected = spawned.Select(p => p.LabelShort).OrderBy(n => n).ToList();
            s.Tick(30);

            var before = s.DrawFrame();
            yield return before;
            var barNames = Find.ColonistBar.Entries.Where(e => e.map == Find.CurrentMap).Select(e => e.pawn).Where(p => p != null).Select(p => p.LabelShort).OrderBy(n => n).ToList();
            s.AreEqual(string.Join(",", expected), string.Join(",", barNames), "the colonist bar names for the viewed map");
            s.Check(RelationsWindow == null, "the Relations tab should be closed before the button is pressed");

            var relations = RelationsButton;
            s.Check(relations.buttonVisible && relations.Worker.Visible, "the Relations main button should be visible");
            FactionsTabSupport.CheckBarLayoutMatchesDrawing(s, before);
            yield return ClickRelationsButton(s);

            yield return s.DrawFrame();
            var after = s.DrawFrame();
            yield return after;

            var window = RelationsWindow;
            s.Check(window != null, "clicking the Relations button should open the Relations tab");
            s.Check(Find.MainTabsRoot.OpenTab == relations, "the open main tab should be the Relations button's tab");
            if (window == null) yield break;

            var scale = Prefs.UIScale;
            var rect = window.windowRect;
            var graphArea = new Rect((rect.xMax - rect.height) * scale, rect.y * scale, rect.height * scale, rect.height * scale);
            var barDrawn = before.Texts.Select(t => (t.Text, (int)t.Ink.x, (int)t.Ink.y)).ToHashSet();
            var mapLabels = spawned.Select(p => (p.LabelShort, at: s.ScreenPixelOf(p.DrawPos) + new Vector2(0f, 25f))).ToList();
            var inGraph = after.Texts
                .Where(t => graphArea.Contains(t.Ink.center))
                .Where(t => !mapLabels.Any(m => m.LabelShort == t.Text && (t.Ink.center - m.at).magnitude < 14f))
                .Where(t => !barDrawn.Contains((t.Text, (int)t.Ink.x, (int)t.Ink.y)))
                .Select(t => t.Text).OrderBy(n => n).ToList();
            s.AreEqual(string.Join(",", expected), string.Join(",", inGraph),
                "the labels drawn in the graph area " + graphArea + " that were not already on screen. Drawn: " + Dump(after.Texts));
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
