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
    static class FactionPageEveryFaction
    {
        const int MinIconPixelsAtScaleOne = 100;
        const int MinCornerIconPixelsAtScaleOne = 15;

        static IEnumerator Body(ScenarioContext s, bool ideology) {
            for (var dx = -8; dx <= 8; dx += 8) s.SpawnColonist(s.Cell(dx, 0));
            var insect = Find.FactionManager.OfInsects;
            s.Check(insect.Hidden && insect.leader == null, "before the test the Insect faction should be hidden and have no leader");
            insect.hidden = false;
            insect.temporary = ideology;
            insect.color = InsectColor;
            var leaderless = MakeLeaderlessHumanlike();
            leaderless.color = LeaderlessColor;
            var normal = AddFaction("TribeCivil");
            normal.color = NormalColor;
            var hiddenWithLeader = AddFaction("OutlanderCivil");
            hiddenWithLeader.hidden = true;
            s.Check(leaderless.leader == null && !leaderless.TryGenerateNewLeader(), "the test faction should be one the game cannot give a leader");
            s.Check(hiddenWithLeader.leader != null && hiddenWithLeader.Hidden, "the hidden faction should have a leader and be hidden");
            s.Check(!leaderless.Hidden && !insect.Hidden, "both leaderless factions should be visible");
            s.Check(ModsConfig.IdeologyActive == ideology, "Ideology should be " + (ideology ? "active" : "off") + " in this launch");
            s.Tick(30);

            var expected = VisibleLabels();
            var hiddenLabel = hiddenWithLeader.GetCallLabel();
            s.Check(expected.Contains(LabelOf(insect)) && expected.Contains(LabelOf(leaderless)) && expected.Contains(LabelOf(normal)), "the visible faction list should hold the Insect, the leaderless and the normal faction: " + string.Join(", ", expected));
            s.Check(!expected.Contains(hiddenLabel), "the visible faction list should not hold the hidden faction '" + hiddenLabel + "'");

            using (var log = new LogWatch()) {
                var page = new OpenPage();
                var open = OpenFactionsPage(s, page);
                while (open.MoveNext()) yield return open.Current;
                var settled = new SlotRead();
                var settle = Settle(s, expected, settled);
                while (settle.MoveNext()) yield return settle.Current;
                var firstSettled = settled.Slots.Count;
                s.AreEqual(expected.Count, firstSettled, "faction nodes drawn once the layout settled");

                s.AreEqual(string.Join(",", expected.OrderBy(n => n)), string.Join(",", settled.Slots.Select(n => n.name).OrderBy(n => n)), "the faction node labels drawn on the factions page");
                var hiddenFrame = s.DrawFrame();
                yield return hiddenFrame;
                s.AreEqual(0, ReadSlots(hiddenFrame.Texts, new[] { hiddenLabel }).Slots.Count, "nodes drawn for the hidden faction");

                var preselect = ClickNode(s, LabelOf(leaderless), expected);
                while (preselect.MoveNext()) yield return preselect.Current;
                settle = Settle(s, expected, settled);
                while (settle.MoveNext()) yield return settle.Current;

                var shot = new Capture();
                var take = TakeCapture(s, "faction-page-" + (ideology ? "ideology" : "core"), shot);
                while (take.MoveNext()) yield return take.Current;
                var area = GraphAreaPixels(page.Window);
                var after = ReadSlots(shot.Texts, expected);
                s.Check(OutsideGraphArea(area, after).Length == 0, "every node should be inside the graph area " + area + "; outside: " + OutsideGraphArea(area, after));
                foreach (var (faction, color, what) in new[] { (insect, InsectColor, "the Insect faction"), (leaderless, LeaderlessColor, "the leaderless humanlike faction") }) {
                    var slot = after.Slots.Single(n => n.name == LabelOf(faction)).slot;
                    var pixels = PixelsNear(shot, IconArea(slot), color);
                    var needed = MinIconPixelsAtScaleOne * Prefs.UIScale * Prefs.UIScale;
                    s.Check(pixels >= needed, what + " has no leader, so its faction icon in its own color should fill the middle of its slot (at least " + needed + " pixels); there are " + pixels);
                    var inCorner = PixelsNear(shot, CornerArea(slot), color);
                    s.Check(inCorner == 0, what + " already shows its faction icon as its slot, so the small corner icon should not repeat it; pixels of its color in the top left corner box: " + inCorner);
                }
                var normalSlot = after.Slots.Single(n => n.name == LabelOf(normal)).slot;
                var normalCorner = PixelsNear(shot, CornerArea(normalSlot), NormalColor);
                var cornerNeeded = MinCornerIconPixelsAtScaleOne * Prefs.UIScale * Prefs.UIScale;
                s.Check(normalCorner >= cornerNeeded, "a faction with a leader shows its leader in the slot, so its small faction icon in its own color should stay in the top left corner (at least " + cornerNeeded + " pixels); there are " + normalCorner);

                var details = new List<DrawnText>();
                foreach (var f in new[] { insect, leaderless, normal }) {
                    var click = ClickNode(s, LabelOf(f), expected);
                    while (click.MoveNext()) yield return click.Current;
                    var frame = s.DrawFrame();
                    yield return frame;
                    var texts = Details(frame).Select(t => t.Text).ToList();
                    var all = string.Join(" | ", texts);
                    s.Check(texts.Any(t => t == LabelOf(f)), "the details of '" + LabelOf(f) + "' should show its name; they show: " + all);
                    s.Check(texts.Any(t => t.Contains(f.def.leaderTitle.CapitalizeFirst() + ": ")), "the details of '" + LabelOf(f) + "' should show the leader line; they show: " + all);
                    if (f == normal) {
                        var leaderName = normal.leader.Name.ToStringFull;
                        s.Check(texts.Any(t => t.Contains(leaderName)), "the details of '" + LabelOf(f) + "' should name its leader '" + leaderName + "'; they show: " + all);
                    } else {
                        s.Check(texts.Any(t => t.EndsWith(": " + "Nobody".Translate())), "the details of the leaderless '" + LabelOf(f) + "' should say '" + "Nobody".Translate() + "' for the leader; they show: " + all);
                    }
                    foreach (var other in Find.FactionManager.AllFactionsVisible.Where(o => o != f && o.RelationWith(f, true) != null)) {
                        var row = ": " + Mathf.RoundToInt(other.GoodwillWith(f));
                        s.Check(texts.Any(t => t.Contains(LabelOf(other)) && t.EndsWith(row)), "the details of '" + LabelOf(f) + "' should list '" + LabelOf(other) + "' with the goodwill the game holds (ending '" + row + "'); they show: " + all);
                    }
                    var ideoTitle = "Fluffy_Relations.Ideoligion".Translate().CapitalizeFirst().ToString();
                    if (ideology && f.ideos?.PrimaryIdeo != null) {
                        s.Check(texts.Contains(ideoTitle) && texts.Any(t => t.Contains(f.ideos.PrimaryIdeo.name)), "the details of '" + LabelOf(f) + "' should show its primary ideoligion '" + f.ideos.PrimaryIdeo.name + "' under '" + ideoTitle + "'; they show: " + all);
                    } else {
                        s.Check(!texts.Contains(ideoTitle), "the details of '" + LabelOf(f) + "' should not show an ideoligion block; they show: " + all);
                    }
                }

                var rowClick = ClickDetailRow(s, LabelOf(insect), ": " + Mathf.RoundToInt(insect.GoodwillWith(normal)));
                while (rowClick.MoveNext()) yield return rowClick.Current;
                var afterRow = s.DrawFrame();
                yield return afterRow;
                s.Check(Details(afterRow).Any(t => t.Text == LabelOf(insect)), "clicking the Insect faction's row in the details should select it; the details show: " + string.Join(" | ", Details(afterRow).Select(t => t.Text)));
                rowClick = ClickDetailRow(s, LabelOf(leaderless), ": " + Mathf.RoundToInt(leaderless.GoodwillWith(insect)));
                while (rowClick.MoveNext()) yield return rowClick.Current;
                afterRow = s.DrawFrame();
                yield return afterRow;
                s.Check(Details(afterRow).Any(t => t.Text == LabelOf(leaderless)), "clicking the leaderless faction's row in the details should select it; the details show: " + string.Join(" | ", Details(afterRow).Select(t => t.Text)));

                yield return ScenarioContext.Frames(60);
                s.Check(log.NoLeaderLines.Length == 0, "the log should hold no 'has no leader' line after the page was open and used; it holds: " + log.NoLeaderLines);
            }
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("faction-page-shows-every-visible-faction-leaderless-included", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 150)]
        static IEnumerator Core(ScenarioContext s) => Body(s, false);

        [Scenario("faction-page-shows-leaderless-factions-with-ideology", MapSize = 40, Windowed = true, OnlyOn = "1.7", Mods = "ludeon.rimworld.ideology", MaxSeconds = 150)]
        static IEnumerator WithIdeology(ScenarioContext s) => Body(s, true);
    }
}
