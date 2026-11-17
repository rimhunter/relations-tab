using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimhunterTestRunner.Scenarios
{
    sealed class EditButton
    {
        public Rect Pixels;
        public Rect ScrollOutPixels;
        public float ScrollViewHeight;
        public float ScrollOutHeight;
    }

    static class LeaderProbe
    {
        static readonly Regex Tags = new Regex("<[^>]+>");
        static bool patched;
        static bool recording;
        static bool toDevice;
        static readonly List<string> labels = new List<string>();
        static readonly List<EditButton> buttons = new List<EditButton>();
        static readonly Stack<(Rect outPixels, float viewHeight, float outHeight)> scrolls = new Stack<(Rect, float, float)>();

        static void Patch() {
            if (patched) return;
            var harmony = new Harmony("rimhunter.relationstab.testrunner.leader");
            harmony.Patch(AccessTools.Method(typeof(Widgets), nameof(Widgets.Label), new[] { typeof(Rect), typeof(string) }), prefix: new HarmonyMethod(typeof(LeaderProbe), nameof(OnLabel)));
            harmony.Patch(AccessTools.Method(typeof(Widgets), nameof(Widgets.ButtonImage), new[] { typeof(Rect), typeof(Texture2D), typeof(bool), typeof(string) }), prefix: new HarmonyMethod(typeof(LeaderProbe), nameof(OnButtonImage)));
            harmony.Patch(AccessTools.Method(typeof(Widgets), nameof(Widgets.BeginScrollView)), prefix: new HarmonyMethod(typeof(LeaderProbe), nameof(OnBeginScroll)));
            harmony.Patch(AccessTools.Method(typeof(Widgets), nameof(Widgets.EndScrollView)), prefix: new HarmonyMethod(typeof(LeaderProbe), nameof(OnEndScroll)));
            patched = true;
        }

        public static void Begin(bool convertToDevicePixels) {
            Patch();
            labels.Clear();
            buttons.Clear();
            scrolls.Clear();
            toDevice = convertToDevicePixels;
            recording = true;
        }

        public static (List<string> labels, List<EditButton> buttons) End() {
            recording = false;
            return (new List<string>(labels), new List<EditButton>(buttons));
        }

        static Rect ToScreen(Rect r) {
#if RW_1_7
            var a = UnityStubs.Gui.GuiBackend.Current.GuiToDevicePoint(new Vector2(r.xMin, r.yMin));
            var b = UnityStubs.Gui.GuiBackend.Current.GuiToDevicePoint(new Vector2(r.xMax, r.yMax));
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
#else
            throw new ScenarioFailure("device rectangles are implemented for 1.7 only");
#endif
        }

        static bool Active => recording && Event.current?.type == EventType.Repaint;

        static void OnLabel(Rect rect, string label) {
            if (Active && !string.IsNullOrEmpty(label)) labels.Add(Tags.Replace(label, ""));
        }

        static void OnButtonImage(Rect butRect, Texture2D tex) {
            if (!Active || tex == null || tex.name != "edit") return;
            var button = new EditButton();
            if (toDevice) {
                if (scrolls.Count == 0) throw new ScenarioFailure("the edit button was drawn outside a scroll view");
                var top = scrolls.Peek();
                button.Pixels = ToScreen(butRect);
                button.ScrollOutPixels = top.outPixels;
                button.ScrollViewHeight = top.viewHeight;
                button.ScrollOutHeight = top.outHeight;
            }
            buttons.Add(button);
        }

        static void OnBeginScroll(Rect outRect, Rect viewRect) {
            if (!Active) return;
            scrolls.Push((toDevice ? ToScreen(outRect) : outRect, viewRect.height, outRect.height));
        }

        static void OnEndScroll() {
            if (Active && scrolls.Count > 0) scrolls.Pop();
        }
    }

    static class LeaderSupport
    {
        public const string MayorDefName = "Mayor";
        public const string MayorFixture = "TestRunner/Fixtures/psychology-mayor";
        const long TicksPerYear = 3600000L;

        public static Pawn SpawnColonistAged(ScenarioContext s, int dx, int years) {
            var pawn = s.SpawnColonist(s.Cell(dx, 0));
            pawn.ageTracker.AgeBiologicalTicks = years * TicksPerYear;
            Find.ColonistBar.MarkColonistsDirty();
            return pawn;
        }

        public static string LeaderRowOf(IEnumerable<string> labels) {
            var title = Faction.OfPlayer.def.leaderTitle.CapitalizeFirst() + ": ";
            var rows = labels.Where(l => l.StartsWith(title)).Distinct().ToList();
            if (rows.Count != 1) throw new ScenarioFailure("expected one '" + title + "' row in the faction details, drawn: " + string.Join(" | ", labels));
            return rows[0].Substring(title.Length);
        }

        public sealed class ColonyDetails
        {
            public List<string> Labels = new List<string>();
            public int EditButtons;
            public EditButton Button;
        }

        public static IEnumerator ShowColonyDetails(ScenarioContext s, OpenPage page, ColonyDetails into) {
            if (page.Window == null) {
                var open = FactionPageSupport.OpenFactionsPage(s, page);
                while (open.MoveNext()) yield return open.Current;
            }
            Traverse.Create(page.Window).Property("SelectedFaction").SetValue(Faction.OfPlayer);
            yield return ScenarioContext.Frames(3);
            LeaderProbe.Begin(true);
            yield return ScenarioContext.Frames(2);
            var (labels, buttons) = LeaderProbe.End();
            into.Labels = labels;
            into.EditButtons = buttons.GroupBy(b => b.Pixels).Count();
            into.Button = buttons.FirstOrDefault();
        }

        public static string ChosenLeaderName() {
            var type = AccessTools.TypeByName("Fluffy_Relations.GameComponent_Leader");
            if (type == null) throw new ScenarioFailure("GameComponent_Leader is not loaded");
            return ((Pawn)AccessTools.PropertyGetter(type, "Leader").Invoke(null, null))?.Name.ToStringFull;
        }

        public static string LeaderFromRules() {
            var helper = AccessTools.TypeByName("Fluffy_Relations.RelationsHelper");
            if (helper == null) throw new ScenarioFailure("RelationsHelper is not loaded");
            var leader = (Pawn)AccessTools.Method(helper, "Leader", new[] { typeof(Faction) }).Invoke(null, new object[] { Faction.OfPlayer });
            return leader?.Name.ToStringFull;
        }

        public static void NewGameComponent() {
            var type = AccessTools.TypeByName("Fluffy_Relations.GameComponent_Leader");
            if (type == null) throw new ScenarioFailure("GameComponent_Leader is not loaded");
            Activator.CreateInstance(type, Current.Game);
        }

        public static void SetChosenLeader(Pawn pawn) {
            var type = AccessTools.TypeByName("Fluffy_Relations.GameComponent_Leader");
            if (type == null) throw new ScenarioFailure("GameComponent_Leader is not loaded");
            AccessTools.PropertySetter(type, "Leader").Invoke(null, new object[] { pawn });
        }

        public static Pawn MakeMayor(Pawn pawn) {
            var def = DefDatabase<HediffDef>.GetNamed(MayorDefName);
            pawn.health.AddHediff(def);
            return pawn;
        }
    }
}
