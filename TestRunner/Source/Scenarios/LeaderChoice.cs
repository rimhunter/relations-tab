using System.Collections;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionPageSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;
using static RimhunterTestRunner.Scenarios.LeaderSupport;

namespace RimhunterTestRunner.Scenarios
{
    static class LeaderChoice
    {
        const int FactionCount = 90;

        static DrawnText LastRelationRow(ScenarioContext.DrawnFrame frame) {
            var rows = Details(frame).Where(r => r.Text.Contains(": ")).ToList();
            if (rows.Count == 0) throw new ScenarioFailure("no relations rows are drawn in the details");
            return rows[rows.Count - 1];
        }

        [Scenario("leader-button-is-clear-of-the-scrollbar-and-opens-the-leader-choice", MapSize = 60, Windowed = true, OnlyOn = "1.7", MaxSeconds = 170)]
        static IEnumerator ButtonClickable(ScenarioContext s) {
            SetChosenLeader(null);
            var existing = Find.FactionManager.AllFactionsListForReading.ToList();
            try {
                var run = ButtonClickableBody(s);
                while (run.MoveNext()) yield return run.Current;
            }
            finally {
                SetChosenLeader(null);
                PointerHold.Release();
                foreach (var added in Find.FactionManager.AllFactionsListForReading.Where(f => !existing.Contains(f)).ToList())
                    added.hidden = true;
            }
        }

        static IEnumerator ButtonClickableBody(ScenarioContext s) {
            var oldest = SpawnColonistAged(s, -4, 110);
            var middle = SpawnColonistAged(s, 0, 90);
            var youngest = SpawnColonistAged(s, 4, 70);
            foreach (var i in Enumerable.Range(0, FactionCount))
                FactionGenerator.CreateFactionAndAddToManager(DefDatabase<FactionDef>.GetNamed(FactionDefNames[i % FactionDefNames.Length]));
            s.Tick(30);
            var labels = VisibleLabels();
            s.Check(labels.Count > FactionCount, "more than " + FactionCount + " visible factions should exist; there are " + labels.Count);

            var page = new OpenPage();
            var open = OpenFactionsPage(s, page);
            while (open.MoveNext()) yield return open.Current;
            Traverse.Create(page.Window).Property("SelectedFaction").SetValue(Faction.OfPlayer);
            yield return ScenarioContext.Frames(3);

            LeaderProbe.Begin(true);
            yield return ScenarioContext.Frames(2);
            var (drawn, all) = LeaderProbe.End();
            var buttons = all.GroupBy(b => b.Pixels).Select(g => g.First()).ToList();
            s.AreEqual(1, buttons.Count, "change-leader buttons drawn for the colony");
            if (buttons.Count != 1) throw new ScenarioFailure("no change-leader button to click");
            var button = buttons[0];
            s.Check(button.ScrollViewHeight > button.ScrollOutHeight, "the colony details should be taller than the panel so the scrollbar shows; content " + button.ScrollViewHeight + ", panel " + button.ScrollOutHeight);
            var scrollbar = GenUI.ScrollBarWidth * Prefs.UIScale;
            s.Check(button.Pixels.xMax <= button.ScrollOutPixels.xMax - scrollbar + 0.5f,
                "the change-leader button " + button.Pixels + " should end left of the scrollbar strip, which starts at x " + (button.ScrollOutPixels.xMax - scrollbar) + " in the panel " + button.ScrollOutPixels);
            s.Check(button.Pixels.xMin >= button.ScrollOutPixels.xMin - 0.5f && button.Pixels.yMin >= button.ScrollOutPixels.yMin - 0.5f && button.Pixels.yMax <= button.ScrollOutPixels.yMax + 0.5f,
                "the change-leader button " + button.Pixels + " should be inside the panel " + button.ScrollOutPixels);
            var textFrame = s.DrawFrame();
            yield return textFrame;
            var underScrollbar = Details(textFrame).Where(t => t.Box.xMax > button.ScrollOutPixels.xMax - scrollbar + 0.5f).Select(t => t.ToString()).ToList();
            s.Check(underScrollbar.Count == 0, "no detail text should be laid out under the scrollbar, which starts at x " + (button.ScrollOutPixels.xMax - scrollbar) + "; under it: " + string.Join(" | ", underScrollbar.Take(5)));
            var lastBefore = LastRelationRow(textFrame);
            s.Check(lastBefore.Box.yMax > button.ScrollOutPixels.yMax, "before scrolling, the last relations row should lie below the panel (else the details do not need to scroll); it is at " + lastBefore);
            var maxScroll = button.ScrollViewHeight - button.ScrollOutHeight;
            Traverse.Create(page.Window).Field("_factionDetailScrollPosition").SetValue(new Vector2(0f, maxScroll));
            yield return ScenarioContext.Frames(3);
            var endFrame = s.DrawFrame();
            yield return endFrame;
            var lastAfter = LastRelationRow(endFrame);
            s.Check(lastAfter.Box.yMin >= button.ScrollOutPixels.yMin - 0.5f && lastAfter.Box.yMax <= button.ScrollOutPixels.yMax + 0.5f,
                "with the details scrolled to their end, the last relations row " + lastAfter + " should be inside the panel " + button.ScrollOutPixels);
            s.Check(lastAfter.Text == lastBefore.Text, "the last relations row should be the same row after scrolling; before '" + lastBefore.Text + "', after '" + lastAfter.Text + "'");
            Traverse.Create(page.Window).Field("_factionDetailScrollPosition").SetValue(Vector2.zero);
            yield return ScenarioContext.Frames(3);
            s.AreEqual(oldest.Name.ToStringFull, LeaderRowOf(drawn), "the leader shown before a choice is made (the oldest colonist)");
            s.Check(Find.WindowStack.Windows.OfType<FloatMenu>().Count() == 0, "no leader choice should be open before the button is clicked");

            try {
                yield return s.Click(ClickTarget(button.Pixels.center));
                PointerHold.At(button.Pixels.center);
                yield return ScenarioContext.Frames(3);
                s.Check(Find.WindowStack.Windows.OfType<FloatMenu>().Count() == 1, "clicking the change-leader button at " + button.Pixels.center + " should open the leader choice; open windows: " + string.Join(", ", Find.WindowStack.Windows.Select(w => w.GetType().Name + " " + w.windowRect)));
                var menu = s.DrawFrame();
                yield return menu;
                var menuRect = Find.WindowStack.Windows.OfType<FloatMenu>().Single().windowRect;
                var scale = Prefs.UIScale;
                var inMenu = menu.Texts.Where(t => new Rect(menuRect.x * scale, menuRect.y * scale, menuRect.width * scale, menuRect.height * scale).Contains(t.Ink.center)).ToList();
                var names = new[] { oldest, middle, youngest }.Select(p => p.Name.ToStringShort).ToList();
                foreach (var name in names)
                    s.Check(inMenu.Any(t => t.Text == name), "the leader choice should list " + name + "; it drew: " + string.Join(" | ", inMenu));
                var choice = inMenu.FirstOrDefault(t => t.Text == youngest.Name.ToStringShort);
                if (choice == null) throw new ScenarioFailure("the leader choice does not list " + youngest.Name.ToStringShort);
                PointerHold.Release();
                yield return s.Click(choice);
                yield return ScenarioContext.Frames(4);

            }
            finally {
                PointerHold.Release();
            }

            LeaderProbe.Begin(true);
            yield return ScenarioContext.Frames(2);
            var (after, _) = LeaderProbe.End();
            s.AreEqual(youngest.Name.ToStringFull, LeaderRowOf(after), "the leader shown after the youngest colonist was chosen");
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
