using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimhunterTestRunner.Scenarios
{
    class SlotRead
    {
        public List<(string name, Rect slot)> Slots = new List<(string, Rect)>();
    }

    static class LayoutScenarioHelpers
    {
        public const string TabWindowType = "Fluffy_Relations.MainTabWindow_Relations";
        public const float SlotSize = 70f;
        public const float LabelHeight = 16f;
        public const float WindowMargin = 18f;
        public const float IconSize = 24f;
        public const float IconStep = 40f;
        public const float IconInset = 16f;

        public static MainButtonDef RelationsButton =>
            DefDatabase<MainButtonDef>.AllDefsListForReading.Single(d => d.tabWindowClass?.FullName == TabWindowType);

        public static Window RelationsWindow =>
            Find.WindowStack.Windows.FirstOrDefault(w => w.GetType().FullName == TabWindowType);

        public static List<(MainButtonDef def, Rect pixels)> BarButtons() {
            var buttons = DefDatabase<MainButtonDef>.AllDefs.OrderBy(d => d.order).Where(d => d.Worker.Visible).ToList();
            var wide = (int)(UI.screenWidth / buttons.Sum(d => d.minimized ? 0.5f : 1f));
            var result = new List<(MainButtonDef, Rect)>();
            var left = 0;
            for (var i = 0; i < buttons.Count; i++) {
                var width = buttons[i].minimized ? wide / 2 : wide;
                if (i == buttons.Count - 1) width = UI.screenWidth - left;
                var scale = Prefs.UIScale;
                result.Add((buttons[i], new Rect(left * scale, (UI.screenHeight - 35) * scale, width * scale, 36f * scale)));
                left += width;
            }
            return result;
        }

        public static Rect RelationsButtonPixels() {
            var relations = RelationsButton;
            var found = BarButtons().Where(b => b.def == relations).ToList();
            if (found.Count != 1) throw new ScenarioFailure("the Relations main button is not in the visible bottom bar");
            return found[0].pixels;
        }

        public static IEnumerator ClickRelationsButton(ScenarioContext s) {
            var target = RelationsButtonPixels();
            yield return s.Click(new DrawnText { Text = RelationsButton.LabelCap.ToString(), Box = target, Ink = target });
        }

        public static string Dump(IEnumerable<DrawnText> texts) => string.Join(" | ", texts.Select(t => t.ToString()));

        public const int SettleStepFrames = 60;
        public const int SettleBoundSteps = 30;
        public const float SettledWithinPixels = 0.5f;

        public static IEnumerator ReadNamed(ScenarioContext s, IList<string> names, string what, SlotRead into) {
            var frame = s.DrawFrame();
            yield return frame;
            var read = ReadSlots(frame.Texts, names);
            s.AreEqual(string.Join(",", names.OrderBy(n => n)), string.Join(",", read.Slots.Select(n => n.name).OrderBy(n => n)), "the node labels drawn " + what);
            into.Slots = read.Slots;
        }

        public static IEnumerator SettleLayout(ScenarioContext s, Func<SlotRead, IEnumerator> read, SlotRead settled) {
            var previous = new SlotRead();
            var step = read(previous);
            while (step.MoveNext()) yield return step.Current;
            for (var i = 0; i < SettleBoundSteps; i++) {
                yield return ScenarioContext.Frames(SettleStepFrames);
                var current = new SlotRead();
                step = read(current);
                while (step.MoveNext()) yield return step.Current;
                if (MaxMove(previous, current) <= SettledWithinPixels) {
                    settled.Slots = current.Slots;
                    yield break;
                }
                previous = current;
            }
            throw new ScenarioFailure("the layout did not settle within " + SettleBoundSteps * SettleStepFrames + " frames");
        }

        public static Rect CanvasPixels(Window window) {
            var scale = Prefs.UIScale;
            var r = window.windowRect;
            return new Rect((r.x + WindowMargin) * scale, (r.y + WindowMargin) * scale, (r.width - 2f * WindowMargin) * scale, (r.height - 2f * WindowMargin) * scale);
        }

        public static Rect GraphAreaPixels(Window window) {
            var scale = Prefs.UIScale;
            var net = Traverse.Create(window).Field("networkRect").GetValue<Rect>();
            var canvas = CanvasPixels(window);
            var area = new Rect(canvas.x + net.x * scale, canvas.y + net.y * scale, net.width * scale, net.height * scale);
            if (area.width <= 0f || Mathf.Abs(area.width - area.height) > 0.5f)
                throw new ScenarioFailure("the graph area should be a square, it is " + area);
            if (area.x < canvas.x - 0.5f || area.y < canvas.y - 0.5f || area.xMax > canvas.xMax + 0.5f || area.yMax > canvas.yMax + 0.5f)
                throw new ScenarioFailure("the graph area " + area + " should be inside the window canvas " + canvas);
            return area;
        }

        public static Vector2 IconCenterPixels(Window window, int index) {
            var scale = Prefs.UIScale;
            var canvas = CanvasPixels(window);
            return new Vector2(canvas.xMax - (IconStep * index - IconSize / 2f) * scale, canvas.y + (IconInset + IconSize / 2f) * scale);
        }

        public static DrawnText ClickTarget(Vector2 point) =>
            new DrawnText { Text = "", Box = new Rect(point.x - 1f, point.y - 1f, 2f, 2f), Ink = new Rect(point.x - 1f, point.y - 1f, 2f, 2f) };

        public static SlotRead ReadSlots(IEnumerable<DrawnText> texts, IEnumerable<string> names) {
            var scale = Prefs.UIScale;
            var wanted = new HashSet<string>(names);
            var read = new SlotRead();
            foreach (var t in texts.Where(t => wanted.Contains(t.Text) && Mathf.Abs(t.Box.height / scale - LabelHeight) < 0.5f))
                read.Slots.Add((t.Text, new Rect(t.Box.center.x - SlotSize * scale / 2f, t.Box.yMax - SlotSize * scale, SlotSize * scale, SlotSize * scale)));
            return read;
        }

        public static string OutsideGraphArea(Rect area, SlotRead read) =>
            string.Join("; ", read.Slots
                .Where(n => n.slot.x < area.x - 1f || n.slot.y < area.y - 1f || n.slot.xMax > area.xMax + 1f || n.slot.yMax > area.yMax + 1f)
                .Select(n => "'" + n.name + "' slot " + n.slot));

        public static string Stacked(SlotRead read, float minCenterDistance) {
            var problems = new List<string>();
            for (var i = 0; i < read.Slots.Count; i++)
                for (var j = i + 1; j < read.Slots.Count; j++)
                    if ((read.Slots[i].slot.center - read.Slots[j].slot.center).magnitude < minCenterDistance * Prefs.UIScale)
                        problems.Add("'" + read.Slots[i].name + "' and '" + read.Slots[j].name + "' at " + read.Slots[i].slot.center + " and " + read.Slots[j].slot.center);
            return string.Join("; ", problems);
        }

        public const float SlotsPerNode = 3f;
        public const float MaxRequiredAreaShare = 0.5f;

        public static string Overlapping(SlotRead read) {
            var problems = new List<string>();
            for (var i = 0; i < read.Slots.Count; i++)
                for (var j = i + 1; j < read.Slots.Count; j++)
                    if (read.Slots[i].slot.Overlaps(read.Slots[j].slot))
                        problems.Add("'" + read.Slots[i].name + "' " + read.Slots[i].slot + " and '" + read.Slots[j].name + "' " + read.Slots[j].slot);
            return string.Join("; ", problems);
        }

        public static string TooCompact(Rect area, SlotRead read) {
            var slot = SlotSize * Prefs.UIScale;
            var left = read.Slots.Min(n => n.slot.xMin);
            var right = read.Slots.Max(n => n.slot.xMax);
            var top = read.Slots.Min(n => n.slot.yMin);
            var bottom = read.Slots.Max(n => n.slot.yMax);
            var covered = (right - left) * (bottom - top);
            var required = Mathf.Min(read.Slots.Count * SlotsPerNode * slot * slot, MaxRequiredAreaShare * area.width * area.height);
            if (covered >= required) return "";
            return "the slots' bounding box covers " + covered + " px squared of the graph area's " + area.width * area.height + "; " + read.Slots.Count + " nodes need at least " + required;
        }

        public static float MaxMove(SlotRead a, SlotRead b) {
            if (a.Slots.Count != b.Slots.Count) return float.MaxValue;
            var most = 0f;
            for (var i = 0; i < a.Slots.Count; i++) {
                if (a.Slots[i].name != b.Slots[i].name) return float.MaxValue;
                most = Mathf.Max(most, (a.Slots[i].slot.center - b.Slots[i].slot.center).magnitude);
            }
            return most;
        }
    }
}
