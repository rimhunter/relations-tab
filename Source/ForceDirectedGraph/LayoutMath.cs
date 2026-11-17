using System;

namespace Fluffy_Relations.ForceDirectedGraph {
    public static class LayoutMath {
        public const float MaxStepInSlots = .5f;
        public const float MaxIdealDistanceInSlots = 5f;
        public const int ReferenceCount = 14;
        public const float RepulsionDensityExponent = 3f;
        public const float AttractionDensityExponent = 2f;

        public static float IdealDistance(float width, float height, int nodeCount, float slotSize) {
            if (nodeCount < 1) {
                throw new ArgumentOutOfRangeException(nameof(nodeCount), "a graph needs at least one node");
            }

            double ideal = Math.Sqrt((double) width * height) / nodeCount;
            return (float) Math.Max(slotSize, Math.Min(slotSize * MaxIdealDistanceInSlots, ideal));
        }

        public static float Density(int nodeCount) {
            if (nodeCount < 1) {
                throw new ArgumentOutOfRangeException(nameof(nodeCount), "a graph needs at least one node");
            }

            return Math.Max(1f, (float) nodeCount / ReferenceCount);
        }

        public static float RepulsionScale(float density) {
            CheckDensity(density);
            return (float) Math.Pow(density, RepulsionDensityExponent);
        }

        public static float AttractionScale(float density) {
            CheckDensity(density);
            return (float) (1.0 / Math.Pow(density, AttractionDensityExponent));
        }

        private static void CheckDensity(float density) {
            if (float.IsNaN(density) || density < 1f) {
                throw new ArgumentOutOfRangeException(nameof(density), $"density {density} is below 1");
            }
        }

        public static float ClampCenter(float value, float areaSize, float slotSize) {
            if (float.IsNaN(value)) {
                throw new ArgumentOutOfRangeException(nameof(value), "node position is not a number");
            }

            if (areaSize < slotSize) {
                throw new ArgumentOutOfRangeException(nameof(areaSize), $"graph area {areaSize} is smaller than one slot ({slotSize})");
            }

            float half = slotSize / 2f;
            return Math.Max(half, Math.Min(areaSize - half, value));
        }

        public static (float x, float y) CapStep(float x, float y, float maxLength) {
            if (maxLength < 0f || float.IsNaN(maxLength)) {
                throw new ArgumentOutOfRangeException(nameof(maxLength), "step cap must be zero or more");
            }

            if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y)) {
                throw new ArgumentOutOfRangeException(nameof(x), $"node step ({x}, {y}) is not finite");
            }

            double length = Math.Sqrt(((double) x * x) + ((double) y * y));
            if (length <= maxLength) {
                return (x, y);
            }

            double scale = maxLength / length;
            return ((float) (x * scale), (float) (y * scale));
        }

        public static (float x, float y) CoincidentDirection(int fromIndex, int toIndex) {
            if (fromIndex == toIndex) {
                throw new ArgumentException("a node has no direction to itself");
            }

            return (fromIndex < toIndex ? 1f : -1f, 0f);
        }

        public static float StepCap(float temperature, float maxTemperature, float slotSize) {
            if (!(maxTemperature > 0f)) {
                throw new ArgumentOutOfRangeException(nameof(maxTemperature), $"maximum temperature {maxTemperature} is not above 0");
            }

            if (temperature < 0f || float.IsNaN(temperature) || temperature > maxTemperature) {
                throw new ArgumentOutOfRangeException(nameof(temperature), $"temperature {temperature} is outside 0 to {maxTemperature}");
            }

            return slotSize * MaxStepInSlots * (temperature / maxTemperature);
        }
    }
}
