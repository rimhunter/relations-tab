using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    sealed class ColonistPage
    {
        readonly ScenarioContext s;
        ScenarioContext.DrawnFrame before;

        public ScenarioContext.DrawnFrame Latest { get; private set; }

        public ColonistPage(ScenarioContext s) { this.s = s; }

        public IEnumerator Open() {
            s.Tick(30);
            before = s.DrawFrame();
            yield return before;
            s.Check(RelationsWindow == null, "the Relations tab should be closed before the button is pressed");
            yield return ClickRelationsButton(s);
            yield return Frames(3);
            s.Check(RelationsWindow != null, "clicking the Relations button should open the Relations tab");
            if (RelationsWindow == null) throw new ScenarioFailure("the Relations tab did not open");
        }

        public IEnumerator Frames(int count) {
            for (var i = 0; i < count; i++) {
                Latest = s.DrawFrame();
                yield return Latest;
            }
        }

        public Rect DetailArea() {
            var graph = GraphAreaPixels(RelationsWindow);
            return new Rect(0f, 0f, graph.x, graph.yMax);
        }

        public IEnumerator ClickPixel(Vector2 point) {
            yield return s.Click(new DrawnText { Text = "", Box = new Rect(point.x, point.y, 1f, 1f), Ink = new Rect(point.x, point.y, 1f, 1f) });
            yield return Frames(2);
        }

        public Dictionary<string, Vector2> NodeCenters(IReadOnlyList<DrawnText> texts, IEnumerable<string> names) {
            var wanted = names.ToHashSet();
            var graph = GraphAreaPixels(RelationsWindow);
            var barDrawn = before.Texts.Select(t => (t.Text, (int)t.Ink.x, (int)t.Ink.y)).ToHashSet();
            var mapLabels = Find.CurrentMap.mapPawns.AllPawnsSpawned.Select(p => (p.LabelShort, at: s.ScreenPixelOf(p.DrawPos) + new Vector2(0f, 25f))).ToList();
            var result = new Dictionary<string, Vector2>();
            foreach (var t in texts) {
                if (!wanted.Contains(t.Text) || !graph.Contains(t.Ink.center)) continue;
                if (mapLabels.Any(m => m.LabelShort == t.Text && (t.Ink.center - m.at).magnitude < 14f)) continue;
                if (barDrawn.Contains((t.Text, (int)t.Ink.x, (int)t.Ink.y))) continue;
                if (result.ContainsKey(t.Text)) throw new ScenarioFailure("two labels '" + t.Text + "' in the graph area " + graph + ": " + string.Join(" | ", texts.Where(x => x.Text == t.Text)) + "; map labels " + string.Join(" | ", mapLabels.Where(m => m.LabelShort == t.Text)) + "; bar " + string.Join(" | ", before.Texts.Where(x => x.Text == t.Text)));
                var scale = Prefs.UIScale;
                result[t.Text] = new Vector2(t.Ink.center.x, t.Ink.center.y - (SlotSize / 2f - LabelHeight / 2f) * scale);
            }
            return result;
        }

        public List<string> GraphLabels(ScenarioContext.DrawnFrame frame, IEnumerable<Pawn> candidates) {
            var known = candidates.Select(p => p.LabelShort).ToHashSet();
            var graph = GraphAreaPixels(RelationsWindow);
            var barDrawn = before.Texts.Select(t => (t.Text, (int)t.Ink.x, (int)t.Ink.y)).ToHashSet();
            var mapLabels = Find.CurrentMap.mapPawns.AllPawnsSpawned.Select(p => (p.LabelShort, at: s.ScreenPixelOf(p.DrawPos) + new Vector2(0f, 25f))).ToList();
            return frame.Texts
                .Where(t => graph.Contains(t.Ink.center) && known.Contains(t.Text))
                .Where(t => !mapLabels.Any(m => m.LabelShort == t.Text && (t.Ink.center - m.at).magnitude < 14f))
                .Where(t => !barDrawn.Contains((t.Text, (int)t.Ink.x, (int)t.Ink.y)))
                .Select(t => t.Text).OrderBy(n => n).ToList();
        }

        public static Map SecondMap(ScenarioContext s, int size) {
            var first = s.Map;
            PlanetTile tile;
            do tile = TileFinder.RandomStartingTile();
            while (Find.WorldObjects.AnyWorldObjectAt(tile));
            var parent = (MapParent)WorldObjectMaker.MakeWorldObject(tile.LayerDef.SettlementWorldObjectDef);
            parent.Tile = tile;
            parent.SetFaction(Faction.OfPlayer);
            Find.WorldObjects.Add(parent);
            var map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(size, 1, size), null);
            map.fogGrid.ClearAllFog();
            Current.Game.CurrentMap = first;
            return map;
        }

        public static Pawn SpawnOn(Map map, int dx) {
            var req = new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.PlayerStarter, forceGenerateNewPawn: true, canGeneratePawnRelations: false);
            var pawn = PawnGenerator.GeneratePawn(req);
            GenSpawn.Spawn(pawn, new IntVec3(map.Center.x + dx, 0, map.Center.z), map);
            return pawn;
        }

        public static Pawn Spawn(ScenarioContext s, int dx) => s.SpawnColonist(s.Cell(dx, 0));

        public static void SetOpinion(Pawn thinker, Pawn target, ThoughtDef memory, Func<int, bool> reached) {
            for (var i = 0; i < 10 && !reached(thinker.relations.OpinionOf(target)); i++)
                thinker.needs.mood.thoughts.memories.TryGainMemory(memory, target);
            if (!reached(thinker.relations.OpinionOf(target)))
                throw new ScenarioFailure(thinker.LabelShort + "'s opinion of " + target.LabelShort + " is " + thinker.relations.OpinionOf(target) + " after 10 memories of " + memory.defName);
        }

        public static bool IsRed((byte r, byte g, byte b) c) => c.r >= 70 && c.r - c.g >= 30 && c.r - c.b >= 30;
        public static bool IsGreen((byte r, byte g, byte b) c) => c.g >= 70 && c.g - c.r >= 30 && c.g - c.b >= 30;

        public static float LineFraction(Capture shot, Vector2 a, Vector2 b, Func<(byte r, byte g, byte b), bool> color) {
            var dir = (b - a).normalized;
            return shot.FractionAlong(a + dir * 55f, b - dir * 45f, color, 0f, 1f, 41, 3);
        }

        public static SettingsAccess Settings => new SettingsAccess();

        public sealed class SettingsAccess
        {
            public float Negative {
                get => (float)AccessTools.Field(Type("Fluffy_Relations.RelationsHelper"), "OPINION_THRESHOLD_NEG").GetValue(null);
                set => AccessTools.Field(Type("Fluffy_Relations.RelationsHelper"), "OPINION_THRESHOLD_NEG").SetValue(null, value);
            }

            public float Positive {
                get => (float)AccessTools.Field(Type("Fluffy_Relations.RelationsHelper"), "OPINION_THRESHOLD_POS").GetValue(null);
                set => AccessTools.Field(Type("Fluffy_Relations.RelationsHelper"), "OPINION_THRESHOLD_POS").SetValue(null, value);
            }

            static Type Type(string name) => AccessTools.TypeByName(name) ?? throw new ScenarioFailure("the type " + name + " is not loaded");
        }
    }
}
