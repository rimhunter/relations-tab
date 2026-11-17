using System.Collections;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimhunterTestRunner.Scenarios
{
    static class IdentityPreview
    {
        [Scenario("screenshot-preview-image-source", MapSize = 40, Windowed = true, MaxSeconds = 150, OnlyOn = "1.7")]
        static IEnumerator PreviewImageSource(ScenarioContext s) {
            var p = Enumerable.Range(0, 6).Select(i => s.SpawnColonist(s.Cell(-6 + i * 2, 0))).ToArray();
            p[0].relations.AddDirectRelation(PawnRelationDefOf.Spouse, p[1]);
            p[2].relations.AddDirectRelation(PawnRelationDefOf.Sibling, p[3]);
            p[1].relations.AddDirectRelation(PawnRelationDefOf.Parent, p[4]);
            p[0].relations.AddDirectRelation(PawnRelationDefOf.Parent, p[4]);
            p[5].relations.AddDirectRelation(PawnRelationDefOf.ExLover, p[2]);
            p[3].relations.AddDirectRelation(PawnRelationDefOf.Lover, p[5]);
            s.Tick(30);
            var button = DefDatabase<MainButtonDef>.AllDefsListForReading.Single(d => d.tabWindowClass?.FullName == "Fluffy_Relations.MainTabWindow_Relations");
            Find.MainTabsRoot.SetCurrentTab(button, playSound: false);
            yield return s.DrawFrame();
            var window = Find.WindowStack.Windows.Single(w => w.GetType().FullName == "Fluffy_Relations.MainTabWindow_Relations");
            for (var i = 0; i < 300; i++) {
                window.windowRect = new Rect(UI.screenWidth * 0.03f, UI.screenHeight * 0.07f, UI.screenWidth * 0.94f, UI.screenHeight * 0.86f);
                yield return null;
            }
            var take = FactionPageSupport.TakeCapture(s, "preview-image-source", new Capture());
            while (take.MoveNext()) yield return take.Current;
        }
    }
}
