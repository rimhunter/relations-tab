using System.Collections;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionPageSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class FactionPageGoodwill
    {
        static void SetBothSides(Faction a, Faction b, int goodwill, FactionRelationKind kind) {
            foreach (var (x, y) in new[] { (a, b), (b, a) }) {
                var relation = x.RelationWith(y);
                relation.baseGoodwill = goodwill;
                relation.kind = kind;
            }
        }

        static bool Reddish((byte r, byte g, byte b) p) => p.r > 120 && p.g * 3 < p.r && p.b * 3 < p.r;

        [Scenario("faction-page-shows-the-goodwill-the-game-holds-between-two-factions", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 150)]
        static IEnumerator ShowsGameGoodwill(ScenarioContext s) {
            s.SpawnColonist(s.Cell(0, 0));
            var a = AddFaction("OutlanderCivil");
            var b = AddFaction("TribeCivil");
            var c = AddFaction("OutlanderRough");
            SetBothSides(a, c, 0, FactionRelationKind.Neutral);
            SetBothSides(a, b, 0, FactionRelationKind.Neutral);
            var bSide = b.RelationWith(a);
            bSide.baseGoodwill = -80;
            bSide.kind = FactionRelationKind.Hostile;
            s.AreEqual(0, a.GoodwillWith(b), "the first faction's own goodwill with the second, as the game holds it");
            s.AreEqual(-80, b.GoodwillWith(a), "the second faction's goodwill with the first, as the game holds it");
            s.Tick(30);

            var labels = VisibleLabels();
            var page = new OpenPage();
            var open = OpenFactionsPage(s, page);
            while (open.MoveNext()) yield return open.Current;
            var settled = new SlotRead();
            var settle = Settle(s, labels, settled);
            while (settle.MoveNext()) yield return settle.Current;

            var click = ClickNode(s, LabelOf(a), labels);
            while (click.MoveNext()) yield return click.Current;
            settle = Settle(s, labels, settled);
            while (settle.MoveNext()) yield return settle.Current;
            var frame = s.DrawFrame();
            yield return frame;
            s.Check(Details(frame).Any(t => t.Text.Contains(LabelOf(b)) && t.Text.EndsWith(": -80")), "the details of '" + LabelOf(a) + "' should list '" + LabelOf(b) + "' with goodwill -80, the value the game holds for it; they show: " + string.Join(" | ", Details(frame).Select(t => t.Text)));

            var shot = new Capture();
            var take = TakeCapture(s, "faction-page-goodwill-line", shot);
            while (take.MoveNext()) yield return take.Current;
            var read = ReadSlots(shot.Texts, labels);
            var from = read.Slots.Single(n => n.name == LabelOf(a)).slot.center;
            var toB = read.Slots.Single(n => n.name == LabelOf(b)).slot.center;
            var toC = read.Slots.Single(n => n.name == LabelOf(c)).slot.center;
            var toBRed = shot.FractionAlong(from, toB, Reddish);
            var toCRed = shot.FractionAlong(from, toC, Reddish);
            s.Check(toBRed >= 0.9f, "the line from '" + LabelOf(a) + "' to '" + LabelOf(b) + "' should be drawn red for goodwill -80; red along it: " + toBRed);
            s.Check(toCRed <= 0.2f, "the line from '" + LabelOf(a) + "' to '" + LabelOf(c) + "' at goodwill 0 should not be red; red along it: " + toCRed);

            click = ClickNode(s, LabelOf(b), labels);
            while (click.MoveNext()) yield return click.Current;
            frame = s.DrawFrame();
            yield return frame;
            s.Check(Details(frame).Any(t => t.Text.Contains(LabelOf(a)) && t.Text.EndsWith(": 0")), "the details of '" + LabelOf(b) + "' should list '" + LabelOf(a) + "' with goodwill 0, the value the game holds for that side; they show: " + string.Join(" | ", Details(frame).Select(t => t.Text)));

            settle = Settle(s, labels, settled);
            while (settle.MoveNext()) yield return settle.Current;
            var shotB = new Capture();
            take = TakeCapture(s, "faction-page-goodwill-line-from-b", shotB);
            while (take.MoveNext()) yield return take.Current;
            var readB = ReadSlots(shotB.Texts, labels);
            var bCenter = readB.Slots.Single(n => n.name == LabelOf(b)).slot.center;
            var aCenter = readB.Slots.Single(n => n.name == LabelOf(a)).slot.center;
            var bToARed = shotB.FractionAlong(bCenter, aCenter, Reddish);
            s.Check(bToARed <= 0.2f, "with '" + LabelOf(b) + "' selected, the line to '" + LabelOf(a) + "' should follow the goodwill 0 the game holds for '" + LabelOf(a) + "' and not be red; red along it: " + bToARed);

            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
