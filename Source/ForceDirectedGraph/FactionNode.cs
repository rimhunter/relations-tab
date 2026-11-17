using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy_Relations.ForceDirectedGraph {
    public class FactionNode: Node {
        public Faction faction;

        public FactionNode(Faction faction, Vector2 position, Graph graph, bool frozen = false) : base(LeaderOf(faction), position, graph, frozen) {
            this.faction = faction;
        }

        private static Pawn LeaderOf(Faction faction) {
            if (faction == null) {
                throw new ArgumentNullException(nameof(faction));
            }

            return faction.Leader();
        }

        public override void AttractedTo(Node other) {
            if (frozen) {
                return;
            }

            // pull node towards other
            float force = Graph.ATTRACTIVE_CONSTANT * Mathf.Max( this.DistanceTo( other ) - Graph.idealDistance, 0f ) * graph.AttractionScale;

            // increase force if opinion > 1
            float opinion = 999;
            if (other is FactionNode otherFaction) {
                opinion = faction.GoodwillWith(otherFaction.faction);
                if (opinion > 1f) {
                    force *= Mathf.Sqrt(opinion);
                }
            }

            // apply force in direction of other
            velocity += force * this.DirectionTo(other);

#if DEBUG_GRAPH
            Graph.msg.AppendLine("\t\tOpinion: " + opinion + ", Force: " + force + ", Vector: " + (force * this.DirectionTo(other)));
#endif
        }

        private void DrawFactionIconSlot() {
            string label = faction.GetFactionLabel();
            PawnSlotDrawer.DrawTextureColoured(slot.ContractedBy(Constants.Inset), faction.def.FactionIcon, faction.Color);
            Rect labelRect = PawnSlotDrawer.LabelRect(label, slot);
            Text.Font = GameFont.Tiny;
            GUI.DrawTexture(labelRect, Resources.SlightlyDarkBG);
            Widgets.Label(labelRect, label);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        public override void Draw() {
            // call extra draw handlers
            PreDrawExtras?.Invoke();

            // draw basic slot, with faction label
            if (pawn != null) {
                PawnSlotDrawer.DrawSlot(pawn, slot, false, label: faction.GetFactionLabel());
            } else {
                DrawFactionIconSlot();
            }

            // call extra draw handlers
            PostDrawExtras?.Invoke();

            // do interactions, with all their handlers and stuff
            Interactions();
        }
    }
}
