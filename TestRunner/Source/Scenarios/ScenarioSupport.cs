using System;
using System.Collections;
using System.Linq;
using RimWorld;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class ShotSupport
    {
        public static IEnumerator Shoot(ScenarioContext s, string name, Capture into) {
            var dir = Environment.GetEnvironmentVariable("RIMHUNTER_SCREENSHOT_DIR");
            var take = FactionPageSupport.TakeCapture(s, name, into);
            while (take.MoveNext()) yield return take.Current;
            s.ScreenshotFiles.Add(System.IO.Path.Combine(dir, name + ".png"));
            s.Check(into.Texts.Count > 0, "no text was drawn while " + name + " was taken");
        }

        public static Pawn[] Family(ScenarioContext s) {
            var p = Enumerable.Range(0, 6).Select(i => s.SpawnColonist(s.Cell(-10 + i * 4, 0))).ToArray();
            p[0].relations.AddDirectRelation(PawnRelationDefOf.Spouse, p[1]);
            p[2].relations.AddDirectRelation(PawnRelationDefOf.Sibling, p[3]);
            p[1].relations.AddDirectRelation(PawnRelationDefOf.Parent, p[4]);
            p[0].relations.AddDirectRelation(PawnRelationDefOf.Parent, p[4]);
            p[3].relations.AddDirectRelation(PawnRelationDefOf.Lover, p[5]);
            ColonistPage.SetOpinion(p[0], p[2], ThoughtDef.Named("Insulted"), o => o <= -60);
            ColonistPage.SetOpinion(p[2], p[4], ThoughtDef.Named("KindWords"), o => o >= 60);
            return p;
        }
    }

    static class LanguageScenarios
    {
        public static string Legacy(string folderName) {
            var bracket = folderName.IndexOf('(');
            return (bracket < 0 ? folderName : folderName.Substring(0, bracket)).Trim();
        }

        public static void Switch(LoadedLanguage lang) {
            lang.LoadData();
            LanguageDatabase.activeLanguage = lang;
        }

        public static LoadedLanguage Find(string folder) {
            var lang = LanguageDatabase.AllLoadedLanguages.SingleOrDefault(l => Legacy(l.folderName) == folder);
            if (lang == null) throw new ScenarioFailure("the game has no language for " + folder);
            return lang;
        }

        public static void InLanguage(string folder, Action<LoadedLanguage> check) {
            var english = LanguageDatabase.defaultLanguage;
            var lang = Find(folder);
            var englishCancel = "Cancel".Translate().ToString();
            Switch(lang);
            try {
                if (lang != english) {
                    if (LanguageDatabase.activeLanguage != lang) throw new ScenarioFailure("the active language is not " + folder);
                    if ("Cancel".Translate().ToString() == englishCancel) throw new ScenarioFailure("the game still shows English core text in " + folder);
                }
                check(lang);
            }
            finally {
                Switch(english);
            }
        }

        public static IEnumerator InLanguage(ScenarioContext s, string folder, IEnumerator body) {
            var lang = Find(folder);
            var englishCancel = "Cancel".Translate().ToString();
            Switch(lang);
            try {
                s.Check("Cancel".Translate().ToString() != englishCancel, "the game should show " + folder + " core text");
                while (body.MoveNext()) yield return body.Current;
            }
            finally {
                Switch(LanguageDatabase.defaultLanguage);
            }
        }
    }
}
