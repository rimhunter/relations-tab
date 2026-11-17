using System.Collections;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class LayoutRelations
    {
        const float MinCloserPixels = 5f;

        [Scenario("colonist-page-settles-lovers-closer-together-than-rivals-in-a-small-colony", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 120)]
        static IEnumerator LoversCloserThanRivals(ScenarioContext s) {
            var colonists = Enumerable.Range(0, 6).Select(i => s.SpawnColonist(s.Cell(-10 + i * 4, 0))).ToList();
            var (loverA, loverB, rivalA, rivalB) = (colonists[0], colonists[3], colonists[1], colonists[4]);
            loverA.relations.AddDirectRelation(PawnRelationDefOf.Lover, loverB);
            var (spouseA, spouseB) = (colonists[2], colonists[5]);
            spouseA.relations.AddDirectRelation(PawnRelationDefOf.Spouse, spouseB);
            rivalA.relations.AddDirectRelation(PawnRelationDefOf.ExSpouse, rivalB);
            var insulted = DefDatabase<ThoughtDef>.GetNamed("Insulted");
            for (var i = 0; i < 10; i++) rivalA.needs.mood.thoughts.memories.TryGainMemory(insulted, rivalB);
            s.Tick(30);
            var loverOpinion = loverA.relations.OpinionOf(loverB);
            var rivalOpinion = rivalA.relations.OpinionOf(rivalB);
            s.Check(loverOpinion > 0 && rivalOpinion <= -20 && loverOpinion > rivalOpinion + 40, "setup: the lovers' opinion should be positive and far above the rivals' strong dislike; lovers " + loverOpinion + ", rivals " + rivalOpinion);

            var names = colonists.Select(p => p.LabelShort).ToList();
            s.AreEqual(6, names.Distinct().Count(), "distinct colonist names");
            yield return ScenarioContext.Frames(2);
            var before = s.DrawFrame();
            yield return before;
            s.Check(RelationsWindow == null, "the Relations tab should be closed before the button is pressed");
            yield return ClickRelationsButton(s);
            yield return ScenarioContext.Frames(2);
            var window = RelationsWindow;
            s.Check(window != null, "clicking the Relations button should open the Relations tab");
            if (window == null) yield break;

            var settled = new SlotRead();
            var settle = SettleLayout(s, into => ReadNamed(s, names, "of the colonists", into), settled);
            while (settle.MoveNext()) yield return settle.Current;

            Vector2 Center(Pawn p) => settled.Slots.Single(n => n.name == p.LabelShort).slot.center;
            var area = GraphAreaPixels(window);
            var loverDistance = (Center(loverA) - Center(loverB)).magnitude / Prefs.UIScale;
            var rivalDistance = (Center(rivalA) - Center(rivalB)).magnitude / Prefs.UIScale;
            s.Check(OutsideGraphArea(area, settled).Length == 0, "every colonist node should be inside the graph area; outside: " + OutsideGraphArea(area, settled));
            s.Check(Overlapping(settled).Length == 0, "no two colonist slots should overlap; overlapping: " + Overlapping(settled));
            var spouseDistance = (Center(spouseA) - Center(spouseB)).magnitude / Prefs.UIScale;
            var spouseOpinion = spouseA.relations.OpinionOf(spouseB);
            var detail = "lovers (opinion " + loverOpinion + ") " + loverDistance + " px apart, spouses (opinion " + spouseOpinion + ") " + spouseDistance + " px, rivals (opinion " + rivalOpinion + ") " + rivalDistance + " px";
            s.Check(spouseOpinion > 0, "setup: the spouses' opinion should be positive; " + detail);
            s.Check(loverDistance < rivalDistance - MinCloserPixels && spouseDistance < rivalDistance - MinCloserPixels,
                "the colonists who like each other should settle at least " + MinCloserPixels + " px closer together than the rivals; " + detail);
            var pinned = settled.Slots.Count(n => n.slot.xMin <= area.xMin + 1f || n.slot.yMin <= area.yMin + 1f || n.slot.xMax >= area.xMax - 1f || n.slot.yMax >= area.yMax - 1f);
            s.Check(pinned <= 2, "no more than 2 of 6 colonist nodes should sit against the edge of the graph area; " + pinned + " do");
            var shot = new Capture();
            var take = FactionPageSupport.TakeCapture(s, "colonist-page-lovers-and-rivals", shot);
            while (take.MoveNext()) yield return take.Current;
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
