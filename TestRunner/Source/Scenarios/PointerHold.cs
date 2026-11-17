using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace RimhunterTestRunner.Scenarios
{
    static class PointerHold
    {
        static bool patched;
        static Vector2? devicePoint;
        static bool dragging;
        static bool held;
        static bool pressNow;
        static bool releaseNow;

        static void Patch() {
            if (patched) return;
            var harmony = new Harmony("rimhunter.relationstab.testrunner.pointer");
            harmony.Patch(AccessTools.PropertyGetter(typeof(Input), nameof(Input.mousePosition)), prefix: new HarmonyMethod(typeof(PointerHold), nameof(MousePosition)));
            harmony.Patch(AccessTools.Method(typeof(Input), nameof(Input.GetMouseButton)), prefix: new HarmonyMethod(typeof(PointerHold), nameof(Held)));
            harmony.Patch(AccessTools.Method(typeof(Input), nameof(Input.GetMouseButtonDown)), prefix: new HarmonyMethod(typeof(PointerHold), nameof(Down)));
            harmony.Patch(AccessTools.Method(typeof(Input), nameof(Input.GetMouseButtonUp)), prefix: new HarmonyMethod(typeof(PointerHold), nameof(Up)));
            patched = true;
        }

        public static void At(Vector2 point) {
            Patch();
            devicePoint = point;
        }

        public static void Release() => devicePoint = null;

        static bool MousePosition(ref Vector3 __result) {
            if (devicePoint == null) return true;
            __result = new Vector3(devicePoint.Value.x, Screen.height - 1f - devicePoint.Value.y, 0f);
            return false;
        }

        static bool Held(int button, ref bool __result) {
            if (!dragging || button != 0) return true;
            __result = held;
            return false;
        }

        static bool Down(int button, ref bool __result) {
            if (!dragging || button != 0) return true;
            __result = pressNow;
            return false;
        }

        static bool Up(int button, ref bool __result) {
            if (!dragging || button != 0) return true;
            __result = releaseNow;
            return false;
        }

        public static IEnumerator Drag(Vector2 from, Vector2 to, int steps) {
            Patch();
            try {
                dragging = true;
                devicePoint = from;
                yield return null;
                yield return null;
                pressNow = true;
                held = true;
                yield return null;
                pressNow = false;
                yield return null;
                for (var i = 1; i <= steps; i++) {
                    devicePoint = Vector2.Lerp(from, to, i / (float)steps);
                    yield return null;
                }
                yield return null;
                held = false;
                releaseNow = true;
                yield return null;
                releaseNow = false;
                yield return null;
            }
            finally {
                dragging = false;
                pressNow = held = releaseNow = false;
                devicePoint = null;
            }
        }
    }
}
