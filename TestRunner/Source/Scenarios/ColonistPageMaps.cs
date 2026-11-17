using System.Collections;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class ColonistPageMaps
    {
        static string Joined(System.Collections.Generic.IEnumerable<string> names) => string.Join(",", names.OrderBy(n => n));

        [Scenario("colonist-page-shows-the-colonists-of-the-viewed-map", MapSize = 40, Windowed = true, OnlyOn = "1.7", Mods = "ludeon.rimworld.odyssey", MaxSeconds = 150)]
        static IEnumerator ViewedMap(ScenarioContext s) {
            var first = new[] { ColonistPage.Spawn(s, -6), ColonistPage.Spawn(s, 0), ColonistPage.Spawn(s, 6) };
            var secondMap = ColonistPage.SecondMap(s, 40);
            var second = new[] { ColonistPage.SpawnOn(secondMap, -5), ColonistPage.SpawnOn(secondMap, 5) };
            var firstNames = first.Select(p => p.LabelShort).ToList();
            var secondNames = second.Select(p => p.LabelShort).ToList();
            s.Check(Find.CurrentMap == s.Map, "the scenario map should be the viewed map to start with");
            s.Check(firstNames.Concat(secondNames).Distinct().Count() == 5, "setup: the five colonists should have different short names: " + Joined(firstNames.Concat(secondNames)));

            var page = new ColonistPage(s);
            yield return page.Open();
            yield return page.Frames(5);
            s.AreEqual(Joined(firstNames), Joined(page.GraphLabels(page.Latest, first.Concat(second))), "the graph shows the first map's three colonists, not the second map's");
            RelationsWindow.Close(false);
            yield return page.Frames(3);
            s.Check(RelationsWindow == null, "the Relations tab should close");

            Current.Game.CurrentMap = secondMap;
            Find.CameraDriver.JumpToCurrentMapLoc(secondMap.Center);
            var page2 = new ColonistPage(s);
            yield return page2.Open();
            yield return page2.Frames(5);
            s.AreEqual(Joined(secondNames), Joined(page2.GraphLabels(page2.Latest, first.Concat(second))), "after viewing the second map the graph shows its two colonists only");
            RelationsWindow.Close(false);
            yield return page2.Frames(3);
            Current.Game.CurrentMap = s.Map;
            Current.Game.DeinitAndRemoveMap(secondMap, false);
            Find.WorldObjects.Remove(secondMap.Parent);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("colonist-page-leaves-out-a-colonist-in-a-caravan", MapSize = 40, Windowed = true, OnlyOn = "1.7")]
        static IEnumerator CaravanColonistIsNotANode(ScenarioContext s) {
            var onMap = new[] { ColonistPage.Spawn(s, -6), ColonistPage.Spawn(s, 6) };
            var away = ColonistPage.Spawn(s, 0);
            var awayName = away.LabelShort;
            var names = onMap.Select(p => p.LabelShort).ToList();
            s.Check(!names.Contains(awayName), "setup: the three colonists should have different short names");
            away.DeSpawn();
            var caravan = CaravanMaker.MakeCaravan(new[] { away }, Faction.OfPlayer, s.Map.Tile, true);
            s.Check(caravan.PawnsListForReading.Contains(away), "the colonist should be in the caravan");
            s.Check(!away.Spawned && away.IsCaravanMember(), "the colonist is away with the caravan, not on the map");

            var page = new ColonistPage(s);
            yield return page.Open();
            yield return page.Frames(5);
            var drawn = page.GraphLabels(page.Latest, onMap.Concat(new[] { away }));
            s.AreEqual(Joined(names), Joined(drawn), "the graph shows the two colonists on the map; '" + awayName + "' is in a caravan and is not a node");
            RelationsWindow.Close(false);
            yield return page.Frames(2);
            Find.WorldObjects.Remove(caravan);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
