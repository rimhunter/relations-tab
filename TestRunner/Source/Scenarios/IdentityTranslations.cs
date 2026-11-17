using System.IO;
using System.Linq;
using System.Xml;
using RimWorld;
using Verse;

namespace RimhunterTestRunner.Scenarios
{
    static class IdentityTranslations
    {
        const string OurPackageId = "rimhunter.relationstab";
        const string ButtonLabelKey = "RelationsTab.label";
        const string RelationsWordKey = "Fluffy_Relations.Relations";
        static readonly string[] Folders = { "ChineseSimplified", "ChineseTraditional", "English", "French", "German", "Korean", "PortugueseBrazilian", "Spanish", "SpanishLatin" };

        static XmlElement[] Entries(string path) {
            var doc = new XmlDocument();
            doc.Load(path);
            return doc.DocumentElement.ChildNodes.OfType<XmlElement>().ToArray();
        }

        [Scenario("every-existing-translation-loads-with-no-error", MapSize = 30, MaxSeconds = 120)]
        static void EveryTranslationLoads(ScenarioContext s) {
            var mod = ModLister.GetActiveModWithIdentifier(OurPackageId, ignorePostfix: true);
            if (mod == null) throw new ScenarioFailure("the mod " + OurPackageId + " is not active");
            var root = mod.RootDir.FullName;
            var loaded = LanguageDatabase.AllLoadedLanguages.ToList();
            var defNames = DefDatabase<MainButtonDef>.AllDefsListForReading.Select(d => d.defName).ToHashSet();
            foreach (var folder in Folders) {
                var lang = loaded.SingleOrDefault(l => LanguageScenarios.Legacy(l.folderName) == folder);
                if (lang == null) {
                    s.Fail("the game has no language for the folder Languages/" + folder + "; it has: " + string.Join(", ", loaded.Select(l => l.folderName)));
                    continue;
                }
                lang.LoadData();
                s.Check(lang.loadErrors.Count == 0, folder + " loaded with errors: " + string.Join(" | ", lang.loadErrors));
                s.Check(!lang.anyKeyedReplacementsXmlParseError, folder + " has a Keyed file that does not parse: " + lang.lastKeyedReplacementsXmlParseErrorInFile);
                s.Check(!lang.anyDefInjectionsXmlParseError, folder + " has a DefInjected file that does not parse: " + lang.lastDefInjectionsXmlParseErrorInFile);

                var keyed = Path.Combine(root, "Languages", folder, "Keyed");
                var keys = Directory.GetFiles(keyed, "*.xml").SelectMany(Entries).ToList();
                s.Check(keys.Count > 0, folder + " should ship Keyed entries");
                foreach (var e in keys)
                    s.Check(lang.keyedReplacements.TryGetValue(e.Name, out var r) && r.value == e.InnerText.Replace("\\n", "\n"),
                        folder + " did not load the key " + e.Name + " from its Keys.xml");

                var package = lang.defInjections.SingleOrDefault(p => p.defType == typeof(MainButtonDef));
                if (folder != "English") {
                    DefInjectionPackage.DefInjection label = null;
                    var hasLabel = package != null && package.injections.TryGetValue(ButtonLabelKey, out label);
                    s.Check(hasLabel, folder + " has no " + ButtonLabelKey + ", so the Relations bottom bar button would read the English label instead of this language's word for relations");
                    var word = lang.keyedReplacements.TryGetValue(RelationsWordKey, out var relations) ? relations.value : null;
                    s.Check(word != null, folder + " has no " + RelationsWordKey + " text to take the button label from");
                    if (hasLabel && word != null)
                        s.Check(string.Equals(label.injection, word, System.StringComparison.OrdinalIgnoreCase),
                            folder + " labels the Relations button '" + label.injection + "', which is not the word its translators wrote for relations in " + RelationsWordKey + " ('" + word + "')");
                }

                var injected = Path.Combine(root, "Languages", folder, "DefInjected");
                if (!Directory.Exists(injected)) continue;
                foreach (var file in Directory.GetFiles(injected, "*.xml", SearchOption.AllDirectories))
                foreach (var e in Entries(file)) {
                    DefInjectionPackage.DefInjection injection = null;
                    s.Check(package != null && package.injections.TryGetValue(e.Name, out injection) && injection.injection == e.InnerText && injection.fileSource == Path.GetFileName(file),
                        folder + " did not load the DefInjected key " + e.Name + " from this mod; the game holds: " + (injection == null ? "nothing" : injection.injection + " from " + injection.fileSource));
                    var defName = e.Name.Split('.')[0];
                    s.Check(defNames.Contains(defName), folder + " translates the main button " + defName + ", which no def has; the defs are: " + string.Join(", ", defNames));
                }
            }
        }
    }
}
