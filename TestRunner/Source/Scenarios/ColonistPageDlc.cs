using System.Collections;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class ColonistPageDlc
    {
        [Scenario("colonist-page-anomaly-bonded-animal-logs-no-error", MapSize = 40, Windowed = true, OnlyOn = "1.7", Mods = "ludeon.rimworld.anomaly")]
        static IEnumerator BondedAnimalLogsNoError(ScenarioContext s) {
            var owner = ColonistPage.Spawn(s, -4);
            var other = ColonistPage.Spawn(s, 4);
            var animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Husky"), Faction.OfPlayer, forceGenerateNewPawn: true));
            GenSpawn.Spawn(animal, s.Cell(0, 4), s.Map);
            owner.relations.AddDirectRelation(PawnRelationDefOf.Bond, animal);
            s.Check(owner.relations.DirectRelationExists(PawnRelationDefOf.Bond, animal), "the colonist should be bonded to the animal before the tab opens");
            s.Check(ModsConfig.AnomalyActive, "Anomaly should be active in this launch");
            s.Check(animal.story == null, "an animal has no story, which is what makes the Joyous thought throw");
            s.RequireNoLogErrors();

            var page = new ColonistPage(s);
            yield return page.Open();
            yield return page.Frames(5);

            var expected = new[] { owner.LabelShort, other.LabelShort }.OrderBy(n => n).ToList();
            var everyone = new[] { owner, other, animal };
            s.AreEqual(string.Join(",", expected), string.Join(",", page.GraphLabels(page.Latest, everyone)), "the labels in the graph before the family tree icon is pressed; the bonded animal is not a node");
            s.RequireNoLogErrors();

            yield return page.ClickPixel(IconCenterPixels(RelationsWindow, 3));
            yield return page.Frames(60);
            var withFamily = new[] { owner.LabelShort, other.LabelShort, animal.LabelShort }.OrderBy(n => n).ToList();
            s.AreEqual(string.Join(",", withFamily), string.Join(",", page.GraphLabels(page.Latest, everyone)), "the labels in the graph after the family tree icon is pressed; the bonded animal is a node");
            s.RequireNoLogErrors();
            yield return page.ClickPixel(IconCenterPixels(RelationsWindow, 3));
            yield return page.Frames(3);
            s.AreEqual(string.Join(",", expected), string.Join(",", page.GraphLabels(page.Latest, everyone)), "the labels in the graph after the family tree icon is pressed again");
            s.RequireNoPlayerLogErrors();
        }
    }
}
