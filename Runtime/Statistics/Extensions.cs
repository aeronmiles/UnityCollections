using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace AM.Unity.Statistics
{
    public static class StatisticsExt
    {

        public static List<float> Errors(this IEnumerable<float> values, float referenceValue)
        {
            List<float> errors = values.ToList();
            foreach (var v in values)
                errors.Add(math.distance(v, referenceValue));

            return errors;
        }

        public static float Mean(this IEnumerable<float> floats)
        {
            float m = 0f;
            foreach (var v in floats)
                m += v;

            return m / floats.ToArray().Length;
        }

        public static float Min(this IEnumerable<float> floats)
        {
            float m = float.MaxValue;
            foreach (var v in floats)
                m = math.min(v, m);

            return m;
        }

        public static float Max(this IEnumerable<float> floats)
        {
            float m = float.MinValue;
            foreach (var v in floats)
                m = math.max(v, m);

            return m;
        }

        public static float Median(this IEnumerable<float> floats)
        {
            //Framework 2.0 version of this method. there is an easier way in F4  
            var sortedPNumbers = floats.ToArray();
            int l = sortedPNumbers.Length;
            if (floats == null || l == 0)
                throw new System.Exception("Median of empty array not defined.");

            //make sure the list is sorted, but use a new array
            Array.Sort(sortedPNumbers);

            //get the median
            int mid = l / 2;
            float median = (l % 2 != 0) ? sortedPNumbers[mid] : (sortedPNumbers[mid] + sortedPNumbers[mid - 1]) / 2;

            return median;
        }

        public static float Squared(this float f) => math.sqrt(f);

        public static float Variance(this IEnumerable<float> floats)
        {
            int l = 0;
            float sumOfSquares = 0.0f;
            float mean = floats.Mean();
            foreach (var f in floats)
            {
                l++;
                sumOfSquares += math.pow(f - mean, 2.0f);
            }
            
            if (l > 0)
                return sumOfSquares / l;
            else
                return 0f;
        }


        public static MeanMedianVarMinMax ToMeanMedianVarMinMax(this IEnumerable<float> floats)
        {
            MeanMedianVarMinMax mmv = new MeanMedianVarMinMax();

            mmv.mean = floats.Mean();
            mmv.median = floats.Median();
            mmv.variance = floats.Variance();
            mmv.min = floats.Min();
            mmv.max = floats.Max();

            return mmv;
        }

        public static float2 Mean(this float2[] vs)
        {
            float2 average = new float2();
            int l = vs.Length;
            for (int i = 0; i < l; i++)
            {
                average += vs[i];
            }

            return average / l;
        }

        public static Vector2 Mean(this Vector2[] vs)
        {
            Vector2 average = new Vector2();
            int l = vs.Length;
            for (int i = 0; i < l; i++)
            {
                average += vs[i];
            }

            return average / l;
        }

        public static long Mean(this long[] vals)
        {
            long m = 0;
            int l = 0;
            for (int i = 0; i < l; i++)
            {
                m += vals[i];
            }

            return m / l;
        }

        public static float MmToMeters(this float f) => f / 1000f;
        public static float MetersToMMs(this float f) => f * 1000f;

    }
}
