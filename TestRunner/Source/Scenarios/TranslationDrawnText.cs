using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using RimWorld;
using Verse;
using static RimhunterTestRunner.Scenarios.FactionPageSupport;

namespace RimhunterTestRunner.Scenarios
{
    static class TranslationDrawnText
    {
        const string OurPackageId = "rimhunter.relationstab";
        static readonly string[] CodeKeysOutsideTheKeyedFile = { "Nobody", "HostileTo" };

        static ModContentPack Package(string packageId) {
            var pack = LoadedModManager.RunningModsListForReading.FirstOrDefault(m => string.Equals(m.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
            if (pack == null) throw new ScenarioFailure("the mod " + packageId + " is not running");
            return pack;
        }

        static void AddKeyed(Dictionary<string, string> into, XmlDocument doc) {
            foreach (var e in doc.DocumentElement.ChildNodes.OfType<XmlElement>()) {
                var text = e.InnerText.Replace("\\n", "\n");
                if (text.Length > 0 && text != "TODO") into[e.Name] = text;
            }
        }

        static Dictionary<string, string> KeyedInFolder(string folder) {
            var texts = new Dictionary<string, string>();
            var keyed = Path.Combine(folder, "Keyed");
            if (!Directory.Exists(keyed)) return texts;
            foreach (var file in Directory.GetFiles(keyed, "*.xml")) {
                var doc = new XmlDocument();
                doc.Load(file);
                AddKeyed(texts, doc);
            }
            return texts;
        }

        static Dictionary<string, string> KeyedInTar(string tarPath) {
            var texts = new Dictionary<string, string>();
            using (var stream = File.OpenRead(tarPath)) {
                var header = new byte[512];
                while (ReadBlock(stream, header)) {
                    if (header.All(b => b == 0)) break;
                    var name = Encoding.UTF8.GetString(header, 0, 100).TrimEnd('\0');
                    var size = Convert.ToInt64(Encoding.ASCII.GetString(header, 124, 11).Trim('\0', ' '), 8);
                    var body = new byte[size];
                    var read = 0;
                    while (read < size) {
                        var n = stream.Read(body, read, (int)size - read);
                        if (n <= 0) throw new ScenarioFailure("the archive " + tarPath + " ends inside " + name);
                        read += n;
                    }
                    var padding = (512 - (int)(size % 512)) % 512;
                    stream.Seek(padding, SeekOrigin.Current);
                    if (name.StartsWith("Keyed/") && name.EndsWith(".xml")) {
                        var doc = new XmlDocument();
                        using (var memory = new MemoryStream(body)) doc.Load(memory);
                        AddKeyed(texts, doc);
                    }
                }
            }
            return texts;
        }

        static bool ReadBlock(Stream stream, byte[] block) {
            var read = 0;
            while (read < block.Length) {
                var n = stream.Read(block, read, block.Length - read);
                if (n <= 0) return false;
                read += n;
            }
            return true;
        }

        static Dictionary<string, string> PackageTexts(string packageId, string languageFolder) {
            var languages = Path.Combine(Package(packageId).RootDir, "Languages");
            var tar = Path.Combine(languages, languageFolder + ".tar");
            if (File.Exists(tar)) return KeyedInTar(tar);
            return KeyedInFolder(Path.Combine(languages, languageFolder));
        }

        static Dictionary<string, string> ModTexts(string folder) =>
            KeyedInFolder(Path.Combine(Package(OurPackageId).RootDir, "Languages", folder));

        static void CheckKeys(ScenarioContext s, LoadedLanguage lang) {
            var legacy = LanguageScenarios.Legacy(lang.folderName);
            var ownEnglish = ModTexts("English");
            var ownLanguage = ModTexts(legacy);
            var coreEnglish = PackageTexts("ludeon.rimworld", "English");
            var coreLanguage = PackageTexts("ludeon.rimworld", lang.folderName);
            var ideologyEnglish = ModsConfig.IdeologyActive ? PackageTexts("ludeon.rimworld.ideology", "English") : new Dictionary<string, string>();
            var ideologyLanguage = ModsConfig.IdeologyActive ? PackageTexts("ludeon.rimworld.ideology", lang.folderName) : new Dictionary<string, string>();
            var sources = new List<(string key, Dictionary<string, string> language, Dictionary<string, string> english, string where)>();
            foreach (var key in ownEnglish.Keys) sources.Add((key, ownLanguage, ownEnglish, "mod"));
            foreach (var key in CodeKeysOutsideTheKeyedFile) sources.Add((key, coreLanguage, coreEnglish, "Core"));
            if (ModsConfig.IdeologyActive) sources.Add(("Certainty", ideologyLanguage, ideologyEnglish, "Ideology"));
            foreach (var (key, language, english, where) in sources) {
                var shown = key.Translate().ToString();
                s.Check(!key.StartsWith("Fluffy") || (shown != key && !shown.Contains(key)), lang.folderName + " draws the raw key " + key + " instead of text");
                var expected = language.TryGetValue(key, out var own) ? own : english.TryGetValue(key, out var fallback) ? fallback : null;
                s.Check(expected != null, "neither the " + where + " files for " + lang.folderName + " nor its English files hold a text for " + key);
                if (expected != null)
                    s.AreEqual(expected, shown, lang.folderName + " text for " + key + " (read from the " + where + " files on disk: the language's own, else English)");
            }
        }

        [Scenario("translation-english-game-draws-no-missing-key", MapSize = 30, MaxSeconds = 120)]
        static void English(ScenarioContext s) {
            s.Check(LanguageDatabase.activeLanguage == LanguageDatabase.defaultLanguage, "the game should be in English");
            CheckKeys(s, LanguageDatabase.activeLanguage);
            s.RequireNoLogErrors();
        }

        static void Each(ScenarioContext s, params string[] folders) {
            foreach (var folder in folders) LanguageScenarios.InLanguage(folder, lang => CheckKeys(s, lang));
            s.RequireNoLogErrors();
        }

        [Scenario("translation-chinese-korean-and-french-draw-no-missing-key", MapSize = 30, MaxSeconds = 150)]
        static void FirstGroup(ScenarioContext s) => Each(s, "ChineseSimplified", "ChineseTraditional", "Korean", "French");

        [Scenario("translation-german-spanish-and-portuguese-draw-no-missing-key", MapSize = 30, MaxSeconds = 150)]
        static void SecondGroup(ScenarioContext s) => Each(s, "German", "Spanish", "SpanishLatin", "PortugueseBrazilian");

        [Scenario("translation-german-leaderless-faction-details-have-no-raw-key", MapSize = 40, Windowed = true, OnlyOn = "1.7", Mods = "ludeon.rimworld.ideology", MaxSeconds = 150)]
        static IEnumerator GermanFactionDetails(ScenarioContext s) => LanguageScenarios.InLanguage(s, "German", GermanBody(s));

        static IEnumerator GermanBody(ScenarioContext s) {
            for (var dx = -8; dx <= 8; dx += 8) s.SpawnColonist(s.Cell(dx, 0));
            var leaderless = MakeLeaderlessHumanlike();
            AddFaction("TribeCivil");
            s.Tick(30);
            var expected = VisibleLabels();
            var page = new OpenPage();
            var open = OpenFactionsPage(s, page);
            while (open.MoveNext()) yield return open.Current;
            var settled = new SlotRead();
            var settle = Settle(s, expected, settled);
            while (settle.MoveNext()) yield return settle.Current;
            var click = ClickNode(s, LabelOf(leaderless), expected);
            while (click.MoveNext()) yield return click.Current;
            var frame = s.DrawFrame();
            yield return frame;
            var texts = frame.Texts.Select(t => t.Text).ToList();
            var all = string.Join(" | ", texts);
            s.Check(Details(frame).Any(t => t.Text.EndsWith(": Niemand")), "the leaderless faction's details should end the leader line with Core's German \"Niemand\"; they show: " + all);
            s.Check(!texts.Any(t => t.Contains("Fluffy_Relations") || t.Contains("Fluffy.Relations") || t.Contains("Nobody")), "no drawn text should hold a raw key; drawn: " + all);
            s.RequireNoLogErrors();
        }
    }
}
