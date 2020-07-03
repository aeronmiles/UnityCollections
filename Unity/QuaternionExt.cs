using UnityEngine;
using Unity.Mathematics;

public static class QuaternionExt
{
    public static Quaternion[] Randomize(this Quaternion[] vs, int seed)
    {
        UnityEngine.Random.InitState(seed);
        int l = vs.Length;
        for (int i = 0; i < l; i++)
        {
            vs[i] = UnityEngine.Random.rotation;
        }
        return vs;
    }

    public static Quaternion[] RandomAngleStep(this Quaternion[] vs, int seed, Vector3 angleStep)
    {
        int l = vs.Length;
        float3 div = new float3(360f);
        div.x /= angleStep.x;
        div.y /= angleStep.y;
        div.z /= angleStep.z;

        var r = new Unity.Mathematics.Random((uint)seed);

        for (int i = 0; i < l; i++)
        {
            var x = (int)(r.NextFloat() * div.x) * angleStep.x;
            var y = (int)(r.NextFloat() * div.y) * angleStep.y;
            var z = (int)(r.NextFloat() * div.z) * angleStep.z;

            vs[i] = Quaternion.Euler(x, y, z);
        }
        return vs;
    }

    public static Quaternion Average(this Quaternion[] qArray)
    {
        if (qArray.Length == 0)
        {
            Debug.LogWarning("Trying to average Quaternion array of Length 0");
            return Quaternion.identity;
        }

        Quaternion qAvg = qArray[0];
        for (int i = 1; i < qArray.Length; i++)
        {
            qAvg = Quaternion.Slerp(qAvg, qArray[i], 1.0f / (float)(i + 1));
        }
        return qAvg;
    }

    public static Quaternion[] MultiplyEuler(this Quaternion[] vs, Vector3 v)
    {
        int l = vs.Length;
        for (int i = 0; i < l; i++)
        {
            var vx = vs[i].eulerAngles;
            vs[i] = Quaternion.Euler(vx.x * v.x, vx.y * v.y, vx.z * v.z);
        }
        return vs;
    }

    public static Quaternion Multiply(this Quaternion vs, Vector3 v)
    {
        var vx = vs.eulerAngles;
        return Quaternion.Euler(vx.x * v.x, vx.y * v.y, vx.z * v.z);
    }

}