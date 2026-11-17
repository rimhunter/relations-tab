using System.Collections;
using System.Linq;
using Verse;
using static RimhunterTestRunner.Scenarios.LeaderSupport;

namespace RimhunterTestRunner.Scenarios
{
    static class LeaderRules
    {
        [Scenario("leader-with-none-chosen-is-the-oldest-colonist-and-the-button-is-offered", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 120)]
        static IEnumerator OldestWhenNoneChosen(ScenarioContext s) {
            SetChosenLeader(null);
            try {
                var run = OldestWhenNoneChosenBody(s);
                while (run.MoveNext()) yield return run.Current;
            }
            finally {
                SetChosenLeader(null);
            }
        }

        static IEnumerator OldestWhenNoneChosenBody(ScenarioContext s) {
            var oldest = SpawnColonistAged(s, -4, 110);
            SpawnColonistAged(s, 0, 90);
            SpawnColonistAged(s, 4, 70);
            var page = new OpenPage();
            var details = new ColonyDetails();
            var show = ShowColonyDetails(s, page, details);
            while (show.MoveNext()) yield return show.Current;
            s.AreEqual(oldest.Name.ToStringFull, LeaderRowOf(details.Labels), "the leader shown with none chosen");
            s.AreEqual(1, details.EditButtons, "change-leader buttons offered with no mayor");
            s.Check(details.Button.ScrollViewHeight <= details.Button.ScrollOutHeight, "the details of a colony with no other factions should fit the panel, so no scrollbar shows; content " + details.Button.ScrollViewHeight + ", panel " + details.Button.ScrollOutHeight);
            s.Check(details.Button.Pixels.xMax <= details.Button.ScrollOutPixels.xMax, "the change-leader button " + details.Button.Pixels + " should end inside the panel " + details.Button.ScrollOutPixels);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("leader-psychology-mayor-is-shown-and-the-button-is-not-offered", MapSize = 40, Windowed = true, OnlyOn = "1.7", Mods = MayorFixture, MaxSeconds = 120)]
        static IEnumerator MayorShown(ScenarioContext s) {
            SetChosenLeader(null);
            try {
                var run = MayorShownBody(s);
                while (run.MoveNext()) yield return run.Current;
            }
            finally {
                SetChosenLeader(null);
            }
        }

        static IEnumerator MayorShownBody(ScenarioContext s) {
            var oldest = SpawnColonistAged(s, -4, 110);
            var chosen = SpawnColonistAged(s, 0, 90);
            var mayor = SpawnColonistAged(s, 4, 70);
            s.Check(DefDatabase<HediffDef>.GetNamedSilentFail(MayorDefName) != null, "the stand-in mod should define the Mayor hediff");
            var page = new OpenPage();
            var before = new ColonyDetails();
            var show = ShowColonyDetails(s, page, before);
            while (show.MoveNext()) yield return show.Current;
            s.AreEqual(oldest.Name.ToStringFull, LeaderRowOf(before.Labels), "the leader shown before the mayor is appointed");
            s.AreEqual(1, before.EditButtons, "change-leader buttons offered before the mayor is appointed");
            SetChosenLeader(chosen);
            MakeMayor(mayor);
            var after = new ColonyDetails();
            show = ShowColonyDetails(s, page, after);
            while (show.MoveNext()) yield return show.Current;
            s.AreEqual(mayor.Name.ToStringFull, LeaderRowOf(after.Labels), "the leader shown with a Mayor hediff on the youngest colonist and another colonist chosen");
            s.AreEqual(0, after.EditButtons, "change-leader buttons offered with a mayor");
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("leader-chosen-leader-is-kept-after-a-save-and-load", MapSize = 40, OnlyOn = "1.7", SaveWithMods = "{mod}", SetupInSaveLaunch = true, ReloadInSaveLaunch = true)]
        static void ChosenKeptAfterLoad(ScenarioContext s) {
            const string chosenName = "Kept 'Chosen' Leader";
            if (s.Stage == SaveStage.Setup) {
                SetChosenLeader(null);
                SpawnColonistAged(s, -4, 110);
                var chosen = SpawnColonistAged(s, 0, 90);
                SpawnColonistAged(s, 4, 70);
                chosen.Name = new NameTriple("Kept", "Chosen", "Leader");
                s.Check(ChosenLeaderName() == null, "no leader should be chosen before the choice");
                SetChosenLeader(chosen);
                s.AreEqual(chosenName, LeaderFromRules(), "the leader the tab's rules give after the choice");
                return;
            }
            s.AreEqual(chosenName, LeaderFromRules(), "the leader the tab's rules give after the save was loaded (" + s.Stage + ")");
            SetChosenLeader(null);
            s.RequireNoLogErrors();
        }

        [Scenario("leader-a-new-game-does-not-keep-the-previous-games-chosen-leader", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 120)]
        static IEnumerator NewGameForgetsLeader(ScenarioContext s) {
            SetChosenLeader(null);
            try {
                var run = NewGameForgetsLeaderBody(s);
                while (run.MoveNext()) yield return run.Current;
            }
            finally {
                SetChosenLeader(null);
            }
        }

        static IEnumerator NewGameForgetsLeaderBody(ScenarioContext s) {
            var oldest = SpawnColonistAged(s, -4, 110);
            SpawnColonistAged(s, 0, 90);
            var youngest = SpawnColonistAged(s, 4, 70);
            var page = new OpenPage();
            var details = new ColonyDetails();
            SetChosenLeader(youngest);
            var show = ShowColonyDetails(s, page, details);
            while (show.MoveNext()) yield return show.Current;
            s.AreEqual(youngest.Name.ToStringFull, LeaderRowOf(details.Labels), "the leader shown while the youngest colonist is chosen");
            NewGameComponent();
            var after = new ColonyDetails();
            show = ShowColonyDetails(s, page, after);
            while (show.MoveNext()) yield return show.Current;
            s.AreEqual(oldest.Name.ToStringFull, LeaderRowOf(after.Labels), "the leader shown once a new game's leader component exists (the oldest colonist, not the previous game's choice)");
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
