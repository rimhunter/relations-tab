using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionPageSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;
using static RimhunterTestRunner.Scenarios.LeaderSupport;
using static RimhunterTestRunner.Scenarios.ShotSupport;

namespace RimhunterTestRunner.Scenarios
{
    static class VerificationShotsNoClick
    {
        const string Only = "1.6";

        static IEnumerator OpenTab(ScenarioContext s, string page) {
            Find.MainTabsRoot.SetCurrentTab(RelationsButton, playSound: false);
            yield return s.DrawFrame();
            var window = RelationsWindow;
            s.Check(window != null, "setting the Relations tab as the current tab should open it");
            if (window == null) throw new ScenarioFailure("the Relations tab did not open");
            Traverse.Create(window).Property("CurrentPage").SetValue(System.Enum.Parse(AccessTools.TypeByName("Fluffy_Relations.Page"), page));
            yield return ScenarioContext.Frames(3);
        }

        static IEnumerator CloseTab(ScenarioContext s) {
            Find.MainTabsRoot.EscapeCurrentTab(false);
            yield return s.DrawFrame();
            yield return s.DrawFrame();
        }

        static void SetMode(Window window, string mode) {
            Traverse.Create(window).Field("_mode").SetValue(System.Enum.Parse(AccessTools.TypeByName("Fluffy_Relations.GraphMode"), mode));
            Traverse.Create(window).Method("BuildPawnList").GetValue();
        }

        static void SetVanillaFactionsTab(bool on) {
            AccessTools.Field(AccessTools.TypeByName("Fluffy_Relations.Settings"), "keepVanillaFactionsTab").SetValue(null, on);
            AccessTools.Method(AccessTools.TypeByName("Fluffy_Relations.FactionsTabButton"), "Apply").Invoke(null, null);
        }

        [Scenario("screenshot-bottom-bars-and-colonist-page-in-both-layouts-without-clicks", MapSize = 40, Windowed = true, OnlyOn = Only, MaxSeconds = 150)]
        static IEnumerator BarsAndColonistPage(ScenarioContext s) {
            var family = Family(s);
            var names = family.Select(p => p.LabelShort).ToList();
            s.Tick(30);
            yield return ScenarioContext.Frames(5);
            var bar = new Capture();
            yield return Shoot(s, "bottom-bar-relations-replaces-factions", bar);
            s.AreEqual("Architect Work Schedule Assign Animals Wildlife Research Quests World History RelationsTab Menu", FactionsTabSupport.VisibleButtons(), "the default bottom bar");
            SetVanillaFactionsTab(true);
            try {
                yield return ScenarioContext.Frames(5);
                var both = new Capture();
                yield return Shoot(s, "bottom-bar-relations-beside-factions", both);
                s.AreEqual("Architect Work Schedule Assign Animals Wildlife Research Quests World History Factions RelationsTab Menu", FactionsTabSupport.VisibleButtons(), "the bottom bar with the vanilla Factions tab kept");
            }
            finally {
                SetVanillaFactionsTab(false);
            }
            yield return OpenTab(s, "Colonists");
            var window = RelationsWindow;
            yield return ScenarioContext.Frames(300);
            Traverse.Create(window).Property("SelectedPawn").SetValue(family[0]);
            yield return ScenarioContext.Frames(100);
            var dynamic = new Capture();
            yield return Shoot(s, "colonist-page-dynamic-layout", dynamic);
            foreach (var n in names) s.Check(dynamic.Texts.Any(t => t.Text == n), "the colonist " + n + " should be a labeled node in the dynamic layout");
            s.Check(dynamic.Texts.Any(t => t.Text == family[0].Name.ToStringFull), "the details should be titled with " + family[0].Name.ToStringFull);
            SetMode(window, "Circle");
            yield return ScenarioContext.Frames(10);
            var circle = new Capture();
            yield return Shoot(s, "colonist-page-circle-layout", circle);
            foreach (var n in names) s.Check(circle.Texts.Any(t => t.Text == n), "the colonist " + n + " should be a labeled node in the circle layout");
            SetMode(window, "ForceDirected");
            yield return CloseTab(s);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("screenshot-graphs-at-40-colonists-and-30-factions-without-clicks", MapSize = 60, Windowed = true, OnlyOn = Only, MaxSeconds = 170)]
        static IEnumerator Graphs(ScenarioContext s) {
            var existing = Find.FactionManager.AllFactionsListForReading.ToList();
            var colonists = new List<Pawn>();
            for (var dx = -14; dx <= 14; dx += 4)
                for (var dz = -8; dz <= 8; dz += 4)
                    colonists.Add(s.SpawnColonist(s.Cell(dx, dz)));
            foreach (var i in Enumerable.Range(0, 30))
                FactionGenerator.CreateFactionAndAddToManager(DefDatabase<FactionDef>.GetNamed(FactionDefNames[i % FactionDefNames.Length]));
            s.Tick(30);
            var factionLabels = VisibleLabels();
            try {
                var run = GraphsBody(s, colonists, factionLabels);
                while (run.MoveNext()) yield return run.Current;
            }
            finally {
                foreach (var added in Find.FactionManager.AllFactionsListForReading.Where(f => !existing.Contains(f)).ToList()) added.hidden = true;
            }
        }

        static IEnumerator GraphsBody(ScenarioContext s, List<Pawn> colonists, List<string> factionLabels) {
            yield return OpenTab(s, "Colonists");
            yield return ScenarioContext.Frames(600);
            var people = new Capture();
            yield return Shoot(s, "colonist-page-40-colonists", people);
            foreach (var p in colonists) s.Check(people.Texts.Any(t => t.Text == p.LabelShort), "the colonist " + p.LabelShort + " should be a labeled node");
            Traverse.Create(RelationsWindow).Property("CurrentPage").SetValue(System.Enum.Parse(AccessTools.TypeByName("Fluffy_Relations.Page"), "Factions"));
            yield return ScenarioContext.Frames(600);
            var groups = new Capture();
            yield return Shoot(s, "faction-page-30-factions-settled", groups);
            foreach (var l in factionLabels) s.Check(groups.Texts.Any(t => t.Text == l), "the faction " + l + " should be a labeled node");
            yield return CloseTab(s);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("screenshot-leaderless-faction-and-change-leader-button-without-clicks", MapSize = 40, Windowed = true, OnlyOn = Only, MaxSeconds = 150)]
        static IEnumerator Leaders(ScenarioContext s) {
            SetChosenLeader(null);
            var old = SpawnColonistAged(s, -6, 90);
            SpawnColonistAged(s, 0, 60);
            SpawnColonistAged(s, 6, 40);
            var leaderless = MakeLeaderlessHumanlike();
            AddFaction("TribeCivil");
            AddFaction("Pirate");
            s.Tick(30);
            yield return OpenTab(s, "Factions");
            var window = RelationsWindow;
            yield return ScenarioContext.Frames(300);
            Traverse.Create(window).Property("SelectedFaction").SetValue(leaderless);
            yield return ScenarioContext.Frames(100);
            var none = new Capture();
            yield return Shoot(s, "faction-page-leaderless-faction", none);
            s.Check(none.Texts.Any(t => t.Text.EndsWith(": " + "Nobody".Translate())), "the leaderless faction's details should read 'Nobody' for the leader");
            Traverse.Create(window).Property("SelectedFaction").SetValue(Faction.OfPlayer);
            yield return ScenarioContext.Frames(10);
            LeaderProbe.Begin(false);
            yield return ScenarioContext.Frames(2);
            var (labels, buttons) = LeaderProbe.End();
            s.AreEqual(1, buttons.Count > 0 ? 1 : 0, "change-leader buttons drawn for the colony");
            s.AreEqual(old.Name.ToStringFull, LeaderRowOf(labels), "the leader shown for the colony");
            var button = new Capture();
            yield return Shoot(s, "faction-page-change-leader-button", button);
            SetChosenLeader(null);
            yield return CloseTab(s);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("screenshot-german-tab-without-clicks", MapSize = 40, Windowed = true, OnlyOn = Only, MaxSeconds = 150)]
        static IEnumerator German(ScenarioContext s) {
            var run = LanguageScenarios.InLanguage(s, "German", GermanBody(s));
            while (run.MoveNext()) yield return run.Current;
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        static IEnumerator GermanBody(ScenarioContext s) {
            var family = Family(s);
            yield return OpenTab(s, "Colonists");
            yield return ScenarioContext.Frames(300);
            Traverse.Create(RelationsWindow).Property("SelectedPawn").SetValue(family[0]);
            yield return ScenarioContext.Frames(100);
            var colonists = new Capture();
            yield return Shoot(s, "colonist-page-german", colonists);
            var pageKey = "Fluffy_Relations.Colonists".Translate().ToString();
            s.Check(colonists.Texts.Any(t => t.Text == pageKey), "the German tab should draw the translated page button '" + pageKey + "'");
            s.Check(!colonists.Texts.Any(t => t.Text.Contains("Fluffy_Relations") || t.Text.Contains("Fluffy.Relations")), "no drawn text should hold a raw key");
            Traverse.Create(RelationsWindow).Property("CurrentPage").SetValue(System.Enum.Parse(AccessTools.TypeByName("Fluffy_Relations.Page"), "Factions"));
            yield return ScenarioContext.Frames(300);
            var factions = new Capture();
            yield return Shoot(s, "faction-page-german", factions);
            var factionsKey = "Fluffy_Relations.Factions".Translate().ToString();
            s.Check(factions.Texts.Any(t => t.Text == factionsKey), "the German factions page should draw the translated page button '" + factionsKey + "'");
            s.Check(!factions.Texts.Any(t => t.Text.Contains("Fluffy_Relations") || t.Text.Contains("Fluffy.Relations")), "no drawn text should hold a raw key on the factions page");
            yield return CloseTab(s);
        }
    }
}
