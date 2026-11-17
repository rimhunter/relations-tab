using System;
using UnityEngine;
using Verse;

namespace Fluffy_Relations.ForceDirectedGraph {
    public class Node {
        #region Fields

        protected bool frozen;
        public Pawn pawn;
        public Vector2 position;
        public Vector2 velocity = Vector2.zero;
        public Graph graph;
        public bool secondary = false;

        public Action OnLeftClick;
        public Action OnRightClick;
        public Action OnHover;
        public Action PostDrawExtras;
        public Action PreDrawExtras;

        #endregion Fields

        #region Constructors

        public Node(Pawn pawn, Vector2 position, Graph graph, bool frozen = false, bool secondary = false) {
            this.pawn = pawn;
            this.position = position;
            this.frozen = frozen;
            this.graph = graph;
            this.secondary = secondary;

            OnRightClick += new Action(() => Frozen = false);

#if DEBUG
            OnHover += new Action(() => Log.Message("OnHover()" + Frozen));
            OnLeftClick += new Action(() => Log.Message("OnLeftClick()" + Frozen));
            OnRightClick += new Action(() => Log.Message("OnRightClick()" + Frozen));
#endif
        }

        #endregion Constructors

        public bool Frozen {
            get => frozen;
            set => frozen = value;
        }

        #region Methods

        public virtual void AttractedTo(Node other) {
            if (frozen) {
                return;
            }

            // pull node towards other
            float force = Graph.ATTRACTIVE_CONSTANT * Mathf.Max( this.DistanceTo( other ) - Graph.idealDistance, 0f ) * graph.AttractionScale;
            velocity += force * this.DirectionTo(other);

#if DEBUG_GRAPH
            Graph.msg.AppendLine("\t\tForce: " + force + ", Vector: " + (force * this.DirectionTo(other)));
#endif
        }

        public virtual void RepulsedBy(Node other) {
            if (frozen) {
                return;
            }

            // remove square?
            float force = -(  Graph.REPULSIVE_CONSTANT / Mathf.Pow( this.DistanceTo( other ), 2 )  * Graph.idealDistance * graph.RepulsionScale );
            velocity += force * this.DirectionTo(other);

#if DEBUG_GRAPH
            Graph.msg.AppendLine("\t\tRepulsion from " + other.pawn.Name.ToStringShort + other.position + "; Distance: " + this.DistanceTo(other) + " (" + (Mathf.Pow(this.DistanceTo(other), 2) * Graph.idealDistance) + "), Force: " + force + ", Vector: " + (force * this.DirectionTo(other)));
#endif
        }

        public virtual void Clamp(Vector2 size) {
            position.x = LayoutMath.ClampCenter(position.x, size.x, Constants.SlotSize);
            position.y = LayoutMath.ClampCenter(position.y, size.y, Constants.SlotSize);
        }

        public Rect slot {
            get {
                Rect slot = Resources.baseSlot;
                slot.center = position;
                return slot;
            }
        }

        public virtual void Draw() {
            // call extra draw handlers
            PreDrawExtras?.Invoke();

            // draw basic slot
            pawn.DrawSlot(slot, false, label: pawn.LabelShort, secondary: secondary);

            // call extra draw handlers
            PostDrawExtras?.Invoke();

            // do interactions, with all their handlers and stuff
            Interactions();
        }

        private bool _pressed;
        private bool _dragged;

        // RimWorld 1.7's GUI.Button only completes a click when the button's rect is identical on mouse down and mouse up,
        // so a node that is still moving never receives its click. Clicks are detected from the mouse events instead.
        public virtual void Interactions() {
            Event current = Event.current;
            bool over = Mouse.IsOver(slot);

            if (current.rawType == EventType.MouseDown) {
                _dragged = false;
                _pressed = false;
            }

            if (current.type == EventType.MouseDown) {
                _pressed = over && (current.button == 0 || current.button == 1);
                if (_pressed) {
                    current.Use();
                }
            }

            if (over) {
                OnHover?.Invoke();

                if (current.button == 0 && current.type == EventType.MouseDrag) {
                    position += current.delta;
                    Clamp(graph.size);
                    frozen = true;
                    _dragged = true;
                    graph.Restart();
                }
            }

            if (current.type == EventType.MouseUp && _pressed) {
                _pressed = false;
                if (over && !_dragged) {
                    int button = current.button;
                    current.Use();
                    if (button == 1) {
                        OnRightClick?.Invoke();
                    }

                    if (button == 0) {
                        OnLeftClick?.Invoke();
                    }
                }
            }
        }

        #endregion Methods
    }
}
