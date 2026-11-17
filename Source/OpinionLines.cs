namespace Fluffy_Relations {
    public static class OpinionLines {
        public const float SelectedThreshold = 5f;

        public static bool IsStrong(float opinionAB, float opinionBA, float negativeThreshold, float positiveThreshold) {
            float max = opinionAB > opinionBA ? opinionAB : opinionBA;
            float min = opinionAB < opinionBA ? opinionAB : opinionBA;
            return Beyond(max, min, negativeThreshold, positiveThreshold);
        }

        static bool Beyond(float max, float min, float negativeThreshold, float positiveThreshold) {
            return max > positiveThreshold || min < negativeThreshold;
        }

        public static bool IsShown(float opinionAB, float opinionBA, bool selected, float negativeThreshold, float positiveThreshold) {
            float max = opinionAB > opinionBA ? opinionAB : opinionBA;
            float min = opinionAB < opinionBA ? opinionAB : opinionBA;
            if (Beyond(max, min, negativeThreshold, positiveThreshold)) {
                return true;
            }

            return selected && (max > SelectedThreshold || min < -SelectedThreshold);
        }
    }
}
