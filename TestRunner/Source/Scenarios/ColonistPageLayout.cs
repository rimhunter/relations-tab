using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class ColonistPageLayout
    {
        static Dictionary<string, Vector2> Positions(ColonistPage page, IEnumerable<string> names) => page.NodeCenters(page.Latest.Texts, names);

        static float MaxMove(Dictionary<string, Vector2> from, Dictionary<string, Vector2> to) =>
            from.Max(kv => (kv.Value - to[kv.Key]).magnitude);

        static string Show(Dictionary<string, Vector2> p) => string.Join(" ", p.Select(kv => kv.Key + "=" + kv.Value));

        static float RadiusSpread(Dictionary<string, Vector2> p) {
            var center = p.Values.Aggregate(Vector2.zero, (a, b) => a + b) / p.Count;
            var radii = p.Values.Select(v => (v - center).magnitude).ToList();
            return radii.Max() - radii.Min();
        }

        static float SideSpread(List<Vector2> ring) {
            var sides = Enumerable.Range(0, ring.Count).Select(i => (ring[i] - ring[(i + 1) % ring.Count]).magnitude).ToList();
            return sides.Max() - sides.Min();
        }

        [Scenario("colonist-page-layout-buttons-switch-mode-and-reset", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 150)]
        static IEnumerator LayoutButtons(ScenarioContext s) {
            var pawns = new[] { -12, -6, 0, 6, 12 }.Select(dx => ColonistPage.Spawn(s, dx)).ToList();
            var names = pawns.Select(p => p.LabelShort).ToList();
            var page = new ColonistPage(s);
            yield return page.Open();
            yield return page.Frames(150);
            var dynamicSettled = Positions(page, names);
            s.AreEqual(5, dynamicSettled.Count, "all five colonists are nodes in the dynamic layout");
            if (dynamicSettled.Count != 5) yield break;

            yield return page.Frames(20);
            s.Check(MaxMove(dynamicSettled, Positions(page, names)) < 1f, "before any click the settled dynamic layout should be at rest");

            yield return page.ClickPixel(IconCenterPixels(RelationsWindow, 1));
            yield return page.Frames(3);
            var circle = Positions(page, names);
            yield return page.Frames(30);
            var circleLater = Positions(page, names);
            s.Check(MaxMove(circle, circleLater) < 1f, "the circle layout should hold still; it moved " + MaxMove(circle, circleLater));
            s.Check(RadiusSpread(circle) < 3f, "in the circle layout every colonist should be the same distance from the center; spread " + RadiusSpread(circle) + ": " + Show(circle));
            var order = names;
            var sideSpread = SideSpread(order.Select(n => circle[n]).ToList());
            s.Check(sideSpread < 3f, "in the circle layout the five colonists should be evenly spaced; side spread " + sideSpread);
            s.Check(MaxMove(dynamicSettled, circle) > 10f, "the circle layout should differ from the settled dynamic layout; max move " + MaxMove(dynamicSettled, circle));

            yield return page.ClickPixel(IconCenterPixels(RelationsWindow, 1));
            yield return page.Frames(3);
            var dynamicStart = Positions(page, names);
            yield return page.Frames(60);
            var dynamicRunning = Positions(page, names);
            s.Check(MaxMove(dynamicStart, dynamicRunning) > 5f, "back in the dynamic layout the colonists should move while it settles; max move " + MaxMove(dynamicStart, dynamicRunning));
            yield return page.Frames(150);
            var settledAgain = Positions(page, names);
            s.Check(MaxMove(circle, settledAgain) > 10f, "the settled dynamic layout should differ from the circle positions; max move " + MaxMove(circle, settledAgain));

            yield return page.ClickPixel(IconCenterPixels(RelationsWindow, 2));
            var afterReset = Positions(page, names);
            var fromSettled = MaxMove(settledAgain, afterReset);
            var fromCircle = MaxMove(circle, afterReset);
            s.Check(fromSettled > 100f, "the reset button should pull the colonists out of the settled layout; max move " + fromSettled);
            s.Check(fromCircle < fromSettled / 2f, "just after the reset the layout should be nearer the circle start (" + fromCircle + ") than the settled layout (" + fromSettled + "): " + Show(afterReset) + " vs circle " + Show(circle));
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
