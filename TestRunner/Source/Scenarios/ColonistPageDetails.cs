using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class ColonistPageDetails
    {
        static string TitleOf(ColonistPage page, IReadOnlyList<DrawnText> texts, IEnumerable<Pawn> candidates) {
            var detail = page.DetailArea();
            var full = candidates.Select(p => p.Name.ToStringFull).ToHashSet();
            return texts.Where(t => detail.Contains(t.Ink.center) && full.Contains(t.Text)).OrderBy(t => t.Ink.y).Select(t => t.Text).FirstOrDefault();
        }

        static IEnumerable<DrawnText> RowOf(IReadOnlyList<DrawnText> texts, DrawnText name) =>
            texts.Where(t => System.Math.Abs(t.Ink.center.y - name.Ink.center.y) < 6f);

        [Scenario("colonist-page-click-lists-relations-and-opinions-of-the-game", MapSize = 40, Windowed = true, OnlyOn = "1.7")]
        static IEnumerator ClickListsRelations(ScenarioContext s) {
            var a = ColonistPage.Spawn(s, -6);
            var b = ColonistPage.Spawn(s, 0);
            var c = ColonistPage.Spawn(s, 6);
            var all = new[] { a, b, c };
            a.relations.AddDirectRelation(PawnRelationDefOf.Lover, b);
            ColonistPage.SetOpinion(a, b, ThoughtDef.Named("KindWords"), o => o >= 35);
            ColonistPage.SetOpinion(b, a, ThoughtDef.Named("Insulted"), o => o <= -10);
            ColonistPage.SetOpinion(a, c, ThoughtDef.Named("Insulted"), o => o <= -10);
            s.Check(a.GetRelations(b).Contains(PawnRelationDefOf.Lover), "setup: a and b should be lovers");

            var page = new ColonistPage(s);
            yield return page.Open();
            yield return page.Frames(150);
            var openingTitle = TitleOf(page, page.Latest.Texts, all);
            s.Check(openingTitle != null, "the details panel should show a colonist's name when the tab opens. Drawn: " + Dump(page.Latest.Texts));
            var target = openingTitle == a.Name.ToStringFull ? b : a;
            var other = target == a ? b : a;
            s.Check(openingTitle != target.Name.ToStringFull, "before the click the details show somebody else than the colonist about to be clicked");

            var centers = page.NodeCenters(page.Latest.Texts, all.Select(p => p.LabelShort));
            s.AreEqual(3, centers.Count, "the three colonists are nodes");
            yield return page.ClickPixel(centers[target.LabelShort]);
            yield return page.Frames(3);

            var texts = page.Latest.Texts;
            s.AreEqual(target.Name.ToStringFull, TitleOf(page, texts, all), "after the click the details panel is titled with the clicked colonist");
            var detail = page.DetailArea();
            var inPanel = texts.Where(t => detail.Contains(t.Ink.center)).ToList();
            foreach (var q in all.Where(p => p != target)) {
                var mine = target.relations.OpinionOf(q);
                var his = q.relations.OpinionOf(target);
                if (mine == 0 && his == 0 && target.GetRelations(q).Count() == 0) continue;
                var name = inPanel.FirstOrDefault(t => t.Text == q.Name.ToStringFull && t.Ink.y > inPanel.First(x => x.Text == target.Name.ToStringFull).Ink.y + 40f);
                s.Check(name != null, "the details should list " + q.Name.ToStringFull + " with " + target.LabelShort + ". Drawn: " + Dump(inPanel));
                if (name == null) continue;
                var row = RowOf(inPanel, name).Select(t => t.Text).ToList();
                s.Check(row.Contains(mine.ToStringWithSign()), q.LabelShort + "'s row should show " + target.LabelShort + "'s opinion " + mine.ToStringWithSign() + ". Row: " + string.Join(" | ", row));
                s.Check(row.Contains("(" + his.ToStringWithSign() + ")"), q.LabelShort + "'s row should show their opinion of " + target.LabelShort + " as (" + his.ToStringWithSign() + "). Row: " + string.Join(" | ", row));
                if (q == other)
                    s.Check(row.Contains(PawnRelationDefOf.Lover.GetGenderSpecificLabelCap(q)), "the row of the lover should show the relation '" + PawnRelationDefOf.Lover.GetGenderSpecificLabelCap(q) + "'. Row: " + string.Join(" | ", row));
            }
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("colonist-page-ideoligion-icon-on-each-colonist", MapSize = 40, Windowed = true, OnlyOn = "1.7", Mods = "ludeon.rimworld.ideology")]
        static IEnumerator IdeoIcons(ScenarioContext s) {
            var pawns = new[] { ColonistPage.Spawn(s, -6), ColonistPage.Spawn(s, 0), ColonistPage.Spawn(s, 6) };
            foreach (var p in pawns) s.Check(p.Ideo != null, "setup: " + p.LabelShort + " should follow an ideoligion");
            var page = new ColonistPage(s);
            yield return page.Open();
            yield return page.Frames(60);
            var grab = new Capture();
            yield return FactionPageSupport.TakeCapture(s, "colonist-page-ideo-icons", grab, 10);
            var centers = page.NodeCenters(grab.Texts, pawns.Select(p => p.LabelShort));
            s.AreEqual(3, centers.Count, "the three colonists are nodes");
            var scale = Prefs.UIScale;
            foreach (var p in pawns.Where(p => centers.ContainsKey(p.LabelShort) && p.Ideo != null)) {
                var color = p.Ideo.Color;
                bool Near((byte r, byte g, byte b) px) => Mathf.Abs(px.r - color.r * 255f) < 30f && Mathf.Abs(px.g - color.g * 255f) < 30f && Mathf.Abs(px.b - color.b * 255f) < 30f;
                var topLeft = centers[p.LabelShort] - new Vector2(SlotHalf, SlotHalf) * scale;
                var icon = new Rect(topLeft.x, topLeft.y, 16f * scale, 16f * scale);
                var beside = new Rect(topLeft.x, topLeft.y + 22f * scale, 16f * scale, 16f * scale);
                var inIcon = grab.CountIn(icon, Near);
                var inControl = grab.CountIn(beside, Near);
                s.Check(inIcon >= 15, p.LabelShort + "'s slot should show the ideoligion icon in " + p.Ideo.name + "'s color " + color + " at its top left corner; pixels of that color there: " + inIcon);
                s.Check(inControl < 5, "the empty left edge of the same slot, under the icon, should not hold the ideoligion color; pixels: " + inControl);
            }
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("colonist-page-dragged-colonist-pins-and-stays-in-the-graph", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 150)]
        static IEnumerator DragPins(ScenarioContext s) {
            var pawns = new[] { ColonistPage.Spawn(s, -6), ColonistPage.Spawn(s, 0), ColonistPage.Spawn(s, 6) };
            var names = pawns.Select(p => p.LabelShort).ToList();
            var page = new ColonistPage(s);
            yield return page.Open();
            yield return page.Frames(150);
            var start = page.NodeCenters(page.Latest.Texts, names);
            s.AreEqual(3, start.Count, "the three colonists are nodes");
            if (start.Count != 3) yield break;
            var dragged = pawns[0].LabelShort;
            var graph = GraphAreaPixels(RelationsWindow);
            var scale = Prefs.UIScale;
            var half = SlotHalf * scale;

            var inside = graph.center + new Vector2(-250f, -200f);
            yield return PointerHold.Drag(start[dragged], inside, 60);
            yield return page.Frames(3);
            var dropped = page.NodeCenters(page.Latest.Texts, names);
            s.Check((dropped[dragged] - inside).magnitude < (start[dragged] - inside).magnitude / 4f, "dragging a colonist inside the graph should drop it near where the mouse let go: wanted " + inside + ", it is at " + dropped[dragged]);
            s.Check((dropped[dragged] - start[dragged]).magnitude > 100f, "the dragged colonist should have moved away from " + start[dragged]);
            yield return page.Frames(120);
            var later = page.NodeCenters(page.Latest.Texts, names);
            s.Check((later[dragged] - dropped[dragged]).magnitude < 1f, "a dragged colonist is pinned and should stay where it was dropped; it moved from " + dropped[dragged] + " to " + later[dragged]);
            s.Check(names.Where(n => n != dragged).Any(n => (later[n] - start[n]).magnitude > 3f), "the other colonists should react to the pinned one and move");

            foreach (var n in names.Where(n => n != dragged)) {
                var at = page.NodeCenters(page.Latest.Texts, names)[n];
                yield return PointerHold.Drag(at, at + new Vector2(40f, 40f), 20);
                yield return page.Frames(3);
            }
            later = page.NodeCenters(page.Latest.Texts, names);
            var farOutside = new Vector2(graph.x - 250f, later[dragged].y);
            yield return PointerHold.Drag(later[dragged], farOutside, 200);
            yield return page.Frames(3);
            var clamped = page.NodeCenters(page.Latest.Texts, names);
            s.Check(clamped.ContainsKey(dragged), "the dragged colonist should still be drawn after being dragged out of the graph; graph " + graph + " later " + later[dragged] + " target " + farOutside + " drawn " + string.Join(" | ", page.Latest.Texts.Where(t => names.Contains(t.Text)).Select(t => t.ToString())));
            if (clamped.ContainsKey(dragged)) {
                var p = clamped[dragged];
                s.Check(p.x >= graph.x + half - 2f && p.x <= graph.xMax - half + 2f && p.y >= graph.y + half - 2f && p.y <= graph.yMax - half + 2f,
                    "a colonist dragged beyond the graph should stay inside the graph area " + graph + " with its whole slot visible; it is at " + p);
            }
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        const float SlotHalf = 35f;
    }
}
