using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    sealed class OpenPage
    {
        public Window Window;
    }

    sealed class LogWatch : IDisposable
    {
        readonly List<string> noLeaderLines = new List<string>();

        public LogWatch() {
            Application.logMessageReceivedThreaded += OnLog;
        }

        void OnLog(string condition, string stackTrace, LogType type) {
            if (condition.Contains("has no leader")) lock (noLeaderLines) noLeaderLines.Add(condition);
        }

        public string NoLeaderLines {
            get { lock (noLeaderLines) return string.Join(" || ", noLeaderLines); }
        }

        public void Dispose() {
            Application.logMessageReceivedThreaded -= OnLog;
        }
    }

    sealed class Capture
    {
        public Png Image;
        public IReadOnlyList<DrawnText> Texts = new List<DrawnText>();

        public int CountIn(Rect area, Func<(byte r, byte g, byte b), bool> color) {
            var n = 0;
            for (var y = Mathf.Max(0, (int)area.yMin); y < Mathf.Min(Image.Height, (int)area.yMax); y++)
                for (var x = Mathf.Max(0, (int)area.xMin); x < Mathf.Min(Image.Width, (int)area.xMax); x++)
                    if (color(Image.Pixel(x, y))) n++;
            return n;
        }

        public float FractionAlong(Vector2 from, Vector2 to, Func<(byte r, byte g, byte b), bool> color, float t0 = 0.25f, float t1 = 0.75f, int samples = 21, int radius = 2) {
            var hits = 0;
            for (var i = 0; i < samples; i++) {
                var p = Vector2.Lerp(from, to, Mathf.Lerp(t0, t1, i / (float)(samples - 1)));
                var hit = false;
                for (var dy = -radius; dy <= radius && !hit; dy++)
                    for (var dx = -radius; dx <= radius && !hit; dx++) {
                        int x = Mathf.RoundToInt(p.x) + dx, y = Mathf.RoundToInt(p.y) + dy;
                        if (x >= 0 && y >= 0 && x < Image.Width && y < Image.Height && color(Image.Pixel(x, y))) hit = true;
                    }
                if (hit) hits++;
            }
            return hits / (float)samples;
        }
    }

    static class FactionPageSupport
    {
        public static IEnumerator TakeCapture(ScenarioContext s, string name, Capture into, int settleFrames = 30) {
            var dir = Environment.GetEnvironmentVariable("RIMHUNTER_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(dir)) throw new InvalidOperationException("environment variable RIMHUNTER_SCREENSHOT_DIR is required to take a screenshot and is not set");
            var path = System.IO.Path.Combine(dir, name + ".png");
            if (System.IO.File.Exists(path)) throw new ScenarioFailure("screenshot " + path + " already exists");
            for (var i = 0; i < settleFrames; i++) yield return null;
            UiRecorder.Begin();
            yield return null;
            into.Texts = UiRecorder.End();
            ScreenCapture.CaptureScreenshot(path);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            byte[] data = null;
            while (data == null) {
                yield return null;
                if (System.IO.File.Exists(path)) {
                    var bytes = System.IO.File.ReadAllBytes(path);
                    if (Png.IsComplete(bytes)) data = bytes;
                }
                if (data == null && clock.Elapsed.TotalSeconds > 20) throw new ScenarioFailure("the game did not write the screenshot " + path + " within 20 s");
            }
            into.Image = Png.Decode(data);
            s.Check(into.Image.Width == Screen.width && into.Image.Height == Screen.height, "screenshot " + path + " is " + into.Image.Width + "x" + into.Image.Height + ", the screen is " + Screen.width + "x" + Screen.height);
        }

        public const string LeaderlessDefName = "FactionPageTestLeaderless";

        public static readonly string[] FactionDefNames = { "Pirate", "OutlanderCivil", "TribeCivil", "OutlanderRough", "TribeRough", "TribeSavage" };

        public static readonly Color LeaderlessColor = new Color(1f, 0f, 1f);
        public static readonly Color InsectColor = new Color(0f, 1f, 1f);
        public static readonly Color NormalColor = new Color(1f, 1f, 0f);

        public static void HideWorldFactions() {
            foreach (var f in Find.FactionManager.AllFactionsVisible.Where(f => !f.IsPlayer).ToList())
                f.hidden = true;
        }

        public static string LabelOf(Faction f) =>
            f.IsPlayer ? (f.HasName ? f.Name : "Fluffy.Relations.Colony".Translate().ToString()) : f.GetCallLabel();

        public static List<string> VisibleLabels() =>
            Find.FactionManager.AllFactionsVisible.Select(LabelOf).ToList();

        static FactionDef LeaderlessDef() {
            var existing = DefDatabase<FactionDef>.GetNamed(LeaderlessDefName, false);
            if (existing != null) return existing;
            var source = DefDatabase<FactionDef>.GetNamed("OutlanderCivil");
            var clone = (FactionDef)typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(source, null);
            clone.defName = LeaderlessDefName;
            clone.pawnGroupMakers = null;
            clone.fixedLeaderKinds = null;
            clone.generateNewLeaderFromMapMembersOnly = false;
            clone.requiredCountAtGameStart = 0;
            clone.maxCountAtGameStart = 0;
            clone.ResolveReferences();
            DefDatabase<FactionDef>.Add(clone);
            return clone;
        }

        public static Faction MakeLeaderlessHumanlike() {
            var clone = LeaderlessDef();
            var faction = AddFaction(clone);
            faction.temporary = true;
            return faction;
        }

        public static Faction AddFaction(FactionDef def) {
            FactionGenerator.CreateFactionAndAddToManager(def);
            return Find.FactionManager.AllFactionsListForReading.Last(f => f.def == def);
        }

        public static Faction AddFaction(string defName) => AddFaction(DefDatabase<FactionDef>.GetNamed(defName));

        public static IEnumerator OpenFactionsPage(ScenarioContext s, OpenPage into) {
            s.Check(RelationsWindow == null, "the Relations tab should be closed before the button is pressed");
            yield return ClickRelationsButton(s);
            yield return ScenarioContext.Frames(2);
            var window = RelationsWindow;
            s.Check(window != null, "clicking the Relations button should open the Relations tab");
            if (window == null) throw new ScenarioFailure("the Relations tab did not open");
            into.Window = window;
            var colonists = "Fluffy_Relations.Colonists".Translate().ToString();
            var factions = "Fluffy_Relations.Factions".Translate().ToString();
            var shot = s.DrawFrame();
            yield return shot;
            var canvas = CanvasPixels(window);
            var buttons = shot.Texts.Where(t => (t.Text == colonists || t.Text == factions) && canvas.Contains(t.Ink.center)).OrderBy(t => t.Ink.y).ToList();
            s.Check(buttons.Count > 0, "the page button should be drawn at the top left of the tab");
            if (buttons.Count == 0) throw new ScenarioFailure("no page button to switch to the factions page");
            if (buttons[0].Text == colonists) {
                yield return s.Click(buttons[0]);
                yield return ScenarioContext.Frames(2);
            }
            var after = s.DrawFrame();
            yield return after;
            s.Check(after.Texts.Any(t => t.Text == factions && canvas.Contains(t.Ink.center)), "the page button should read '" + factions + "' on the factions page");
        }

        public static IEnumerator SwitchPage(ScenarioContext s, Window window, string currentPageLabelKey) {
            var key = currentPageLabelKey.Translate().ToString();
            var frame = s.DrawFrame();
            yield return frame;
            var canvas = CanvasPixels(window);
            var button = frame.Texts.Where(t => t.Text == key && canvas.Contains(t.Ink.center)).OrderBy(t => t.Ink.y).FirstOrDefault();
            s.Check(button != null, "the page button '" + key + "' should be drawn at the top left of the tab");
            if (button == null) throw new ScenarioFailure("no page button '" + key + "' to click");
            yield return s.Click(button);
            yield return ScenarioContext.Frames(2);
        }

        public static IEnumerator ReadLabels(ScenarioContext s, IList<string> names, SlotRead into) {
            var frame = s.DrawFrame();
            yield return frame;
            into.Slots = ReadSlots(frame.Texts, names).Slots;
        }

        public static IEnumerator Settle(ScenarioContext s, IList<string> names, SlotRead settled) =>
            SettleLayout(s, into => ReadLabels(s, names, into), settled);

        static Vector2 FreePoint(Rect inside, IList<Rect> avoid) {
            var step = 4f * Prefs.UIScale;
            Vector2? free = null;
            var best = float.MaxValue;
            for (var x = inside.xMin + step; x < inside.xMax; x += step)
                for (var y = inside.yMin + step; y < inside.yMax; y += step) {
                    var p = new Vector2(x, y);
                    if (avoid.Any(o => o != inside && o.Contains(p))) continue;
                    var d = (p - inside.center).sqrMagnitude;
                    if (d < best) { best = d; free = p; }
                }
            if (free == null) throw new ScenarioFailure("no point of " + inside + " is free of the nodes, so it cannot be clicked");
            return free.Value;
        }

        public static IEnumerator ClickNode(ScenarioContext s, string label, IList<string> names) {
            var still = new SlotRead();
            var settle = Settle(s, names, still);
            while (settle.MoveNext()) yield return settle.Current;
            var matches = still.Slots.Where(n => n.name == label).ToList();
            s.AreEqual(1, matches.Count, "node labels drawn for '" + label + "'");
            if (matches.Count != 1) throw new ScenarioFailure("cannot click the node '" + label + "'");
            var target = matches[0].slot;
            var others = still.Slots.Where(n => n.slot != target).Select(n => n.slot).ToList();
            var free = FreePoint(target, others);
            yield return s.Click(ClickTarget(free));
            yield return ScenarioContext.Frames(3);
        }

        public static IEnumerator ClickDetailRow(ScenarioContext s, string label, string ending) {
            var frame = s.DrawFrame();
            yield return frame;
            var rows = frame.Texts.Where(t => t.InScrollView && t.Text.Contains(label) && t.Text.EndsWith(ending)).ToList();
            s.AreEqual(1, rows.Count, "detail rows for '" + label + "' ending in '" + ending + "'; drawn in the details: " + string.Join(" | ", frame.Texts.Where(t => t.InScrollView)));
            if (rows.Count != 1) throw new ScenarioFailure("cannot click the details row '" + label + "'");
            yield return s.Click(rows[0]);
            yield return ScenarioContext.Frames(3);
        }

        public static IEnumerable<DrawnText> Details(ScenarioContext.DrawnFrame frame) => frame.Texts.Where(t => t.InScrollView);

        public static int PixelsNear(Capture shot, Rect area, Color color) =>
            shot.CountIn(area, p => Mathf.Abs(p.r - color.r * 255f) < 40f && Mathf.Abs(p.g - color.g * 255f) < 40f && Mathf.Abs(p.b - color.b * 255f) < 40f);

        public static Rect IconArea(Rect slot) => slot.ContractedBy(Constants16 * Prefs.UIScale);

        public static Rect CornerArea(Rect slot) => new Rect(slot.xMin, slot.yMin, (Constants16 - 2f) * Prefs.UIScale, (Constants16 - 2f) * Prefs.UIScale);

        const float Constants16 = 16f;
    }
}
