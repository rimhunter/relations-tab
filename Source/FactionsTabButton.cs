using RimWorld;
using Verse;

namespace Fluffy_Relations {
    [StaticConstructorOnStartup]
    public static class FactionsTabButton {
        static FactionsTabButton() {
            Apply();
        }

        public static void Apply() {
            MainButtonDefOf.Factions.buttonVisible = Settings.keepVanillaFactionsTab;
        }
    }
}
