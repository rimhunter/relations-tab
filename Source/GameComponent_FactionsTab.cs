using Verse;

namespace Fluffy_Relations {
    public class GameComponent_FactionsTab : GameComponent {
        public GameComponent_FactionsTab(Game game) : base() { }

        public override void FinalizeInit() {
            base.FinalizeInit();
            FactionsTabButton.Apply();
        }
    }
}
