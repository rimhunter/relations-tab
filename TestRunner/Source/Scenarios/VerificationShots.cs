using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionPageSupport;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;
using static RimhunterTestRunner.Scenarios.LeaderSupport;
using static RimhunterTestRunner.Scenarios.ShotSupport;

namespace RimhunterTestRunner.Scenarios
{
    static class VerificationShots
    {
        static IEnumerator EnsureColonistPage(ScenarioContext s, Window window) {
            var frame = s.DrawFrame();
            yield return frame;
            var canvas = CanvasPixels(window);
            var onFactions = "Fluffy_Relations.Factions".Translate().ToString();
            var onColonists = "Fluffy_Relations.Colonists".Translate().ToString();
            var buttons = frame.Texts.Where(t => (t.Text == onFactions || t.Text == onColonists) && canvas.Contains(t.Ink.center)).OrderBy(t => t.Ink.y).ToList();
            s.Check(buttons.Count > 0, "the page button '" + onFactions + "' or '" + onColonists + "' should be drawn at the top left of the tab; drawn in the tab: " + Dump(frame.Texts.Where(t => canvas.Contains(t.Ink.center))));
            if (buttons.Count == 0) throw new ScenarioFailure("no page button to tell which page the tab is on");
            if (buttons[0].Text != onFactions) yield break;
            yield return s.Click(buttons[0]);
            yield return ScenarioContext.Frames(2);
            var after = s.DrawFrame();
            yield return after;
            s.Check(after.Texts.Any(t => t.Text == onColonists && canvas.Contains(t.Ink.center)), "the page button should read '" + onColonists + "' on the colonist page");
        }

        [Scenario("screenshot-colonist-page-in-both-layouts", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 150)]
        static IEnumerator ColonistPageBothLayouts(ScenarioContext s) {
            var family = Family(s);
            var names = family.Select(p => p.LabelShort).ToList();
            var page = new ColonistPage(s);
            yield return page.Open();
            yield return EnsureColonistPage(s, RelationsWindow);
            yield return page.Frames(200);
            var centers = page.NodeCenters(page.Latest.Texts, names);
            s.AreEqual(6, centers.Count, "colonist nodes in the dynamic layout");
            if (centers.Count != 6) yield break;
            yield return page.ClickPixel(centers[family[0].LabelShort]);
            yield return page.Frames(100);
            var dynamic = new Capture();
            yield return Shoot(s, "colonist-page-dynamic-layout", dynamic);
            s.Check(dynamic.Texts.Any(t => t.Text == family[0].Name.ToStringFull), "the details should be titled with " + family[0].Name.ToStringFull);

            yield return page.ClickPixel(IconCenterPixels(RelationsWindow, 1));
            yield return page.Frames(10);
            var circle = new Capture();
            yield return Shoot(s, "colonist-page-circle-layout", circle);
            var after = page.NodeCenters(circle.Texts, names);
            s.AreEqual(6, after.Count, "colonist nodes in the circle layout");
            yield return page.ClickPixel(IconCenterPixels(RelationsWindow, 1));
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("screenshot-graphs-at-40-colonists-and-30-factions", MapSize = 60, Windowed = true, OnlyOn = "1.7", MaxSeconds = 170)]
        static IEnumerator Graphs(ScenarioContext s) {
            var colonists = new List<Pawn>();
            for (var dx = -14; dx <= 14; dx += 4)
                for (var dz = -8; dz <= 8; dz += 4)
                    colonists.Add(s.SpawnColonist(s.Cell(dx, dz)));
            foreach (var i in Enumerable.Range(0, 30))
                FactionGenerator.CreateFactionAndAddToManager(DefDatabase<FactionDef>.GetNamed(FactionDefNames[i % FactionDefNames.Length]));
            s.Tick(30);
            var colonistNames = colonists.Select(p => p.LabelShort).ToList();
            var factionLabels = VisibleLabels();
            s.Check(factionLabels.Count >= 31, "at least 31 visible factions should exist; there are " + factionLabels.Count);

            var page = new OpenPage();
            var open = new ColonistPage(s);
            yield return open.Open();
            page.Window = RelationsWindow;
            yield return EnsureColonistPage(s, page.Window);
            var settled = new SlotRead();
            var settle = Settle(s, colonistNames, settled);
            while (settle.MoveNext()) yield return settle.Current;
            s.AreEqual(40, settled.Slots.Count, "colonist nodes drawn on the settled graph");
            var colonistsShot = new Capture();
            yield return Shoot(s, "colonist-page-40-colonists", colonistsShot);

            var switching = SwitchPage(s, page.Window, "Fluffy_Relations.Colonists");
            while (switching.MoveNext()) yield return switching.Current;
            settled = new SlotRead();
            settle = Settle(s, factionLabels, settled);
            while (settle.MoveNext()) yield return settle.Current;
            s.AreEqual(factionLabels.Count, settled.Slots.Count, "faction nodes drawn on the settled graph");
            var factionsShot = new Capture();
            yield return Shoot(s, "faction-page-30-factions-settled", factionsShot);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("screenshot-leaderless-faction-and-change-leader-button", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 150)]
        static IEnumerator LeaderScreens(ScenarioContext s) {
            SetChosenLeader(null);
            try {
                var run = LeaderScreensBody(s);
                while (run.MoveNext()) yield return run.Current;
            }
            finally {
                SetChosenLeader(null);
                PointerHold.Release();
            }
        }

        static IEnumerator LeaderScreensBody(ScenarioContext s) {
            var old = SpawnColonistAged(s, -6, 90);
            SpawnColonistAged(s, 0, 60);
            SpawnColonistAged(s, 6, 40);
            var leaderless = MakeLeaderlessHumanlike();
            AddFaction("TribeCivil");
            AddFaction("Pirate");
            s.Tick(30);
            var labels = VisibleLabels();
            var page = new OpenPage();
            var open = OpenFactionsPage(s, page);
            while (open.MoveNext()) yield return open.Current;
            var click = ClickNode(s, LabelOf(leaderless), labels);
            while (click.MoveNext()) yield return click.Current;
            var frame = s.DrawFrame();
            yield return frame;
            s.Check(Details(frame).Any(t => t.Text.EndsWith(": " + "Nobody".Translate())), "the leaderless faction's details should read 'Nobody' for the leader; drawn: " + string.Join(" | ", Details(frame)));
            var leaderlessShot = new Capture();
            yield return Shoot(s, "faction-page-leaderless-faction", leaderlessShot);

            var details = new ColonyDetails();
            var show = ShowColonyDetails(s, page, details);
            while (show.MoveNext()) yield return show.Current;
            s.AreEqual(old.Name.ToStringFull, LeaderRowOf(details.Labels), "the leader shown for the colony");
            s.AreEqual(1, details.EditButtons, "change-leader buttons drawn");
            if (details.Button == null) throw new ScenarioFailure("no change-leader button drawn");
            var buttonShot = new Capture();
            yield return Shoot(s, "faction-page-change-leader-button", buttonShot);

            yield return s.Click(ClickTarget(details.Button.Pixels.center));
            PointerHold.At(details.Button.Pixels.center);
            yield return ScenarioContext.Frames(3);
            s.AreEqual(1, Find.WindowStack.Windows.OfType<FloatMenu>().Count(), "leader choice windows open after the button is clicked");
            var menuShot = new Capture();
            yield return Shoot(s, "faction-page-change-leader-choice", menuShot);
            PointerHold.Release();
            foreach (var menu in Find.WindowStack.Windows.OfType<FloatMenu>().ToList()) menu.Close(false);
            yield return FactionsTabSupport.CloseOpenTab(s);
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        static IEnumerator ShootBar(ScenarioContext s, string name, string expectedButtons) {
            s.AreEqual(expectedButtons, FactionsTabSupport.VisibleButtons(), "the bottom bar buttons for " + name);
            var shot = new Capture();
            yield return Shoot(s, name, shot);
            foreach (var (def, _) in BarButtons().Where(b => !b.def.minimized))
                s.Check(shot.Texts.Any(t => t.Text == def.LabelCap.ToString()), "the bar should draw '" + def.LabelCap + "' for " + name);
        }

        [Scenario("screenshot-bottom-bar-with-and-without-the-vanilla-factions-tab", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 150)]
        static IEnumerator BottomBars(ScenarioContext s) {
            s.SpawnColonist(s.Cell(0, 0));
            s.Tick(30);
            yield return ShootBar(s, "bottom-bar-relations-replaces-factions", "Architect Work Schedule Assign Animals Wildlife Research Quests World History RelationsTab Menu");
            yield return FactionsTabSupport.ToggleKeepVanillaFactionsTab(s);
            yield return ShootBar(s, "bottom-bar-relations-beside-factions", "Architect Work Schedule Assign Animals Wildlife Research Quests World History Factions RelationsTab Menu");
            yield return FactionsTabSupport.ToggleKeepVanillaFactionsTab(s);
            s.AreEqual("Architect Work Schedule Assign Animals Wildlife Research Quests World History RelationsTab Menu", FactionsTabSupport.VisibleButtons(), "the bottom bar after the setting is turned off again");
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("screenshot-german-tab", MapSize = 40, Windowed = true, OnlyOn = "1.7", MaxSeconds = 150)]
        static IEnumerator GermanTab(ScenarioContext s) => LanguageScenarios.InLanguage(s, "German", GermanBody(s));

        static IEnumerator GermanBody(ScenarioContext s) {
            var family = Family(s);
            var page = new ColonistPage(s);
            yield return page.Open();
            yield return EnsureColonistPage(s, RelationsWindow);
            yield return page.Frames(200);
            var colonists = new Capture();
            yield return Shoot(s, "colonist-page-german", colonists);
            var tip = "Fluffy_Relations.Relations".Translate().ToString();
            s.Check(colonists.Texts.Any(t => t.Text.Contains(tip)), "the German colonist page should draw the translated '" + tip + "'");
            var pageKey = "Fluffy_Relations.Colonists".Translate().ToString();
            s.Check(colonists.Texts.Any(t => t.Text == pageKey), "the German tab should draw the translated page button '" + pageKey + "'");
            s.Check(!colonists.Texts.Any(t => t.Text.Contains("Fluffy_Relations") || t.Text.Contains("Fluffy.Relations")), "no drawn text should hold a raw key");

            var switching = SwitchPage(s, RelationsWindow, "Fluffy_Relations.Colonists");
            while (switching.MoveNext()) yield return switching.Current;
            yield return page.Frames(200);
            var factions = new Capture();
            yield return Shoot(s, "faction-page-german", factions);
            s.Check(!factions.Texts.Any(t => t.Text.Contains("Fluffy_Relations") || t.Text.Contains("Fluffy.Relations")), "no drawn text should hold a raw key on the factions page");
            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
