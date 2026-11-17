using System.Collections;
using System.Linq;
using RimWorld;
using Verse;

namespace RimhunterTestRunner.Scenarios
{
    static class IdentityModList
    {
        const string OurPackageId = "rimhunter.relationstab";
        static readonly string[] Others = { "Fluffy.FluffyRelations", "ckvam.relationstab.separatebutton", "jia.RelationsTabLanguage" };

        static ModMetaData Mod(string packageId) {
            var mod = ModLister.GetModWithIdentifier(packageId, ignorePostfix: true);
            if (mod == null) throw new ScenarioFailure("the stand-in mod " + packageId + " is not in the Mods folder");
            return mod;
        }

        [Scenario("screenshot-mod-list-warns-when-fluffys-relations-tab-is-also-enabled", MapSize = 40, Windowed = true, MaxSeconds = 150,
            StageMods = "TestRunner/Fixtures/fluffy-relations")]
        static IEnumerator ModListWarningFluffy(ScenarioContext s) {
            var page = new Page_ModsConfig();
            Find.WindowStack.Add(page);
            yield return null;
            var fluffy = Mod("Fluffy.FluffyRelations");
            var ours = Mod(OurPackageId);
            s.Check(!fluffy.Active, "Fluffy's Relations Tab should start inactive");
            try {
                ModsConfig.SetActive(fluffy, true);
                page.SelectMod(ours);
                var shot = s.Screenshot("01-mod-list-incompatibility-warning");
                yield return shot;
                ModsConfig.GetModWarnings().TryGetValue(ours.PackageId, out var warning);
                s.Check((warning ?? "").IndexOf(fluffy.Name, System.StringComparison.OrdinalIgnoreCase) >= 0, "the game's warning for this mod should name Fluffy's Relations Tab, was: " + warning);
                var incompatible = "ModIncompatibleWith".Translate("").Resolve().Trim();
                s.Check(shot.ShowsText(incompatible), "the mod list should show \"" + incompatible + "\" for this mod; it showed: " + shot.TextsMatching("incompatible"));
                s.Check(shot.Texts.Count(t => t.Text == fluffy.Name) >= 2, "the mod list should show Fluffy's Relations Tab among the enabled mods and in the incompatibility row; it showed: " + shot.TextsMatching("Relations Tab"));
            }
            finally {
                ModsConfig.SetActive(fluffy, false);
                page.Close(doCloseSound: false);
            }
            yield return null;
            s.Check(!page.IsOpen, "the mod list should close without saving once Fluffy's Relations Tab is turned off again");
        }

        [Scenario("mod-list-warns-for-each-other-relations-tab-build", MapSize = 30, MaxSeconds = 90,
            StageMods = "TestRunner/Fixtures/fluffy-relations TestRunner/Fixtures/separate-button-relations TestRunner/Fixtures/relations-tab-language")]
        static void ModListWarnsForEach(ScenarioContext s) {
            var ours = Mod(OurPackageId);
            foreach (var id in Others) {
                var other = Mod(id);
                try {
                    s.Check(!other.Active, id + " should start inactive");
                    ModsConfig.SetActive(other, true);
                    ModsConfig.GetModWarnings().TryGetValue(ours.PackageId, out var warning);
                    s.Check((warning ?? "").IndexOf(other.Name, System.StringComparison.OrdinalIgnoreCase) >= 0, "the game's warning for this mod should name " + id + " (" + other.Name + "), was: " + warning);
                }
                finally {
                    ModsConfig.SetActive(other, false);
                }
            }
        }

        [Scenario("mod-list-does-not-warn-when-nothing-else-is-enabled", MapSize = 30, MaxSeconds = 90)]
        static void NoWarningAlone(ScenarioContext s) {
            var ours = Mod(OurPackageId);
            ModsConfig.GetModWarnings().TryGetValue(ours.PackageId, out var warning);
            s.Check(string.IsNullOrEmpty(warning), "the mod list should not warn about this mod when no other Relations Tab is enabled, was: " + warning);
        }
    }
}
