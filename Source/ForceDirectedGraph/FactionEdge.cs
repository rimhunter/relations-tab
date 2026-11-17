using RimWorld;
using Verse;

namespace Fluffy_Relations.ForceDirectedGraph {
    public class FactionEdge: Edge {
        #region Constructors

        public FactionEdge(FactionNode nodeA, FactionNode nodeB) : base(nodeA, nodeB) {
        }

        #endregion Constructors

        #region Methods

        public override void Draw() {
            // get direction between nodes so we can offset our lines perpendicular to the line between nodes.
            Faction factionA = ( nodeA as FactionNode )?.faction;
            Faction factionB = ( nodeB as FactionNode )?.faction;

            if (factionA == null || factionB == null) {
                Log.Error("FactionEdge with non-FactionNode node(s), or null faction.");
                return;
            }

            float opinion = factionB.GoodwillWith(factionA);

            // draw lines
            Helpers.DrawBiDirectionalArrow(nodeA.position, nodeB.position, RelationsHelper.GetRelationColor(opinion));
        }

        #endregion Methods
    }
}
