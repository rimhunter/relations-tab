using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimhunterTestRunner.Scenarios.LayoutScenarioHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class FactionsTabSupport
    {
        public const string FactionsWindowType = "RimWorld.MainTabWindow_Factions";
        public const string ControllerType = "Fluffy_Relations.Controller";

        public static MainButtonDef FactionsButton =>
            DefDatabase<MainButtonDef>.AllDefsListForReading.Single(d => d.tabWindowClass?.FullName == FactionsWindowType);

        public static Window WindowOfType(string type) =>
            Find.WindowStack.Windows.FirstOrDefault(w => w.GetType().FullName == type);

        public static string VisibleButtons() =>
            string.Join(" ", DefDatabase<MainButtonDef>.AllDefs.OrderBy(d => d.order).Where(d => d.Worker.Visible).Select(d => d.defName));

        public static void CheckBarLayoutMatchesDrawing(ScenarioContext s, ScenarioContext.DrawnFrame frame) {
            foreach (var (def, pixels) in BarButtons().Where(b => !b.def.minimized)) {
                var label = frame.Texts.FirstOrDefault(t => t.Text == def.LabelCap.ToString() && t.Ink.center.y > pixels.y);
                s.Check(label != null && label.Ink.x >= pixels.x && label.Ink.xMax <= pixels.xMax,
                    "the game should have drawn the '" + def.LabelCap + "' button label inside the bar rect worked out for it " + pixels + "; it drew: " + Dump(frame.Texts.Where(t => t.Ink.center.y > pixels.y)));
            }
        }

        public static IEnumerator ClickBarButton(ScenarioContext s, MainButtonDef def) {
            var target = BarButtons().Single(b => b.def == def).pixels;
            yield return s.Click(new DrawnText { Text = def.LabelCap.ToString(), Box = target, Ink = target });
            yield return s.DrawFrame();
            yield return s.DrawFrame();
        }

        public static string SettingsFileText() {
            var mod = LoadedModManager.ModHandles.Single(m => m.GetType().FullName == ControllerType);
            var path = System.IO.Path.Combine(GenFilePaths.ConfigFolderPath, "Mod_" + mod.Content.FolderName + "_" + mod.GetType().Name + ".xml");
            return System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "";
        }

        public static IEnumerator CloseOpenTab(ScenarioContext s) {
            Find.MainTabsRoot.EscapeCurrentTab(false);
            yield return s.DrawFrame();
            yield return s.DrawFrame();
        }

        public static IEnumerator ToggleKeepVanillaFactionsTab(ScenarioContext s) {
            var mod = LoadedModManager.ModHandles.Single(m => m.GetType().FullName == ControllerType);
            Find.WindowStack.Add(new Dialog_ModSettings(mod));
            yield return s.DrawFrame();
            var frame = s.DrawFrame();
            yield return frame;
            var label = "Fluffy_Relations.KeepVanillaFactionsTab".Translate().ToString();
            var checkbox = frame.Texts.FirstOrDefault(t => t.Text == label);
            s.Check(checkbox != null, "the settings window should show the '" + label + "' setting; it drew: " + Dump(frame.Texts));
            if (checkbox == null) yield break;
            yield return s.Click(checkbox);
            yield return s.DrawFrame();
            var after = s.DrawFrame();
            yield return after;
            var close = after.Texts.FirstOrDefault(t => t.Text == "CloseButton".Translate().ToString());
            s.Check(close != null, "the settings window should show its Close button; it drew: " + Dump(after.Texts));
            if (close == null) yield break;
            yield return s.Click(close);
            yield return s.DrawFrame();
            yield return s.DrawFrame();
            s.Check(!Find.WindowStack.Windows.Any(w => w is Dialog_ModSettings), "the settings window should be closed");
        }
    }
}
