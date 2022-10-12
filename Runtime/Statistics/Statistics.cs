
using Unity.Mathematics;

namespace AM.Unity.Statistics
{
    public static class Statistics
    {
        public const float Z80 = 1.28f;
        public const float Z90 = 1.645f;
        public const float Z95 = 1.96f;
        public const float Z99 = 2.58f;
        public static float Confidence(float std, int n, float z) => z * (std / math.sqrt(n));

    }
}
