using System.Collections;
using System.IO;
using HarmonyLib;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class ColonistPageLines
    {
        const string MildLineScenario = "colonist-page-saved-thresholds-apply-after-restart";

        struct Trio
        {
            public Pawn A, B, C;
        }

        // a dislikes b strongly (-61 to -75 expected), b likes c strongly (+61 to +75), a dislikes c mildly (-30 to -50 exclusive of both bounds), every other direction stays inside -50..50.
        static Trio SetUp(ScenarioContext s) {
            var t = new Trio { A = ColonistPage.Spawn(s, -6), B = ColonistPage.Spawn(s, 0), C = ColonistPage.Spawn(s, 6) };
            ColonistPage.SetOpinion(t.A, t.B, ThoughtDef.Named("Insulted"), o => o <= -60);
            ColonistPage.SetOpinion(t.B, t.C, ThoughtDef.Named("KindWords"), o => o >= 60);
            ColonistPage.SetOpinion(t.A, t.C, ThoughtDef.Named("Insulted"), o => o <= -30);
            return t;
        }

        static void CheckSetUp(ScenarioContext s, Trio t) {
            int Op(Pawn x, Pawn y) => x.relations.OpinionOf(y);
            s.Check(Op(t.A, t.B) < -50 && Op(t.A, t.B) > -100, "setup: a's opinion of b should be a strong dislike, it is " + Op(t.A, t.B));
            s.Check(Op(t.B, t.C) > 50 && Op(t.B, t.C) < 100, "setup: b's opinion of c should be a strong like, it is " + Op(t.B, t.C));
            s.Check(Op(t.A, t.C) < -20 && Op(t.A, t.C) >= -50, "setup: a's opinion of c should be a mild dislike, it is " + Op(t.A, t.C));
            foreach (var (x, y) in new[] { (t.B, t.A), (t.C, t.B), (t.C, t.A) })
                s.Check(Op(x, y) >= -50 && Op(x, y) <= 50, "setup: " + x.LabelShort + "'s opinion of " + y.LabelShort + " should be inside -50..50, it is " + Op(x, y));
        }

        sealed class Lines
        {
            public float AbRed, AbGreen, BcGreen, AcRed, AcGreen;
            public string Seen;
        }

        static IEnumerator ShootLines(ScenarioContext s, ColonistPage page, Trio t, string shotName, Lines into) {
            yield return page.Frames(60);
            var shot = new Capture();
            yield return FactionPageSupport.TakeCapture(s, shotName, shot, 10);
            var centers = page.NodeCenters(shot.Texts, new[] { t.A.LabelShort, t.B.LabelShort, t.C.LabelShort });
            s.AreEqual(3, centers.Count, "the three colonists should be nodes in the graph; found " + string.Join(", ", centers.Keys));
            if (centers.Count != 3) yield break;
            var a = centers[t.A.LabelShort];
            var b = centers[t.B.LabelShort];
            var c = centers[t.C.LabelShort];
            into.AbRed = ColonistPage.LineFraction(shot, a, b, ColonistPage.IsRed);
            into.AbGreen = ColonistPage.LineFraction(shot, a, b, ColonistPage.IsGreen);
            into.BcGreen = ColonistPage.LineFraction(shot, b, c, ColonistPage.IsGreen);
            into.AcRed = ColonistPage.LineFraction(shot, a, c, ColonistPage.IsRed);
            into.AcGreen = ColonistPage.LineFraction(shot, a, c, ColonistPage.IsGreen);
            into.Seen = "a-b red " + into.AbRed + " green " + into.AbGreen + ", b-c green " + into.BcGreen + ", a-c red " + into.AcRed + " green " + into.AcGreen + "; nodes a=" + a + " b=" + b + " c=" + c;
        }

        [Scenario("colonist-page-opinion-lines-default-thresholds", MapSize = 40, Windowed = true, OnlyOn = "1.7")]
        static IEnumerator DefaultThresholds(ScenarioContext s) {
            var t = SetUp(s);
            CheckSetUp(s, t);
            var page = new ColonistPage(s);
            yield return page.Open();
            var lines = new Lines();
            yield return ShootLines(s, page, t, "colonist-page-lines-default", lines);
            s.Check(lines.AbRed >= 0.5f, "a strong dislike (below -50) should draw a red line from a to b. " + lines.Seen);
            s.Check(lines.BcGreen >= 0.5f, "a strong like (above 50) should draw a green line from b to c. " + lines.Seen);
            s.Check(lines.AcRed < 0.1f && lines.AcGreen < 0.1f, "a mild dislike (-30 to -50) should draw no line from a to c. " + lines.Seen);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario(MildLineScenario, MapSize = 40, Windowed = true, OnlyOn = "1.7", SelfTest = true)]
        static IEnumerator SavedThresholdsApply(ScenarioContext s) {
            var t = SetUp(s);
            CheckSetUp(s, t);
            var page = new ColonistPage(s);
            yield return page.Open();
            var lines = new Lines();
            yield return ShootLines(s, page, t, "colonist-page-lines-saved-thresholds", lines);
            s.Check(lines.AcRed >= 0.5f, "the saved dislike threshold of -20 should draw a red line for the mild dislike from a to c. " + lines.Seen);
            s.Check(lines.AbRed >= 0.5f, "the strong dislike should still draw its line. " + lines.Seen);
            s.Check(t.B.relations.OpinionOf(t.C) < 80, "setup: b's opinion of c should stay below the saved like threshold of 80, it is " + t.B.relations.OpinionOf(t.C));
            s.Check(lines.BcGreen < 0.1f, "the saved like threshold of 80 should hide the line for a like of 61 to 75 from b to c. " + lines.Seen);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        const string ControllerType = "Fluffy_Relations.Controller";
        const string RunnerPackageId = "rimhunter.relationstab.testrunner";
        public const string SettingsFixture = "Fixtures/Mod_rimhunter.relationstab_Controller.xml";

        static Mod RelationsMod => LoadedModManager.ModHandles.Single(m => m.GetType().FullName == ControllerType);

        static string SettingsFile => Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename("Mod_" + RelationsMod.Content.FolderName + "_Controller.xml"));

        static string FixturePath => Path.Combine(LoadedModManager.RunningModsListForReading.Single(m => m.PackageIdPlayerFacing.ToLowerInvariant() == RunnerPackageId).RootDir, SettingsFixture);

        static System.Collections.Generic.Dictionary<string, object> SettingsValues =>
            (System.Collections.Generic.Dictionary<string, object>)AccessTools.Field(AccessTools.TypeByName("Fluffy_Relations.Settings") ?? throw new ScenarioFailure("Fluffy_Relations.Settings is not loaded"), "values").GetValue(null);

        [Scenario("colonist-page-thresholds-set-in-settings-are-written-to-the-file", MapSize = 40, Windowed = true, OnlyOn = "1.7")]
        static IEnumerator ThresholdsWrittenToFile(ScenarioContext s) {
            var settings = ColonistPage.Settings;
            var oldNegative = settings.Negative;
            var oldPositive = settings.Positive;
            var lowerLabel = "Fluffy_Relations.LowerOpinionThreshold".Translate().ToString();
            var upperLabel = "Fluffy_Relations.UpperOpinionThreshold".Translate().ToString();
            var values = SettingsValues;
            var oldValues = values.ToDictionary(kv => kv.Key, kv => kv.Value);
            try {
                s.AreEqual(-50f, settings.Negative, "the dislike threshold before any change");
                s.AreEqual(50f, settings.Positive, "the like threshold before any change");
                s.Check(!File.Exists(SettingsFile), "no settings file before the settings window closes: " + SettingsFile);
                var dialog = new Dialog_ModSettings(RelationsMod);
                Find.WindowStack.Add(dialog);
                var frame = s.DrawFrame();
                yield return frame;
                yield return s.DrawFrame();
                s.Check(frame.Texts.Any(t => t.Text == lowerLabel) && frame.Texts.Any(t => t.Text == upperLabel),
                    "the settings window should show both threshold labels '" + lowerLabel + "' and '" + upperLabel + "'. Drawn: " + Dump(frame.Texts));
                s.Check(values.ContainsKey(lowerLabel) && values.ContainsKey(upperLabel), "the settings window should hold a text field for each threshold");
                values[lowerLabel] = "-20";
                values[upperLabel] = "80";
                for (var i = 0; i < 3; i++) yield return s.DrawFrame();
                var closing = s.DrawFrame();
                yield return closing;
                var close = closing.Texts.FirstOrDefault(t => t.Text == "CloseButton".Translate().ToString());
                s.Check(close != null, "the settings window should have a close button. Drawn: " + Dump(closing.Texts));
                if (close == null) yield break;
                yield return s.Click(close);
                for (var i = 0; i < 3; i++) yield return s.DrawFrame();
                s.Check(!Find.WindowStack.Windows.Contains(dialog), "the close button should close the settings window");
                s.Check(File.Exists(SettingsFile), "closing the settings window should write " + SettingsFile);
                if (!File.Exists(SettingsFile)) yield break;
                var written = File.ReadAllLines(SettingsFile).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                var recorded = File.ReadAllLines(FixturePath).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                s.Check(written.SequenceEqual(recorded), "the settings file should match the recorded " + FixturePath + ". Written: " + string.Join(" ", written));
            }
            finally {
                settings.Negative = oldNegative;
                settings.Positive = oldPositive;
                foreach (var kv in oldValues) values[kv.Key] = kv.Value;
            }
            s.RequireNoLogErrors();
        }
    }
}
